/*--------------------------------------------------------------------------------
	FILE INFORMATION:
    Name: Program.cs [./restbot-src/Program.cs]
    Description: This file is the "central hub" of all of the programs resources

	LICENSE:
		This file is part of the RESTBot Project.

		Copyright (C) 2007-2008 PLEIADES CONSULTING, INC

		This program is free software: you can redistribute it and/or modify
		it under the terms of the GNU Affero General Public License as
		published by the Free Software Foundation, either version 3 of the
		License, or (at your option) any later version.

		This program is distributed in the hope that it will be useful,
		but WITHOUT ANY WARRANTY; without even the implied warranty of
		MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
		GNU Affero General Public License for more details.

		You should have received a copy of the GNU Affero General Public License
		along with this program.  If not, see <http://www.gnu.org/licenses/>.
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using OpenMetaverse;
using System.Threading;
using System.Reflection;
using System.IO;
using System.ComponentModel;
using RESTBot.Server;

namespace RESTBot
{
	/// <summary>Establishes parameters defining a session.</summary>
  public class Session
  {
		/// <summary>Session ID, possibly UUID.Zero</summary>
    public UUID ID = UUID.Zero;
		/// <summary>Hostname where RESTbot is running</summary>
    public string? Hostname;	// possibly nullable (gwyneth 20220109)
		/// <summary>Permissions (don't know what this is - gwyneth 20220109)</summary>
    public int Permissions;
		/// <summary>Bot for this session.</summary>
    public RestBot? Bot;	// possibly nullable (gwyneth 20220109)
		/// <summary>Unknown (gwyneth 20220109)</summary>
    public DateTime LastAccessed;
		/// <summary>Called when we need this bot's status</summary>
    public RestBot.BotStatusCallback? StatusCallback;	// possibly nullable (gwyneth 20220109)
		/// <summary>Thread this bot is running on.</summary>
    public Thread? BotThread;	// possibly nullable (gwyneth 20220109)
  }

	/// <summary>Program class</summary>
  class Program
  {
    //static HttpListener Listener;
		/// <summary>(Internal) Web server for the RESTful API</summary>
    static Server.Router? Listener;
		/// <summary>Don't know when this is used, will document later</summary>
    static bool StillRunning;
		/// <summary>List of currently running sessions.</summary>
    public static Dictionary<UUID, Session>? Sessions;

    /// <summary>config file</summary>
    static string configFile = "configuration.xml";
    /// <summary>configuration object ^-- uses this file --^</summary>
    public static XMLConfig.Configuration? config;

    //We need to move this to the security configuration block

    private static DateTime uptime = new DateTime();

		/// <summary>
		/// Bootstrap method.
		/// </summary>
		/// <param name="args">Arguments passed to the application</param>
		/// <remark>The arguments seem to get promptly ignored! (gwyneth 20220109)</param>
    static void Main(string[] args)
    {
      DebugUtilities.WriteInfo("Reading config file");
      config = XMLConfig.Configuration.LoadConfiguration(configFile);

      DebugUtilities.WriteInfo("Restbot startup");
      Sessions = new Dictionary<UUID, Session>();

      DebugUtilities.WriteInfo("Loading plugins");
      RegisterAllCommands(Assembly.GetExecutingAssembly());
      DebugUtilities.WriteDebug("Loading stateful plugins");
      RegisterAllStatefulPlugins(Assembly.GetExecutingAssembly());

      DebugUtilities.WriteInfo("Listening on port " + config.networking.port.ToString());
      // Set up the listener / router
      Listener = new RESTBot.Server.Router(IPAddress.Parse(config.networking.ip), config.networking.port);

      StillRunning = true;
      DebugUtilities.WriteInfo("Startup complete");
      uptime = DateTime.Now;

      while (StillRunning)
        System.Threading.Thread.Sleep(1);
      //TODO: Replace above with a manualresetevent

      Listener.StillRunning = false;
    }

    /// <summary>
    /// Register all RestPlugins to the RestBot static plugin dictionary
    /// </summary>
    /// <param name="assembly"></param>
    static void RegisterAllCommands(Assembly assembly)
    {
      foreach (Type t in assembly.GetTypes())
      {
        try
        {
          if (t.IsSubclassOf(typeof(RestPlugin)))
          {
            ConstructorInfo? info = t.GetConstructor(Type.EmptyTypes);
						if (info == null) {
							DebugUtilities.WriteError("Couldn't get constructor for plugin!");
						} else {
            	RestPlugin plugin = (RestPlugin)info.Invoke(new object[0]);
            	RestBot.AddPlugin(plugin);
						}
          }
        }
        catch (Exception e)
        {
          DebugUtilities.WriteError(e.Message);
        }
      }
    }

    /// <summary>
    /// Grab all the subclass type definitions of StatefulPlugin out of an assembly
    /// </summary>
    /// <param name="assembly">Given assembly to search</param>
    static void RegisterAllStatefulPlugins(Assembly assembly)
    {
      foreach (Type t in assembly.GetTypes())
      {
        if (t.IsSubclassOf(typeof(StatefulPlugin)))
        {
          RestBot.AddStatefulPluginDefinition(t);
        }
      }
    }

		/// <summary>
		/// Process a request (assuming it exists)
		/// </summary>
		/// <param name="headers">Request headers (including path, etc.)</param>
		/// <param name="body">Request body (will usually have all parameters from POST)</param>
    public static string DoProcessing(RequestHeaders headers, string body)
    {
      //Setup variables
      DebugUtilities.WriteDebug("New request - " + headers.RequestLine.Path);
      //Split the URL
      string[] parts = headers.RequestLine.Path.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length < 1)
      {
        return ("<error>invalidmethod</error>");
      }
      string Method = parts[0];
      /// <summary>Process the request params from POST, URL</summary>
      Dictionary<string, string> Parameters = RestBot.HandleDataFromRequest(headers, body);
      string? debugparams = null;
      string? debugparts = null;
      foreach (KeyValuePair<string, string> kvp in Parameters)
      {
        debugparams = debugparams + "[" + kvp.Key + "=" + kvp.Value + "] ";
      }
      foreach (string s in parts)
      {
        debugparts = debugparts + "[ " + s + " ]";
      }
      DebugUtilities.WriteDebug("Parameters - " + debugparams);
      if (Method == "establish_session")
      {
        DebugUtilities.WriteDebug("We have an establish_session method.");
        //Alright, we're going to try to establish a session
        if (parts.Length >= 2 && parts[1] == Program.config.security.serverPass
          && Parameters.ContainsKey("first") && Parameters.ContainsKey("last") && Parameters.ContainsKey("pass"))
        {
          DebugUtilities.WriteDebug("Found required parameters for establish_session");
          foreach (KeyValuePair<UUID, Session> ss in Sessions)
          {
            DebugUtilities.WriteSpecial("Avatar check: [" + ss.Value.Bot.First.ToLower() + "/" + ss.Value.Bot.Last.ToLower() + "] = [" + Parameters["first"].ToLower() + "/" + Parameters["last"].ToLower() + "]");
            if (Parameters["first"].ToLower() == ss.Value.Bot.First.ToLower() &&
                 Parameters["last"].ToLower() == ss.Value.Bot.Last.ToLower()
                 )
            {
                DebugUtilities.WriteWarning("Already running avatar " + Parameters["first"] + " " + Parameters["last"]);
                return ("<existing_session>true</existing_session>\n<session_id>" + ss.Key.ToString() + "</session_id>");
            }
          }
          UUID id = UUID.Random();
          Session s = new Session();
          s.ID = id;
          s.Hostname = headers.Hostname;
          s.LastAccessed = DateTime.Now;
          //Needs the $1$ for the md5 on the login for libsl
          if (!Parameters["pass"].StartsWith("$1$")) Parameters["pass"] = "$1$" + Parameters["pass"];
          s.Bot = new RestBot(s.ID, Parameters["first"], Parameters["last"], Parameters["pass"]);

          lock (Sessions)
          {
            Sessions.Add(id, s);
          }
          RestBot.LoginReply reply = s.Bot.Login();
          if (reply.wasFatal)
          {
            lock (Sessions)
            {
              if (Sessions.ContainsKey(id))
              {
                Sessions.Remove(id);
              }
            }
          }
          return (reply.xmlReply);
        }
        else
        {
          String? result = null;
          if (parts.Length < 2)
          {
            result = "Missing a part.";
          }
          if (!Parameters.ContainsKey("first"))
          {
            result = result + " Missing 'first' arg.";
          }
          if (!Parameters.ContainsKey("last"))
          {
            result = result + " Missing 'last' arg.";
          }
          if (!Parameters.ContainsKey("pass"))
          {
            result = result + " Missing 'pass' arg.";
          }
          return ("<error>arguments: "+result+"</error>");
        }
      }
      else if (Method == "server_quit")
      {
        if (parts[1] == Program.config.security.serverPass )
        {
          foreach (KeyValuePair<UUID, Session> s in Sessions)
          {
            lock (Sessions) DisposeSession(s.Key);
          }
          StillRunning = false;
          return ("<status>success</status>\n");
        }
      }

      //Only a method? pssh.
      if (parts.Length == 1)
      {
        return ("<error>nosession</error>");
      }

      UUID sess = new UUID();
      try
      {
        sess = new UUID(parts[1]);
      }
      catch (FormatException)
      {
        return ("<error>parsesessionkey</error>");
      }
      catch (Exception e)
      {
        DebugUtilities.WriteError(e.Message);
      }

      //Session checking
      if (!ValidSession(sess, headers.Hostname))
      {
        return ("<error>invalidsession</error>");
      }

      //YEY PROCESSING
      RestBot? r = Sessions[sess].Bot;
      //Last accessed for plugins
      Sessions[sess].LastAccessed = DateTime.Now;
      //Pre-error checking
      if (r.myStatus != RestBot.Status.Connected) //Still logging in?
      {
        return ("<error>" + r.myStatus.ToString() + "</error>");
      }
      else if (!r.Client.Network.Connected) //Disconnected?
      {
      	return ("<error>clientdisconnected</error>");
      }
      else if (Method == "exit")
      {
        DisposeSession(sess);
        return ("<disposed>true</disposed>");
      }
      else if (Method == "stats")
      {
        string response = "<bots>" + Sessions.Count.ToString() + "<bots>\n";
        response += "<uptime>" + (DateTime.Now - uptime) + "</uptime>\n";
        return (response);
      }

      return r.DoProcessing(Parameters, parts);
    }

    private static bool ValidSession(UUID key, string hostname)
    {
      return Sessions.ContainsKey(key) && (!config.security.hostnameLock || Sessions[key].Hostname == hostname);
		}

		/// <summary>
		/// Get rid of a specific session.
		/// </summary>
		/// <param name="key">Session UUID</param>
    public static void DisposeSession(UUID key)
    {
      DebugUtilities.WriteDebug("Disposing of session " + key.ToString());
      if (!Sessions.ContainsKey(key))
        return;
      Session s = Sessions[key];
			if (s.StatusCallback is not null) {
      	s.Bot.OnBotStatus -= s.StatusCallback;
			}
      s.Bot.Client.Network.Logout();
      Sessions.Remove(key);
    }
  }
}

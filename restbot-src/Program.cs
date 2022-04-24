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
using System.Diagnostics;			// useful to get the file version info of LibreMetaverse (gwyneth 20220414)
using RESTBot.Server;

namespace RESTBot
{
	/// <summary>Establishes parameters defining a session.</summary>
  public class Session
  {
		/// <summary>Session ID, possibly UUID.Zero</summary>
    public UUID ID = UUID.Zero;
		/// <summary>Hostname where RESTbot is running</summary>
    public string Hostname = "localhost";
		/// <summary>Permissions (don't know what this is - gwyneth 20220109)</summary>
    public int Permissions;
		/// <summary>Bot for this session.</summary>
		/// <remarks>possibly nullable (gwyneth 20220109)</remarks>
    public RestBot? Bot;
		/// <summary>Unknown (gwyneth 20220109)</summary>
    public DateTime LastAccessed;
		/// <summary>Called when we need this bot's status</summary>
		/// <remarks>possibly nullable (gwyneth 20220109)</remarks>
    public RestBot.BotStatusCallback? StatusCallback;
		/// <summary>Thread this bot is running on.</summary>
		/// <remarks>possibly nullable (gwyneth 20220109)</remarks>
    public Thread? BotThread;
  } // end class Session

	/// <summary>Program class</summary>
	/// <remarks>This is the main class that will be launched to read configuration files,
	/// do all plugin initialisation, and even deal with logins!</remarks>
  class Program
  {
    //static HttpListener Listener;
		/// <summary>(Internal) Web server for the RESTful API</summary>
    static Server.Router? Listener;
		/// <summary>Don't know when this is used, will document later</summary>
		/// <remark>I believe this comes from the Think() method on the plugins (gwyneth 20220421)</remark>
    static bool StillRunning;
		/// <summary>List of currently running sessions.</summary>
		/// <remarks>Should never be null!</remarks>
    public static Dictionary<UUID, Session> Sessions = new Dictionary<UUID, Session>();

    /// <summary>config file</summary>
    static string configFile = "configuration.xml";
    /// <summary>configuration object ^-- uses this file --^</summary>
    public static XMLConfig.Configuration? config;

    /// <summary>We need to move this to the security configuration block</summary>
    private static DateTime uptime = new DateTime();

		/// <summary>
		/// Bootstrap method.
		/// </summary>
		/// <param name="args">Arguments passed to the application</param>
		/// <remarks>The arguments seem to get promptly ignored! (gwyneth 20220109)</remarks>
    static void Main(string[] args)
    {
      DebugUtilities.WriteInfo("Reading config file");
      config = XMLConfig.Configuration.LoadConfiguration(configFile);
			if (config == null)
			{
				// configuration is mandatory! (gwyneth 20220213)
				DebugUtilities.WriteError($"Unable to open configuration file {configFile}! Aborting...");
				Environment.Exit(1);
				return;
			}

      DebugUtilities.WriteInfo("RESTbot startup");
			// Sessions should never be null (?) (gwyneth 20220214)
			if (Sessions == null)
			{
				// Trouble expects us later on, when adding and removing sessions...
				DebugUtilities.WriteError("Error initialising Sessions directory; it was set to null!");
			}

			/// <summary>Get the file version of LibreMetaverse.</summary>
			/// <remarks><see href="https://stackoverflow.com/a/14612480/1035977"/> (gwyneth 20220414)</remarks>
			FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(@"LibreMetaverse.dll");

			// Print the file name and version number.
			DebugUtilities.WriteInfo($"LibreMetaverse DLL version: {myFileVersionInfo.FileDescription} {myFileVersionInfo.FileVersion}");

      DebugUtilities.WriteInfo("Loading plugins");
      RegisterAllCommands(Assembly.GetExecutingAssembly());
      DebugUtilities.WriteDebug("Loading stateful plugins");
      RegisterAllStatefulPlugins(Assembly.GetExecutingAssembly());

      DebugUtilities.WriteInfo($"Listening on port {config.networking.port.ToString()}");

      // Set up the listener / router
      Listener = new RESTBot.Server.Router(IPAddress.Parse(config.networking.ip), config.networking.port);

      StillRunning = true;
      DebugUtilities.WriteInfo("Startup complete");
      uptime = DateTime.Now;

			// let's see if we can figure out how much memory is being wasted
			int stupidCounter = 0;
      while (StillRunning) {
        System.Threading.Thread.Sleep(1);
      	//TODO: Replace above with a manualresetevent

				if (stupidCounter % 3600000 == 0) {	// stop every hour and check available memory (gwyneth 20220210)
					CollectGarbage();
				}
				stupidCounter++;
			}

      Listener.StillRunning = false;
    }

    /// <summary>
    /// Register all RestPlugins to the RestBot static plugin dictionary
    /// </summary>
    /// <param name="assembly">Given assembly to search for</param>
    static void RegisterAllCommands(Assembly assembly)
    {
      foreach (Type t in assembly.GetTypes())
      {
        try
        {
          if (t.IsSubclassOf(typeof(RestPlugin)))
          {
            ConstructorInfo? info = t.GetConstructor(Type.EmptyTypes);
						if (info == null)
						{
							DebugUtilities.WriteError("Couldn't get constructor for plugin!");
						}
						else
						{
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
    /// <param name="assembly">Given assembly to search for</param>
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
			// Abort if we don't even have a valid configuration; too many things depend on it... (gwyneth 20220213)
			if (Program.config == null)
			{
				return "<error>No valid configuration loaded, aborting</error>";
			}

      //Setup variables
      DebugUtilities.WriteDebug($"New request - {headers.RequestLine.Path}");
      //Split the URL
      string[] parts = headers.RequestLine.Path.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length < 1)
      {
        return ("<error>invalidmethod</error>");
      }
      string Method = parts[0];
      /// <summary>Process the request params from POST, URL</summary>
      Dictionary<string, string> Parameters = RestBot.HandleDataFromRequest(headers, body);
      string debugparams = String.Empty;
      string debugparts = String.Empty;
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
        // Alright, we're going to try to establish a session
				// Start location is optional (gwyneth 20220421)
        if (parts.Length >= 2 && parts[1] == Program.config.security.serverPass
          && Parameters.ContainsKey("first") && Parameters.ContainsKey("last") && Parameters.ContainsKey("pass"))
        {
          DebugUtilities.WriteDebug("Found required parameters for establish_session");
					if (Sessions != null)
					{
						foreach (KeyValuePair<UUID, Session> ss in Sessions)
						{
							if (ss.Value != null && ss.Value.Bot != null)
							{
								DebugUtilities.WriteSpecial("Avatar check: [" + ss.Value.Bot.First.ToLower() + "/" + ss.Value.Bot.Last.ToLower() + "] = [" + Parameters["first"].ToLower() + "/" + Parameters["last"].ToLower() + "]");
								if (Parameters["first"].ToLower() == ss.Value.Bot.First.ToLower() &&
										Parameters["last"].ToLower() == ss.Value.Bot.Last.ToLower()
										)
								{
										DebugUtilities.WriteWarning($"Already running avatar {Parameters["first"]} {Parameters["last"]}");
										return $"<existing_session>true</existing_session>\n<session_id>{ss.Key.ToString()}</session_id>";
								}
							}
						}
					}
					else
					{
						DebugUtilities.WriteDebug("No available sessions...");
					}
          UUID id = UUID.Random();
          Session s = new Session();
          s.ID = id;
          s.Hostname = headers.Hostname;
          s.LastAccessed = DateTime.Now;
          // Needs the $1$ for the md5 on the login for libsl
          if (!Parameters["pass"].StartsWith("$1$"))
						Parameters["pass"] = "$1$" + Parameters["pass"];
					// check if user has provided us with a starting location (default is to use the last location)
					// (gwyneth 20220420)
					string gridLocation = Parameters.ContainsKey("start") ? Parameters["start"] : "last";
          s.Bot = new RestBot(s.ID, Parameters["first"], Parameters["last"], Parameters["pass"], gridLocation);

					if (Sessions != null)
					{
						lock (Sessions)
						{
							Sessions.Add(id, s);
						}
					}
					else
					{
						// no "else", we have no dictionary
						DebugUtilities.WriteWarning("Possible issue: we have null Sessions when adding, which shouldn't happen");
					}
          RestBot.LoginReply reply = s.Bot.Login();
          if (reply.wasFatal)
          {
						if (Sessions != null)
						{
            	lock (Sessions)
            	{
              	if (Sessions.ContainsKey(id))
              	{
                	Sessions.Remove(id);
              	}
            	}
						}
						else
						{
							// no "else", we have no dictionary
							DebugUtilities.WriteWarning("Possible issue: we have null Sessions when removing, which shouldn't happen");
						}
          }
          return (reply.xmlReply);
        }
        else
        {
          String result = String.Empty;
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
          return $"<error>arguments: {result}</error>";
        }
      }
			// Note: formerly undocumented functionality!! (gwyneth 20220414)
      else if (Method == "server_quit")
      {
				if (parts.Length < 2)
				{
					return ("<error>missing 'pass' arg.</error>");
				}
        if (parts[1] == Program.config.security.serverPass)
        {
					if (Sessions != null)
					{
						foreach (KeyValuePair<UUID, Session> s in Sessions)
						{
							lock (Sessions) DisposeSession(s.Key);
						}
						StillRunning = false;
						// note: a caveat of this undocumented method is that it requires a _new_
						// incoming request to actually kill the server... could be a ping, though. (gwyneth 20220414)
						return ("<status>success - all bot sessions were logged out and a request was made for queued shutdown</status>\n");
					}
					else
					{
						// it's fine if there are no sessions (gwyneth 20220414)
						return "<status>success - no sessions were active</status>";
					}
        }
				else
				{
					// wrong password sent! (gwyneth 20220414)
					return ("<error>server authentication failure</error>");
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
        return "<error>invalidsession</error>";
      }

      //YEY PROCESSING
      RestBot? r = null;

			if (Sessions != null)
			{
				r = Sessions[sess].Bot;
			}

			if (r == null)
			{
				return $"<error>no RestBot found for session {sess.ToString()}</error>";
			}
      //Last accessed for plugins
			if (Sessions != null)
			{
      	Sessions[sess].LastAccessed = DateTime.Now;
			}
      //Pre-error checking
      if (r.myStatus != RestBot.Status.Connected) //Still logging in?
      {
        return $"<error>{r.myStatus.ToString()}</error>";
      }
      else if (!r.Client.Network.Connected) //Disconnected?
      {
      	return "<error>clientdisconnected</error>";
      }
      else if (Method == "exit")
      {
        DisposeSession(sess);
        return ("<disposed>true</disposed>");
      }
      else if (Method == "stats")
      {
        string response = "<bots>" + ((Sessions != null) ? Sessions.Count.ToString() : "NaN") + "<bots>\n";
        response += "<uptime>" + (DateTime.Now - uptime) + "</uptime>\n";
        return (response);
      }

      return r.DoProcessing(Parameters, parts);
    }

		/// <summary>Checks if a session key (UUID) is valid and its hostname is set</summary>
		/// <param name="key">Session UUID to be validated</param>
		/// <param name="hostname">Hostname to be checked if it's part of the session data</param>
		/// <returns>boolean — true for a valid session with the correct hostname, false otherwise</returns>
		/// <remarks>Because there are now plenty of nullable references here, the overall logic is rather
		/// more complex than before, to avoid any compiler warnings! (gwyneth 20220214)</remarks>
    private static bool ValidSession(UUID key, string hostname)
    {
      return Sessions != null
				&& Sessions.ContainsKey(key)
				&& (
					config != null
					&& config.security != null
					&& !config.security.hostnameLock
					|| (
						Sessions[key].Hostname != null
						&& Sessions[key].Hostname == hostname
					)
				);
		}

		/// <summary>
		/// Calls the C# garbage collector
		///
		/// We have major memory leaks, this is an attempt to keep them under control. (gwyneth 20220411)
		/// </summary>
		/// <param />
		/// <returns>void</returns>
		private static void CollectGarbage()
		{
			DateTime t = DateTime.Now;
			DebugUtilities.WriteInfo($"{t.ToString("yyyy-MM-dd HH:mm:ss")} - Memory in use before GC.Collect: {(GC.GetTotalMemory(false)):N0} bytes");
			GC.Collect(); // collect garbage (gwyneth 20220207) and wait for GC to finish.
			t = DateTime.Now;
			DebugUtilities.WriteInfo($"{t.ToString("yyyy-MM-dd HH:mm:ss")} - Memory in use after  GC.Collect: {(GC.GetTotalMemory(true)):N0} bytes");
		}

		/// <summary>
		/// Get rid of a specific session.
		/// </summary>
		/// <remarks>Also calls the garbage collector after a successful bot logout (gwyneth 20220411)</remarks>
		/// <param name="key">Session UUID</param>
    public static void DisposeSession(UUID key)
    {
      DebugUtilities.WriteDebug($"Disposing of session {key.ToString()}");
			if (Sessions != null)
			{
      	if (!Sessions.ContainsKey(key))
        	return;
      	Session s = Sessions[key];
				if (s != null && s.Bot != null)	// should never happen, we checked before
				{
					if (s.StatusCallback != null)
					{
      			s.Bot.OnBotStatus -= s.StatusCallback;
					}
      		s.Bot.Client.Network.Logout();

					// Run garbage collector every time a bot logs out.
					CollectGarbage();
				}
				else
				{
					DebugUtilities.WriteError($"Weird error in logging out session {key.ToString()} - it was on the Sessions dictionary, but strangely without a 'bot attached");
				}
      	Sessions.Remove(key);
			}
			else
			{
				DebugUtilities.WriteError($"DisposeSession called on {key.ToString()}, but we have no Sessions dictionary!");
			}
    } // end DisposeSession()
  } // end class Program
} // end namespace RESTbot

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
		/// <value>(Internal) Web server for the RESTful API</value>
    static Server.Router? Listener;
		/// <summary>Boolean flag requesting associated request thread to start or stop</summary>
		/// <remark><seealso cref="Server.Router" /></remark>
    static bool StillRunning;
		/// <value>List of currently running sessions.</value>
		/// <remarks>Should never be null!</remarks>
    public static Dictionary<UUID, Session> Sessions = new Dictionary<UUID, Session>();

    /// <value>config file</value>
    static string configFile = "configuration.xml";
    /// <value>configuration object ^-- uses this file --^</value>
    public static XMLConfig.Configuration? config;

    /// <value>We need to move this to the security configuration block</value>
		/// <remarks>Why? And where is this 'security configuration block', anyway? (gwyneth 20220425)</remarks>
    private static DateTime uptime = new DateTime();

		/// <value>Some sort of version number that we'll send to LibreMetaverse for its debugging purposes</value>
		public static string Version { get; set; } = "0.0.0.0";					// will be filled in later by Main() (gwyneth 20220425)

		/// <summary>
		/// Bootstrap method.
		/// </summary>
		/// <param name="args">Arguments passed to the application</param>
		/// <remarks>The arguments seem to get promptly ignored! (gwyneth 20220109)</remarks>
    static void Main(string[] args)
    {
			// LogManager.GetLogger(typeof(RestBot));

			// new function to parse some useful arguments and do interesting things (gwyneth 20220425)
			ParseArguments(args);

			// see if we can get the version string
			try
			{
				// Note: we ought to also extract the Assembly name, we presume it's the default (gwyneth 20220426)
#if Windows
				var fileVersionInfo = FileVersionInfo.GetVersionInfo("@RESTbot.dll");
#else
				var fileVersionInfo = FileVersionInfo.GetVersionInfo("@RESTbot");
#endif
				Version = fileVersionInfo.FileVersion + "-file";
			}
			catch (Exception e1)
			{
				// nope, this doesn't work under macOS
				DebugUtilities.WriteDebug($"Cannot retrieve file version, exception caught: {e1.Message}");
				// let's try to get the assembly name instead
				try
				{
					var assembly = Assembly.GetExecutingAssembly();
					Version = assembly.GetName().Version + "-assembly";
				}
				catch (Exception e2)
				{
					// nope, that didn't work either
					DebugUtilities.WriteDebug($"Cannot retrieve assembly version either, exception caught: {e2.Message}");
					// finally, our last choice is trying the Informational Version
					try
					{
						var assembly = Assembly.GetEntryAssembly();
						if (assembly != null)
						{
							var customAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
							if (customAttribute != null)
							{
								var infoVersion = customAttribute.InformationalVersion;

								if (infoVersion != null)
								{
									Version = infoVersion;
								}
							}
						}
					}
					catch (Exception e3)
					{
						// we're out of luck today, we cannot even get the Informational Versoin
						DebugUtilities.WriteDebug($"Also cannot retrieve informational version, exception caught: {e3.Message}");
						// we'll have to stick with the hard-coded default Version instead...
					}
				}
			}
			if (Version == null)
			{
				Version = "0.0.0.0";
			}
			DebugUtilities.WriteInfo($"RESTbot file version: {Version}");

      DebugUtilities.WriteInfo($"Reading config file '{configFile}'...");
      config = XMLConfig.Configuration.LoadConfiguration(configFile);
			if (config == null)
			{
				// configuration is mandatory! (gwyneth 20220213)
				DebugUtilities.WriteError($"Unable to open configuration file '{configFile}'! Aborting...");
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

		/// <summary>Parses some arguments and acts upon them</summary>
		/// <param name="args">Array of strings, directly from the command line</param>
		/// <remarks>We have no arguments for now, this is mostly a placeholder (gwyneth 20220425)</remarks>
		private static void ParseArguments(string[] args)
		{
			// Very, very basic and naïve args passing.
			// There is probably a library to deal with this (gwyneth 20220425)
			if (args.Count() == 0)
			{
				DebugUtilities.WriteDebug("Good, no command-line arguments to parse.");
			}
			else if (args.Count() == 1)
			{
				if (args[0] == "--help")
				{
					DebugUtilities.WriteSpecial("Usage: RESTbot --config /path/to/configfile|--debug|--help");
					Environment.Exit(10);
				}
				else if (args[0] == "--debug")
				{
					DebugUtilities.WriteSpecial("`--debug` doesn't work yet...");
				}
			}
			else if (args.Count() == 2)
			{
				if (args[0] == "--config")
				{
					configFile = args[1];	// should sanitise first (gwyneth 20220425)
					DebugUtilities.WriteDebug($"Command-line argument set configuration file to '{configFile}'");
				}
			}
			else
			{
				DebugUtilities.WriteSpecial("Usage: RESTbot --config /path/to/configfile|--debug|--help");
				Environment.Exit(10);
			}
		} // end ParseArguments

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
            ConstructorInfo? info = t.GetConstructor(Type.EmptyTypes);	// ask for parameter-less constructor for this class, if it exists (gwyneth 20220425)
						if (info == null)
						{
							// Not a serious warning, some plugins might be incorrectly configured but still work well
							DebugUtilities.WriteWarning($"Couldn't get constructor without parameters for plugin {t.GetType().Name}!");
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
      DebugUtilities.WriteInfo($"New request - {headers.RequestLine.Path}");
      //Split the URL
      string[] parts = headers.RequestLine.Path.Split("/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length < 1)
      {
        return "<error>invalidmethod</error>";
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
			DebugUtilities.WriteDebug($"Parameters (total: {Parameters.Count()}) - '{debugparams}'");
      foreach (string s in parts)
      {
        debugparts = debugparts + "[ " + s + " ]";
      }
      DebugUtilities.WriteDebug($"Parts (total: {parts.Count()}) - '{debugparts}'");
      if (Method == "establish_session")
      {
        DebugUtilities.WriteDebug("We have an `establish_session` method.");
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
								DebugUtilities.WriteSpecial($"Avatar check: [{ss.Value.Bot.First.ToLower()}/{ss.Value.Bot.Last.ToLower()}] = [{Parameters["first"].ToLower()}/{Parameters["last"].ToLower()}]");
								if (Parameters["first"].ToLower() == ss.Value.Bot.First.ToLower() &&
										Parameters["last"].ToLower() == ss.Value.Bot.Last.ToLower()
										)
								{
										DebugUtilities.WriteWarning($"Already running avatar {Parameters["first"]} {Parameters["last"]}");

										/// <value>Temporary string to construct a full response, if possible; if not, we catch the
										/// exception and return a much shorter version</value>
										/// <remarks>This is a hack. The issue is that we're probably acessing nullable
										/// elements without checking. (gwyneth 20220428)</remarks>
										string returnString = "";

										try
										{
											// Attempt to get a
											returnString = $@"<existing_session>true</existing_session>
<session_id>{ss.Key.ToString()}</session_id>
<key>{ss.Value.Bot.Client.Self.AgentID.ToString()}</key>
<FirstName>{ss.Value.Bot.Client.Self.FirstName}</FirstName>
<LastName>{ss.Value.Bot.Client.Self.LastName}</LastName>
<CurrentSim>{ss.Value.Bot.Client.Network.CurrentSim.ToString()}</CurrentSim>
<Position>{ss.Value.Bot.Client.Self.SimPosition.X},{ss.Value.Bot.Client.Self.SimPosition.Y},{ss.Value.Bot.Client.Self.SimPosition.Z}</Position>";
										}
										catch (Exception e)
										{
											DebugUtilities.WriteError($"Could not generate full response, error was: '{e.Message}'; falling back to the simple, minimalistic answer");
											returnString = $"<existing_session>true</existing_session><session_id>{ss.Key.ToString()}</session_id>";
										}
										return returnString;
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
          // Needs the $1$ for the md5 on the login for LibreMetaverse
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
          return reply.xmlReply;
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
					return $"<error>{Method}: missing 'pass' arg.</error>";
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
						return "<status>success - all bot sessions were logged out and a request was made for queued shutdown</status>";
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
					return $"<error>{Method}: server authentication failure</error>";
				}
      }
			else if (Method == "ping")
			{
				if (parts.Length < 2)
				{
					return $"<error>{Method}: missing 'pass' arg.</error>";
				}
				if (parts[1] == Program.config.security.serverPass)
				{
					return $"<{Method}>I'm alive!</{Method}>";
				}
				else
				{
					// wrong password sent! (gwyneth 20220414)
					return $"<error>{Method}: server authentication failure</error>";
				}
			}
			else if (Method == "session_list")
			{
				if (parts.Length < 2)
				{
					return "<error>missing 'pass' arg.</error>";
				}
				if (parts[1] == Program.config.security.serverPass)
				{
					bool check = false;
					if (Program.Sessions.Count != 0) // no sessions? that's fine, no need to abort
					{
						check = true;
					}

					string response = $"<{Method}>";
					if (check)	// optimisation: if empty, no need to run the foreach (gwyneth 20220424)
					{
						foreach(KeyValuePair<OpenMetaverse.UUID, RESTBot.Session> kvp in Program.Sessions)
						{
							if (kvp.Value.Bot != null)
							{
								response += $@"
	<session>
		<session_id>{kvp.Key.ToString()}</session_id>
		<key>{kvp.Value.Bot.Client.Self.AgentID.ToString()}</key>
		<FirstName>{kvp.Value.Bot.Client.Self.FirstName}</FirstName>
		<LastName>{kvp.Value.Bot.Client.Self.LastName}</LastName>
		<CurrentSim>{kvp.Value.Bot.Client.Network.CurrentSim.ToString()}</CurrentSim>
		<Position>{kvp.Value.Bot.Client.Self.SimPosition.X},{kvp.Value.Bot.Client.Self.SimPosition.Y},{kvp.Value.Bot.Client.Self.SimPosition.Z}</Position>
	</session>";
							}
							else
							{
								// Somehow, we have a session ID that has no bot assigned;
								// this should never be the case, but... (gwyneth 20220426)
								response += $"<session><session_id>{kvp.Key.ToString()}</session_id><key>{UUID.Zero.ToString()}</key></session>";
							}
						}
					}
					else
					{
						response += "no sessions";
					}
					response += $"</{Method}>";
					return response;
				}
				else
				{
					// wrong password sent! (gwyneth 20220414)
					return $"<error>{Method}: server authentication failure</error>";
				}
			}
			else if (Method == "stats")
			{
				if (parts.Length < 2)
				{
					return "<error>{Method}: missing 'pass' arg.</error>";
				}
				if (parts[1] == Program.config.security.serverPass)
				{
					string response = "<stats><bots>" + ((Sessions != null) ? Sessions.Count.ToString() : "0") + "</bots>"
						+ "<uptime>" + (DateTime.Now - uptime) + "</uptime></stats>";
					return response;
				}
				else
				{
					return $"<error>{Method}: server authentication failure</error>";
				}
			}

      //Only a method? pssh.
      if (parts.Length == 1)
      {
        return "<error>no session key found</error>";
      }

      UUID sess = new UUID();
      try
      {
        sess = new UUID(parts[1]);
      }
      catch (FormatException)
      {
        return "<error>cannot parse the session key</error>";
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
        return "<disposed>true</disposed>";
      }
      else if (Method == "stats")
      {
        string response = "<bots>" + ((Sessions != null) ? Sessions.Count.ToString() : "NaN") + "</bots>";
        response += "<uptime>" + (DateTime.Now - uptime) + "</uptime>";
        return response;
      }

      return r.DoProcessing(Parameters, parts);
    } // end DoProcessing

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

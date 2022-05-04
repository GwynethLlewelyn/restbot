/*--------------------------------------------------------------------------------
	FILE INFORMATION:
    Name: RestBot.cs [./restbot-src/RestBot.cs]
    Description: This file defines the Restbot class

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
		along with this program.  If not, see <http://www.gnu.org/licenses/>
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Timers;
// using OpenMetaverse.Utilities;
using System.Net;
using OpenMetaverse;

// using RESTBot.XMLConfig; // should be redundant...
namespace RESTBot
{
	/// <summary>Main class for all RESTbot-related methods</summary>
	public class RestBot
	{
#region Static Junk
		/// <summary>List of all plugins we've got</summary>
		static Dictionary<string, RestPlugin>
			Plugins = new Dictionary<string, RestPlugin>();

		/// <summary>
		/// Add a new plugin to the system.
		/// </summary>
		/// <param name="Plugin">Plugin object</param>
		public static void AddPlugin(RestPlugin Plugin)
		{
			lock (Plugins)
			{
				if (!Plugins.ContainsKey(Plugin.MethodName))
					Plugins.Add(Plugin.MethodName, Plugin);
			}
		}

		/// <summary>List of all possible plugin definitions.</summary>
		static List<Type> StatefulPluginDefinitions = new List<Type>();

		/// <summary>
		/// StatefulPlugins stuff
		/// </summary>
		/// <param name="defn">List of plugin definitions</param>
		public static void AddStatefulPluginDefinition(Type defn)
		{
			lock (StatefulPluginDefinitions)
			{
				DebugUtilities.WriteDebug($"Plugin definition: {defn.FullName}");
				if (defn.IsSubclassOf(typeof(StatefulPlugin)))
					StatefulPluginDefinitions.Add(defn);
			}
		}

		/// <summary>
		/// Get the data from the request and parse it.
		/// </summary>
		/// <param name="request">Request HTTP headers</param>
		/// <param name="body">Request HTTP body</param>
		public static Dictionary<string, string>
		HandleDataFromRequest(RESTBot.Server.RequestHeaders request, string body)
		{
			Dictionary<string, string> ret = new Dictionary<string, string>();
			string content_type = "";
			foreach (Server.HeaderLine line in request.HeaderLines)
			{
				if (line.Key.ToLower() == "content-type")
				{
					content_type = line.Value.ToLower();
				}
			}
			if (body != String.Empty)
			{
				DebugUtilities.WriteDebug($"request body is: '{body}'");
			}
			if (content_type == "text/xml" || content_type == "xml")
			{
				// make it a string
				System.IO.MemoryStream ms =
					new System.IO.MemoryStream(Encoding.UTF8.GetBytes(body));

				// Woot, XML parsing
				System.Xml.XmlReader r = System.Xml.XmlReader.Create(ms);
				r.Read(); // read the restbotmessage node
				r.Read(); // Advance to the next node
				while (!r.EOF)
				{
					string v = r.ReadString();
					if (v != "")
					{
						if (!ret.ContainsKey(r.Name))
							ret.Add(r.Name, v);
						else
							ret[r.Name] = v;
					}
					r.Read();
				}
			} // Parse it like a normal POST
			else
			{
				// Then do the split
				// Program.debug("Post - " + body);
				string[] ampsplit =
					body.Split("&".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

				// There's no body? Return.
				if (ampsplit.Length == 0) return ret;
				foreach (string pair in ampsplit)
				{
					string[] eqsplit = pair.Split("=".ToCharArray(), 2);
					if (eqsplit.Length == 1)
					{
						if (ret.ContainsKey(eqsplit[0]))
							ret[eqsplit[0]] = "";
						else
							ret.Add(eqsplit[0], "");
					}
					else
					{
						if (ret.ContainsKey(eqsplit[0]))
							ret[eqsplit[0]] = WebUtility.UrlDecode(eqsplit[1]);
						else
							ret.Add(eqsplit[0], WebUtility.UrlDecode(eqsplit[1]));
					}
				}
			}
			return ret;
		}
#endregion // Static Junk

#region SomeNewLoginCode
		/// <summary>Reply for the new login code</summary>
		public struct LoginReply
		{
			public bool wasFatal; /// <value>set to true if the error was fatal; this is necessary because we may get 'unknown' errors that aren't fatal and thus allows the connection to proceed.</value>

			public string xmlReply; /// <value>Login reply message from grid, XML-encoded</value>
		}
#endregion // SomeNewLoginCode


		/// <summary>All possible connection status</summary>
		public enum Status
		{
			Offline,
			Connected,
			LoggingIn,
			Reconnecting,
			LoggingOut,
			UnknownError
		}

		// private const string VERSION = "8.3.1";	// this is clearly a 'fake' version; the variable was moved to class Program, so that we don't need to call it every time we instantiate a new RestBot object

		public string First; /// <value>Avatar first name</value>

		public string Last; /// <value>Avatar last name</value>

		public string MD5Password; /// <value>Avatar password, MD5-encoded</value>

		public GridClient Client; /// <value>LibreMetaverse grid client, i.e. this app</value>

		public Status myStatus; /// <value>Current bot's connection status</value>

		public UUID sessionid; /// <value>Session ID for the current bot (it's an UUID)</value>

		private DateTime uptime = new DateTime();

		/// <value><para>The agents starting location home or last</para>
		/// <para>Equivalent to LibreMetaverse's own Start variable</para></value>
		/// <remarks>Either "last", "home", or a string encoded URI
		/// containing the simulator name and x/y/z coordinates e.g: uri:hooper&amp;128&amp;152&amp;17</remarks>
		public string Start;

		private readonly Dictionary<string, StatefulPlugin> StatefulPlugins;

		private readonly System.Timers.Timer ReloginTimer;

		public delegate void BotStatusCallback(UUID Session, Status status); /// <summary>Callback for the bot status</summary>

		/// <value>Event called when requesting the bot status</value>
		/// <remarks><para>It's not used here, but invoked from Program.DisposeSession() (declared on Program.cs)
		/// (remarks by gwyneth 20220126)</para>
		public event BotStatusCallback OnBotStatus;

		private System.Timers.Timer updateTimer;

		#region Uptime Retrieval Functions
		// There seemed to be some reasons for keeping the uptime hidden.
		// So we expose it via a few useful methods (gwyneth 20220424).

		/// <summary>Returns the uptime as a <c>DateTime</c> object</summary>
		/// <returns>uptime as <c>DateTime</c></returns>
		public DateTime getUptime()
		{
			return uptime;
		}

		/// <summary>Returns the uptime as a formatted string</summary>
		/// <param name="string">DateTime format string, same as <see cref="DateTime" /></param>
		/// <returns>uptime expressed as a string</returns>
		public string getUptime(string format)
		{
			return uptime.ToString(format);
		}

		/// <summary>Returns the uptime as a string, formatted according to <see href="https://www.iso.org/iso-8601-date-and-time-format.html">ISO 8601</see></summary>
		/// <returns>uptime expressed as a string formatted according to ISO 8601</returns>
		/// <remarks>C# does not offer ISO 8601 as a 'default' time format</remarks>
		public string getUptimeISO8601()
		{
			return uptime.ToString("yyyyMMddTHH:mm:ssZ");
		}

		#endregion Uptime Retrieval Functions

		/// <summary>
		/// Main entry point for logging in with a bot.
		/// </summary>
		/// <param name="session">Current session UUID</param>
		/// <param name="f">Login first name</param>
		/// <param name="l">Login last name</param>
		/// <param name="p">MD5-encoded password</param>
		/// <param name="s">Start location is "last", "home" or could be a <seealso cref="T:OpenMetaverse.URI"/></param>

		public RestBot(UUID session, string f, string l, string p, string s)
		{
			//setting up some class variables
			sessionid = session;
			myStatus = Status.Offline;
			Client = new GridClient();
			First = f;
			Last = l;
			MD5Password = p;
			uptime = DateTime.Now;
			Start = (s == String.Empty) ? "last" : s;
			ReloginTimer = new System.Timers.Timer();
			ReloginTimer.Elapsed += new ElapsedEventHandler(ReloginTimer_Elapsed);

			// Some callbacks..
			DebugUtilities.WriteDebug(session.ToString() + " Initializing callbacks");

			// Client.Network.OnDisconnected += new NetworkManager.DisconnectedCallback(Network_OnDisconnected);
			Client.Network.Disconnected += Network_OnDisconnected; // new syntax

			// Timer used to update an active plugin.
			updateTimer = new System.Timers.Timer(500);
			updateTimer.Elapsed +=
				new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);

			// Initialize StatefulPlugins
			DebugUtilities.WriteDebug(session.ToString() + " Initializing plugins");
			StatefulPlugins = new Dictionary<string, StatefulPlugin>();
			foreach (Type t in RestBot.StatefulPluginDefinitions)
			{
				ConstructorInfo? info = t.GetConstructor(Type.EmptyTypes);
				if (info == null)
				{
					DebugUtilities
						.WriteDebug(session.ToString() +
						" could not get constructor for type " +
						t.ToString());
					continue;
				}
				StatefulPlugin sp = (StatefulPlugin) info.Invoke(new object[0]);

				// Add it to the dictionary
				RegisterStatefulPlugin(sp.MethodName, sp);
				DebugUtilities
					.WriteDebug(session.ToString() + " * added " + sp.MethodName);

				// Initialize all the handlers, etc
				sp.Initialize(this);
			}
			updateTimer.Start();

			/// The strange lambda assignment is due to insane new rules regarding constructors
			/// in recent versions of C#. (gwyneth 20220213)
			/// <see href="https://stackoverflow.com/a/70146798/1035977" />
			OnBotStatus = new BotStatusCallback((sender, e) => {});
		}

		/// <summary>
		/// Called once the timer for the login reconnect event finishes.
		/// </summary>
		/// <param name="sender">Sender object</param>
		/// <param name="e">Arguments for the elapsed event</param>
		void ReloginTimer_Elapsed(object? sender, ElapsedEventArgs e)
		{
			ReloginTimer.Stop();
			DebugUtilities.WriteInfo(sessionid.ToString() + " relogging...");
			Login();
			//This is where we can handle relogin failures, too.
		}

		private void updateTimer_Elapsed(
			object? sender,
			System.Timers.ElapsedEventArgs e
		)
		{
			foreach (StatefulPlugin sp in StatefulPlugins.Values)
			{
				if (sp.Active)
				{
					sp.Think();
				}
			}
		}

		/// <summary>
		/// Callback for when this bot gets disconnected, attempting to connect again in 5 minutes.
		/// </summary>
		/// <param name="sender">Sender object</param>
		/// <param name="e">Arguments for the disconnected event</param>
		/// <remarks>rewrote to show message</remarks>
		void Network_OnDisconnected(object? sender, DisconnectedEventArgs e)
		{
			if (e.Reason != NetworkManager.DisconnectType.ClientInitiated)
			{
				myStatus = Status.Reconnecting;
				DebugUtilities.WriteWarning($"{sessionid.ToString()} was disconnected ({e.Message.ToString()}), but I'm logging back in again in 5 minutes.");
				ReloginTimer.Stop();
				ReloginTimer.Interval = 5 * 60 * 1000;
				ReloginTimer.Start();
			}
		}

		/// <summary>
		/// Register a stateful RESTbot plugin.
		/// </summary>
		/// <param="method">Method name to register</param>
		/// <param="sp">Stateful plugin object</param>
		public void RegisterStatefulPlugin(string method, StatefulPlugin sp)
		{
			StatefulPlugins.Add(method, sp);
		}

		/// <summary>
		/// Login block
		/// </summary>
		public LoginReply Login()
		{
			LoginReply response = new LoginReply();

			DebugUtilities.WriteSpecial("Login block was called in Login()");
			if (Client.Network.Connected)
			{
				DebugUtilities.WriteError("Uhm, Login() was called when we where already connected. Hurr");
				return new LoginReply();
			}

			//Client.Network.LoginProgress +=
			//    delegate(object? sender, LoginProgressEventArgs e)
			//    {
			//        DebugUtilities.WriteDebug($"Login {e.Status}: {e.Message}");
			//        if (e.Status == LoginStatus.Success)
			//        {
			//            DebugUtilities.WriteSpecial("Logged in successfully");
			//            myStatus = Status.Connected;
			//            response.wasFatal = false;
			//            response.xmlReply = "<success><session_id>" + sessionid.ToString() + "</session_id></success>";
			//        }
			//        else if (e.Status == LoginStatus.Failed)
			//        {
			//            DebugUtilities.WriteError("$There was an error while connecting: {Client.Network.LoginErrorKey}");
			//            response.wasFatal = true;
			//            response.xmlReply = "<error></error>";
			//        }
			//    };
			// Optimize the throttle
			Client.Throttle.Wind = 0;
			Client.Throttle.Cloud = 0;
			Client.Throttle.Land = 1000000;
			Client.Throttle.Task = 1000000;

			// we add this check here because LOGIN_SERVER should never be assigned null (gwyneth 20220213)
			if (Program.config != null && Program.config.networking.loginuri != null)
			{
				Client.Settings.LOGIN_SERVER = Program.config.networking.loginuri;	// could be String.Empty, so we check below...
			}
			else if (RESTBot.XMLConfig.Configuration.defaultLoginURI != null)
			{
				Client.Settings.LOGIN_SERVER = RESTBot.XMLConfig.Configuration.defaultLoginURI;	// could ALSO be String.Empty, so we check below...
			}
			else
			{
				Client.Settings.LOGIN_SERVER = String.Empty;
			}

			// Any of the above _might_ have set LOGIN_SERVER to an empty string, so we check first if we have
			// something inside the string. (gwyneth 20220213)
			// To-do: validate the URL first? It's not clear if .NET 6 already does that at some point...
			if (Client.Settings.LOGIN_SERVER == String.Empty)
			{
				// we don't know where to login to!
				response.wasFatal = true;
				response.xmlReply =	"<error fatal=\"true\">No login URI provided</error>";
				DebugUtilities.WriteError("No login URI provided; aborting...");
				return response;
			}
			DebugUtilities.WriteDebug($"Login URI: {Client.Settings.LOGIN_SERVER}");

			LoginParams loginParams =
				Client.Network.DefaultLoginParams(First, Last, MD5Password, "RestBot", Program.Version);

			loginParams.Start = Start;

			if (Client.Network.Login(loginParams))
			{
				DebugUtilities.WriteSpecial($"{First} {Last} logged in successfully");
				myStatus = Status.Connected;
				response.wasFatal = false;
				response.xmlReply =
					$@"<success>
	<session_id>{sessionid.ToString()}</session_id>
	<key>{Client.Self.AgentID.ToString()}</key>
	<name>{Client.Self.FirstName} {Client.Self.LastName}</name>
	<FirstName>{Client.Self.FirstName}</FirstName>
	<LastName>{Client.Self.LastName}</LastName>
	<CurrentSim>{Client.Network.CurrentSim.ToString()}</CurrentSim>
	<Position>{Client.Self.SimPosition.X},{Client.Self.SimPosition.Y},{Client.Self.SimPosition.Z}</Position>
	<Rotation>{Client.Self.SimRotation.X},{Client.Self.SimRotation.Y},{Client.Self.SimRotation.Z},{Client.Self.SimRotation.W}</Rotation>
</success>";
			}
			else
			{
				DebugUtilities
					.WriteError($"There was an error while connecting: {Client.Network.LoginErrorKey}");
				switch (Client.Network.LoginErrorKey)
				{
					case "connect":
					case "key":
					case "disabled":
						response.wasFatal = true;
						response.xmlReply =
							$"<error fatal=\"true\">{Client.Network.LoginMessage}</error>";
						break;
					case "presence":
					case "timed out":
					case "god":
						DebugUtilities
							.WriteWarning("Nonfatal error while logging in.. this may be normal");
						response.wasFatal = false;
						response.xmlReply =
							$"<error fatal=\"false\">{Client.Network.LoginMessage}</error><retry>10</retry><session_id>{sessionid}</session_id>";

						DebugUtilities
							.WriteSpecial("Relogin attempt will be made in 10 minutes");
						ReloginTimer.Interval = 10 * 60 * 1000; //10 minutes
						ReloginTimer.Start();
						break;
					default:
						DebugUtilities
							.WriteError($"{sessionid.ToString()} UNKNOWN ERROR {Client.Network.LoginErrorKey} WHILE ATTEMPTING TO LOGIN");
						response.wasFatal = true;
						response.xmlReply =
							$"<error fatal=\"true\">Unknown error '{Client.Network.LoginErrorKey}' has occurred.</error>";
						break;
				}

				if (response.wasFatal == false) myStatus = Status.Reconnecting;
			}

			//Client.Network.BeginLogin(loginParams);
			return response;
		} // end Login()

		/// <summary>
		/// Checks for a method in the list of registered plugins, and, if found, executes it.
		/// </summary>
		/// <param name="Parameters"></param>
		/// <param name="parts"></param>
		public string
		DoProcessing(Dictionary<string, string> Parameters, string[] parts)
		{
			string Method = parts[0];
			string? debugparams = null; // must allow null (gwyneth 20220109)
			foreach (KeyValuePair<string, string> kvp in Parameters)
			{
				debugparams = $"{debugparams} [{kvp.Key}={kvp.Value}] ";
			}
			DebugUtilities.WriteDebug($"Session ID: {sessionid}, Method: {Method}, Parameters: {debugparams}");

			//Actual processing
			if (Plugins.ContainsKey(Method))
			{
				return Plugins[Method].Process(this, Parameters);
			}
			else //Process the stateful plugins
			if (StatefulPlugins.ContainsKey(Method))
			{
				return StatefulPlugins[Method].Process(this, Parameters);
			}
			else if (Method == "stat")
			{
				string response = $@"<{Method}>
	<name>{Client.Self.FirstName} {Client.Self.LastName}</name>
	<key>{Client.Self.AgentID.ToString()}</key>
	<uptime>{(DateTime.Now - uptime)}</uptime>
</{Method}>
";
				return response;
			}
			else if (Method == "status")
			{
				return $"<{Method}>{myStatus.ToString()}</{Method}>";
			}
			return "<error>novalidplugin</error>";
		} // end DoProcessing
	} // end class RestBot
} // end namespace RestBot

/*--------------------------------------------------------------------------------
 FILE INFORMATION:
     Name: RestBot.cs [./restbot-src/RestBot.cs]
     Description: This file defines the Restbot class

 LICENSE:
     This file is part of the RESTBot Project.

     RESTbot is free software; you can redistribute it and/or modify it under
     the terms of the Affero General Public License Version 1 (March 2002)

     RESTBot is distributed in the hope that it will be useful,
     but WITHOUT ANY WARRANTY; without even the implied warranty of
     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
     Affero General Public License for more details.

     You should have received a copy of the Affero General Public License
     along with this program (see ./LICENSING) If this is missing, please
     contact alpha.zaius[at]gmail[dot]com and refer to
     <http://www.gnu.org/licenses/agpl.html> for now.

 COPYRIGHT:
     RESTBot Codebase (c) 2007-2008 PLEIADES CONSULTING, INC
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Utilities;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Timers;
namespace RESTBot
{
    public class RestBot
    {
        #region Static Junk
        static Dictionary<string, RestPlugin> Plugins = new Dictionary<string, RestPlugin>();
        public static void AddPlugin(RestPlugin Plugin)
        {
            lock (Plugins)
            {
                if (!Plugins.ContainsKey(Plugin.MethodName))
                    Plugins.Add(Plugin.MethodName, Plugin);
            }
        }
        //StatefulPlugins stuff
        static List<Type> StatefulPluginDefinitions = new List<Type>();
        public static void AddStatefulPluginDefinition(Type defn)
        {

            lock (StatefulPluginDefinitions)
            {
                DebugUtilities.WriteDebug("Plugin Def: " + defn.FullName);
                if (defn.IsSubclassOf(typeof(StatefulPlugin)))
                    StatefulPluginDefinitions.Add(defn);
            }
        }

        public static Dictionary<string, string> HandleDataFromRequest(RESTBot.Server.RequestHeaders request, string body)
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
            if (content_type == "text/xml" || content_type == "xml")
            {
                //make it a string
                System.IO.MemoryStream ms = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(body));
                //Woot, XML parsing
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
            }
            else //Parse it like a normal POST
            {

                //Then do the split
				//Program.debug("Post - " + body);
                string[] ampsplit = body.Split("&".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                //There's no body? Return.
                if (ampsplit.Length == 0)
                    return ret;
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
                            ret[eqsplit[0]] = eqsplit[1];
                        else
                            ret.Add(eqsplit[0], eqsplit[1]);
                    }
                }
            }
            return ret;
        }
        #endregion

        #region SomeNewLoginCode
        public struct LoginReply
        {
            public bool wasFatal;
            public string xmlReply;
        }
        #endregion

        public enum Status
		{
			Offline,
			Connected,
			LoggingIn,
			Reconnecting,
            LoggingOut,
			UnknownError
		}

        const string VERSION = "8.3.1";

        public string First;
        public string Last;
        public string MD5Password;

        public GridClient Client;
        public Status myStatus;
        public UUID sessionid;

		private DateTime uptime = new DateTime();

		private readonly Dictionary<string, StatefulPlugin> StatefulPlugins;

		private readonly System.Timers.Timer ReloginTimer;
		public delegate void BotStatusCallback(UUID Session, Status status);
		public event BotStatusCallback OnBotStatus;

        private System.Timers.Timer updateTimer;

        public RestBot(UUID session, string f, string l, string p)
        {
            //setting up some class variables
            sessionid = session;
			myStatus = Status.Offline;
            Client = new GridClient();
            First = f;
            Last = l;
            MD5Password = p;
			uptime = DateTime.Now;
            ReloginTimer = new System.Timers.Timer();
            ReloginTimer.Elapsed += new ElapsedEventHandler(ReloginTimer_Elapsed);
            //Some callbacks..
			DebugUtilities.WriteDebug(session.ToString() + " Initializing callbacks");
            // Client.Network.OnDisconnected += new NetworkManager.DisconnectedCallback(Network_OnDisconnected);
            Client.Network.Disconnected += Network_OnDisconnected; // new syntax

            // Timer used to update an active plugin.
            updateTimer = new System.Timers.Timer(500);
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);

            //Initialize StatefulPlugins
			DebugUtilities.WriteDebug(session.ToString() + " Initializing plugins");
            StatefulPlugins = new Dictionary<string, StatefulPlugin>();
            foreach (Type t in RestBot.StatefulPluginDefinitions)
            {
                ConstructorInfo info = t.GetConstructor(Type.EmptyTypes);
                StatefulPlugin sp = (StatefulPlugin)info.Invoke(new object[0]);
                //Add it to the dictionary
                RegisterStatefulPlugin(sp.MethodName, sp);
				DebugUtilities.WriteDebug(session.ToString() + " * added " + sp.MethodName);
                //Initialize all the handlers, etc
                sp.Initialize(this);
            }
            updateTimer.Start();
        }

        void ReloginTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ReloginTimer.Stop();
            DebugUtilities.WriteInfo(sessionid.ToString() + " relogging..");
            Login();
            //This is where we can handle relogin failures, too.
        }

        private void updateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (StatefulPlugin sp in StatefulPlugins.Values)
            {
                if (sp.Active)
                {
                    sp.Think();
                }
            }
        }

        // rewrote to show message
        void Network_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            if(e.Reason != NetworkManager.DisconnectType.ClientInitiated)
            {
                myStatus = Status.Reconnecting;
                DebugUtilities.WriteWarning(sessionid.ToString() + " was disconnected (" + e.Message.ToString() + "), but I'm logging back in again in 5 minutes.");
                ReloginTimer.Stop();
                ReloginTimer.Interval = 5 * 60 * 1000;
                ReloginTimer.Start();
            }
        }

        public void RegisterStatefulPlugin(string method, StatefulPlugin sp)
        {
            StatefulPlugins.Add(method, sp);
        }

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
            //    delegate(object sender, LoginProgressEventArgs e)
            //    {
            //        DebugUtilities.WriteDebug(String.Format("Login {0}: {1}", e.Status, e.Message));

            //        if (e.Status == LoginStatus.Success)
            //        {
            //            DebugUtilities.WriteSpecial("Logged in successfully");
            //            myStatus = Status.Connected;
            //            response.wasFatal = false;
            //            response.xmlReply = "<success><session_id>" + sessionid.ToString() + "</session_id></success>";
            //        }
            //        else if (e.Status == LoginStatus.Failed)
            //        {
            //            DebugUtilities.WriteError("There was an error while connecting: " + Client.Network.LoginErrorKey);
            //            response.wasFatal = true;
            //            response.xmlReply = "<error></error>";
            //        }
            //    };

            // Optimize the throttle
            Client.Throttle.Wind = 0;
            Client.Throttle.Cloud = 0;
            Client.Throttle.Land = 1000000;
            Client.Throttle.Task = 1000000;
            Client.Settings.LOGIN_SERVER = Program.config.networking.loginuri;

            DebugUtilities.WriteDebug("Login URI: " + Client.Settings.LOGIN_SERVER);

            LoginParams loginParams = Client.Network.DefaultLoginParams(
                    First, Last, MD5Password, "RestBot", VERSION);

            if (Client.Network.Login(loginParams))
            {
                DebugUtilities.WriteSpecial("Logged in successfully");
                myStatus = Status.Connected;
                response.wasFatal = false;
                response.xmlReply = "<success><session_id>" + sessionid.ToString() + "</session_id></success>";
            }
            else
            {
                DebugUtilities.WriteError("There was an error while connecting: " + Client.Network.LoginErrorKey);
                switch (Client.Network.LoginErrorKey)
                {
                    case "connect":
                    case "key":
                    case "disabled":
                        response.wasFatal = true;
                        response.xmlReply = "<error fatal=\"true\">" + Client.Network.LoginMessage + "</error>";
                        break;
                    case "presence":
                    case "timed out":
                    case "god":
                        DebugUtilities.WriteWarning("Nonfatal error while logging in.. this may be normal");
                        response.wasFatal = false;
                        response.xmlReply = "<error fatal=\"false\">" + Client.Network.LoginMessage + "</error><retry>10</retry>\n<session_id>" + sessionid + "</session_id>";

                        DebugUtilities.WriteSpecial("Relogin attempt will be made in 10 minutes");
                        ReloginTimer.Interval = 10 * 60 * 1000; //10 minutes
                        ReloginTimer.Start();
                        break;
                    default:
                        DebugUtilities.WriteError(sessionid.ToString() + " UNKNOWN ERROR ATTEMPTING TO LOGIN");
                        response.wasFatal = true;
                        response.xmlReply = "<error fatal=\"true\">Unknown error has occurred.</error>";
                        break;
                }

                if (response.wasFatal == false) myStatus = Status.Reconnecting;
            }
            //Client.Network.BeginLogin(loginParams);
            return response;
        }

        public LoginReply LoginOLD()
        {
            DebugUtilities.WriteSpecial("Login block was called");
            if (Client.Network.Connected)
            {
                DebugUtilities.WriteError("Uhm, Login() was called when we where already connected. Hurr");
                return new LoginReply();
            }

            ReloginTimer.Stop(); //to stop any relogin timers

            myStatus = Status.LoggingIn;
            //Set up some settings
            //Client.Settings.DEBUG = Program.config.debug.slDebug; //obsolete setting?
            Client.Settings.SIMULATOR_TIMEOUT = 30000; //30 seconds
            Client.Settings.MULTIPLE_SIMS = false; //not for now.
            Client.Settings.SEND_PINGS = true;
            Client.Settings.LOGIN_SERVER = Program.config.networking.loginuri;
            Client.Throttle.Total = Program.config.networking.throttle;

			DebugUtilities.WriteDebug("Login URI: " + Client.Settings.LOGIN_SERVER);

            LoginReply response = new LoginReply();
            string start = "";
            if (Program.config.location.startSim.Trim() != "") start = OpenMetaverse.NetworkManager.StartLocation(Program.config.location.startSim, Program.config.location.x, Program.config.location.y, Program.config.location.z);
            else start = "last";

            if (Client.Network.Login(First, Last, MD5Password, "RESTBot", start, "Jesse Malthus / Pleiades Consulting"))
            {
                DebugUtilities.WriteSpecial("Logged in successfully");
                myStatus = Status.Connected;
                response.wasFatal = false;
                response.xmlReply = "<success><session_id>" + sessionid.ToString() + "</session_id></success>";
            }
            else
            {
                DebugUtilities.WriteError("There was an error while connecting: " + Client.Network.LoginErrorKey);
                switch (Client.Network.LoginErrorKey)
                {
                    case "connect":
                    case "key":
                    case "disabled":
                        response.wasFatal = true;
                        response.xmlReply = "<error fatal=\"true\">" + Client.Network.LoginMessage + "</error>";
                        break;
                    case "presence":
                    case "timed out":
                    case "god":
                        DebugUtilities.WriteWarning("Nonfatal error while logging in.. this may be normal");
                        response.wasFatal = false;
                        response.xmlReply = "<error fatal=\"false\">" + Client.Network.LoginMessage + "</error><retry>10</retry>\n<session_id>" + sessionid + "</session_id>";

                        DebugUtilities.WriteSpecial("Relogin attempt will be made in 10 minutes");
                        ReloginTimer.Interval = 10 * 60 * 1000; //10 minutes
                        ReloginTimer.Start();
                        break;
                    default:
                        DebugUtilities.WriteError(sessionid.ToString() + " UNKNOWN ERROR ATTEMPTING TO LOGIN");
                        response.wasFatal = true;
                        response.xmlReply = "<error fatal=\"true\">Unknown error has occurred.</error>";
                        break;
                }

                if (response.wasFatal == false) myStatus = Status.Reconnecting;
            }

            //yay return
            return response;
        }

        public string DoProcessing(Dictionary<string,string> Parameters, string[] parts)
        {
            string Method = parts[0];
			string debugparams = null;
			foreach (KeyValuePair<string, string> kvp in Parameters) {
				debugparams = debugparams + "[ " + kvp.Key + "=" + kvp.Value + "] ";
			}
			DebugUtilities.WriteDebug(sessionid + "Method - " + Method + " Parameters - " + debugparams);
            //Actual processing
            if (Plugins.ContainsKey(Method))
            {
                return Plugins[Method].Process(this, Parameters);
            }
            //Process the stateful plugins
            else if (StatefulPlugins.ContainsKey(Method))
            {
                return StatefulPlugins[Method].Process(this, Parameters);
            }
			else if ( Method == "stat" )
			{
				string response = "<name>" + Client.Self.FirstName + " " + Client.Self.LastName + "</name>\n";
				response += "<uptime>" + (DateTime.Now - uptime) + "</uptime>\n";
                return response;
			}
			else if ( Method == "status" )
			{
				return("<status>" + myStatus.ToString() + "</status>");
			}
            return ("<error>novalidplugin</error>");
        }
	}
}
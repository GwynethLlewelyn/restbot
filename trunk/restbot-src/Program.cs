/*--------------------------------------------------------------------------------
 FILE INFORMATION:
     Name: Program.cs [./restbot-src/Program.cs]
     Description: This file is the "central hub" of all of the programs resources

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
using System.Net;
using OpenMetaverse;
using System.Threading;
using System.Reflection;
using System.IO;
using System.ComponentModel;
using RESTBot.Server;

namespace RESTBot
{
    public class Session
    {
        public UUID ID;
        public string Hostname;
        public int Permissions;
        public RestBot Bot;
        public DateTime LastAccessed;
        public RestBot.BotStatusCallback StatusCallback;
        public Thread BotThread;
    }

    class Program
    {

        //static HttpListener Listener;
        static Server.Router Listener;
        static bool StillRunning;
        public static Dictionary<UUID, Session> Sessions;

        //config file
        static string configFile = "configuration.xml";
        //configuration object ^-- uses this file --^
        public static XMLConfig.Configuration config;

        //We need to move this to the security configuration block
        
        private static DateTime uptime = new DateTime();

        static void Main(string[] args)
        {
            DebugUtilities.WriteInfo("Restbot startup");
            Sessions = new Dictionary<UUID, Session>();

            DebugUtilities.WriteInfo("Loading plugins");
            RegisterAllCommands(Assembly.GetExecutingAssembly());
            DebugUtilities.WriteDebug("Loading stateful plugins");
            RegisterAllStatefulPlugins(Assembly.GetExecutingAssembly());

            DebugUtilities.WriteInfo("Reading config file");
            config = XMLConfig.Configuration.LoadConfiguration(configFile);
            DebugUtilities.WriteInfo("Listening on port " + config.networking.port.ToString());
            //Set up the listener / router
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
                        ConstructorInfo info = t.GetConstructor(Type.EmptyTypes);
                        RestPlugin plugin = (RestPlugin)info.Invoke(new object[0]);
                        RestBot.AddPlugin(plugin);
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
            //Process the request params from POST, URL
            Dictionary<string, string> Parameters = RestBot.HandleDataFromRequest(headers, body);
            string debugparams = null;
            string debugparts = null;
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
                //Alright, we're going to try to establish a session
                if (parts.Length >= 2 && parts[1] == Program.config.security.serverPass
                    && Parameters.ContainsKey("first") && Parameters.ContainsKey("last") && Parameters.ContainsKey("pass"))
                {
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
                    return ("<error>arguments</error>");
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
            RestBot r = Sessions[sess].Bot;
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

        
        public static void DisposeSession(UUID key)
        {
            DebugUtilities.WriteDebug("Disposing of session " + key.ToString());
            if (!Sessions.ContainsKey(key))
                return;
            Session s = Sessions[key];
            s.Bot.OnBotStatus -= s.StatusCallback;
            s.Bot.Client.Network.Logout();
            Sessions.Remove(key);
        }
    }
}
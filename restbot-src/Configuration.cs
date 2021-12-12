/*--------------------------------------------------------------------------------
 FILE INFORMATION:
     Name: Configuration.cs [./restbot-src/Configuration.cs]
     Description: This file contains classes used by the .net's internal xml parser
                  to define configuration options as seen in configuration.xml.

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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace RESTBot.XMLConfig
{
		/// <summary>
		/// Configuration class designed for the `bots array found in the xml config file
		/// </summary>
		/// <remarks>To find a copy of this class, see the Configuration class</remarks>

		[XmlRoot("restbot")]
    public class Configuration
    {
        [XmlElement("networking")]
        public NetworkConfig networking = new NetworkConfig();
        [XmlElement("debug")]
        public DebugConfig debug = new DebugConfig();
        [XmlElement("location")]
        public StartLocationConfig location = new StartLocationConfig();
        [XmlElement("security")]
        public SecurityConfig security = new SecurityConfig();

        public static Configuration LoadConfiguration(string configuration_file)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
						FileStream? fileStream = null;
						try
            {
                 fileStream = new FileStream(configuration_file, FileMode.Open);
            }
            catch(Exception e)
            {
                DebugUtilities.WriteError("Could not open configuration file!");
                DebugUtilities.WriteError("\t" + e.Message);
                Environment.Exit(1);
            }
            try
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                return (Configuration)serializer.Deserialize(fileStream);
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError("Could not parse XML file!");
                DebugUtilities.WriteError("\t" + e.Message);
                Environment.Exit(1);
            }
            return null;
        }

    }
}

namespace RESTBot.XMLConfig
{
    public class NetworkConfig
    {
        [XmlElement("ip")]
        public string ip = "0.0.0.0";
        [XmlElement("port")]
        public int port = 9080;
        [XmlElement("loginuri")]
        public string loginuri = "https://login.agni.lindenlab.com/cgi-bin/login.cgi"; //for special login url. You gotta have a compatible libsl version though!!
        [XmlElement("throttle")]
        public float throttle = 1572864.0f;
		[XmlElement("webapi-url")]
		public string backendURL = "https://localhost/actorbot/pipe.php";
	}
}

public class StartLocationConfig
{
    //TODO: Make x,y,z floats but round them off when using them (this is so the parser can read a decimal instead of errroring out)

    [XmlElement("sim")]
    public string startSim = "strace island";
    [XmlElement("x")]
    public int x = 128;
    [XmlElement("y")]
    public int y = 128;
    [XmlElement("z")]
    public int z = 128;
}

public class DebugConfig
{
    //itty bitty file - can be expanded onto later

    [XmlElement("restbot")]
    public bool restbotDebug = false;
    [XmlElement("libsl")]
    public bool slDebug = true;
}

public class SecurityConfig
{
    [XmlElement("hostnamelock")]
    public bool hostnameLock = false;
    [XmlElement("serverpassword")]
    public string serverPass = "pass";
}

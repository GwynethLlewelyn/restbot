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

		Additional (sub)classes by Gwyneth Llewelyn in 2022
--------------------------------------------------------------------------------*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace RESTBot.XMLConfig
{
	/// <summary>
	/// Configuration class designed for the `bots array found in the xml config file
	/// </summary>
	/// <remarks>To find a copy of this class, see the <c>Configuration</c> class</remarks>
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

		[XmlElement("plugin")]
		public PluginConfig plugin = new PluginConfig();

		/// <value>The default URI to connect to the Linden Lab grid</value>
		public static string
			defaultLoginURI = "https://login.agni.lindenlab.com/cgi-bin/login.cgi";

		/// <summary>
		/// Load the configuration file (if it exists).
		/// </summary>
		/// <param name="configuration_file">path to the configuration file</param>
		public static Configuration? LoadConfiguration(string configuration_file)
		{
			XmlSerializer? serializer = new XmlSerializer(typeof(Configuration));
			FileStream? fileStream = null;
			try
			{
				fileStream = new FileStream(configuration_file, FileMode.Open);
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError("Could not open configuration file; error was: " + e.Message);
				Environment.Exit(1);
			}
			try
			{
				// fileStream/serializer are now nullable; return null if that's the case. (gwyneth 20220127)
				if (fileStream != null && serializer != null)
				{
					fileStream.Seek(0, SeekOrigin.Begin);
					return (Configuration?) serializer.Deserialize(fileStream);
				}
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError("Could not parse XML file - error was: " + e.Message);
				Environment.Exit(1); // this is overkill... we might be able to deal with most errors using reasonable defaults... (gwyneth 20220127)
			}
			return null;
		} // end constructor
	} // end class Configuration
} // end namespace RESTBot.XMLConfig, but, weirdly enough, it continues below...

namespace RESTBot.XMLConfig
{
	/// <summary>Class to define the network configuration</summary>
	/// <remarks>It sets a few reasonable defaults</remarks>
	public class NetworkConfig
	{
		[XmlElement("ip")]
		/// <value>IP address ought to be set to 127.0.0.1 for localhost connections only;
		///					 use 0.0.0.0 to have it bound to _all_ interfaces and be accessible from the Internet</value>
		public string ip = "0.0.0.0";

		[XmlElement("port")]
		public int port = 9080;

		[XmlElement("loginuri")]
		/// <summary>for special login url. You gotta have a compatible libsl version though!!</summary>
		public string loginuri = Configuration.defaultLoginURI;

		[XmlElement("throttle")]
		public float throttle = 1572864.0f;

		[XmlElement("webapi-url")]
		/// <value>What _is_ this!? It's not included anywhere... (gwyneth 20220109)</value>
		public string backendURL = "https://localhost/actorbot/pipe.php";
	}

	/// <summary>Class to configure the start location</summary>
	public class StartLocationConfig
	{
		//TODO: Make x,y,z floats but round them off when using them (this is so the parser can read a decimal instead of errroring out)
		[XmlElement("sim")]
		/// <value>Name of the region for the RESTbot to start, by default</value>
		public string startSim = "strace island"; // 'Ahern' ought to be a better default... (gwyneth 20220109)

		[XmlElement("x")]
		public int x = 128;

		[XmlElement("y")]
		public int y = 128;

		[XmlElement("z")]
		public int z = 128; /// 20 or 30 ought to be more reasonable starting points, since that's the level that water is set (gwyneth 20220109)
	}

	/// <summary>Class to deal with debugging</summary>
	/// <remarks>
	///   <para>[Original comment:] itty bitty file - can be expanded onto later</para>
	///		<para>Note that the DebugUtilities class checks if the <c>restbotDebug</c> variable is set, falling back to hard-coded #defines if not (gwyneth 20220109)</para>
	///   <para>Also note that you can have both settings simultaneously configured</para>
	///   <para>Note that if <c>Log4Net</c> is wrongly configured (using <c>restbot.exe.config</c>),
	///					everything will get logged to the console instead (duplicate messages possible)</para>
	///  </remarks>
	public class DebugConfig
	{
		[XmlElement("restbot")]
		/// <value>Log debugging specific to RESTbot</value>
		public bool restbotDebug = false;

		[XmlElement("libsl")]
		/// <value>Log debugging specific to the underlying LibreMetaverse layer</value>
		/// <remarks>probably ignored (gwyneth 20220427)</remarks>
		public bool slDebug = true;
	}

	/// <summary>Class to deal with security configurations</summary>
	/// <remarks>Password MUST be changed when in production! (gwyneth 20220109)</remarks>
	public class SecurityConfig
	{
		[XmlElement("hostnamelock")]
		/// <value>I have no idea what this is for...</value>
		public bool hostnameLock = false;

		[XmlElement("serverpassword")]
		/// <value>Main server password, used in the API; please change it!</value>
		/// <remarks>Should be a URL-safe password, so beware of very strange unencoded characters.. (gwyneth 20220427)
		public string serverPass = "pass"; // Change me!
	}

	/// <summary>
	/// Class to deal with specific plugin configurations
  ///
	/// <para>Since 8.1.5</para>
	///	<para>Here we will have a few special debug flags for plugins that have zero configuration
	/// and were never meant to be configured manually.</para>
	/// <para>Currently only used for the Reaper plugin, but more may be added in the future!</para>
	/// </summary>
	public class PluginConfig
	{
		[XmlElement("reaper")]
		/// <value>The Reaper plugin seems to have no possible configuration to turn on/off, so we set it here</value>
		public bool reaper = true;

		[XmlElement("reaper-session-timeout")]
		/// <value>Session timeout timespan</value>
		public TimeSpan reaperSessionTimeout = new TimeSpan(1, 0, 0); // default: one hour (h, m, s)

		[XmlElement("reaper-sweep-interval")]
		/// <value>interval between reaper sweeps in ms</value>
		public double reaperSweepInterval = 10000; // default: 10 seconds
	}
}

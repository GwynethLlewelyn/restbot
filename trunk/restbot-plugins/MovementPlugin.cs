/*--------------------------------------------------------------------------------
 LICENSE:
	 This file is part of the RESTBot Project.
 
	 RESTbot is free software; you can redistribute it and/or modify it under
	 the terms of the Affero General Public License Version 1 (March 2002)
 
	 RESTBot is distributed in the hope that it will be useful,
	 but WITHOUT ANY WARRANTY; without even the implied warranty of
	 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.	See the
	 Affero General Public License for more details.

	 You should have received a copy of the Affero General Public License
	 along with this program (see ./LICENSING) If this is missing, please 
	 contact alpha.zaius[at]gmail[dot]com and refer to 
	 <http://www.gnu.org/licenses/agpl.html> for now.
	 
	 Further changes by Gwyneth Llewelyn

 COPYRIGHT: 
	 RESTBot Codebase (c) 2007-2008 PLEIADES CONSULTING, INC
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System.Xml;
using System.Xml.Serialization;
using System.Net;
using System.IO;
using System.Threading;

// movemeny functions; based on TestClient.exe code
namespace RESTBot
{
	// show current location
	public class CurrentLocationPlugin : StatefulPlugin
	{
		private UUID session;
		
		public CurrentLocationPlugin()
		{
			MethodName = "location";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}
		
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			try
			{
				return "<location><CurrentSim>" + b.Client.Network.CurrentSim.ToString() + "</CurrentSim><Position>" + 
                b.Client.Self.SimPosition.ToString() + "</Position>";
  			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>" + e.Message + "</error>";
			}
		}
	} // end location
	
	// move to location; parameters are sim, x, y, z
	public class GotoPlugin : StatefulPlugin
	{
		private UUID session;
		
		public GotoPlugin()
		{
			MethodName = "goto";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}
		
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			try
			{
				string sim = "";
				float x = 128.0f, y = 128.0f, z = 30.0f;
				bool check = true;
			
				if (Parameters.ContainsKey("sim"))
				{
					sim = Parameters["sim"].ToString();
				}
				else check = false;

				if (Parameters.ContainsKey("x"))
				{
					check &= float.TryParse(Parameters["x"], out x);
				}				
				else check = false;

				if (Parameters.ContainsKey("y"))
				{
					check &= float.TryParse(Parameters["y"], out y);
				}				
				else check = false;
				
				if (Parameters.ContainsKey("z"))
				{
					check &= float.TryParse(Parameters["z"], out z);
				}				
				else check = false;
							
				if (!check)
				{
					return "<error>parameters have to be simulator name, x, y, z</error>";
				}
				
	            if (b.Client.Self.Teleport(sim, new Vector3(x, y, z)))
	                return "<teleport>" + b.Client.Network.CurrentSim + "</teleport>";
	            else
	                return "<error>Teleport failed: " + b.Client.Self.TeleportMessage + "</error>";
	  		}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>" + e.Message + "</error>";
			}
		}
	} // end goto
}
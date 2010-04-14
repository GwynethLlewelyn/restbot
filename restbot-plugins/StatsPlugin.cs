/*--------------------------------------------------------------------------------
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
using libsecondlife;
using System.Net;
using System.Threading;

// avatar info functions
namespace RESTBot
{
    public class DilationPlugin : RestPlugin
    {
        public DilationPlugin()
        {
            MethodName = "dilation";
        }
		
        public override string Process(RestBot b, Dictionary<string, string> Paramaters)
        {
			b.Client.Settings.ALWAYS_DECODE_OBJECTS=true;
            string dilation = b.Client.Network.CurrentSim.Dilation.ToString();
            b.Client.Settings.ALWAYS_DECODE_OBJECTS = false;
			return("<dilation>" + dilation + "</dilation>\n");
		}
    }
	
    public class SimStatPlugin : RestPlugin
    {
        public SimStatPlugin()
        {
            MethodName = "sim_stat";
        }
		
        public override string Process(RestBot b, Dictionary<string, string> Paramaters)
        {
			bool check = false;
			while ( !check ) {
				if ( b.Client.Network.CurrentSim.FPS != 0 ) {
					check = true;
				}
			}
			string response = "<stats>\n";
			response += "<dilation>" + b.Client.Network.CurrentSim.Dilation.ToString() + "</dilation>\n";
			response += "<inbps>" + b.Client.Network.CurrentSim.IncomingBPS.ToString() + "</inbps>\n";
			response += "<outbps>" + b.Client.Network.CurrentSim.OutgoingBPS.ToString() + "</outbps>\n";
			response += "<resentout>" + b.Client.Network.CurrentSim.ResentPackets.ToString() + "</resentout>\n";
			response += "<resentin>" + b.Client.Network.CurrentSim.ReceivedResends.ToString() + "</resentin>\n";
			response += "<queue>" + b.Client.Network.InboxCount.ToString() + "</queue>\n";
			response += "<fps>" + b.Client.Network.CurrentSim.FPS.ToString() + "</fps>\n";
			response += "<physfps>" + b.Client.Network.CurrentSim.PhysicsFPS.ToString() + "</physfps>\n";
			response += "<agentupdates>" + b.Client.Network.CurrentSim.AgentUpdates.ToString() + "</agentupdates>\n";
			response += "<objects>" + b.Client.Network.CurrentSim.Objects.ToString() + "</objects>\n";
			response += "<scriptedobjects>" + b.Client.Network.CurrentSim.ScriptedObjects.ToString() + "</scriptedobjects>\n";
			response += "<agents>" + b.Client.Network.CurrentSim.Agents.ToString() + "</agents>\n";
			response += "<childagents>" + b.Client.Network.CurrentSim.ChildAgents.ToString() + "</childagents>\n";
			response += "<activescripts>" + b.Client.Network.CurrentSim.ActiveScripts.ToString() + "</activescripts>\n";
			response += "<lslips>" + b.Client.Network.CurrentSim.LSLIPS.ToString() + "</lslips>\n";
			response += "<inpps>" + b.Client.Network.CurrentSim.INPPS.ToString() + "</inpps>\n";
			response += "<outpps>" + b.Client.Network.CurrentSim.OUTPPS.ToString() + "</outpps>\n";
			response += "<pendingdownloads>" + b.Client.Network.CurrentSim.PendingDownloads.ToString() + "</pendingdownloads>\n";
			response += "<pendinguploads>" + b.Client.Network.CurrentSim.PendingUploads.ToString() + "</pendinguploads>\n";
			response += "<virtualsize>" + b.Client.Network.CurrentSim.VirtualSize.ToString() + "</virtualsize>\n";
			response += "<residentsize>" + b.Client.Network.CurrentSim.ResidentSize.ToString() + "</residentsize>\n";
			response += "<pendinglocaluploads>" + b.Client.Network.CurrentSim.PendingLocalUploads.ToString() + "</pendinglocaluploads>\n";
			response += "<unackedbytes>" + b.Client.Network.CurrentSim.UnackedBytes.ToString() + "</unackedbytes>\n";
			response += "<time>\n";
			response += "<frame>" + b.Client.Network.CurrentSim.FrameTime.ToString() + "</frame>\n";
			response += "<image>" + b.Client.Network.CurrentSim.ImageTime.ToString() + "</image>\n";
			response += "<physics>" + b.Client.Network.CurrentSim.PhysicsTime.ToString() + "</physics>\n";
			response += "<script>" + b.Client.Network.CurrentSim.ScriptTime.ToString() + "</script>\n";
			response += "<other>" + b.Client.Network.CurrentSim.OtherTime.ToString() + "</other>\n";
			response += "</time>\n";
			response += "</stats>\n";
            return (response);
		}
    }
}

/*--------------------------------------------------------------------------------
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
using OpenMetaverse;
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

            string dilation = b.Client.Network.CurrentSim.Stats.Dilation.ToString();
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
				if ( b.Client.Network.CurrentSim.Stats.FPS != 0 ) {
					check = true;
				}
			}
			string response = "<stats>\n";
			response += "<dilation>" + b.Client.Network.CurrentSim.Stats.Dilation.ToString() + "</dilation>\n";
			response += "<inbps>" + b.Client.Network.CurrentSim.Stats.IncomingBPS.ToString() + "</inbps>\n";
			response += "<outbps>" + b.Client.Network.CurrentSim.Stats.OutgoingBPS.ToString() + "</outbps>\n";
			response += "<resentout>" + b.Client.Network.CurrentSim.Stats.ResentPackets.ToString() + "</resentout>\n";
			response += "<resentin>" + b.Client.Network.CurrentSim.Stats.ReceivedResends.ToString() + "</resentin>\n";
			response += "<queue>" + b.Client.Network.InboxCount.ToString() + "</queue>\n";
			response += "<fps>" + b.Client.Network.CurrentSim.Stats.FPS.ToString() + "</fps>\n";
			response += "<physfps>" + b.Client.Network.CurrentSim.Stats.PhysicsFPS.ToString() + "</physfps>\n";
			response += "<agentupdates>" + b.Client.Network.CurrentSim.Stats.AgentUpdates.ToString() + "</agentupdates>\n";
			response += "<objects>" + b.Client.Network.CurrentSim.Stats.Objects.ToString() + "</objects>\n";
			response += "<scriptedobjects>" + b.Client.Network.CurrentSim.Stats.ScriptedObjects.ToString() + "</scriptedobjects>\n";
			response += "<agents>" + b.Client.Network.CurrentSim.Stats.Agents.ToString() + "</agents>\n";
			response += "<childagents>" + b.Client.Network.CurrentSim.Stats.ChildAgents.ToString() + "</childagents>\n";
			response += "<activescripts>" + b.Client.Network.CurrentSim.Stats.ActiveScripts.ToString() + "</activescripts>\n";
			response += "<lslips>" + b.Client.Network.CurrentSim.Stats.LSLIPS.ToString() + "</lslips>\n";
			response += "<inpps>" + b.Client.Network.CurrentSim.Stats.INPPS.ToString() + "</inpps>\n";
			response += "<outpps>" + b.Client.Network.CurrentSim.Stats.OUTPPS.ToString() + "</outpps>\n";
			response += "<pendingdownloads>" + b.Client.Network.CurrentSim.Stats.PendingDownloads.ToString() + "</pendingdownloads>\n";
			response += "<pendinguploads>" + b.Client.Network.CurrentSim.Stats.PendingUploads.ToString() + "</pendinguploads>\n";
			response += "<virtualsize>" + b.Client.Network.CurrentSim.Stats.VirtualSize.ToString() + "</virtualsize>\n";
			response += "<residentsize>" + b.Client.Network.CurrentSim.Stats.ResidentSize.ToString() + "</residentsize>\n";
			response += "<pendinglocaluploads>" + b.Client.Network.CurrentSim.Stats.PendingLocalUploads.ToString() + "</pendinglocaluploads>\n";
			response += "<unackedbytes>" + b.Client.Network.CurrentSim.Stats.UnackedBytes.ToString() + "</unackedbytes>\n";
			response += "<time>\n";
			response += "<frame>" + b.Client.Network.CurrentSim.Stats.FrameTime.ToString() + "</frame>\n";
			response += "<image>" + b.Client.Network.CurrentSim.Stats.ImageTime.ToString() + "</image>\n";
			response += "<physics>" + b.Client.Network.CurrentSim.Stats.PhysicsTime.ToString() + "</physics>\n";
			response += "<script>" + b.Client.Network.CurrentSim.Stats.ScriptTime.ToString() + "</script>\n";
			response += "<other>" + b.Client.Network.CurrentSim.Stats.OtherTime.ToString() + "</other>\n";
			response += "</time>\n";
			response += "</stats>\n";
            return (response);
		}
    }
}

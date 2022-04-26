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
		MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.	See the
		GNU Affero General Public License for more details.

		You should have received a copy of the GNU Affero General Public License
		along with this program.	If not, see <http://www.gnu.org/licenses/>.
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using System.Net;
using System.Threading;

namespace RESTBot
{
		/// <summary>
		/// Class to return the current simulator dilation (a measure of lag).
		/// </summary>
		public class DilationPlugin : RestPlugin
		{
				/// <summary>
				/// Sets the plugin name for the router.
				/// </summary>
				public DilationPlugin()
				{
						MethodName = "dilation";
				}

				/// <summary>
				/// Handler event for this plugin.
				/// </summary>
				/// <param name="b">A currently active RestBot</param>
				/// <param name="Parameters">not used</param>
				public override string Process(RestBot b, Dictionary<string, string> Parameters)
				{
						b.Client.Settings.ALWAYS_DECODE_OBJECTS = true;

						string dilation = b.Client.Network.CurrentSim.Stats.Dilation.ToString();
						b.Client.Settings.ALWAYS_DECODE_OBJECTS = false;
						return $"<{MethodName}>{dilation}</{MethodName}>";
				}
		}

		/// <summary>
		/// Class to return full statistics of the simulator the 'bot is in.
		/// </summary>
		public class SimStatPlugin : RestPlugin
		{
				/// <summary>
				/// Sets the plugin name for the router.
				/// </summary>
				public SimStatPlugin()
				{
						MethodName = "sim_stat";
				}

				/// <summary>
				/// Handler event for this plugin.
				/// </summary>
				/// <param name="b">A currently active RestBot</param>
				/// <param name="Parameters">not used</param>
				/// <remarks>This will list statistics only for the simulator where the 'bot currently is.</remarks>
				public override string Process(RestBot b, Dictionary<string, string> Parameters)
				{
						bool check = false;
						while ( !check ) {
								if ( b.Client.Network.CurrentSim.Stats.FPS != 0 ) {
								check = true;
						}
				}
				string response = $@"<stats>
	<dilation>{b.Client.Network.CurrentSim.Stats.Dilation.ToString()}</dilation>
	<inbps>{b.Client.Network.CurrentSim.Stats.IncomingBPS.ToString()}</inbps>
	<outbps>{b.Client.Network.CurrentSim.Stats.OutgoingBPS.ToString()}</outbps>
	<resentout>{b.Client.Network.CurrentSim.Stats.ResentPackets.ToString()}</resentout>
	<resentin>{b.Client.Network.CurrentSim.Stats.ReceivedResends.ToString()}</resentin>
	<queue>{b.Client.Network.InboxCount.ToString()}</queue>
	<fps>{b.Client.Network.CurrentSim.Stats.FPS.ToString()}</fps>
	<physfps>{b.Client.Network.CurrentSim.Stats.PhysicsFPS.ToString()}</physfps>
	<agentupdates>{b.Client.Network.CurrentSim.Stats.AgentUpdates.ToString()}</agentupdates>
	<objects>{b.Client.Network.CurrentSim.Stats.Objects.ToString()}</objects>
	<scriptedobjects>{b.Client.Network.CurrentSim.Stats.ScriptedObjects.ToString()}</scriptedobjects>
	<agents>{b.Client.Network.CurrentSim.Stats.Agents.ToString()}</agents>
	<childagents>{b.Client.Network.CurrentSim.Stats.ChildAgents.ToString()}</childagents>
	<activescripts>{b.Client.Network.CurrentSim.Stats.ActiveScripts.ToString()}</activescripts>
	<lslips>{b.Client.Network.CurrentSim.Stats.LSLIPS.ToString()}</lslips>
	<inpps>{b.Client.Network.CurrentSim.Stats.INPPS.ToString()}</inpps>
	<outpps>{b.Client.Network.CurrentSim.Stats.OUTPPS.ToString()}</outpps>
	<pendingdownloads>{b.Client.Network.CurrentSim.Stats.PendingDownloads.ToString()}</pendingdownloads>
	<pendinguploads>{b.Client.Network.CurrentSim.Stats.PendingUploads.ToString()}</pendinguploads>
	<virtualsize>{b.Client.Network.CurrentSim.Stats.VirtualSize.ToString()}</virtualsize>
	<residentsize>{b.Client.Network.CurrentSim.Stats.ResidentSize.ToString()}</residentsize>
	<pendinglocaluploads>{b.Client.Network.CurrentSim.Stats.PendingLocalUploads.ToString()}</pendinglocaluploads>
	<unackedbytes>{b.Client.Network.CurrentSim.Stats.UnackedBytes.ToString()}</unackedbytes>
	<time>
		<frame>{b.Client.Network.CurrentSim.Stats.FrameTime.ToString()}</frame>
		<image>{b.Client.Network.CurrentSim.Stats.ImageTime.ToString()}</image>
		<physics>{b.Client.Network.CurrentSim.Stats.PhysicsTime.ToString()}</physics>
		<script>{b.Client.Network.CurrentSim.Stats.ScriptTime.ToString()}</script>
		<other>{b.Client.Network.CurrentSim.Stats.OtherTime.ToString()}</other>
	</time>
</stats>";
				return response;
				}
		}

		/// <summary>
		/// Class to return all currently valid sessions and associated avatar key.
		/// </summary>
		/// <remarks><para>I need this to check upstream which RESTbots have valid sessions.</para>
		/// <para>(gwyneth 20220424)</para>
		/// <para><seealso cref="SessionPlugin"></para></remarks>
		public class SessionListPlugin : RestPlugin
		{
				/// <summary>
				/// Sets the plugin name for the router.
				/// </summary>
				public SessionListPlugin()
				{
						MethodName = "session_list";
				}

				/// <summary>
				/// Handler event for this plugin.
				/// </summary>
				/// <param name="b">A currently active RestBot</param>
				/// <param name="Parameters">not used</param>
				/// <remarks><para>This will list all currently known sessions.</para>
				/// <para>Also note that there is a way to retrieve all sessions (same method)
				/// without requiring a valid bot session, just for my own purposes; <see cref="Program.Main" /></para>
				/// </remarks>
				public override string Process(RestBot b, Dictionary<string, string> Parameters)
				{
						bool check = false;
						if (Program.Sessions.Count != 0) // no sessions? that's fine, no need to abort
						{
							check = true;
						}

						string response = $"<{MethodName}>";
						if (check)	// optimisation: if empty, no need to run the foreach (gwyneth 20220424)
						{
								foreach(KeyValuePair<OpenMetaverse.UUID, RESTBot.Session> kvp in Program.Sessions)
								{
										response += $"<session key={kvp.Key.ToString()}>{kvp.Value.ID}</session>";
								}
						}
						else
						{
							response += "no sessions";
						}
						response += $"</{MethodName}>";
						return response;
				} // end Process
		} // end SessionListPlugin
} // end namespace

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
using System.Net;
using System.Threading;

// group functions; based on TestClient.exe code
namespace RESTBot
{
	// Set active group, using group UUID
	public class ActivateGroupKeyPlugin : StatefulPlugin
	{
		protected ManualResetEvent GroupsEvent = new ManualResetEvent(false);
				
		private UUID session;
		private string activeGroup;
		
		public ActivateGroupKeyPlugin()
		{
			MethodName = "group_key_activate";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID groupUUID;
			DebugUtilities.WriteDebug("TR - Entering group key parser");
			try
			{
				bool check = false;
				if ( Parameters.ContainsKey("key") ) {
					DebugUtilities.WriteDebug("TR - Attempting to parse from POST");
					check = UUID.TryParse(Parameters["key"].ToString().Replace("_"," "), out groupUUID);
					DebugUtilities.WriteDebug("TR - Succesfully parsed POST");
				} else {
					return "<error>arguments</error>";
				}
				if ( check ) {
					DebugUtilities.WriteDebug("TR - Activating group");
					string response = activateGroup(b, groupUUID);
					DebugUtilities.WriteDebug("TR - Complete");
					return "<active>" + response.Trim() + "</active>\n";
				} else {
					return "<error>parsekey</error>";
				}
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>";
			}
			
		}
		
		private string activateGroup(RestBot b, UUID groupUUID)
		{
			DebugUtilities.WriteInfo(session.ToString() + " " + MethodName + " Activating group " + groupUUID.ToString());
			EventHandler<PacketReceivedEventArgs> pcallback = AgentDataUpdateHandler;
			b.Client.Network.RegisterCallback(PacketType.AgentDataUpdate, pcallback);
			b.Client.Groups.ActivateGroup(groupUUID);
			
			if ( ! GroupsEvent.WaitOne(15000, true) ) {
				DebugUtilities.WriteWarning(session + " " + MethodName + " timed out on setting active group");
			}

			// ignore everything and just reset the event
			b.Client.Network.UnregisterCallback(PacketType.AgentDataUpdate, pcallback);
			GroupsEvent.Reset();			

			if (String.IsNullOrEmpty(activeGroup))
			   	DebugUtilities.WriteWarning(session + " " + MethodName + " Failed to activate the group " + groupUUID);

            return activeGroup;
		}
		
		private void AgentDataUpdateHandler(object sender, PacketReceivedEventArgs e)
        {
            AgentDataUpdatePacket p = (AgentDataUpdatePacket)e.Packet;
            //if (p.AgentData.AgentID == Client.Self.AgentID)
            //{
                activeGroup = Utils.BytesToString(p.AgentData.GroupName) + " (" + Utils.BytesToString(p.AgentData.GroupTitle) + ")";
                GroupsEvent.Set();
            //}
        }	
	}
	
	// Set active group, using group Name
	public class ActivateGroupNamePlugin : StatefulPlugin
	{
		protected ManualResetEvent GroupsEvent = new ManualResetEvent(false);
		public Dictionary<UUID, Group> GroupsCache = null;
		private UUID session;
		private string activeGroup;
		
		public ActivateGroupNamePlugin()
		{
			MethodName = "group_name_activate";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID groupUUID;
			string groupName;
			DebugUtilities.WriteDebug("TR - Entering group key parser");
			try
			{
				if ( Parameters.ContainsKey("name") ) {
					groupName = Parameters["name"].ToString().Replace("%20"," ").Replace("+"," ");
				} else {
					return "<error>arguments</error>";
				}
				DebugUtilities.WriteDebug("TR - Activating group");
					
				groupUUID = GroupName2UUID(b, groupName);
            	if (UUID.Zero != groupUUID)
            	{
					string response = activateGroup(b, groupUUID);
					DebugUtilities.WriteDebug("TR - Complete");
					return "<active>" + response.Trim() + "</active>\n";
				}
				else
				{
					DebugUtilities.WriteDebug("TR - Error: group " + groupName + " doesn't exist");
					return "<error>group name '" + groupName + "' doesn't exist.</error>";
				}
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>";
			}
			
		}
		
        public void ReloadGroupsCache(RestBot b)
        {
            b.Client.Groups.CurrentGroups += Groups_CurrentGroups;            
            b.Client.Groups.RequestCurrentGroups();
            GroupsEvent.WaitOne(10000, false);
            b.Client.Groups.CurrentGroups -= Groups_CurrentGroups;
            GroupsEvent.Reset();
        }

        void Groups_CurrentGroups(object sender, CurrentGroupsEventArgs e)
        {
            if (null == GroupsCache)
                GroupsCache = e.Groups;
            else
                lock (GroupsCache) { GroupsCache = e.Groups; }
            GroupsEvent.Set();
        }

        public UUID GroupName2UUID(RestBot b, String groupName)
        {
            UUID tryUUID;
            if (UUID.TryParse(groupName,out tryUUID))
                    return tryUUID;
            if (null == GroupsCache) {
                    ReloadGroupsCache(b);
                if (null == GroupsCache)
                    return UUID.Zero;
            }
            lock(GroupsCache) {
                if (GroupsCache.Count > 0) {
                    foreach (Group currentGroup in GroupsCache.Values)
                        if (currentGroup.Name.ToLower() == groupName.ToLower())
                            return currentGroup.ID;
                }
            }
            return UUID.Zero;
        }
		
		private string activateGroup(RestBot b, UUID groupUUID)
		{
			DebugUtilities.WriteInfo(session.ToString() + " " + MethodName + " Activating group " + groupUUID.ToString());
			EventHandler<PacketReceivedEventArgs> pcallback = AgentDataUpdateHandler;
			b.Client.Network.RegisterCallback(PacketType.AgentDataUpdate, pcallback);
			b.Client.Groups.ActivateGroup(groupUUID);
			
			if ( ! GroupsEvent.WaitOne(15000, true) ) {
				DebugUtilities.WriteWarning(session + " " + MethodName + " timed out on setting active group");
			}

			// ignore everything and just reset the event
			b.Client.Network.UnregisterCallback(PacketType.AgentDataUpdate, pcallback);
			GroupsEvent.Reset();			

			if (String.IsNullOrEmpty(activeGroup))
			   	DebugUtilities.WriteWarning(session + " " + MethodName + " Failed to activate the group " + groupUUID);

            return activeGroup;
		}
		
		private void AgentDataUpdateHandler(object sender, PacketReceivedEventArgs e)
        {
            AgentDataUpdatePacket p = (AgentDataUpdatePacket)e.Packet;
            //if (p.AgentData.AgentID == Client.Self.AgentID)
            //{
                activeGroup = Utils.BytesToString(p.AgentData.GroupName) + " (" + Utils.BytesToString(p.AgentData.GroupTitle) + ")";
                GroupsEvent.Set();
            //}
        }	
	}

}
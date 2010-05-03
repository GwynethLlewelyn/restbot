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

	// Send Group Instant Message (Group Chat)
	public class GroupIMPlugin : StatefulPlugin
	{
		protected ManualResetEvent WaitForSessionStart = new ManualResetEvent(false);
		private UUID session;
		
		public GroupIMPlugin()
		{
			MethodName = "group_im"; // parameters are key (Group UUID) and message
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}
		
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID groupUUID;
			string message;
			
			try
			{
				bool check = false;
				if ( Parameters.ContainsKey("key") )
				{
					check = UUID.TryParse(Parameters["key"].ToString().Replace("_"," "), out groupUUID);
				}
				else
				{
					return "<error>arguments: no key</error>";
				}
				if ( check ) 
				{
					if ( Parameters.ContainsKey("message") )
					{
						message = Parameters["message"].ToString().Replace("%20"," ").Replace("+"," ");
					}
					else
					{
						return "<error>arguments: no message</error>";
					}
					
				}
				else
				{
					return "<error>parsekey</error>";
				}

				message = message.TrimEnd();
				if (message.Length > 1023)
				{
					message = message.Remove(1023);
					DebugUtilities.WriteDebug(session + " " + MethodName + " Message truncated at 1024 characters");
				}

				string response = sendIMGroup(b, groupUUID, message);
					
				return "<message>" + response.Trim() + "</message>\n";
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>loads of errors</error>";
			}
			
		}
				
		private string sendIMGroup(RestBot b, UUID groupUUID, string message)
		{
			DebugUtilities.WriteInfo(session.ToString() + " " + MethodName + " Sending message '" + message + "' to group UUID " + groupUUID.ToString());
			b.Client.Self.GroupChatJoined += Self_GroupChatJoined;
			
			if (!b.Client.Self.GroupChatSessions.ContainsKey(groupUUID))
			{
				WaitForSessionStart.Reset();
				b.Client.Self.RequestJoinGroupChat(groupUUID);
			}
			else
			{
				WaitForSessionStart.Set();
			}
			
			if (WaitForSessionStart.WaitOne(20000, false))
			{
				b.Client.Self.InstantMessageGroup(groupUUID, message);
			}
			else
			{
				DebugUtilities.WriteInfo(session.ToString() + " " + MethodName + " Timeout waiting for group session start");
				return "timeout";
			}

			b.Client.Self.GroupChatJoined -= Self_GroupChatJoined;
			DebugUtilities.WriteInfo(session.ToString() + " " + MethodName + " Instant Messaged group " + groupUUID.ToString() + " with message: " + message);
			
			return "message sent";
		}
		
		void Self_GroupChatJoined(object sender, GroupChatJoinedEventArgs e)
		{
			if (e.Success)
			{
				DebugUtilities.WriteInfo(session.ToString() + " " + MethodName + "Joined {0} Group Chat Success!");
				WaitForSessionStart.Set();
			}
			else
			{
				DebugUtilities.WriteInfo(session.ToString() + " " + MethodName + "Join Group Chat failed :(");
			}
		}
	}
	
	// Invite avatar to join a group
	//	 Syntax similar to TestClient, e.g. avatar UUID, Group UUID, Role UUID
	public class InviteGroupPlugin : StatefulPlugin
	{
		// protected ManualResetEvent WaitForSessionStart = new ManualResetEvent(false);
		private UUID session;
		
		public InviteGroupPlugin()
		{
			MethodName = "group_invite"; // parameters are key (Group UUID) and message
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}
		
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID avatar = UUID.Zero;
			UUID group = UUID.Zero;
			UUID role = UUID.Zero;
			List<UUID> roles = new List<UUID>();
			
			try
			{
				bool check = false;
				if ( Parameters.ContainsKey("avatar") )
				{
					check = UUID.TryParse(Parameters["avatar"].ToString().Replace("_"," "), out avatar);
				}
				else
				{
					return "<error>arguments: no avatar key</error>";
				}
				if ( check ) 
				{
					if ( Parameters.ContainsKey("group") )
					{
						check = UUID.TryParse(Parameters["group"].ToString().Replace("_"," "), out group);
					}
					else
					{
						return "<error>arguments: no group key</error>";
					}					
					
					// to-do: avatars can be invited to multiple roles.
					if ( Parameters.ContainsKey("role") )
					{
						if (! UUID.TryParse(Parameters["role"].ToString().Replace("_"," "), out role) )
						{
							// just a warning, role is optional
							DebugUtilities.WriteDebug(session + " " + MethodName + " no role found, but that's ok");
						}
						roles.Add(role);
					}
					else
					{
						roles.Add(UUID.Zero); // no roles to add
					}
				}
				else
				{
					return "<error>parsekey</error>";
				}
				
				
				
				DebugUtilities.WriteDebug(session + " " + MethodName + " Group UUID: " + group + " Avatar UUID to join group: " + avatar + " Role UUID for avatar to join: " + roles.ToString() + " (NULL_KEY is fine)");

				b.Client.Groups.Invite(group, roles, avatar);
					
				return "<invitation>invited " + avatar + " to " + group + "</invitation>\n";
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>loads of errors</error>";
			}
			
		}
	}
	
	// Send group notice
	//	No working example to copy from!
	//  Requires group UUID, subject, message, attachment UUID
	
	public class SendGroupNoticePlugin : StatefulPlugin
	{
		//protected ManualResetEvent WaitForSessionStart = new ManualResetEvent(false);
		private UUID session;
		
		public SendGroupNoticePlugin()
		{
			MethodName = "group_notice"; // parameters are key (Group UUID) and message
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}
		
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{	
			string message;
			string subject;
			UUID groupUUID = UUID.Zero;
			UUID attachmentUUID = UUID.Zero;
			GroupNotice notice;
		
			try
			{
				if ( Parameters.ContainsKey("subject") )
				{
					subject = Parameters["subject"].ToString().Replace("%20"," ").Replace("+"," ");
				}
				else
				{
					return "<error>No notice subject</error>";
				}
					
				if ( Parameters.ContainsKey("message") )
				{
					message = Parameters["message"].ToString().Replace("%20"," ").Replace("+"," ");
				}
				else
				{
					return "<error>No notice message</error>";
				}				
			
				if ( Parameters.ContainsKey("group") )
				{
					if (! UUID.TryParse(Parameters["group"].ToString().Replace("_"," "), out groupUUID) )
					{
						return "<error>parsekey group</error>";
					}				
				}
				else
				{
					return "<error>arguments: no group key</error>";
				}
				
				if ( Parameters.ContainsKey("attachment") )
				{
					if (!  UUID.TryParse(Parameters["attachment"].ToString().Replace("_"," "), out attachmentUUID) )
					{
						return "<error>parsekey attachment</error>";
					}					
				}
				else
				{
					// just a warning, attachment can be empty
					DebugUtilities.WriteWarning(session + " " + MethodName + " Notice has no attachment (no problem)");
				}
				
				DebugUtilities.WriteDebug(session + " " + MethodName + " Attempting to create a notice");
				
				notice = new GroupNotice();
				
				notice.Subject = subject;
				notice.Message = message;
				notice.AttachmentID = attachmentUUID;
				notice.OwnerID = b.Client.Self.AgentID;
				
				b.Client.Groups.SendGroupNotice(groupUUID, notice);
				
				DebugUtilities.WriteDebug(session + " " + MethodName + " Sent Notice to group: " + groupUUID.ToString() + " subject: " + subject + " message: " + message + " Optional attachment: " + attachmentUUID.ToString());
					
				return "<notice>sent</notice>\n";
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>loads of errors</error>";
			}		
		}
	}
}
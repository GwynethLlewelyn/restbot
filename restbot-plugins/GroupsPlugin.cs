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

	Further changes by Gwyneth Llewelyn
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System.Net;
using System.Threading;

/// <summary>
/// group functions; based on TestClient.exe code
/// </summary>
namespace RESTBot
{
	/// <summary>
	/// Set active group, using group UUID
	/// </summary>
	public class ActivateGroupKeyPlugin : StatefulPlugin
	{
		protected ManualResetEvent GroupsEvent = new ManualResetEvent(false);

		private UUID session = UUID.Zero;	// no nulls here! (gwyneth 20220127)
		private string? activeGroup;	// possibly null if this avatar does not belong to any group

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public ActivateGroupKeyPlugin()
		{
			MethodName = "group_key_activate";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the group UUID</param>
		/// <returns>XML with information on the activated group key if successful,
		/// XML error otherwise</returns>
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID groupUUID;
			DebugUtilities.WriteDebug("TR - Entering group key parser");
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key"))
				{
					DebugUtilities.WriteDebug("TR - Attempting to parse from POST");
					check = UUID.TryParse(Parameters["key"].ToString().Replace("_"," "), out groupUUID);
					DebugUtilities.WriteDebug("TR - Succesfully parsed POST");
				}
				else
				{
					return "<error>invalid arguments</error>\n";
				}
				if (check)
				{
					DebugUtilities.WriteDebug("TR - Activating group");
					string? response = activateGroup(b, groupUUID);
					DebugUtilities.WriteDebug("TR - Complete");
					if (response != null)
						return "<active>" + response.Trim() + "</active>\n";
					else
						return "<error>group could not be activated</error>\n";
				}
				else
				{
					return "<error>parsekey</error>\n";
				}
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>\n";
			}
		}

		/// <summary>
		/// Internal function that wil set the bot's group to a certain group UUID.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the UUID for a group.</param>
		/// <returns>String with the group name, or null if group not found</returns>
		private string? activateGroup(RestBot b, UUID groupUUID)
		{
			DebugUtilities.WriteInfo(session.ToString() + " " + MethodName + " Activating group " + groupUUID.ToString());
			EventHandler<PacketReceivedEventArgs> pcallback = AgentDataUpdateHandler;
			b.Client.Network.RegisterCallback(PacketType.AgentDataUpdate, pcallback);
			b.Client.Groups.ActivateGroup(groupUUID);

			if (!GroupsEvent.WaitOne(15000, true))
			{
				DebugUtilities.WriteWarning(session + " " + MethodName + " timed out on setting active group");
			}

			// ignore everything and just reset the event
			b.Client.Network.UnregisterCallback(PacketType.AgentDataUpdate, pcallback);
			GroupsEvent.Reset();

			if (String.IsNullOrEmpty(activeGroup))
				  	DebugUtilities.WriteWarning(session + " " + MethodName + " Failed to activate the group " + groupUUID);

			return activeGroup;
		}

		private void AgentDataUpdateHandler(object? sender, PacketReceivedEventArgs e)
		{
			AgentDataUpdatePacket p = (AgentDataUpdatePacket)e.Packet;
			//if (p.AgentData.AgentID == Client.Self.AgentID)
			//{
				activeGroup = Utils.BytesToString(p.AgentData.GroupName) + " (" + Utils.BytesToString(p.AgentData.GroupTitle) + ")";
				GroupsEvent.Set();
			//}
		}
	}

	/// <summary>
	/// Set active group, using group Name
	/// </summary>
	public class ActivateGroupNamePlugin : StatefulPlugin
	{
		protected ManualResetEvent GroupsEvent = new ManualResetEvent(false);
		public Dictionary<UUID, Group>? GroupsCache = null;	// should *not* be set to null!
		private UUID session = UUID.Zero;	// no nulls here (gwyneth 20220127)
		private string? activeGroup;	// may be null if avatar has no groups

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public ActivateGroupNamePlugin()
		{
			MethodName = "group_name_activate";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <remarks>It sets the UUID for the current but session, if one exists; if not, sessionid should be set
		/// to UUID.Zero</remarks>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;	// should never be null; worst-case scenario, it's UUID.Zero (gwyneth 20220127)
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the group name, or UUID</param>
		/// <returns>XML containing the group name that was activated, if successful; XML error otherwise.</returns>
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID groupUUID;
			string groupName;
			DebugUtilities.WriteDebug("TR - Entering group key parser");
			try
			{
				if (Parameters.ContainsKey("name"))
				{
					groupName = Parameters["name"].ToString().Replace("%20"," ").Replace("+"," ");
				}
				else
				{
					return "<error>arguments</error>";
				}
				DebugUtilities.WriteDebug("TR - Activating group");

				groupUUID = GroupName2UUID(b, groupName);
				if (UUID.Zero != groupUUID)
				{
					string response = activateGroup(b, groupUUID);
					DebugUtilities.WriteDebug("TR - Complete");
					if (response != null)
						return "<active>" + response.Trim() + "</active>\n";
					else
						return "<error>group could not be activated</error>\n";
				}
				else
				{
					DebugUtilities.WriteDebug("TR - Error: group " + groupName + " doesn't exist");
					return "<error>group name '" + groupName + "' doesn't exist.</error>";
				}
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>";
			}
		}

		/// <summary>
		/// Clears the group cache for this 'bot, and reloads it with all
		/// the current groups that this 'bot is in.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <returns>void</returns>
		public void ReloadGroupsCache(RestBot b)
		{
			b.Client.Groups.CurrentGroups += Groups_CurrentGroups;
			b.Client.Groups.RequestCurrentGroups();
			GroupsEvent.WaitOne(10000, false);
			b.Client.Groups.CurrentGroups -= Groups_CurrentGroups;
			GroupsEvent.Reset();
		}

		/// <summary>
		/// Notification event with the list of groups belonging to an avatar
		/// </summary>
		/// <param name="sender">Event owner</param>
		/// <param name="e">Arguments for event</param>
		/// <returns>void</returns>
		void Groups_CurrentGroups(object? sender, CurrentGroupsEventArgs e)
		{
			if (null == GroupsCache)
				GroupsCache = e.Groups;
			else
				lock (GroupsCache) { GroupsCache = e.Groups; }
			GroupsEvent.Set();
		}

		/// <summary>
		/// Returns the UUID of a group, given its name
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="groupName">Group name to search for</param>
		/// <returns>UUID for the group name, if it exists; UUID.Zero otherwise</returns>
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

		/// <summary>
		/// Activates a group for a 'bot, given the group key.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="groupUUID">UUID of the group to activate</param>
		/// <returns>Activated group name, or empty if activation failed</returns>
		private string activateGroup(RestBot b, UUID groupUUID)
		{
			DebugUtilities.WriteInfo(session.ToString() + " " + MethodName + " Activating group " + groupUUID.ToString());
			EventHandler<PacketReceivedEventArgs> pcallback = AgentDataUpdateHandler;
			b.Client.Network.RegisterCallback(PacketType.AgentDataUpdate, pcallback);
			b.Client.Groups.ActivateGroup(groupUUID);

			if (!GroupsEvent.WaitOne(15000, true)) {
				DebugUtilities.WriteWarning(session + " " + MethodName + " timed out on setting active group");
			}

			// ignore everything and just reset the event
			b.Client.Network.UnregisterCallback(PacketType.AgentDataUpdate, pcallback);
			GroupsEvent.Reset();

			if (String.IsNullOrEmpty(activeGroup))
			{
				DebugUtilities.WriteWarning(session + " " + MethodName + " Failed to activate the group " + groupUUID);
				return "";	// maybe we ought to return something else? (gwyneth 20220127)
			}
			return activeGroup;	// guaranteed *not* to be null, nor empty! (gwyneth 20220127)
		}

		private void AgentDataUpdateHandler(object? sender, PacketReceivedEventArgs e)
		{
			AgentDataUpdatePacket p = (AgentDataUpdatePacket)e.Packet;
			//if (p.AgentData.AgentID == Client.Self.AgentID)
			//{
				activeGroup = Utils.BytesToString(p.AgentData.GroupName) + " (" + Utils.BytesToString(p.AgentData.GroupTitle) + ")";
				GroupsEvent.Set();
			//}
		}
	}

	/// <summary>Send Group Instant Message (Group Chat)</summary>
	public class GroupIMPlugin : StatefulPlugin
	{
		protected ManualResetEvent WaitForSessionStart = new ManualResetEvent(false);
		private UUID session = UUID.Zero;	// no nulls here! (gwyneth 20220127)

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public GroupIMPlugin()
		{
			MethodName = "group_im"; // parameters are key (Group UUID) and message
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the group UUID and the message to send to group IM</param>
		/// <returns>XML with information on the sent group IM message, if successful;
		/// XML error otherwise</returns>
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID groupUUID;
			string message;

			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key"))
				{
					check = UUID.TryParse(Parameters["key"].ToString().Replace("_"," "), out groupUUID);
				}
				else
				{
					return "<error>arguments: no key</error>";
				}
				if (check)
				{
					if (Parameters.ContainsKey("message"))
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

				if (string.IsNullOrEmpty(response))
					return "<error>group message not sent, or answer was empty</error>\n";

				return "<message>" + response.Trim() + "</message>\n";
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>loads of errors</error>";
			}
		}

		/// <summary>
		/// Internal method to send a message to a group the 'bot is in.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="groupUUID">Group UUID</param>
		/// <param name="message">Message to send to the group IM</param>
		/// <returns>String with "message sent", if successful; "timeout" otherwise</returns>
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

		void Self_GroupChatJoined(object? sender, GroupChatJoinedEventArgs e)
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

	/// <summary>
	/// Invite avatar to join a group
	///	 Syntax similar to TestClient, e.g. avatar UUID, Group UUID, Role UUID
	/// </summary>
	public class InviteGroupPlugin : StatefulPlugin
	{
		// protected ManualResetEvent WaitForSessionStart = new ManualResetEvent(false);
		private UUID session = UUID.Zero;	// no nulls here! (gwyneth 20220127)

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public InviteGroupPlugin()
		{
			MethodName = "group_invite"; // parameters are key (Group UUID) and message
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the UUID of the avatar to invite, the UUID of
		/// the group to invite, and the role UUID to invite the avatar in to</param>
		/// <returns>XML with information about a successful invite to a group/role; XML error otherwise</returns>
		/// <remarks><para>Currently, avatars can only be invited to a singke role at the time.</para>
		/// <para>To-do: work on allowing invitations to multiple roles simultaenously!</para></remarks>
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID avatar = UUID.Zero;
			UUID group = UUID.Zero;
			UUID role = UUID.Zero;
			List<UUID> roles = new List<UUID>();

			try
			{
				bool check = false;
				if (Parameters.ContainsKey("avatar"))
				{
					check = UUID.TryParse(Parameters["avatar"].ToString().Replace("_"," "), out avatar);
				}
				else
				{
					return "<error>arguments: no avatar key</error>";
				}
				if (check)
				{
					if (Parameters.ContainsKey("group"))
					{
						check = UUID.TryParse(Parameters["group"].ToString().Replace("_"," "), out group);
					}
					else
					{
						return "<error>arguments: no group key</error>";
					}

					// to-do: avatars can be invited to multiple roles.
					if (Parameters.ContainsKey("role"))
					{
						if (!UUID.TryParse(Parameters["role"].ToString().Replace("_"," "), out role))
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
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>loads of errors</error>";
			}

		}
	}

	/// <summary>
	/// Send group notice
	///  Requires group UUID, subject, message, attachment item UUID (not asset UUID)
	/// </summary>
	public class SendGroupNoticePlugin : StatefulPlugin
	{
		//protected ManualResetEvent WaitForSessionStart = new ManualResetEvent(false);
		private UUID session = UUID.Zero;	// no nulls here! (gwyneth 20220127)

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public SendGroupNoticePlugin()
		{
			MethodName = "group_notice"; // parameters are key (Group UUID) and message
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the message </param>
		/// <returns></returns>
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			string message;
			string subject;
			UUID groupUUID = UUID.Zero;
			UUID attachmentUUID = UUID.Zero;
			GroupNotice notice;

			try
			{
				if (Parameters.ContainsKey("subject"))
				{
					subject = Parameters["subject"].ToString().Replace("%20"," ").Replace("+"," ");
				}
				else
				{
					return "<error>No notice subject</error>";
				}

				if (Parameters.ContainsKey("message"))
				{
					message = Parameters["message"].ToString().Replace("%20"," ").Replace("+"," ");
				}
				else
				{
					return "<error>No notice message</error>";
				}

				if (Parameters.ContainsKey("group"))
				{
					if (!UUID.TryParse(Parameters["group"].ToString().Replace("_"," "), out groupUUID))
					{
						return "<error>parsekey group</error>";
					}
				}
				else
				{
					return "<error>arguments: no group key</error>";
				}

				if (Parameters.ContainsKey("attachment"))
				{
					if (!UUID.TryParse(Parameters["attachment"].ToString().Replace("_"," "), out attachmentUUID))
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

				/* This doesn't work as it should!
				if (!b.Client.Inventory.Store.Contains(attachmentUUID))
				{
					DebugUtilities.WriteWarning(session + " " + MethodName + " Item UUID " + attachmentUUID.ToString() + " not found on inventory (are you using an Asset UUID by mistake?)");
					attachmentUUID = UUID.Zero;
				}
				*/

				notice = new GroupNotice();

				notice.Subject = subject;
				notice.Message = message;
				notice.AttachmentID = attachmentUUID; // this is the inventory UUID, not the asset UUID
				notice.OwnerID = b.Client.Self.AgentID;

				b.Client.Groups.SendGroupNotice(groupUUID, notice);

				DebugUtilities.WriteDebug(session + " " + MethodName + " Sent Notice from avatar " + notice.OwnerID.ToString() + " to group: " + groupUUID.ToString() + " subject: '" + notice.Subject.ToString() + "' message: '" + notice.Message.ToString() + "' Optional attachment: " + notice.AttachmentID.ToString() + " Serialisation: " + Utils.BytesToString(notice.SerializeAttachment()));

				return "<notice>sent</notice>\n";
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>loads of errors</error>";
			}
		}
	}
}

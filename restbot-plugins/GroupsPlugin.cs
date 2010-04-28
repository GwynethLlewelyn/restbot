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
	public class ActivateGroupPlugin : StatefulPlugin
	{
		protected ManualResetEvent GroupsEvent = new ManualResetEvent(false);
		// protected Dictionary<UUID, AutoResetEvent> ActivateGroupEvents = new Dictionary<UUID, AutoResetEvent>();
		// protected Dictionary<UUID, String> groupThingy = new Dictionary<UUID, String>();
		
		private UUID session;
		
		public ActivateGroupPlugin()
		{
			MethodName = "group_UUID_activate";
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
			
			b.Client.Groups.ActivateGroup(groupUUID);
			
			string response = "false";
			
			if ( ! GroupsEvent.WaitOne(15000, true) ) {
				DebugUtilities.WriteWarning(session + " " + MethodName + " timed out on setting active group");
			}
			else
			{
				response = "true";
			}

			// ignore everything and just reset the event
			GroupsEvent.Reset();			

			return response;
		}
		
		/* 
		// obsolete syntax changed	 
		private void Avatars_OnAvatarNames(object sender, UUIDNameReplyEventArgs e)
		{
			DebugUtilities.WriteInfo(session.ToString() + " Processing " + e.Names.Count.ToString() + " AvatarNames replies");
			foreach (KeyValuePair<UUID, string> kvp in e.Names) {
				if (!avatarNames.ContainsKey(kvp.Key) || avatarNames[kvp.Key] == null) {
					DebugUtilities.WriteInfo(session.ToString() + " Reply Name: " + kvp.Value + " Key : " + kvp.Key.ToString());
					lock (avatarNames) {
						// avatarNames[kvp.Key] = new Avatar(); // why all this trouble?
						// FIXME: Change this to .name when we move inside libsecondlife
						// avatarNames[kvp.Key].Name = kvp.Value; // protected
						avatarNames[kvp.Key] = kvp.Value;
					}
					if (NameLookupEvents.ContainsKey(kvp.Key)) {
						NameLookupEvents[kvp.Key].Set();
					}
				}
			}
		}
	}
	*/
	
	// more commands to follow
	}
}
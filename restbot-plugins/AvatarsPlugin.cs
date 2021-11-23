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

 COPYRIGHT: 
	 RESTBot Codebase (c) 2007-2008 PLEIADES CONSULTING, INC
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;
using LibreMetaverse; // instead of OpenMetaverse
using OpenMetaverse.Packets;
using System.Net;
using System.Threading;

// avatar info functions
namespace RESTBot
{
	public class AvatarNameLookupPlugin : StatefulPlugin
	{
	
		protected Dictionary<UUID, AutoResetEvent> NameLookupEvents = new Dictionary<UUID, AutoResetEvent>();
		protected Dictionary<UUID, String> avatarNames = new Dictionary<UUID, String>();
		
		private UUID session;
		
		public AvatarNameLookupPlugin()
		{
			MethodName = "avatar_name";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
			// new syntax
			// bot.Client.Avatars.OnAvatarNames += new AvatarManager.AvatarNamesCallback(Avatars_OnAvatarNames); // obsolete
			bot.Client.Avatars.UUIDNameReply += Avatars_OnAvatarNames;
		}
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID agentKey;
			DebugUtilities.WriteDebug("TR - Entering avatarname parser");
			try
			{
				bool check = false;
				if ( Parameters.ContainsKey("key") ) {
					DebugUtilities.WriteDebug("TR - Attempting to parse from POST");
					check = UUID.TryParse(Parameters["key"].ToString().Replace("_"," "), out agentKey);
					DebugUtilities.WriteDebug("TR - Succesfully parsed POST");
				} else {
					return "<error>arguments</error>";
				}
				if ( check ) {
					DebugUtilities.WriteDebug("TR - Parsing name");
					string response = getName(b, agentKey);
					DebugUtilities.WriteDebug("TR - Parsed name");
					DebugUtilities.WriteDebug("TR - Complete");
					return "<name>" + response.Trim() + "</name>\n";
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
		
		private string getName(RestBot b, UUID id)
		{
			DebugUtilities.WriteInfo(session.ToString() + " " + MethodName + " Looking up name for " + id.ToString());
			lock (NameLookupEvents) {
				NameLookupEvents.Add(id, new AutoResetEvent(false));
			}
			
			b.Client.Avatars.RequestAvatarName(id);
			
			if ( ! NameLookupEvents[id].WaitOne(15000, true) ) {
				DebugUtilities.WriteWarning(session + " " + MethodName + " timed out on avatar name lookup");
			}
			lock (NameLookupEvents) {
				NameLookupEvents.Remove(id);
			}
			string response = null;
			if ( avatarNames.ContainsKey(id) ) {
				response = avatarNames[id]; // .Name removed
				lock ( avatarNames ) {
					avatarNames.Remove(id);
				}
			} else {
				response = String.Empty;
			}
			return response;
		}
		
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
	
	
	public class AvatarKeyLookupPlugin : StatefulPlugin
	{
	
		protected Dictionary<String, AutoResetEvent> KeyLookupEvents = new Dictionary<String, AutoResetEvent>();
		protected Dictionary<String, UUID> avatarKeys = new Dictionary<String, UUID>();
		
		private UUID session;
		
		public AvatarKeyLookupPlugin()
		{
			MethodName = "avatar_key";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
			// bot.Client.Network.RegisterCallback(PacketType.DirPeopleReply, new NetworkManager.PacketCallback(Avatars_OnDirPeopleReply)); // obsolete, now uses DirectoryManager
			bot.Client.Directory.DirPeopleReply += Avatars_OnDirPeopleReply;
		}
		public override string Process(RestBot b, Dictionary<string, string> Paramaters)
		{
			string avname = null;
			if ( Paramaters.ContainsKey("name") ) {
				avname = Paramaters["name"].ToString().Replace("%20"," ").Replace("+"," ");
			} else {
				return "<error>arguments</error>";
			}
			if ( avname != null ) {
				string response = getKey(b,avname).ToString();
				return "<key>" + response + "</key>\n";
			} else {
				return "<error>nokey</error>";
			} 
			
		}
		
		public UUID getKey(RestBot b, String name)
		{
			DebugUtilities.WriteInfo(session + " " + MethodName + " Looking up key for " + name);
			name = name.ToLower();
			DebugUtilities.WriteDebug("Looking up: " + name);
			DebugUtilities.WriteDebug("Key not in cache, requesting directory lookup");
			lock ( KeyLookupEvents) {
				KeyLookupEvents.Add(name, new AutoResetEvent(false));
			}
			DebugUtilities.WriteDebug("Lookup Event added, KeyLookupEvents now has a total of " + KeyLookupEvents.Count.ToString() + " entries");
			DirFindQueryPacket find = new DirFindQueryPacket();
			find.AgentData.AgentID = b.Client.Self.AgentID;	// was Network and not Self
			find.AgentData.SessionID = b.Client.Self.SessionID;
			find.QueryData.QueryFlags = 1;
			//find.QueryData.QueryText = Helpers.StringToField(name);
			find.QueryData.QueryText = Utils.StringToBytes(name);
			find.QueryData.QueryID = new UUID("00000000000000000000000000000001");
			find.QueryData.QueryStart = 0;
			
			b.Client.Network.SendPacket((Packet) find);
			DebugUtilities.WriteDebug("Packet sent - KLE has " + KeyLookupEvents.Count.ToString() + " entries.. now waiting");
			KeyLookupEvents[name].WaitOne(15000,true);
			DebugUtilities.WriteDebug("Waiting done!");
			lock (KeyLookupEvents) {
				KeyLookupEvents.Remove(name);
			}
			DebugUtilities.WriteDebug("Done with KLE, now has " + KeyLookupEvents.Count.ToString() + " entries");
			UUID response = new UUID();
			if ( avatarKeys.ContainsKey(name) ) {
				response = avatarKeys[name];
				lock ( avatarKeys ) {
					avatarKeys.Remove(name);
				}
			}
			return response;
		}
		
		/*
		// obsoleted packet call
		public void Avatars_OnDirPeopleReply(Packet packet, Simulator simulator)
		{
			DirPeopleReplyPacket reply = (DirPeopleReplyPacket)packet;
			DebugUtilities.WriteDebug("Got DirPeopleReply!");
			if (reply.QueryReplies.Length < 1) {
				DebugUtilities.WriteWarning(session + " " + MethodName + " Error - empty people directory reply");
			} else {
				int replyCount = reply.QueryReplies.Length;
				DebugUtilities.WriteInfo(session + " " + MethodName + " Proccesing " + replyCount.ToString() + " DirPeople replies");
				for ( int i = 0 ; i <  replyCount ; i++ ) {
					string avatarName = Utils.BytesToString(reply.QueryReplies[i].FirstName) + " " + Utils.BytesToString(reply.QueryReplies[i].LastName);
					UUID avatarKey = reply.QueryReplies[i].AgentID;
					DebugUtilities.WriteDebug(session + " " + MethodName + " Reply " + (i + 1).ToString() + " of " + replyCount.ToString() + " Key : " + avatarKey.ToString() + " Name : " + avatarName);
					
					if ( !avatarKeys.ContainsKey(avatarName) ) // || avatarKeys[avatarName] == null ) { // apparently dictionary entries cannot be null
						lock ( avatarKeys ) {
							avatarKeys[avatarName.ToLower()] = avatarKey;
						}
					}

					lock(KeyLookupEvents)
					{
						 if ( KeyLookupEvents.ContainsKey(avatarName.ToLower())) {
								 KeyLookupEvents[avatarName.ToLower()].Set();
							DebugUtilities.WriteDebug(avatarName.ToLower() + " KLE set!");
						 }
					}
				}
			} 
		} */
		
		// using new Directory functionality
		public void Avatars_OnDirPeopleReply(object sender, DirPeopleReplyEventArgs e)
		{
			if (e.MatchedPeople.Count < 1)
			{
				DebugUtilities.WriteWarning(session + " " + MethodName + " Error - empty people directory reply");
			}
			else
			{
				int replyCount = e.MatchedPeople.Count;
				
				DebugUtilities.WriteInfo(session + " " + MethodName + " Proccesing " + replyCount.ToString() + " DirPeople replies");
				for ( int i = 0 ; i <  replyCount ; i++ ) 
				{
					string avatarName = e.MatchedPeople[i].FirstName + " " + e.MatchedPeople[i].LastName;
					UUID avatarKey = e.MatchedPeople[i].AgentID;
					DebugUtilities.WriteDebug(session + " " + MethodName + " Reply " + (i + 1).ToString() + " of " + replyCount.ToString() + " Key : " + avatarKey.ToString() + " Name : " + avatarName);
					
					if ( !avatarKeys.ContainsKey(avatarName) ) { /* || avatarKeys[avatarName] == null )	 // apparently dictionary entries cannot be null */
						lock ( avatarKeys )
						{
							avatarKeys[avatarName.ToLower()] = avatarKey;
						}
					}

					lock(KeyLookupEvents)
					{
						 if ( KeyLookupEvents.ContainsKey(avatarName.ToLower())) 
						 {
								 KeyLookupEvents[avatarName.ToLower()].Set();
							DebugUtilities.WriteDebug(avatarName.ToLower() + " KLE set!");
						 }
					}
				}
			}
		}
	}
	public class AvatarOnlineLookupPlugin : StatefulPlugin
	{
	
		protected Dictionary<UUID, AutoResetEvent> OnlineLookupEvents = new Dictionary<UUID, AutoResetEvent>();
		protected Dictionary<UUID, bool> avatarOnline = new Dictionary<UUID, bool>();

		
		private UUID session;
		
		public AvatarOnlineLookupPlugin()
		{
			MethodName = "avatar_online";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
			// bot.Client.Network.RegisterCallback(PacketType.AvatarPropertiesReply, new NetworkManager.PacketCallback(AvatarPropertiesReply)); // obsolete
			bot.Client.Avatars.AvatarPropertiesReply += AvatarPropertiesReply;
		}
		public override string Process(RestBot b, Dictionary<string, string> Paramaters)
		{
			UUID agentKey;
			try
			{
				bool check = false;
				if (Paramaters.ContainsKey("key")) {
					check = UUID.TryParse(Paramaters["key"].ToString().Replace("_"," "), out agentKey);
				} else {
					return "<error>arguments</error>";
				}
				if ( check ) {
					bool response = getOnline(b, agentKey);
					return "<online>" + response.ToString() + "</online>\n";
				} else {
					return "<error>unknown</error>";
				}
				
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>";
			}
			
		}
		
		public bool getOnline(RestBot b, UUID key)
		{
			DebugUtilities.WriteInfo(session + " " + MethodName + " Looking up online status for " + key.ToString());
			
			lock ( OnlineLookupEvents ) {
				OnlineLookupEvents.Add(key, new AutoResetEvent(false) );
			}
			
			// obsolete
			/*
			AvatarPropertiesRequestPacket p = new AvatarPropertiesRequestPacket();
			p.AgentData.AgentID = b.Client.Network.AgentID;
			p.AgentData.SessionID = b.Client.Network.SessionID;
			p.AgentData.AvatarID = key;
			
			b.Client.Network.SendPacket( (Packet) p);
			*/
			b.Client.Avatars.RequestAvatarProperties(key);
			
			OnlineLookupEvents[key].WaitOne(15000, true);
			
			lock ( OnlineLookupEvents ) {
				OnlineLookupEvents.Remove(key);
			}
			bool response = avatarOnline[key];
			lock ( avatarOnline ) {
				avatarOnline.Remove(key);
			}
			return response;
		}

		// updated for new callbacks
		public void AvatarPropertiesReply(object sender, AvatarPropertiesReplyEventArgs e)
		{
			/*
			AvatarPropertiesReplyPacket reply = (AvatarPropertiesReplyPacket)packet;
			bool status = false;
			if ( (reply.PropertiesData.Flags & 1 << 4 ) != 0 ) {
				status = true;
			} else {
				status = false;
			}
			*/
			
			Avatar.AvatarProperties Properties = new Avatar.AvatarProperties();
			Properties = e.Properties;
			
			DebugUtilities.WriteInfo(session + " " + MethodName + " Processing AvatarPropertiesReply for " + e.AvatarID.ToString() + " is " + Properties.Online.ToString());
			lock ( avatarOnline ) {
				avatarOnline[e.AvatarID] = Properties.Online;
			}
			if ( OnlineLookupEvents.ContainsKey(e.AvatarID) ) {
				OnlineLookupEvents[e.AvatarID].Set();
			}
		}
	}
	
	
	public class AvatarProfileLookupPlugin : StatefulPlugin
	{
		private UUID session;
		protected Dictionary<UUID, AutoResetEvent> ProfileLookupEvents = new Dictionary<UUID, AutoResetEvent>();
		protected Dictionary<UUID, Avatar.AvatarProperties> avatarProfile = new Dictionary<UUID, Avatar.AvatarProperties>();
		
		public AvatarProfileLookupPlugin()
		{
			MethodName = "avatar_profile";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
			// bot.Client.Avatars.OnAvatarProperties += new AvatarManager.AvatarPropertiesCallback(Avatars_OnAvatarProperties); // obsolete
			bot.Client.Avatars.AvatarPropertiesReply += Avatars_OnAvatarProperties;
		}
		public override string Process(RestBot b, Dictionary<string, string> Paramaters)
		{
			UUID agentKey;
			try
			{
				bool check = false;
				if (Paramaters.ContainsKey("key")) {
					check = UUID.TryParse(Paramaters["key"].ToString().Replace("_"," "), out agentKey);
				} else {
					return "<error>arguments</error>";
				}
				if ( check ) {
					string response = getProfile(b, agentKey);
					if ( response == null ) {
						return "<error>not found</error>";
					} else {
						return response;
					}
				} else {
					return "<error>unknown</error>";
				}
				
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>";
			}
			
		}

		public string getProfile(RestBot b, UUID key)
		{
			DebugUtilities.WriteInfo(session + " " + MethodName + " Looking up profile for " + key.ToString());
			
			lock ( ProfileLookupEvents ) {
				ProfileLookupEvents.Add(key, new AutoResetEvent(false) );
			}
			b.Client.Avatars.RequestAvatarProperties(key);
			
			ProfileLookupEvents[key].WaitOne(15000, true);
			
			lock ( ProfileLookupEvents ) {
				ProfileLookupEvents.Remove(key);
			}
			if ( avatarProfile.ContainsKey(key) ) {
				Avatar.AvatarProperties p = avatarProfile[key];
				string response = "<profile>\n";
				response += "<publish>" + p.AllowPublish.ToString() + "</publish>\n";
				response += "<firstlife>\n";
				response += "<text>" + p.FirstLifeText.Replace(">", "%3C").Replace("<", "%3E") + "</text>\n";
				response += "<image>" + p.FirstLifeImage.ToString() + "</image>\n";
				response += "</firstlife>\n";
				response += "<partner>" + p.Partner.ToString() + "</partner>\n";
				response += "<born>" + p.BornOn + "</born>\n";
				response += "<about>" + p.AboutText.Replace(">", "%3C").Replace("<", "%3E") + "</about>\n";
				response += "<charter>" + p.CharterMember + "</charter>\n";
				response += "<profileimage>" + p.ProfileImage.ToString() + "</profileimage>\n";
				response += "<mature>" + p.MaturePublish.ToString() + "</mature>\n";
				response += "<identified>" + p.Identified.ToString() + "</identified>\n";
				response += "<transacted>" + p.Transacted.ToString() + "</transacted>\n";
				response += "<url>" + p.ProfileURL + "</url>\n";
				response += "</profile>\n";
				lock ( avatarProfile ) {
					avatarProfile.Remove(key);
				}
				return response;
			} else {
				return null;
			}
		}
		
		// changed to deal with new replies
		public void Avatars_OnAvatarProperties(object sender, AvatarPropertiesReplyEventArgs e)
		{
			lock (avatarProfile) {
				avatarProfile[e.AvatarID] = e.Properties;
			}
			if ( ProfileLookupEvents.ContainsKey(e.AvatarID) ) {
				ProfileLookupEvents[e.AvatarID].Set();
			}
			
		}
	}
	
	public class AvatarGroupsLookupPlugin : StatefulPlugin
	{
		private UUID session;
		protected Dictionary<UUID, AutoResetEvent> GroupsLookupEvents = new Dictionary<UUID, AutoResetEvent>();
		// protected Dictionary<UUID, AvatarGroupsReplyPacket.GroupDataBlock[] > avatarGroups = new Dictionary<UUID, AvatarGroupsReplyPacket.GroupDataBlock[]>(); // too cumbersome and now obsolete
		protected Dictionary<UUID, List<AvatarGroup>> avatarGroups = new Dictionary<UUID, List<AvatarGroup>>();
				
		public AvatarGroupsLookupPlugin()
		{
			MethodName = "avatar_groups";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
			// obsolete
			// bot.Client.Avatars.OnAvatarGroups += new AvatarManager.AvatarGroupsCallback(Avatar_OnAvatarGroups);
			bot.Client.Avatars.AvatarGroupsReply += Avatar_OnAvatarGroups;
		}
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID agentKey;
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key")) {
					check = UUID.TryParse(Parameters["key"].ToString().Replace("_"," "), out agentKey);
				} else {
					return "<error>arguments</error>";
				}
				if ( check ) {
					string response = getGroups(b, agentKey);
					if ( response == null ) {
						return "<error>not found</error>";
					} else {
						return response;
					}
				} else {
					return "<error>unknown</error>";
				}
				
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>";
			}
			
		}

		public string getGroups(RestBot b, UUID key)
		{
			DebugUtilities.WriteInfo(session + " " + MethodName + " Looking up groups for " + key.ToString());
			
			lock ( GroupsLookupEvents ) {
				GroupsLookupEvents.Add(key, new AutoResetEvent(false) );
			}
			b.Client.Avatars.RequestAvatarProperties(key);
			
			GroupsLookupEvents[key].WaitOne(15000, true);
			
			lock ( GroupsLookupEvents ) {
				GroupsLookupEvents.Remove(key);
			}
			
			if (null == avatarGroups)
			{
				DebugUtilities.WriteInfo(session + " " + MethodName + " Groups cache failed.");
				return null;
			}
			if (0 == avatarGroups.Count)
			{
				DebugUtilities.WriteInfo(session + " " + MethodName + " No groups");
				return null;
			}
			if ( avatarGroups.ContainsKey(key) ) {
				string response = "<groups>\n";
			
				foreach (AvatarGroup g in avatarGroups[key])
				{
					response += "<group>\n";
					response += "<name>" + g.GroupName.Replace(">", "%3C").Replace("<", "%3E") + "</name>\n";
					response += "<key>" + g.GroupID.ToString() + "</key>\n";
					response += "<title>" + g.GroupTitle.Replace(">", "%3C").Replace("<", "%3E") + "</title>\n";
					response += "<notices>" + g.AcceptNotices.ToString() + "</notices>\n";
					response += "<powers>" + g.GroupPowers.ToString() + "</powers>\n";
					response += "<insignia>" + g.GroupInsigniaID.ToString() + "</insignia>\n";
					response += "</group>\n";
				}
				response += "</groups>\n";
	
				lock ( avatarGroups ) {
					avatarGroups.Remove(key);
				}
				return response;
			} else {
				return null;
			}

		}
		
		public void Avatar_OnAvatarGroups(object sender, AvatarGroupsReplyEventArgs e)
		{
			lock (avatarGroups)
			{ 
				avatarGroups[e.AvatarID] = e.Groups; 
			}
			if ( GroupsLookupEvents.ContainsKey(e.AvatarID) ) 
			{
				GroupsLookupEvents[e.AvatarID].Set();
			}
		}

	}

    // avatar position; parameters are first, last
    public class AvatarPositionPlugin : StatefulPlugin
    {
        private UUID session;

        public AvatarPositionPlugin()
        {
            MethodName = "avatar_position";
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
                string name = "";
                bool check = true;

                if (Parameters.ContainsKey("target"))
                {
                    name = Parameters["target"].ToString().Replace("+", " ");
                }
                else check = false;

                if (!check)
                {
                    return "<error>parameters have to be target name</error>";
                }

                lock (b.Client.Network.Simulators)
                {
                    for (int i = 0; i < b.Client.Network.Simulators.Count; i++)
                    {
                        DebugUtilities.WriteDebug("Found Avatars: " + b.Client.Network.Simulators[i].ObjectsAvatars.Count);
                        Avatar target = b.Client.Network.Simulators[i].ObjectsAvatars.Find(
                            delegate(Avatar avatar)
                            {
                                DebugUtilities.WriteDebug("Found avatar: " + avatar.Name);
                                return avatar.Name == name;
                            }
                        );

                        if (target != null)
                        {                            
                            return String.Format("<goal_position>{0},{1},{2}</goal_position><curr_position>{3},{4},{5}</curr_position>",
                                target.Position.X, target.Position.Y, target.Position.Z, b.Client.Self.SimPosition.X, b.Client.Self.SimPosition.Y, b.Client.Self.SimPosition.Z);
                        }
                        else
                        {
                            DebugUtilities.WriteError("Error obtaining the avatar: " + name);
                        }
                    }
                }
                return "<error>avatar_position failed.</error>";
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return "<error>" + e.Message + "</error>";
            }
        }
    } // end avatar_position

    // avatar position; parameters are first, last
    public class MyPositionPlugin : StatefulPlugin
    {
        private UUID session;

        public MyPositionPlugin()
        {
            MethodName = "my_position";
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
                return String.Format("<position>{0},{1},{2}</error>", b.Client.Self.SimPosition.X, b.Client.Self.SimPosition.Y, b.Client.Self.SimPosition.Z);
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return "<error>" + e.Message + "</error>";
            }
        }
    } // end avatar_position
}

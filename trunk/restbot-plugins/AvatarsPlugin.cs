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
using libsecondlife.Packets;
using System.Net;
using System.Threading;

// avatar info functions
namespace RESTBot
{
    public class AvatarNameLookupPlugin : StatefulPlugin
    {
	
		protected Dictionary<LLUUID, AutoResetEvent> NameLookupEvents = new Dictionary<LLUUID, AutoResetEvent>();
		protected Dictionary<LLUUID, Avatar> avatarNames = new Dictionary<LLUUID, Avatar>();
		
		private LLUUID session;
		
        public AvatarNameLookupPlugin()
        {
            MethodName = "avatar_name";
        }
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
			bot.Client.Avatars.OnAvatarNames += new AvatarManager.AvatarNamesCallback(Avatars_OnAvatarNames);
		}
        public override string Process(RestBot b, Dictionary<string, string> Paramaters)
        {
            LLUUID agentKey;
            DebugUtilities.WriteDebug("TR - Entering avatarname parser");
            try
            {
				bool check = false;
                if ( Paramaters.ContainsKey("key") ) {
                    DebugUtilities.WriteDebug("TR - Attempting to parse from POST");
                    check = LLUUID.TryParse(Paramaters["key"].ToString().Replace("_"," "), out agentKey);
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
		
		private string getName(RestBot b, LLUUID id)
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
                response = avatarNames[id].Name;
                lock ( avatarNames ) {
                    avatarNames.Remove(id);
                }
            } else {
                response = String.Empty;
            }
			return response;
        }
        
		private void Avatars_OnAvatarNames(Dictionary<LLUUID, string> names)
        {
            DebugUtilities.WriteInfo(session.ToString() + " Proccesing " + names.Count.ToString() + " AvatarNames replies");
			foreach (KeyValuePair<LLUUID, string> kvp in names) {
				if (!avatarNames.ContainsKey(kvp.Key) || avatarNames[kvp.Key] == null) {
                    DebugUtilities.WriteInfo(session.ToString() + " Reply Name: " + kvp.Value + " Key : " + kvp.Key.ToString());
					lock (avatarNames) {
						avatarNames[kvp.Key] = new Avatar();
						// FIXME: Change this to .name when we move inside libsecondlife
						avatarNames[kvp.Key].Name = kvp.Value;
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
		protected Dictionary<String, LLUUID> avatarKeys = new Dictionary<String, LLUUID>();
		
		private LLUUID session;
		
        public AvatarKeyLookupPlugin()
        {
            MethodName = "avatar_key";
        }
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
			bot.Client.Network.RegisterCallback(PacketType.DirPeopleReply, new NetworkManager.PacketCallback(Avatars_OnDirPeopleReply));
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
		
		public LLUUID getKey(RestBot b, String name)
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
			find.AgentData.AgentID = b.Client.Network.AgentID;
            find.AgentData.SessionID = b.Client.Network.SessionID;
            find.QueryData.QueryFlags = 1;
            find.QueryData.QueryText = Helpers.StringToField(name);
            find.QueryData.QueryID = new LLUUID("00000000000000000000000000000001");
            find.QueryData.QueryStart = 0;
			
			b.Client.Network.SendPacket((Packet) find);
            DebugUtilities.WriteDebug("Packet sent - KLE has " + KeyLookupEvents.Count.ToString() + " entries.. now waiting");
			KeyLookupEvents[name].WaitOne(15000,true);
            DebugUtilities.WriteDebug("Waiting done!");
			lock (KeyLookupEvents) {
				KeyLookupEvents.Remove(name);
			}
            DebugUtilities.WriteDebug("Done with KLE, now has " + KeyLookupEvents.Count.ToString() + " entries");
            LLUUID response = new LLUUID();
            if ( avatarKeys.ContainsKey(name) ) {
                response = avatarKeys[name];
                lock ( avatarKeys ) {
                    avatarKeys.Remove(name);
                }
            }
			return response;
		}
		
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
					string avatarName = Helpers.FieldToUTF8String(reply.QueryReplies[i].FirstName) + " " + Helpers.FieldToUTF8String(reply.QueryReplies[i].LastName);
					LLUUID avatarKey = reply.QueryReplies[i].AgentID;
                    DebugUtilities.WriteDebug(session + " " + MethodName + " Reply " + (i + 1).ToString() + " of " + replyCount.ToString() + " Key : " + avatarKey.ToString() + " Name : " + avatarName);
					
                    if ( !avatarKeys.ContainsKey(avatarName) || avatarKeys[avatarName] == null ) {
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
		}
    }
    public class AvatarOnlineLookupPlugin : StatefulPlugin
    {
	
		protected Dictionary<LLUUID, AutoResetEvent> OnlineLookupEvents = new Dictionary<LLUUID, AutoResetEvent>();
		protected Dictionary<LLUUID, bool> avatarOnline = new Dictionary<LLUUID, bool>();

		
		private LLUUID session;
		
        public AvatarOnlineLookupPlugin()
        {
            MethodName = "avatar_online";
        }
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
			bot.Client.Network.RegisterCallback(PacketType.AvatarPropertiesReply, new NetworkManager.PacketCallback(AvatarPropertiesReply));
		}
        public override string Process(RestBot b, Dictionary<string, string> Paramaters)
        {
            LLUUID agentKey;
            try
            {
				bool check = false;
                if (Paramaters.ContainsKey("key")) {
                    check = LLUUID.TryParse(Paramaters["key"].ToString().Replace("_"," "), out agentKey);
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
		
		public bool getOnline(RestBot b, LLUUID key)
		{
            DebugUtilities.WriteInfo(session + " " + MethodName + " Looking up online status for " + key.ToString());
			
			lock ( OnlineLookupEvents ) {
				OnlineLookupEvents.Add(key, new AutoResetEvent(false) );
			}
			AvatarPropertiesRequestPacket p = new AvatarPropertiesRequestPacket();
			p.AgentData.AgentID = b.Client.Network.AgentID;
			p.AgentData.SessionID = b.Client.Network.SessionID;
			p.AgentData.AvatarID = key;
			
			b.Client.Network.SendPacket( (Packet) p);
			
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

		public void AvatarPropertiesReply(Packet packet, Simulator sim)
		{
			AvatarPropertiesReplyPacket reply = (AvatarPropertiesReplyPacket)packet;
			bool status = false;
			if ( (reply.PropertiesData.Flags & 1 << 4 ) != 0 ) {
				status = true;
			} else {
				status = false;
			}
			DebugUtilities.WriteInfo(session + " " + MethodName + " Processing AvatarPropertiesReply for " + reply.AgentData.AvatarID.ToString() + " is " + status.ToString());
			lock ( avatarOnline ) {
				avatarOnline[reply.AgentData.AvatarID] = status;
			}
			if ( OnlineLookupEvents.ContainsKey(reply.AgentData.AvatarID) ) {
				OnlineLookupEvents[reply.AgentData.AvatarID].Set();
			}
		}
	}
	
	
    public class AvatarProfileLookupPlugin : StatefulPlugin
	{
		private LLUUID session;
        protected Dictionary<LLUUID, AutoResetEvent> ProfileLookupEvents = new Dictionary<LLUUID, AutoResetEvent>();
        protected Dictionary<LLUUID, Avatar.AvatarProperties> avatarProfile = new Dictionary<LLUUID, Avatar.AvatarProperties>();
        
        public AvatarProfileLookupPlugin()
        {
            MethodName = "avatar_profile";
        }
        
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
			bot.Client.Avatars.OnAvatarProperties += new AvatarManager.AvatarPropertiesCallback(Avatars_OnAvatarProperties);
		}
		public override string Process(RestBot b, Dictionary<string, string> Paramaters)
		{
            LLUUID agentKey;
            try
            {
				bool check = false;
                if (Paramaters.ContainsKey("key")) {
                    check = LLUUID.TryParse(Paramaters["key"].ToString().Replace("_"," "), out agentKey);
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

        public string getProfile(RestBot b, LLUUID key)
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
		
        public void Avatars_OnAvatarProperties(LLUUID avatarID, Avatar.AvatarProperties properties)
        {
            lock (avatarProfile) {
                avatarProfile[avatarID] = properties;
            }
            if ( ProfileLookupEvents.ContainsKey(avatarID) ) {
                ProfileLookupEvents[avatarID].Set();
            }
            
        }
    }
    
    public class AvatarGroupsLookupPlugin : StatefulPlugin
	{
		private LLUUID session;
        protected Dictionary<LLUUID, AutoResetEvent> GroupsLookupEvents = new Dictionary<LLUUID, AutoResetEvent>();
        protected Dictionary<LLUUID, AvatarGroupsReplyPacket.GroupDataBlock[] > avatarGroups = new Dictionary<LLUUID, AvatarGroupsReplyPacket.GroupDataBlock[]>();
        
        public AvatarGroupsLookupPlugin()
        {
            MethodName = "avatar_groups";
        }
        
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
			bot.Client.Avatars.OnAvatarGroups += new AvatarManager.AvatarGroupsCallback(Avatar_OnAvatarGroups);
		}
		public override string Process(RestBot b, Dictionary<string, string> Paramaters)
		{
            LLUUID agentKey;
            try
            {
				bool check = false;
                if (Paramaters.ContainsKey("key")) {
                    check = LLUUID.TryParse(Paramaters["key"].ToString().Replace("_"," "), out agentKey);
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

        public string getGroups(RestBot b, LLUUID key)
		{
            DebugUtilities.WriteInfo(session + " " + MethodName + " Looking up profile for " + key.ToString());
			
			lock ( GroupsLookupEvents ) {
				GroupsLookupEvents.Add(key, new AutoResetEvent(false) );
			}
			b.Client.Avatars.RequestAvatarProperties(key);
			
			GroupsLookupEvents[key].WaitOne(15000, true);
			
			lock ( GroupsLookupEvents ) {
				GroupsLookupEvents.Remove(key);
			}
			if ( avatarGroups.ContainsKey(key) ) {
                string response = "<groups>\n";
                foreach ( AvatarGroupsReplyPacket.GroupDataBlock g in avatarGroups[key] ) {
                    response += "<group>\n";
                    response += "<name>" + Helpers.FieldToUTF8String(g.GroupName).Replace(">", "%3C").Replace("<", "%3E") + "</name>\n";
                    response += "<key>" + g.GroupID.ToString() + "</key>\n";
                    response += "<title>" + Helpers.FieldToUTF8String(g.GroupTitle).Replace(">", "%3C").Replace("<", "%3E") + "</title>\n";
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
		
        public void Avatar_OnAvatarGroups(LLUUID avatarID, AvatarGroupsReplyPacket.GroupDataBlock[] groups)
        {
            lock (avatarGroups) {
                avatarGroups[avatarID] = groups;
            }
            if ( GroupsLookupEvents.ContainsKey(avatarID) ) {
                GroupsLookupEvents[avatarID].Set();
            }
            
        }
    }

}

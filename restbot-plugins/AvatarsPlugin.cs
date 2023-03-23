/*--------------------------------------------------------------------------------
	FILE INFORMATION:
	Name: AvatarsPlugin.cs [./restbot-plugins/AvatarsPlugin.cs]
	Description: Messages related to avatar information.

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
using System.Net;
using System.Text;
using System.Threading;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace RESTBot
{
	/// <summary>
	/// Looks up an avatar name, based on its UUID (key2name).
	/// </summary>
	public class AvatarNameLookupPlugin : StatefulPlugin
	{
		protected Dictionary<UUID, AutoResetEvent>
			NameLookupEvents = new Dictionary<UUID, AutoResetEvent>();

		protected Dictionary<UUID, String>
			avatarNames = new Dictionary<UUID, String>();

		private UUID session;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public AvatarNameLookupPlugin()
		{
			MethodName = "avatar_name";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");

			// new syntax
			// bot.Client.Avatars.OnAvatarNames += new AvatarManager.AvatarNamesCallback(Avatars_OnAvatarNames); // obsolete
			bot.Client.Avatars.UUIDNameReply += Avatars_OnAvatarNames;
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the avatar key UUID to look up</param>
		/// <returns>XML-encoded avatar name, if found</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID agentKey;
			DebugUtilities.WriteDebug("Entering avatarname parser");
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key"))
				{
					DebugUtilities.WriteDebug("Attempting to parse from POST");
					check =
						UUID
							.TryParse(Parameters["key"].ToString().Replace("_", " "),
							out agentKey);
					DebugUtilities.WriteDebug("Succesfully parsed POST");
				}
				else
				{
					return "<error>arguments</error>";
				}
				if (check)
				{
					DebugUtilities.WriteDebug("Parsing name");
					string response = getName(b, agentKey);
					DebugUtilities.WriteDebug("Parsed name");
					DebugUtilities.WriteDebug("Complete");
					return $"<name>{response.Trim()}</name>";
				}
				else
				{
					return "<error>parsekey</error>";
				}
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>";
			}
		}

		/// <summary>
		/// key2name (given an avatar UUID, returns the avatar name, if it exists)
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="key">UUID of avatar to check</param>
		/// <returns>Name of the avatar if it exists; String.Empty if not</returns>
		private string getName(RestBot b, UUID id)
		{
			DebugUtilities
				.WriteInfo($"{session.ToString()} {MethodName} Looking up name for {id.ToString()}");
			lock (NameLookupEvents)
			{
				NameLookupEvents.Add(id, new AutoResetEvent(false));
			}

			b.Client.Avatars.RequestAvatarName(id);

			if (!NameLookupEvents[id].WaitOne(15000, true))
			{
				DebugUtilities
					.WriteWarning($"{session.ToString()} {MethodName} timed out on avatar name lookup");
			}
			lock (NameLookupEvents)
			{
				NameLookupEvents.Remove(id);
			}

			// C# 8+ is stricter with null assignments.
			// string? response = null;	// technically this cannot ever be null, so it doesn't make sense...
			string response = String.Empty;
			if (avatarNames.ContainsKey(id))
			{
				response = avatarNames[id]; // .Name removed
				lock (avatarNames)
				{
					avatarNames.Remove(id);
				}
			}

			/*			else
			{
				response = String.Empty;
			} */
			return response;
		}

		/// <summary>
		/// Loop through all (pending) replies for UUID/Avatar names
		/// and process them if they contain any key we're looking for.
		/// </summary>
		/// <param name="sender">parameter ignored</param>
		/// <param name="e">List of UUID/Avatar names</param>
		/// <returns>void</returns>
		/// <remarks>obsolete syntax changed</remarks>
		private void Avatars_OnAvatarNames(object? sender, UUIDNameReplyEventArgs e)
		{
			DebugUtilities.WriteDebug($"{session.ToString()} Processing {e.Names.Count.ToString()} AvatarNames replies");
			foreach (KeyValuePair<UUID, string> kvp in e.Names)
			{
				if (!avatarNames.ContainsKey(kvp.Key) || avatarNames[kvp.Key] == null)
				{
					DebugUtilities.WriteDebug($"{session.ToString()} Reply Name: {kvp.Value} Key: {kvp.Key.ToString()}");
					lock (avatarNames)
					{
						// avatarNames[kvp.Key] = new Avatar(); // why all this trouble?
						// FIXME: Change this to .name when we move inside libsecondlife
						// avatarNames[kvp.Key].Name = kvp.Value; // protected
						avatarNames[kvp.Key] = kvp.Value;
					}
					if (NameLookupEvents.ContainsKey(kvp.Key))
					{
						NameLookupEvents[kvp.Key].Set();
					}
				}
			}
		}
	}

	/// <summary>
	/// Looks up an avatar UUID, based on its name (name2key).
	/// </summary>
	public class AvatarKeyLookupPlugin : StatefulPlugin
	{
		protected Dictionary<String, AutoResetEvent>
			KeyLookupEvents = new Dictionary<String, AutoResetEvent>();

		protected Dictionary<String, UUID>
			avatarKeys = new Dictionary<String, UUID>();

		private UUID session;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public AvatarKeyLookupPlugin()
		{
			MethodName = "avatar_key";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");

			// bot.Client.Network.RegisterCallback(PacketType.DirPeopleReply, new NetworkManager.PacketCallback(Avatars_OnDirPeopleReply)); // obsolete, now uses DirectoryManager
			bot.Client.Directory.DirPeopleReply += Avatars_OnDirPeopleReply;
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the avatar name to look up</param>
		/// <returns>XML-encoded avatar key UUID, if found</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			string? avname = null; // C# is stricter about setting things to null
			if (Parameters.ContainsKey("name"))
			{
				avname =
					Parameters["name"].ToString().Replace("%20", " ").Replace("+", " ");
			}
			else
			{
				return "<error>arguments</error>";
			}
			if (avname != null)
			{
				string response = getKey(b, avname).ToString();
				return $"<key>{response}</key>";
			}
			else
			{
				return "<error>nokey</error>";
			}
		}

		/// <summary>
		/// name2key (avatar name to UUID)
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="name">Name of avatar to check</param>
		/// <returns>UUID of corresponding avatar, if it exists</returns>
		public UUID getKey(RestBot b, String name)
		{
			DebugUtilities
				.WriteInfo($"{session} {MethodName} Looking up key for {name}");
			name = name.ToLower();
			DebugUtilities.WriteDebug($"Looking up: {name}");
			DebugUtilities
				.WriteDebug("Key not in cache, requesting directory lookup");
			lock (KeyLookupEvents)
			{
				KeyLookupEvents.Add(name, new AutoResetEvent(false));
			}
			DebugUtilities
				.WriteDebug($"Lookup Event added, KeyLookupEvents now has a total of {KeyLookupEvents.Count.ToString()} entries");
			DirFindQueryPacket find = new DirFindQueryPacket();
			find.AgentData.AgentID = b.Client.Self.AgentID; // was Network and not Self
			find.AgentData.SessionID = b.Client.Self.SessionID;
			find.QueryData.QueryFlags = 1;

			//find.QueryData.QueryText = Helpers.StringToField(name);
			find.QueryData.QueryText = Utils.StringToBytes(name);
			find.QueryData.QueryID = new UUID("00000000000000000000000000000001");
			find.QueryData.QueryStart = 0;

			b.Client.Network.SendPacket((Packet) find);
			DebugUtilities
				.WriteDebug($"Packet sent - KLE has {KeyLookupEvents.Count.ToString()} entries... now waiting");
			KeyLookupEvents[name].WaitOne(15000, true);
			DebugUtilities.WriteDebug("Waiting done!");
			lock (KeyLookupEvents)
			{
				KeyLookupEvents.Remove(name);
			}
			DebugUtilities
				.WriteDebug($"Done with KLE, now has {KeyLookupEvents.Count.ToString()} entries");
			UUID response = new UUID();
			if (avatarKeys.ContainsKey(name))
			{
				response = avatarKeys[name];
				lock (avatarKeys)
				{
					avatarKeys.Remove(name);
				}
			}
			return response;
		}

		/// <summary>
		/// Overloaded getKey() function, using first name and last name as parameters
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="avatarFirstName">First name of avatar to check</param>
		/// <param name="avatarLastName">Last name of avatar to check</param>
		/// <returns>UUID of corresponding avatar, if it exists</returns>
		public UUID getKey(RestBot b, String avatarFirstName, String avatarLastName)
		{
			String avatarFullName = avatarFirstName.ToString() + " " + avatarLastName.ToString();
			return getKey(b, avatarFullName.ToLower());
		}

		/*
		// obsoleted packet call
		public void Avatars_OnDirPeopleReply(Packet packet, Simulator simulator)
		{
			DirPeopleReplyPacket reply = (DirPeopleReplyPacket)packet;
			DebugUtilities.WriteDebug("Got DirPeopleReply!");
			if (reply.QueryReplies.Length < 1) {
				DebugUtilities.WriteWarning(session + " {MethodName + " Error - empty people directory reply");
			} else {
				int replyCount = reply.QueryReplies.Length;
				DebugUtilities.WriteInfo(session + " {MethodName + " Proccesing {replyCount.ToString() + " DirPeople replies");
				for ( int i = 0 ; i <  replyCount ; i++ ) {
					string avatarName = Utils.BytesToString(reply.QueryReplies[i].FirstName) + " {Utils.BytesToString(reply.QueryReplies[i].LastName);
					UUID avatarKey = reply.QueryReplies[i].AgentID;
					DebugUtilities.WriteDebug(session + " {MethodName + " Reply {(i + 1).ToString() + " of {replyCount.ToString() + " Key : {avatarKey.ToString() + " Name : {avatarName);

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
		/// <summary>
		/// Loop through all (pending) replies for UUID/Avatar names
		/// and process them if they contain any key we're looking for.
		/// </summary>
		/// <param name="sender">parameter ignored</param>
		/// <param name="e">List of UUID/Avatar names</param>
		/// <returns>void</returns>
		/// <remarks>using new Directory functionality</remarks>
		public void Avatars_OnDirPeopleReply(
			object? sender,
			DirPeopleReplyEventArgs e
		)
		{
			if (e.MatchedPeople.Count < 1)
			{
				DebugUtilities
					.WriteWarning($"{session} {MethodName} Error - empty people directory reply");
			}
			else
			{
				int replyCount = e.MatchedPeople.Count;

				DebugUtilities
					.WriteInfo($"{session} {MethodName} Processing {replyCount.ToString()} DirPeople replies");
				for (int i = 0; i < replyCount; i++)
				{
					string avatarName =	e.MatchedPeople[i].FirstName.ToLower() + " " + e.MatchedPeople[i].LastName.ToLower();
					UUID avatarKey 		= e.MatchedPeople[i].AgentID;
					DebugUtilities
						.WriteDebug($"{session} {MethodName} Reply {(i + 1).ToString()} of {replyCount.ToString()} Key: {avatarKey.ToString()} Name: {avatarName}");

					if (!avatarKeys.ContainsKey(avatarName))
					{
						/* || avatarKeys[avatarName] == null )	 // apparently dictionary entries cannot be null */
						lock (avatarKeys)
						{
						//	avatarKeys[avatarName.ToLower()] = avatarKey;
							avatarKeys[avatarName] = avatarKey;
						}
					}

					lock (KeyLookupEvents)
					{
/* 						if (KeyLookupEvents.ContainsKey(avatarName.ToLower()))
						{
							KeyLookupEvents[avatarName.ToLower()].Set();
							DebugUtilities.WriteDebug($"{avatarName.ToLower()} KLE set!");
						} */
						if (KeyLookupEvents.ContainsKey(avatarName))
						{
							KeyLookupEvents[avatarName].Set();
							DebugUtilities.WriteDebug($"{avatarName} KLE set!");
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Checks if an avatar is online.
	/// </summary>
	public class AvatarOnlineLookupPlugin : StatefulPlugin
	{
		protected Dictionary<UUID, AutoResetEvent>
			OnlineLookupEvents = new Dictionary<UUID, AutoResetEvent>();

		protected Dictionary<UUID, bool>
			avatarOnline = new Dictionary<UUID, bool>();

		private UUID session;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public AvatarOnlineLookupPlugin()
		{
			MethodName = "avatar_online";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");

			// bot.Client.Network.RegisterCallback(PacketType.AvatarPropertiesReply, new NetworkManager.PacketCallback(AvatarPropertiesReply)); // obsolete
			bot.Client.Avatars.AvatarPropertiesReply += AvatarPropertiesReply;
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the avatar key UUID to check for online status/param>
		/// <returns>XML-encoded online status report (or unknown if request failed)</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID agentKey;
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key"))
				{
					check =
						UUID
							.TryParse(Parameters["key"].ToString().Replace("_", " "),
							out agentKey);
				}
				else
				{
					return "<error>arguments</error>";
				}
				if (check)
				{
					bool response = getOnline(b, agentKey);
					return $"<online>{response.ToString()}</online>";
				}
				else
				{
					return "<error>unknown</error>";
				}
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>";
			}
		}

		/// <summary>
		/// Get online status of an avatar
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="key">UUID of avatar to check</param>
		/// <returns>true or false, if the avatar is or isn't online</returns>
		public bool getOnline(RestBot b, UUID key)
		{
			DebugUtilities
				.WriteInfo($"{session} {MethodName} Looking up online status for {key.ToString()}");

			lock (OnlineLookupEvents)
			{
				OnlineLookupEvents.Add(key, new AutoResetEvent(false));
			}

			// obsolete
			/*
			AvatarPropertiesRequestPacket p = new AvatarPropertiesRequestPacket();
			p.AgentData.AgentID = b.Client.Network.AgentID;
			p.AgentData.SessionID = b.Client.Network.SessionID;
			p.AgentData.AvatarID = key;

			b.Client.Network.SendPacket( (Packet) p);
			*/
			b.Client.Avatars.RequestAvatarProperties (key);

			OnlineLookupEvents[key].WaitOne(15000, true);

			lock (OnlineLookupEvents)
			{
				OnlineLookupEvents.Remove (key);
			}
			bool response = avatarOnline[key];
			lock (avatarOnline)
			{
				avatarOnline.Remove (key);
			}
			return response;
		}

		/// <summary>
		/// Loop through all (pending) replies for avatar properties
		/// and do something as yet unknown to me (gwyneth 20220120)
		/// </summary>
		/// <param name="sender">parameter ignored</param>
		/// <param name="e">List of avatar properties</param>
		/// <returns>void</returns>
		/// <remarks>updated for new callbacks</remarks>
		public void AvatarPropertiesReply(
			object? sender,
			AvatarPropertiesReplyEventArgs e
		)
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

			DebugUtilities
				.WriteInfo($"{session} {MethodName} Processing AvatarPropertiesReply for {e.AvatarID.ToString()} is {Properties.Online.ToString()}");
			lock (avatarOnline)
			{
				avatarOnline[e.AvatarID] = Properties.Online;
			}
			if (OnlineLookupEvents.ContainsKey(e.AvatarID))
			{
				OnlineLookupEvents[e.AvatarID].Set();
			}
		}
	}

	/// <summary>
	/// Gets the profile for a given avatar.
	/// </summary>
	public class AvatarProfileLookupPlugin : StatefulPlugin
	{
		private UUID session;

		protected Dictionary<UUID, AutoResetEvent>
			ProfileLookupEvents = new Dictionary<UUID, AutoResetEvent>();

		protected Dictionary<UUID, Avatar.AvatarProperties>
			avatarProfile = new Dictionary<UUID, Avatar.AvatarProperties>();

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public AvatarProfileLookupPlugin()
		{
			MethodName = "avatar_profile";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");

			// bot.Client.Avatars.OnAvatarProperties += new AvatarManager.AvatarPropertiesCallback(Avatars_OnAvatarProperties); // obsolete
			bot.Client.Avatars.AvatarPropertiesReply += Avatars_OnAvatarProperties;
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the avatar UUID(s) to get the profile for</param>
		/// <returns>XML-encoded profile name, if found</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID agentKey;
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key"))
				{
					check =
						UUID
							.TryParse(Parameters["key"].ToString().Replace("_", " "),
							out agentKey);
				}
				else
				{
					return "<error>arguments</error>";
				}
				if (check)
				{
					string? response = getProfile(b, agentKey); // profile can be null
					if (response == null)
					{
						return "<error>not found</error>";
					}
					else
					{
						return response;
					}
				}
				else
				{
					return "<error>unknown</error>";
				}
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>";
			}
		}

		/// <summary>
		/// Look up the profile for a UUID
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="key">UUID of avatar to retrieve profile</param>
		/// <returns>Full profile as a string, or null if profile is empty/does not exist</returns>
		/// <remarks>C# 8+ is stricter when returning nulls, thus the <c>string?</c> method type.</remarks>
		public string? getProfile(RestBot b, UUID key)
		{
			DebugUtilities
				.WriteInfo(session +
				" " +
				MethodName +
				" Looking up profile for " +
				key.ToString());

			lock (ProfileLookupEvents)
			{
				ProfileLookupEvents.Add(key, new AutoResetEvent(false));
			}
			b.Client.Avatars.RequestAvatarProperties (key);

			ProfileLookupEvents[key].WaitOne(15000, true);

			lock (ProfileLookupEvents)
			{
				ProfileLookupEvents.Remove (key);
			}
			if (avatarProfile.ContainsKey(key))
			{
				Avatar.AvatarProperties p = avatarProfile[key];
				string response = $@"
	<profile>
		<publish>{p.AllowPublish.ToString()}</publish>
		<firstlife>
			<text>
				{p.FirstLifeText.Replace(">", "%3C").Replace("<", "%3E")}
			</text>
			<image>{p.FirstLifeImage.ToString()}</image>
		</firstlife>
		<partner>{p.Partner.ToString()}</partner>
		<born>{p.BornOn}</born>
		<about>
			{p.AboutText.Replace(">", "%3C").Replace("<", "%3E")}
		</about>
		<charter>{p.CharterMember}</charter>
		<profileimage>{p.ProfileImage.ToString()}</profileimage>
		<mature>{p.MaturePublish.ToString()}</mature>
		<identified>{p.Identified.ToString()}</identified>
		<transacted>{p.Transacted.ToString()}</transacted>
		<url>{p.ProfileURL}</url>
	</profile>";
				lock (avatarProfile)
				{
					avatarProfile.Remove(key);
				}
				return response;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Loop through all (pending) replies for avatar profiles
		/// and process them if they contain any key for the avatar we're looking up.
		/// </summary>
		/// <param name="sender">parameter ignored</param>
		/// <param name="e">List of UUID/Avatar names</param>
		/// <returns>void</returns>
		/// <remarks>changed to deal with new replies</remarks>
		public void Avatars_OnAvatarProperties(
			object? sender,
			AvatarPropertiesReplyEventArgs e
		)
		{
			lock (avatarProfile)
			{
				avatarProfile[e.AvatarID] = e.Properties;
			}
			if (ProfileLookupEvents.ContainsKey(e.AvatarID))
			{
				ProfileLookupEvents[e.AvatarID].Set();
			}
		}
	}

	/// <summary>
	/// Retrieves all the groups that an avatar belongs to.
	/// </summary>
	public class AvatarGroupsLookupPlugin : StatefulPlugin
	{
		private UUID session;

		protected Dictionary<UUID, AutoResetEvent>
			GroupsLookupEvents = new Dictionary<UUID, AutoResetEvent>();

		// protected Dictionary<UUID, AvatarGroupsReplyPacket.GroupDataBlock[] > avatarGroups = new Dictionary<UUID, AvatarGroupsReplyPacket.GroupDataBlock[]>(); // too cumbersome and now obsolete
		protected Dictionary<UUID, List<AvatarGroup>>
			avatarGroups = new Dictionary<UUID, List<AvatarGroup>>();

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public AvatarGroupsLookupPlugin()
		{
			MethodName = "avatar_groups";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");

			// obsolete
			// bot.Client.Avatars.OnAvatarGroups += new AvatarManager.AvatarGroupsCallback(Avatar_OnAvatarGroups);
			bot.Client.Avatars.AvatarGroupsReply += Avatar_OnAvatarGroups;
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the avatar key for which we're trying to get the groups</param>
		/// <returns>XML-encoded avatar name, if found</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID agentKey;
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key"))
				{
					check =
						UUID
							.TryParse(Parameters["key"].ToString().Replace("_", " "),
							out agentKey);
				}
				else
				{
					return "<error>arguments</error>";
				}
				if (check)
				{
					string? response = getGroups(b, agentKey); // string can be null
					if (response == null)
					{
						return "<error>not found</error>";
					}
					else
					{
						return response;
					}
				}
				else
				{
					return "<error>unknown</error>";
				}
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>parsekey</error>";
			}
		}

		/// <summary>
		/// Returns all groups that an avatar is in, or null if none
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="key">UUID of avatar to check</param>
		/// <returns>List of groups as a XML-formatted string, or null if the avatar does not belong to any group</returns>
		/// <remarks>
		///   <para>C# 8+ is stricter when returning nulls, thus the <c>string?</c> method type.</para>
		///		<para>Note(gwyneth): Instead of returning null, it would make more sense to return an empty XML!</para>
		/// </remarks>
		public string? getGroups(RestBot b, UUID key)
		{
			DebugUtilities
				.WriteInfo($"{session} {MethodName} Looking up groups for {key.ToString()}");

			lock (GroupsLookupEvents)
			{
				GroupsLookupEvents.Add(key, new AutoResetEvent(false));
			}
			b.Client.Avatars.RequestAvatarProperties (key);

			GroupsLookupEvents[key].WaitOne(15000, true);

			lock (GroupsLookupEvents)
			{
				GroupsLookupEvents.Remove (key);
			}

			if (null == avatarGroups)
			{
				DebugUtilities
					.WriteInfo($"{session} {MethodName} Groups cache failed.");
				return null;
			}
			if (0 == avatarGroups.Count)
			{
				DebugUtilities.WriteInfo($"{session} {MethodName} No groups");
				return null;
			}
			if (avatarGroups.ContainsKey(key))
			{
				string response = "<groups>";

				foreach (AvatarGroup g in avatarGroups[key])
				{
					response += $@"
	<group>
		<name>
			{g.GroupName.Replace(">", "%3C").Replace("<", "%3E")}
		</name>
		<key>{g.GroupID.ToString()}</key>
		<title>
			{g.GroupTitle.Replace(">", "%3C").Replace("<", "%3E")}
		</title>
		<notices>{g.AcceptNotices.ToString()}</notices>
		<powers>{g.GroupPowers.ToString()}</powers>
		<insignia>{g.GroupInsigniaID.ToString()}</insignia>
	</group>";
				}
				response += "</groups>";

				lock (avatarGroups)
				{
					avatarGroups.Remove(key);
				}
				return response;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Loop through all (pending) replies for groups
		/// and process them if they contain any avatar key we're looking for.
		/// </summary>
		/// <param name="sender">parameter ignored</param>
		/// <param name="e">List of UUID/Avatar names</param>
		/// <returns>void</returns>
		public void Avatar_OnAvatarGroups(
			object? sender,
			AvatarGroupsReplyEventArgs e
		)
		{
			lock (avatarGroups)
			{
				avatarGroups[e.AvatarID] = e.Groups;
			}
			if (GroupsLookupEvents.ContainsKey(e.AvatarID))
			{
				GroupsLookupEvents[e.AvatarID].Set();
			}
		}
	}

	// The following two classes were originally undocumented; they appear to have some overlapping
	// functionality with the Movement Plugin! (gwyneth 20220120)
	/// <summary>
	/// Tries to figure out the position of another avatar, known by name.
	/// </summary>
	/// <remarks><para>This is a less sophisticated version of MoveToAvatarPlugin,
	/// probably used to draw a line connecting both, figuring out the distance between them, or similar.</para>
	/// <para>(original comment) avatar position; parameters are first, last</para></remarks>
	public class AvatarPositionPlugin : StatefulPlugin
	{
		private UUID session;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public AvatarPositionPlugin()
		{
			MethodName = "avatar_position";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the avatar name to use as a target</param>
		/// <returns>XML-encoded information on the goal position (target) and the current position ('bot)</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			try
			{
				string name = "";
				bool check = true;

				// "target" is essentially an avatar name (first, last) (gwyneth 20220120)
				if (Parameters.ContainsKey("target"))
				{
					name = Parameters["target"].ToString().Replace("+", " ");
				}
				else
					check = false;

				if (!check)
				{
					return "<error>parameters have to be target avatar name (first, last)</error>";
				}

				lock (b.Client.Network.Simulators)
				{
					for (int i = 0; i < b.Client.Network.Simulators.Count; i++)
					{
						DebugUtilities
							.WriteDebug("Found Avatars: " +
							b.Client.Network.Simulators[i].ObjectsAvatars.Count);
						Avatar? target =
							b
								.Client
								.Network
								.Simulators[i]
								.ObjectsAvatars
								.Find(delegate (Avatar avatar)
								{
									DebugUtilities.WriteDebug($"Found avatar: {avatar.Name}");
									return avatar.Name == name;
								});

						if (target != null)
						{
							return String
								.Format("<goal_position>{0},{1},{2}</goal_position><curr_position>{3},{4},{5}</curr_position>",
								target.Position.X,
								target.Position.Y,
								target.Position.Z,
								b.Client.Self.SimPosition.X,
								b.Client.Self.SimPosition.Y,
								b.Client.Self.SimPosition.Z);
						}
						else
						{
							DebugUtilities.WriteError($"Error obtaining the avatar: {name}");
						}
					}
				}
				return "<error>avatar_position failed.</error>";
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{e.Message}</error>";
			}
		}
	} // end avatar_position

	/// <summary>
	/// Returns the position of the 'bot inside the current region.
	/// </summary>
	/// <remarks>Very similar to CurrentLocationPlugin, but does not return the region name (gwyneth 20220120).</remarks>
	public class MyPositionPlugin : StatefulPlugin
	{
		private UUID session;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public MyPositionPlugin()
		{
			MethodName = "my_position";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">not used</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			try
			{
				return String
					.Format("<position>{0},{1},{2}</position>",
					b.Client.Self.SimPosition.X,
					b.Client.Self.SimPosition.Y,
					b.Client.Self.SimPosition.Z);
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{e.Message}</error>";
			}
		}
	} // end my_position

	/// <summary>
	/// Touch prim; parameter is UUID of object to touch
	/// </summary>
	/// <since>8.1.5</since>
	/// <remarks>RESTful interface to the Touch command on LibreMetaverse's own Touch</remarks>
	/// <author>Gwyneth Llewelyn</author>
	/// <date>20220401</date>
	public class TouchPlugin : StatefulPlugin
	{
		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public TouchPlugin()
		{
			MethodName = "touch";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			DebugUtilities.WriteDebug($"{bot.sessionid} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot.</param>
		/// <param name="Parameters">A dictionary containing the <c>prim</c> (UUID) to touch.</param>
		/// <returns>XML-encoded status of sit attempt</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			/// <summary>UUID for the prim/object that we intend the bot to touch</summary>
			UUID touchTargetID = UUID.Zero;

			DebugUtilities.WriteDebug($"{b.sessionid} {MethodName} - Searching for prim to touch");
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("prim"))
				{
					check =
						UUID
							.TryParse(Parameters["prim"].ToString().Replace("_", " "),
							out touchTargetID);
				}

				if (!check)
				{
					return "<error>prim to touch not specified</error>";
				}

				// If we get to this point means that we have a correctly parsed key for the target prim
				DebugUtilities.WriteDebug($"{b.sessionid} {MethodName} - Trying to touch {touchTargetID.ToString()}...");

				Primitive targetPrim = b.Client.Network.CurrentSim.ObjectsPrimitives.Find(
					prim => prim.ID == touchTargetID
				);

				if (targetPrim != null)
				{
					b.Client.Self.Touch(targetPrim.LocalID);
					return $"<{MethodName}>touching {targetPrim.ID.ToString()} ({targetPrim.LocalID})</{MethodName}>";
				}

				return $"<error>no prim with UUID {touchTargetID} found</error>";
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{e.Message}</error>";
			}
		}
	} // end TouchPlugin

	/// <summary>
	/// Class to return avatar data for its session ID
	/// </summary>
	/// <remarks><seealso cref="SessionListPlugin"></remarks>
	/// <author>Gwyneth Llewelyn</author>
	/// <date>20220424</date>
	public class SessionPlugin : RestPlugin
	{
		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public SessionPlugin()
		{
			MethodName = "session";
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">none</param>
		/// <remarks>This will return (some) avatar data associated with the current session ID</remarks>
		/// if that session is known to exist.</remarks>
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
				if (b == null)	// should be impossible
				{
					return "<error>invalid request</error>";
				}

				/// Note: the description below is outdated! (gwyneth 20220504)
				/// <value>Constructs a XML response with the following data:
				/// <list type="bullet">
				///   <item>
				///     <description>Avatar first name</description>
				///   </item>
				///   <item>
				///     <description>Avatar last name</description>
				///   </item>
				///   <item>
				///     <description>Avatar last namekey (UUID)</description>
				///   </item>
				///   <item>
				///     <description>Current status, same as returned by the "status" member function</description>
				///   </item>
				///   <item>
				///     <description>Session ID (redundant)</description>
				///   </item>
				///   <item>
				///     <description>Uptime (not sure if it's just seconds since login, or date/time of login; assuming the latter)</description>
				///   </item>
				///   <item>
				///     <description>Start position: can be "home", "last", or an URI describing region and position</description>
				///   </item>
				/// </list>
				/// </value>
				string response = $@"<{MethodName}>
	<FirstName>{b.First}</FirstName>
	<LastName>{b.Last}</LastName>
	<name>{b.First} {b.Last}</name>
	<key>{b.Client.Self.AgentID.ToString()}</key>
	<status>{b.myStatus.ToString()}</status>
	<session_id>{b.sessionid.ToString()}</session_id>
	<uptime>{b.getUptimeISO8601()}</uptime>
	<start>{b.Start}</start>
	<CurrentSim>{b.Client.Network.CurrentSim.ToString()}</CurrentSim>
	<Position>{b.Client.Self.SimPosition.X},{b.Client.Self.SimPosition.Y},{b.Client.Self.SimPosition.Z}</Position>
	<Rotation>{b.Client.Self.SimRotation.X},{b.Client.Self.SimRotation.Y},{b.Client.Self.SimRotation.Z},{b.Client.Self.SimRotation.W}</Rotation>
</{MethodName}>
";
				return response;
		} // end Process
	} // end SessionListPlugin
} // end namespace RESTbot

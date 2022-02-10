/*--------------------------------------------------------------------------------
	FILE INFORMATION:
		Name: Utilities.cs [./restbot-src/Utilities.cs]
		Description: Namespace to handle all sorts of utility functions which do not
								 fit anywhere else (gwyneth 20220126).

	LICENSE:
		This file is part of the RESTBot Project.

		Copyright (C) 2007-2008 PLEIADES CONSULTING, INC; (C) 2021 Gwyneth Llewelyn

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

		----------------------------------------------------------------------------

		Author: Gwyneth Llewelyn <gwyneth.llewelyn@gwynethllewelyn.net>
--------------------------------------------------------------------------------*/
using System;
using System.Net;
using System.Threading;
// using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace RESTBot
{
	/// <summary>All-around utility class</summary>
	public static class Utilities
	{
#region name2key/key2name
		/// <summary>Caches for name2key and key2name requests</summary>
		/// <see>AvatarNameLookupPlugin, AvatarKeyLookupPlugin</see>
		private static Dictionary<UUID, AutoResetEvent>
			NameLookupEvents = new Dictionary<UUID, AutoResetEvent>();

		private static Dictionary<UUID, String>
			avatarNames = new Dictionary<UUID, String>();

		private static Dictionary<String, AutoResetEvent>
			KeyLookupEvents = new Dictionary<String, AutoResetEvent>();

		private static Dictionary<String, UUID>
			avatarKeys = new Dictionary<String, UUID>();

		/// <summary>
		/// Loop through all (pending) replies for UUID/Avatar names
		/// and process them if they contain any key we're looking for.
		/// </summary>
		/// <param name="sender">parameter ignored</param>
		/// <param name="e">List of UUID/Avatar names</param>
		/// <returns>void</returns>
		/// <remarks>obsolete syntax changed</remarks>
		private static void Avatars_OnAvatarNames(
			object? sender,
			UUIDNameReplyEventArgs e
		)
		{
			DebugUtilities
				.WriteInfo("Avatars_OnAvatarNames(): Processing " +
				e.Names.Count.ToString() +
				" AvatarNames replies");
			foreach (KeyValuePair<UUID, string> kvp in e.Names)
			{
				if (!avatarNames.ContainsKey(kvp.Key) || avatarNames[kvp.Key] == null)
				{
					DebugUtilities
						.WriteInfo("Avatars_OnAvatarNames(): Reply Name: " +
						kvp.Value +
						" Key : " +
						kvp.Key.ToString());
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
		} // end Avatars_OnAvatarNames()

		/// <summary>
		/// key2name (given an avatar UUID, returns the avatar name, if it exists)
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="key">UUID of avatar to check</param>
		/// <returns>Name of the avatar if it exists; String.Empty if not</returns>
		public static string getName(RestBot b, UUID id)
		{
			DebugUtilities
				.WriteInfo("getName(): Looking up name for " + id.ToString());
			b.Client.Avatars.UUIDNameReply += Avatars_OnAvatarNames;
			lock (NameLookupEvents)
			{
				NameLookupEvents.Add(id, new AutoResetEvent(false));
			}

			b.Client.Avatars.RequestAvatarName (id);

			if (!NameLookupEvents[id].WaitOne(15000, true))
			{
				DebugUtilities
					.WriteWarning("getName(): timed out on avatar name lookup");
			}
			lock (NameLookupEvents)
			{
				NameLookupEvents.Remove (id);
			}

			// C# 8+ is stricter with null assignments.
			// string? response = null;	// technically this cannot ever be null, so it doesn't make sense...
			string response = String.Empty;
			if (avatarNames.ContainsKey(id))
			{
				response = avatarNames[id]; // .Name removed
				lock (avatarNames)
				{
					avatarNames.Remove (id);
				}
			}

			/* 			else
			{
				response = String.Empty;
			} */
			b.Client.Avatars.UUIDNameReply -= Avatars_OnAvatarNames;
			return response;
		} // end getName()

		/// <summary>
		/// Loop through all (pending) replies for UUID/Avatar names
		/// and process them if they contain any key we're looking for.
		/// </summary>
		/// <param name="sender">parameter ignored</param>
		/// <param name="e">List of UUID/Avatar names</param>
		/// <returns>void</returns>
		/// <remarks>using new Directory functionality</remarks>
		public static void Avatars_OnDirPeopleReply(
			object? sender,
			DirPeopleReplyEventArgs e
		)
		{
			if (e.MatchedPeople.Count < 1)
			{
				DebugUtilities
					.WriteWarning("Avatars_OnDirPeopleReply() - Error: empty people directory reply");
			}
			else
			{
				int replyCount = e.MatchedPeople.Count;

				DebugUtilities
					.WriteInfo("Avatars_OnDirPeopleReply() - Processing " +
					replyCount.ToString() +
					" DirPeople replies");
				for (int i = 0; i < replyCount; i++)
				{
					string avatarName =
						e.MatchedPeople[i].FirstName + " " + e.MatchedPeople[i].LastName;
					UUID avatarKey = e.MatchedPeople[i].AgentID;
					DebugUtilities
						.WriteDebug("Avatars_OnDirPeopleReply() -  Reply " +
						(i + 1).ToString() +
						" of " +
						replyCount.ToString() +
						" Key : " +
						avatarKey.ToString() +
						" Name : " +
						avatarName);

					if (!avatarKeys.ContainsKey(avatarName))
					{
						/* || avatarKeys[avatarName] == null )	 // apparently dictionary entries cannot be null */
						lock (avatarKeys)
						{
							avatarKeys[avatarName.ToLower()] = avatarKey;
						}
					}

					lock (KeyLookupEvents)
					{
						if (KeyLookupEvents.ContainsKey(avatarName.ToLower()))
						{
							KeyLookupEvents[avatarName.ToLower()].Set();
							DebugUtilities.WriteDebug(avatarName.ToLower() + " KLE set!");
						}
					}
				}
			}
		} // end Avatars_OnDirPeopleReply()

		/// <summary>
		/// name2key (avatar name to UUID)
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="name">Name of avatar to check</param>
		/// <returns>UUID of corresponding avatar, if it exists</returns>
		public static UUID getKey(RestBot b, String name)
		{
			DebugUtilities.WriteInfo("getKey(): Looking up key for " + name);
			b.Client.Directory.DirPeopleReply += Avatars_OnDirPeopleReply;
			name = name.ToLower();
			DebugUtilities.WriteDebug("getKey(): Looking up: " + name);
			DebugUtilities
				.WriteDebug("getKey(): Key not in cache, requesting directory lookup"); // how do you know? (gwyneth 20220128)
			lock (KeyLookupEvents)
			{
				KeyLookupEvents.Add(name, new AutoResetEvent(false));
			}
			DebugUtilities
				.WriteDebug("getKey(): Lookup Event added, KeyLookupEvents now has a total of " +
				KeyLookupEvents.Count.ToString() +
				" entries");
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
				.WriteDebug("getKey(): Packet sent - KLE has " +
				KeyLookupEvents.Count.ToString() +
				" entries.. now waiting");
			if (!KeyLookupEvents[name].WaitOne(15000, true))
			{
				DebugUtilities
					.WriteWarning("getKey(): timed out on avatar name lookup for " +
					name);
			}
			DebugUtilities.WriteDebug("getKey(): Waiting done!");
			lock (KeyLookupEvents)
			{
				KeyLookupEvents.Remove (name);
			}
			DebugUtilities
				.WriteDebug("getKey(): Done with KLE, now has " +
				KeyLookupEvents.Count.ToString() +
				" entries");
			UUID response = new UUID(); // hopefully this sets the response to UUID.Zero first... (gwyneth 20220128)
			if (avatarKeys.ContainsKey(name))
			{
				response = avatarKeys[name];
				lock (avatarKeys)
				{
					avatarKeys.Remove (name);
				}
			}
			b.Client.Directory.DirPeopleReply -= Avatars_OnDirPeopleReply;
			return response;
		} // end getKey()

		/// <summary>
		/// Overloaded getKey() function, using first name and last name as parameters
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="avatarFirstName">First name of avatar to check</param>
		/// <param name="avatarLastName">Last name of avatar to check</param>
		/// <returns>UUID of corresponding avatar, if it exists</returns>
		public static UUID
		getKey(RestBot b, String avatarFirstName, String avatarLastName)
		{
			String avatarFullName =
				avatarFirstName.ToString() + " " + avatarLastName.ToString();
			return getKey(b, avatarFullName); // it will be set to lowercase by getKey() (gwyneth 20220126).
		}
#endregion name2key/key2name
	} // end class
} // end namespace

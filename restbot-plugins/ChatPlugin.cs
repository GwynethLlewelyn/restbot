/*--------------------------------------------------------------------------------
	FILE INFORMATION:
	Name: ChatPlugin.cs [./restbot-plugins/ChatPlugin.cs]
	Description: Handles outgoing messages (chat, public chat, shout/say/whisper)
							 as well as instant messaging.

	LICENSE:
		This file is part of the RESTBot Project.

		Copyright (C) 2007-2008 PLEIADES CONSULTING, INC and others

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

  Author: Brian Krisler <bkrisler@gmail.com>
	Contributing author: Gwyneth Llewelyn <gwyneth.llewelyn@gwynethllewelyn.net>
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Utilities;

namespace RESTBot
{
	/// <summary>
	/// Class to send messages to a channel in chat
	/// </summary>
	public class SayPlugin : StatefulPlugin
	{
		private UUID session;

		private RestBot? me; // potentially null

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public SayPlugin()
		{
			MethodName = "say";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			me = bot;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");

			base.Initialize(bot);
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the channel, the message, and possibly the chat type</param>
		/// <remarks>channel defaults to 0 (public channel), while chattype defaults to normal chat.</remarks>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			int channel = 0;
			bool check = true;
			string message = String.Empty;
			ChatType chattype = ChatType.Normal;

			if (Parameters.ContainsKey("channel"))
			{
				check &= int.TryParse(Parameters["channel"], out channel);
			}

			if (Parameters.ContainsKey("message"))
			{
				//message = Parameters["message"].ToString().Replace("+", " ");
				message = Parameters["message"];
			}
			else
				check = false;

			// if chattype is not specified, we use normal chat by default.
			if (Parameters.ContainsKey("chattype"))
			{
				switch (Parameters["chattype"])
				{
					case "shout":
						chattype = ChatType.Shout;
						break;
					case "whisper":
						chattype = ChatType.Whisper;
						break;
					default:
						chattype = ChatType.Normal;
						break;
				}
			}

			if (!check)
			{
				return "<error>missing required parameters</error>";
			}

			// Make sure we are not in autopilot.
			// Note: Why not? (gwyneth 20220121)
			b.Client.Self.AutoPilotCancel();

			// Note: when channel is zero, we'll attempt to use Realism.Chat instead, because it looks cooler! (gwyneth 20220121)
			/// <summary><c>Realism</c> is a class in <c>Openmetaverse.Utilities</c>.</summary>
			if (channel != 0)
			{
				b.Client.Self.Chat(message, channel, chattype);
			}
			else
			{
				Realism.Chat(b.Client, message, chattype, 3); // 3 means typing 3 characters per second (gwyneth 20220121)
			}

			return "<say><channel>" +
			channel.ToString() +
			"</channel><message>" +
			message +
			"</message><chattype>" +
			chattype.ToString() +
			"</chattype></say>";
		} // end Process
	} // end SayPlugin

	/// <summary>
	/// Class to send instant messages to an avatar (name or UUID).
	/// </summary>
	/// <remarks>
	/// Heavily inspired by LibreMetaverse's TestClient! (gwyneth 20220212)
	/// It's much cleaner that way; getting the avatar key needed to send the IM is pushed
	/// to the Utilities class.
	/// </remarks>
	/// <author>
	/// Gwyneth Llewelyn <gwyneth.llewelyn@gwynethllewelyn.net>
	/// </author>
	public class InstantMessagePlugin : StatefulPlugin
	{
		private UUID session;

		private RestBot? me; // potentially null

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public InstantMessagePlugin()
		{
			MethodName = "instant_message";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;	// why is this saved? It's never used... (gwyneth 20220213)
			me = bot;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");

			base.Initialize(bot);	// wtf is this for? (gwyneth 20220212)
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the message, avatar first and last name, or UUID</param>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID avatarKey = UUID.Zero;
			bool check = true;
			string message = String.Empty;
			string avatarFirstName = String.Empty;
			string avatarLastName = String.Empty;

			if (Parameters.ContainsKey("key"))
			{
				check =
					UUID
						.TryParse(Parameters["key"].ToString().Replace("_", " "),
						out avatarKey);
			}
			else
			{
				if (Parameters.ContainsKey("last"))
				{
					avatarLastName = Parameters["last"];
				}

				if (Parameters.ContainsKey("first"))
				{
					avatarFirstName = Parameters["first"];
					check = true;
				}
				else
					check = false;

				// Massage the data we got; we can get away with an empty string for the last name (gwyneth 20220122).
				if (avatarLastName == String.Empty)
				{
					avatarLastName = "Resident";
					check = true;
				}

				// InstantMessage *always* needs an avatar UUID!
				// We need to look it up; fortunately, there are plenty of options available to get those.
				String avatarFullName =
					avatarFirstName.ToString() + " " + avatarLastName.ToString();

				avatarKey = Utilities.getKeySimple(b, avatarFullName); // handles conversion to lowercase.

				if (avatarKey == UUID.Zero)
				{
					DebugUtilities
						.WriteWarning($"Key not found for unknown avatar '{avatarFullName}'");
					check = false;
				}
			}

			if (Parameters.ContainsKey("message"))
			{
				message = Parameters["message"];
				if (message == String.Empty)
				{
					check = false;
				}
			}
			else
				check = false;

			if (!check)
			{
				return "<error>wrong parameters passed; IM not sent</error>";
			}

			// make sure message is not too big (gwyneth 20220212)
			message = message.TrimEnd();
			if (message.Length > 1023) message = message.Remove(1023);
			b.Client.Self.InstantMessage(avatarKey, message);
			return $"<instant_message><key>{avatarKey.ToString()}</key><message>{message}</message></instant_message>";
		} // end Process
	} // end InstantMessagePlugin
} // end namespace

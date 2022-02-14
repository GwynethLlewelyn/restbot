/*--------------------------------------------------------------------------------
FILE INFORMATION:
	Name: ListenPlugin.cs [./restbot-plugins/ListenPlugin.cs]
	Description: Listens to chat, and, if matched, calls URL for callback (gwyneth 20220213).

LICENSE:
	This file is part of the RESTBot Project.

	Copyright (C) 2007-2008 PLEIADES CONSULTING, INC; (C) 2021,2022 Gwyneth Llewelyn

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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Utilities;

namespace RESTBot
{
	/// <summary>
	/// Watches for one line of chat, and, if found, returns that line.
	/// </summary>
	/// <remarks><para>It's too hard to do it otherwise!</para>
	/// <para>Since: 8.1.5</para>
	/// </remarks>
	public class ListenPlugin : StatefulPlugin
	{
		/// <summary>System-wide registry for backend chat callbacks</summary>
		/// <remarks>We'll probably not use it</remarks>
		// static private Dictionary<UUID, String> ListenCallbacks;

		/// <summary>manual reset event for listen</summary>
		ManualResetEvent ListenEvent = new ManualResetEvent(false);

		/// <summary>Chat line to be returned, with associated metadata</summary>
		ChatEventArgs chatMetaData;

		/// <summary>Name of avatar or object that is chatting in-world</summary>
		private volatile string listenAgentName = String.Empty;
		/// <summary>UUID of avatar or object that is chatting in-world</summary>
		/// <remarks>Needs to be a string, or you can't lock it (gwyneth 20220213)</remarks>
		private volatile string listenAgentKey = UUID.Zero.ToString();

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public ListenPlugin()
		{
			MethodName = "listen";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		public override void Initialize(RestBot bot)
		{
			// ListenCallbacks = new Dictionary<UUID, String>();
			DebugUtilities.WriteDebug($"{MethodName} initialised.");

			base.Initialize(bot);
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing an avatar or object <c>name</c>, or <c>key</c> (UUID)</param>
		/// <remarks>
		/// <para>If the avatar/object name AND the key parameter are empty,
		/// this will match *any* avatar/object name (!)</para>
		/// <para>To-do: allow regexps in the name field</para>
		/// <para>To-do: allow selectively listening to objects/avatars in the same group as the bot</para>
		/// </remarks>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			bool check = false;

			/// <summary>temporary, so we don't need to lock the name while accessing it</summary>
			string tempName = String.Empty;

			/// <summary>temporary, so we don't need to lock the key while the UUID is being parsed</summary>
			UUID tempKey = UUID.Zero;

			// Name can be either an avatar name, or an object name, we can listen to both (gwyneth 20220213)
			// Note that either one is optional, but if *both* are empty/invalid, then this will listen to anything!
			if (Parameters.ContainsKey("name"))
			{
				tempName = Parameters["name"].ToString().Replace("+", " ");

				if (tempName == String.Empty)
				{
					check = false;
				}
				else
				{
					lock (listenAgentName)	// make it thread-safe!
					{
						listenAgentName = tempName;
					}
					check = true;
				}

				// very likely, this will fail if the name isn't an avatar, but an object, but we'll see,
				// it's possible that we can still do something useful upstream (gwyneth 20220213)
				tempKey = Utilities.getKeySimple(b, tempName);

				if (tempKey == UUID.Zero)
				{
					check = false;
				}
				else
				{
					lock (listenAgentKey)
					{
						listenAgentKey = tempKey.ToString();
					}
				}

				check = true;
			}
			else if (Parameters.ContainsKey("key"))
			{
				check =	UUID.TryParse(Parameters["key"].ToString().Replace("_", " "), out tempKey);

				if (tempKey == UUID.Zero) {
					check = false;
				}
				else
				{
					lock (listenAgentKey)
					{
						listenAgentKey = tempKey.ToString();
					}
					check = true;
				}
			}
			else check = false;	// neither contains name, nor key; we listen to everything instead!

			if (check == false)
			{
				DebugUtilities.WriteWarning("Listen called without valid name/UUID, we're listening to anything");
			}
			else
			{
				DebugUtilities.WriteDebug($"Listen called, after parsing we're going to check for '{tempName}' and/or '{tempKey}'");
			}

			// add the callback, and start listening
			b.Client.Self.ChatFromSimulator += Self_ChatFromSimulator;

			// start listening! Note that this should not be blocking on a timer, but for the sake
			// of expediency, we're pretending that all of this is synchronised and single-threaded... (gwyneth 20220213)
			ListenEvent.WaitOne(30000, false);

			if (chatMetaData != (ChatEventArgs) EventArgs.Empty)
			{
				string ret = $"<listen><simulator>{chatMetaData.Simulator.ToString()}</simulator><message>{chatMetaData.Message}</message><audiblelevel>{chatMetaData.AudibleLevel.ToString()}</audiblelevel><chattype>{chatMetaData.Type.ToString()}</chattype><sourcetype>{chatMetaData.SourceType.ToString()}</sourcetype><fromname>{chatMetaData.FromName}</fromname><sourceid>{chatMetaData.SourceID.ToString()}</sourceid><ownerid>{chatMetaData.OwnerID.ToString()}</ownerid><position>{chatMetaData.Position.ToString()}</position></listen>";

				DebugUtilities.WriteDebug($"Captured message XML: {ret}");
				return ret;
			}
			else
			{
				DebugUtilities.WriteDebug($"Error retrieving chatMetaData, got: '{chatMetaData.ToString()}'");
				return "<error>Listen returned error or empty message</error>";
			}
		}

/* Note: this is the definition of a ChatEventArgs from LibreMetaverse/AgentManager.cs, just copied here for reference: (gwyneth 20220213)
		/// <summary>Get the simulator sending the message</summary>
		public Simulator Simulator { get; }

		/// <summary>Get the message sent</summary>
		public string Message { get; }

		/// <summary>Get the audible level of the message</summary>
		public ChatAudibleLevel AudibleLevel { get; }

		/// <summary>Get the type of message sent: whisper, shout, etc</summary>
		public ChatType Type { get; }

		/// <summary>Get the source type of the message sender</summary>
		public ChatSourceType SourceType { get; }

		/// <summary>Get the name of the agent or object sending the message</summary>
		public string FromName { get; }

		/// <summary>Get the ID of the agent or object sending the message</summary>
		public UUID SourceID { get; }

		/// <summary>Get the ID of the object owner, or the agent ID sending the message</summary>
		public UUID OwnerID { get; }

		/// <summary>Get the position of the agent or object sending the message</summary>
		public Vector3 Position { get; }
*/

		/// <summary>Callback that returns something in chat</summary>
		void Self_ChatFromSimulator(object? sender, ChatEventArgs e)
		{
			if (e.Message.Length > 0) {
			// Check if we have valid key and name

				lock (chatMetaData) {
					chatMetaData = e;
				}
			}
		}
	} // end class
} //end namespace
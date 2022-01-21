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

  Author: Brian Krisler bkrisler@gmail.com
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
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");

            base.Initialize(bot);
        }

				/// <summary>
				/// Handler event for this plugin.
				/// </summary>
				/// <param name="b">A currently active RestBot</param>
				/// <param name="Parameters">A dictionary containing the channel, the message, and possibly the chat type</param>
				/// <remarks>channel defaults to 0 (public channel), while chattype defaults to normal chat.</remarks>
        public override string Process(RestBot b, Dictionary<string, string> Parameters)
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
                message = Parameters["message"].ToString().Replace("+", " ");
            }
            else check = false;

						if (Parameters.ContainsKey("chattype"))
						{
								string strChatType = Parameters["chattype"].ToString().Replace("+", " ");

								switch(strChatType) {
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

            // Make sure we are not in autopilot.
						// Note: Why not? (gwyneth 20220121)
            b.Client.Self.AutoPilotCancel();

						// Note: when channel is zero, we'll attempt to use Realism.Chat instead, because it looks cooler! (gwyneth 20220121)
						/// <summary><c>Realism</c> is a class in <c>Openmetaverse.Utilities</c>.</summary>
						if (channel != 0) {
							b.Client.Self.Chat(message, channel, chattype);
						} else {
							Realism.Chat(b.Client, message, chattype, 3);	// 3 means typing 3 characters per second (gwyneth 20220121)
						}

            return "<say><channel>" + channel + "</channel><message>" + message.ToString() + "</message><chattype>" + chattype.ToString() + "</chattype></say>";
        }
    }
}

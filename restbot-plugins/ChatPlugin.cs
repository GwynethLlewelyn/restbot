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

namespace RESTBot.restbot_plugins
{
    public class SayPlugin : StatefulPlugin
    {
        private UUID session;
        private RestBot? me; // potentially null

        public SayPlugin()
        {
            MethodName = "say";
        }

        public override void Initialize(RestBot bot)
        {
            session = bot.sessionid;
            me = bot;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");

            base.Initialize(bot);
        }

        public override string Process(RestBot b, Dictionary<string, string> Paramaters)
        {
            int channel = 0;
            bool check = true;
            string message = String.Empty;

            if (Paramaters.ContainsKey("channel"))
            {
                check &= int.TryParse(Paramaters["channel"], out channel);
            }

            if (Paramaters.ContainsKey("message"))
            {
                message = Paramaters["message"].ToString().Replace("+", " ");
            }
            else check = false;

            // Make sure we are not in autopilot.
            b.Client.Self.AutoPilotCancel();

            b.Client.Self.Chat(message, channel, ChatType.Normal);

            return "<say><channel>" +channel+"</channel><message>"+ message.ToString() + "</message></say>";
        }
    }
}

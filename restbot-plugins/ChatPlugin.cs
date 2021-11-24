/*--------------------------------------------------------------------------------
 LICENSE:
         This file is part of the RESTBot Project.
 
         RESTbot is free software; you can redistribute it and/or modify it under
         the terms of the Affero General Public License Version 1 (March 2002)
 
         RESTBot is distributed in the hope that it will be useful,
         but WITHOUT ANY WARRANTY; without even the implied warranty of
         MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.   See the
         Affero General Public License for more details.

         You should have received a copy of the Affero General Public License
         along with this program (see ./LICENSING) If this is missing, please 
         contact alpha.zaius[at]gmail[dot]com and refer to 
         <http://www.gnu.org/licenses/agpl.html> for now.
         
         Author: Brian Krisler bkrisler@gmail.com

 COPYRIGHT: 
         RESTBot Codebase (c) 2010-2011 Raytheon BBN Technologies
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
        private RestBot me;

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

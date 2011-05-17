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

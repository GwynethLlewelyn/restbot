using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace OpenMetaverse.TestClient
{
    public class SetMasterKeyCommand : Command
    {
        public DateTime Created = DateTime.Now;

        public SetMasterKeyCommand(TestClient testClient)
        {
            Name = "setMasterKey";
            Description = "Sets the key of the master user.  The master user can IM to run commands.";
            Category = CommandCategory.TestClient;
        }

        public override string Execute(string[] args, UUID fromAgentID)
        {
            Client.MasterKey = UUID.Parse(args[0]);

            lock (Client.Network.Simulators)
            {
                for (int i = 0; i < Client.Network.Simulators.Count; i++)
                {
                    Avatar master = Client.Network.Simulators[i].ObjectsAvatars.Find(
                        delegate(Avatar avatar)
                        {
                            return avatar.ID == Client.MasterKey;
                        }
                    );

                    if (master != null)
                    {
                        Client.Self.InstantMessage(master.ID,
                            "You are now my master. IM me with \"help\" for a command list.");
                        break;
                    }
                }
            }

            return "Master set to " + Client.MasterKey.ToString();
        }
    }
}

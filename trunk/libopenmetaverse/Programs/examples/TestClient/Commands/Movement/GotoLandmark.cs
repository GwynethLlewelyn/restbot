using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace OpenMetaverse.TestClient
{
    public class GotoLandmarkCommand : Command
    {
        public GotoLandmarkCommand(TestClient testClient)
        {
            Name = "goto_landmark";
            Description = "Teleports to a Landmark. Usage: goto_landmark [UUID]";
            Category = CommandCategory.Movement;
        }

        public override string Execute(string[] args, UUID fromAgentID)
        {
            if (args.Length < 1)
            {
                return "Usage: goto_landmark [UUID]";
            }

            UUID landmark = new UUID();
            if (!UUID.TryParse(args[0], out landmark))
            {
                return "Invalid LLUID";
            }
            else
            {
                Console.WriteLine("Teleporting to " + landmark.ToString());
            }
            if (Client.Self.Teleport(landmark))
            {
                return "Teleport Succesful";
            }
            else
            {
                return "Teleport Failed";
            }
        }
    }
}

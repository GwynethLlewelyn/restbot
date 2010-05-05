using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;

namespace OpenMetaverse.TestClient
{
    public class FollowCommand: Command
    {
        const float DISTANCE_BUFFER = 3.0f;
        uint targetLocalID = 0;

		public FollowCommand(TestClient testClient)
		{
			Name = "follow";
			Description = "Follow another avatar. Usage: follow [FirstName LastName]/off.";
            Category = CommandCategory.Movement;

            testClient.Network.RegisterCallback(PacketType.AlertMessage, AlertMessageHandler);
		}

        public override string Execute(string[] args, UUID fromAgentID)
		{
            // Construct the target name from the passed arguments
			string target = String.Empty;
			for (int ct = 0; ct < args.Length; ct++)
				target = target + args[ct] + " ";
			target = target.TrimEnd();

            if (target.Length == 0 || target == "off")
            {
                Active = false;
                targetLocalID = 0;
                Client.Self.AutoPilotCancel();
                return "Following is off";
            }
            else
            {
                if (Follow(target))
                    return "Following " + target;
                else
                    return "Unable to follow " + target + ".  Client may not be able to see that avatar.";
            }
		}

        bool Follow(string name)
        {
            lock (Client.Network.Simulators)
            {
                for (int i = 0; i < Client.Network.Simulators.Count; i++)
                {
                    Avatar target = Client.Network.Simulators[i].ObjectsAvatars.Find(
                        delegate(Avatar avatar)
                        {
                            return avatar.Name == name;
                        }
                    );

                    if (target != null)
                    {
                        targetLocalID = target.LocalID;
                        Active = true;
                        return true;
                    }
                }
            }

            if (Active)
            {
                Client.Self.AutoPilotCancel();
                Active = false;
            }

            return false;
        }

		public override void Think()
		{
            if (Active)
            {
                // Find the target position
                lock (Client.Network.Simulators)
                {
                    for (int i = 0; i < Client.Network.Simulators.Count; i++)
                    {
                        Avatar targetAv;

                        if (Client.Network.Simulators[i].ObjectsAvatars.TryGetValue(targetLocalID, out targetAv))
                        {
                            float distance = 0.0f;

                            if (Client.Network.Simulators[i] == Client.Network.CurrentSim)
                            {
                                distance = Vector3.Distance(targetAv.Position, Client.Self.SimPosition);
                            }
                            else
                            {
                                // FIXME: Calculate global distances
                            }

                            if (distance > DISTANCE_BUFFER)
                            {
                                uint regionX, regionY;
                                Utils.LongToUInts(Client.Network.Simulators[i].Handle, out regionX, out regionY);

                                double xTarget = (double)targetAv.Position.X + (double)regionX;
                                double yTarget = (double)targetAv.Position.Y + (double)regionY;
                                double zTarget = targetAv.Position.Z - 2f;

                                Logger.DebugLog(String.Format("[Autopilot] {0} meters away from the target, starting autopilot to <{1},{2},{3}>",
                                    distance, xTarget, yTarget, zTarget), Client);

                                Client.Self.AutoPilot(xTarget, yTarget, zTarget);
                            }
                            else
                            {
                                // We are in range of the target and moving, stop moving
                                Client.Self.AutoPilotCancel();
                            }
                        }
                    }
                }
            }

			base.Think();
		}

        private void AlertMessageHandler(object sender, PacketReceivedEventArgs e)
        {
            Packet packet = e.Packet;
            
            AlertMessagePacket alert = (AlertMessagePacket)packet;
            string message = Utils.BytesToString(alert.AlertData.Message);

            if (message.Contains("Autopilot cancel"))
            {
                Logger.Log("FollowCommand: " + message, Helpers.LogLevel.Info, Client);
            }
        }
    }
}

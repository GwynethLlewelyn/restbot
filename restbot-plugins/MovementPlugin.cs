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

  Further changes by Gwyneth Llewelyn
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System.Xml;
using System.Xml.Serialization;
using System.Net;
using System.IO;
using System.Threading;

// movemeny functions; based on TestClient.exe code
namespace RESTBot
{
    // show current location
    public class CurrentLocationPlugin : StatefulPlugin
    {
        private UUID session;

        public CurrentLocationPlugin()
        {
            MethodName = "location";
        }

        public override void Initialize(RestBot bot)
        {
            session = bot.sessionid;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
        }

        public override string Process(RestBot b, Dictionary<string, string> Parameters)
        {
            try
            {
                return String.Format("<location><CurrentSim>{0}</CurrentSim><Position>{1},{2},{3}</Position></location>",
                    b.Client.Network.CurrentSim.ToString(), b.Client.Self.SimPosition.X,
                    b.Client.Self.SimPosition.Y, b.Client.Self.SimPosition.Z);
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return "<error>" + e.Message + "</error>";
            }
        }
    } // end location

    // move to location; parameters are sim, x, y, z
    public class GotoPlugin : StatefulPlugin
    {
        private UUID session;

        public GotoPlugin()
        {
            MethodName = "goto";
        }

        public override void Initialize(RestBot bot)
        {
            session = bot.sessionid;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
        }

        public override string Process(RestBot b, Dictionary<string, string> Parameters)
        {
            try
            {
                string sim = "";
                float x = 128.0f, y = 128.0f, z = 30.0f;
                bool check = true;

                if (Parameters.ContainsKey("sim"))
                {
                    sim = Parameters["sim"].ToString();
                }
                else check = false;

                if (Parameters.ContainsKey("x"))
                {
                    check &= float.TryParse(Parameters["x"], out x);
                }
                else check = false;

                if (Parameters.ContainsKey("y"))
                {
                    check &= float.TryParse(Parameters["y"], out y);
                }
                else check = false;

                if (Parameters.ContainsKey("z"))
                {
                    check &= float.TryParse(Parameters["z"], out z);
                }
                else check = false;

                if (!check)
                {
                    return "<error>parameters have to be simulator name, x, y, z</error>";
                }

                if (b.Client.Self.Teleport(sim, new Vector3(x, y, z)))
                    return "<teleport>" + b.Client.Network.CurrentSim + "</teleport>";
                else
                    return "<error>Teleport failed: " + b.Client.Self.TeleportMessage + "</error>";
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return "<error>" + e.Message + "</error>";
            }
        }
    } // end goto

    // move to location; parameters are sim, x, y, z
    public class MoveToPlugin : StatefulPlugin
    {
        private UUID session;
        private Vector3 goalPos;
        const float DISTANCE_BUFFER = 7.0f;
        private RestBot me;
        private float prevDistance;

        public MoveToPlugin()
        {
            MethodName = "moveto";
        }

        public override void Initialize(RestBot bot)
        {
            session = bot.sessionid;
            me = bot;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
        }

        public override void Think()
        {
            if (Active)
            {
                float distance = 0.0f;
                distance = Vector3.Distance(goalPos, me.Client.Self.SimPosition);

                DebugUtilities.WriteDebug("My pos = " + me.Client.Self.SimPosition + " Goal: " + goalPos + " and distance is: " + distance);
                if (distance < DISTANCE_BUFFER)
                {
                    DebugUtilities.WriteDebug("I am close to my goal pos: " +goalPos.X+", "+goalPos.Y+", "+goalPos.Z);
                    me.Client.Self.AutoPilotCancel();
                    DebugUtilities.WriteSpecial("Cancel Autopilot");
                    me.Client.Self.Movement.TurnToward(goalPos);
                    me.Client.Self.Movement.SendUpdate(true);
                    Active = false;
                }
            }
            base.Think();
        }

        public override string Process(RestBot b, Dictionary<string, string> Parameters)
        {
            uint regionX, regionY;
            Utils.LongToUInts(b.Client.Network.CurrentSim.Handle, out regionX, out regionY);

            try
            {
                double x = 0.0f, y = 0.0f, z = 0.0f, distance = 0.0f;
                bool check = true;
                bool run = false;

                if (Parameters.ContainsKey("x"))
                {
                    check &= double.TryParse(Parameters["x"], out x);
                    DebugUtilities.WriteDebug("Parse: " + Parameters["x"] + " into " + x);
                }
                else check = false;

                if (Parameters.ContainsKey("y"))
                {
                    check &= double.TryParse(Parameters["y"], out y);
                }
                else check = false;

                if (Parameters.ContainsKey("z"))
                {
                    check &= double.TryParse(Parameters["z"], out z);
                }
                else check = false;

                if (Parameters.ContainsKey("distance"))
                {
                    check &= double.TryParse(Parameters["distance"], out distance);
                }

                if (Parameters.ContainsKey("run"))
                {
                    check &= bool.TryParse(Parameters["run"], out run);
                }

                if (!check)
                {
                    return "<error>parameters have to be x, y, z, [distance, run]</error>";
                }

                if (run)
                {
                    b.Client.Self.Movement.AlwaysRun = true;
                }
                else
                {
                    b.Client.Self.Movement.AlwaysRun = false;
                }
                goalPos = new Vector3((float)x, (float)y, (float)z);
                b.Client.Self.Movement.TurnToward(goalPos);
                b.Client.Self.Movement.SendUpdate(false);

                prevDistance = Vector3.Distance(goalPos, me.Client.Self.SimPosition);
                // Convert the local coordinates to global ones by adding the region handle parts to x and y
                b.Client.Self.AutoPilot(goalPos.X + regionX, goalPos.Y + regionY, goalPos.Z);
                Active = true;

                while (Active)
                {
                    Thread.Sleep(1 * 1000);
                }
                DebugUtilities.WriteSpecial("End Thread!");
                return String.Format("<move>{0},{1},{2}</move>", b.Client.Self.GlobalPosition.X, b.Client.Self.GlobalPosition.Y, b.Client.Self.GlobalPosition.Z);
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return "<error>" + e.Message + "</error>";
            }
        }
    } // end moveto

    // move to avatar location; parameters are sim, avatar
    public class MoveToAvatarPlugin : StatefulPlugin
    {
        private UUID session;
        private Vector3 goalPos;
        const float DISTANCE_BUFFER = 7.0f;
        private RestBot me;
        private float prevDistance;
        private Avatar target;
        private uint regionX, regionY;

        public MoveToAvatarPlugin()
        {
            MethodName = "moveto-avatar";
        }

        public override void Initialize(RestBot bot)
        {
            session = bot.sessionid;
            me = bot;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
        }

        public override void Think()
        {
            if (Active)
            {
                float distance = 0.0f;
                goalPos = target.Position;
                me.Client.Self.AutoPilot(goalPos.X + regionX, goalPos.Y + regionY, goalPos.Z);
                distance = Vector3.Distance(goalPos, me.Client.Self.SimPosition);
                if (distance > 30)
                {
                    me.Client.Self.Movement.AlwaysRun = true;
                }
                else
                {
                    me.Client.Self.Movement.AlwaysRun = false;
                }
                DebugUtilities.WriteDebug("My pos = " + me.Client.Self.SimPosition + " Goal: " + goalPos + " and distance is: " + distance);
                if (distance < DISTANCE_BUFFER)
                {
                    DebugUtilities.WriteDebug("I am close to my goal pos: " + goalPos.X + ", " + goalPos.Y + ", " + goalPos.Z);
                    me.Client.Self.AutoPilotCancel();
                    DebugUtilities.WriteSpecial("Cancel Autopilot");
                    me.Client.Self.Movement.TurnToward(goalPos);
                    me.Client.Self.Movement.SendUpdate(true);
                    Active = false;
                }
            }
            base.Think();
        }

        public override string Process(RestBot b, Dictionary<string, string> Parameters)
        {

            Utils.LongToUInts(b.Client.Network.CurrentSim.Handle, out regionX, out regionY);

            try
            {
                double x = 0.0f, y = 0.0f, z = 0.0f;
                bool check = true;
                bool run = false;
                string avatarName = String.Empty;

                if (Parameters.ContainsKey("avatar"))
                {
                    avatarName = Parameters["avatar"].ToString().Replace("+", " ");
                }
                else check = false;

                if (!check)
                {
                    return "<error>parameters have to be x, y, z, [distance, run]</error>";
                }

                if (run)
                {
                    b.Client.Self.Movement.AlwaysRun = true;
                }
                else
                {
                    b.Client.Self.Movement.AlwaysRun = false;
                }


                lock (b.Client.Network.Simulators)
                {
                    for (int i = 0; i < b.Client.Network.Simulators.Count; i++)
                    {
                        DebugUtilities.WriteDebug("Found Avatars: " + b.Client.Network.Simulators[i].ObjectsAvatars.Count);
                        target = b.Client.Network.Simulators[i].ObjectsAvatars.Find(
                            delegate(Avatar avatar)
                            {
                                DebugUtilities.WriteDebug("Found avatar: " + avatar.Name);
                                return avatar.Name == avatarName;
                            }
                        );

                        if (target != null)
                        {
                            goalPos = target.Position;
                        }
                        else
                        {
                            DebugUtilities.WriteError("Error obtaining the avatar: " + avatarName);
                            return String.Format("<error>Error obtaining the avatar</error>");
                        }
                    }
                }

                goalPos = new Vector3((float)x, (float)y, (float)z);
                b.Client.Self.Movement.TurnToward(goalPos);
                b.Client.Self.Movement.SendUpdate(false);

                prevDistance = Vector3.Distance(goalPos, me.Client.Self.SimPosition);
                if (prevDistance > 30)
                {
                    b.Client.Self.Movement.AlwaysRun = true;
                }
                else
                {
                    b.Client.Self.Movement.AlwaysRun = false;
                }
                // Convert the local coordinates to global ones by adding the region handle parts to x and y
                b.Client.Self.AutoPilot(goalPos.X + regionX, goalPos.Y + regionY, goalPos.Z);
                Active = true;

                while (Active)
                {
                    Thread.Sleep(1 * 1000);
                }
                DebugUtilities.WriteSpecial("End Thread!");
                return String.Format("<move>{0},{1},{2}</move>", b.Client.Self.GlobalPosition.X, b.Client.Self.GlobalPosition.Y, b.Client.Self.GlobalPosition.Z);
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return "<error>" + e.Message + "</error>";
            }
        }
    } // end movetoavatar

    // follow an avatar; parameters are:
    public class FollowPlugin : StatefulPlugin
    {
        private UUID session;
        const float DISTANCE_BUFFER = 3.0f;
        private RestBot me;
        uint targetLocalID = 0;

        public FollowPlugin()
        {
            MethodName = "follow";
        }

        public override void Initialize(RestBot bot)
        {
            session = bot.sessionid;
            me = bot;
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
        }

        public override void Think()
        {
            if (Active)
            {
                // Find the target position
                lock (me.Client.Network.Simulators)
                {
                    for (int i = 0; i < me.Client.Network.Simulators.Count; i++)
                    {
                        Avatar targetAv;

                        if (me.Client.Network.Simulators[i].ObjectsAvatars.TryGetValue(targetLocalID, out targetAv))
                        {
                            float distance = 0.0f;

                            if (me.Client.Network.Simulators[i] == me.Client.Network.CurrentSim)
                            {
                                distance = Vector3.Distance(targetAv.Position, me.Client.Self.SimPosition);
                            }
                            else
                            {
                                // FIXME: Calculate global distances
                            }

                            if (distance > DISTANCE_BUFFER)
                            {
                                uint regionX, regionY;
                                Utils.LongToUInts(me.Client.Network.Simulators[i].Handle, out regionX, out regionY);

                                double xTarget = (double)targetAv.Position.X + (double)regionX;
                                double yTarget = (double)targetAv.Position.Y + (double)regionY;
                                double zTarget = targetAv.Position.Z - 2f;

                                Logger.DebugLog(String.Format("[Autopilot] {0} meters away from the target, starting autopilot to <{1},{2},{3}>",
                                    distance, xTarget, yTarget, zTarget), me.Client);

                                me.Client.Self.AutoPilot(xTarget, yTarget, zTarget);
                            }
                            else
                            {
                                // We are in range of the target and moving, stop moving
                                me.Client.Self.AutoPilotCancel();
                            }
                        }
                    }
                }
            }
            base.Think();
        }

        public override string Process(RestBot b, Dictionary<string, string> Parameters)
        {
            uint regionX, regionY;
            Utils.LongToUInts(b.Client.Network.CurrentSim.Handle, out regionX, out regionY);

            try
            {
                string target = String.Empty;

                if (Parameters.ContainsKey("target"))
                {
                    target = Parameters["target"].ToString().Replace("%20", " ").Replace("+", " ");
                }
                else
                {
                    return "<error>arguments</error>";
                }

                if (target.Length == 0 || target == "off")
                {
                    Active = false;
                    targetLocalID = 0;
                    b.Client.Self.AutoPilotCancel();
                    return "<follow>off</follow>";
                }
                else
                {
                    if (Follow(target))
                        return "<follow>on</follow>";
                    else
                        return "<follow>error</follow>";
                }
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return "<follow>error: " + e.Message + "</follow>";
            }
        }

        bool Follow(string name)
        {
            lock (me.Client.Network.Simulators)
            {
                for (int i = 0; i < me.Client.Network.Simulators.Count; i++)
                {
                    Avatar target = me.Client.Network.Simulators[i].ObjectsAvatars.Find(
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
                me.Client.Self.AutoPilotCancel();
                Active = false;
            }

            return false;
        }

    } // end follow

}

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
using System.Text;
using OpenMetaverse;
using System.Threading;

namespace RESTBot
{
    public class NearbyPrimsPlugin : StatefulPlugin
    {
        private UUID session;
        private RestBot? me;	// may be null...

        Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        public NearbyPrimsPlugin()
        {
            MethodName = "nearby_prims";
        }

        public override void Initialize(RestBot bot)
        {
            session = bot.sessionid;
            me = bot;
						DebugUtilities.WriteDebug($"{session} {MethodName} startup");

            base.Initialize(bot);
        }

        public override string Process(RestBot b, Dictionary<string, string> Parameters)
        {
            try
            {
                string type = String.Empty;
                bool check = true;
                float radius = 0.0f;

                if (Parameters.ContainsKey("type"))
                {
                    type = Parameters["type"].ToString().Replace("+", " ");
                }
                else check = false;

                if (Parameters.ContainsKey("radius"))
                {
                    check &= float.TryParse(Parameters["radius"], out radius);
                }
                else check = false;

                if (!check)
                {
                    return "<error>parameters have to be type, radius</error>";
                }

                // *** get current location ***
                Vector3 location = b.Client.Self.SimPosition;

                // *** find all objects in radius ***
                List<Primitive> prims = b.Client.Network.CurrentSim.ObjectsPrimitives.FindAll(
                    delegate(Primitive prim)
                    {
                        Vector3 pos = prim.Position;
                        return (prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(pos, location) < radius);
                    }
                );

                // *** request properties of these objects ***
                bool complete = RequestObjectProperties(prims, 0);

                String resultSet = String.Empty;

                foreach (Primitive p in prims)
                {
                    string? name = p.Properties != null ? p.Properties.Name : null;
                    if (String.IsNullOrEmpty(type) || ((name != null) && (name.Contains(type))))
                    {
                        resultSet += $"<prim><name>{name}</name><pos>{p.Position.X},{p.Position.Y},{p.Position.Z}</pos><id>{p.ID}</id></prim>";
                    }
                }
                return "<nearby_prims>" + resultSet + "</nearby_prims>";
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return $"<error>{e.Message}</error>";
            }
        }

        private bool RequestObjectProperties(List<Primitive> objects, int msPerRequest)
        {
            // Create an array of the local IDs of all the prims we are requesting properties for
            uint[] localids = new uint[objects.Count];

            lock (PrimsWaiting)
            {
                PrimsWaiting.Clear();

                for (int i = 0; i < objects.Count; ++i)
                {
                    localids[i] = objects[i].LocalID;
                    PrimsWaiting.Add(objects[i].ID, objects[i]);
                }
            }
						if (me == null)	// it _can_ be null these days, so we return false (gwyneth 20220126)
							return false;

            me.Client.Objects.SelectObjects(me.Client.Network.CurrentSim, localids);

            return AllPropertiesReceived.WaitOne(2000 + msPerRequest * objects.Count, false);
        }
    }

    public class NearbyPrimPlugin : StatefulPlugin
    {
        private UUID session;
        private RestBot? me;

        Dictionary<UUID, Primitive> PrimsWaiting = new Dictionary<UUID, Primitive>();
        AutoResetEvent AllPropertiesReceived = new AutoResetEvent(false);

        public NearbyPrimPlugin()
        {
            MethodName = "nearby_prim";
        }

        public override void Initialize(RestBot bot)
        {
            session = bot.sessionid;
            me = bot;
						DebugUtilities.WriteDebug($"{session} {MethodName} startup");

            base.Initialize(bot);
        }

        public override string Process(RestBot b, Dictionary<string, string> Parameters)
        {
            try
            {
                string type = String.Empty;
                bool check = true;
                float radius = 0.0f;

                if (Parameters.ContainsKey("type"))
                {
                    type = Parameters["type"].ToString().Replace("+", " ");
                }
                else check = false;

                if (Parameters.ContainsKey("radius"))
                {
                    check &= float.TryParse(Parameters["radius"], out radius);
                }
                else check = false;

                if (!check)
                {
                    return "<error>parameters have to be type, radius</error>";
                }

                // *** get current location ***
                Vector3 location = b.Client.Self.SimPosition;

                Primitive found = b.Client.Network.CurrentSim.ObjectsPrimitives.Find(
                    delegate(Primitive prim) {
                        return prim.Properties.Name == type;
                    });


                return $"<nearby_prim><pos>{found.Position.X},{found.Position.Y},{found.Position.Z}</pos></nearby_prim>";
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return $"<error>{MethodName}: {e.Message}</error>";
            }
        }

        private bool RequestObjectProperties(List<Primitive> objects, int msPerRequest)
        {
            // Create an array of the local IDs of all the prims we are requesting properties for
            uint[] localids = new uint[objects.Count];

            lock (PrimsWaiting)
            {
                PrimsWaiting.Clear();

                for (int i = 0; i < objects.Count; ++i)
                {
                    localids[i] = objects[i].LocalID;
                    PrimsWaiting.Add(objects[i].ID, objects[i]);
                }
            }
						if (me == null)	// it _can_ be null these days, so we return false (gwyneth 20220213)
							return false;

            me.Client.Objects.SelectObjects(me.Client.Network.CurrentSim, localids);

            return AllPropertiesReceived.WaitOne(2000 + msPerRequest * objects.Count, false);
        }
    }
}

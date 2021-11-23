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
using System.Text;
using LibreMetaverse; // instead of OpenMetaverse
using System.Threading;

namespace RESTBot
{
    public class NearbyPrimsPlugin : StatefulPlugin
    {
        private UUID session;
        private RestBot me;

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
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");

            base.Initialize(bot);
        }

        public override string Process(RestBot b, Dictionary<string, string> Paramaters)
        {
            try
            {
                string type = String.Empty;
                bool check = true;
                float radius = 0.0f;

                if (Paramaters.ContainsKey("type"))
                {
                    type = Paramaters["type"].ToString().Replace("+", " ");
                }
                else check = false;

                if (Paramaters.ContainsKey("radius"))
                {
                    check &= float.TryParse(Paramaters["radius"], out radius);
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
                        return ((prim.ParentID == 0) && (pos != Vector3.Zero) && (Vector3.Distance(pos, location) < radius));
                    }
                );

                // *** request properties of these objects ***
                bool complete = RequestObjectProperties(prims, 0);

                String resultSet = String.Empty;

                foreach (Primitive p in prims)
                {
                    string name = p.Properties != null ? p.Properties.Name : null;
                    if (String.IsNullOrEmpty(type) || ((name != null) && (name.Contains(type))))
                    {
                        resultSet += String.Format("<prim><name>{0}</name><pos>{1},{2},{3}</pos></prim>", name, p.Position.X, p.Position.Y, p.Position.Z);
                    }
                }
                return "<nearby_prims>" + resultSet + "</nearby_prims>";
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return "<error>" + e.Message + "</error>";
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

            me.Client.Objects.SelectObjects(me.Client.Network.CurrentSim, localids);

            return AllPropertiesReceived.WaitOne(2000 + msPerRequest * objects.Count, false);
        }
    }

    public class NearbyPrimPlugin : StatefulPlugin
    {
        private UUID session;
        private RestBot me;

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
            DebugUtilities.WriteDebug(session + " " + MethodName + " startup");

            base.Initialize(bot);
        }

        public override string Process(RestBot b, Dictionary<string, string> Paramaters)
        {
            try
            {
                string type = String.Empty;
                bool check = true;
                float radius = 0.0f;

                if (Paramaters.ContainsKey("type"))
                {
                    type = Paramaters["type"].ToString().Replace("+", " ");
                }
                else check = false;

                if (Paramaters.ContainsKey("radius"))
                {
                    check &= float.TryParse(Paramaters["radius"], out radius);
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


                return "<nearby_prim>" + String.Format("<pos>{1},{2},{3}</pos>",
                    found.Position.X, found.Position.Y, found.Position.Z) + "</nearby_prim>";
            }
            catch (Exception e)
            {
                DebugUtilities.WriteError(e.Message);
                return "<error>" + e.Message + "</error>";
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

            me.Client.Objects.SelectObjects(me.Client.Network.CurrentSim, localids);

            return AllPropertiesReceived.WaitOne(2000 + msPerRequest * objects.Count, false);
        }
    }
}

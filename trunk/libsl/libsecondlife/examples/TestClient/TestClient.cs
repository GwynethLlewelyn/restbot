using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using libsecondlife;
using libsecondlife.Packets;

namespace libsecondlife.TestClient
{
    public class TestClient : SecondLife
    {
        public delegate void PrimCreatedCallback(Simulator simulator, Primitive prim);

        public event PrimCreatedCallback OnPrimCreated;

        public Dictionary<Simulator, Dictionary<uint, Primitive>> SimPrims;
        public LLUUID GroupID = LLUUID.Zero;
        public Dictionary<LLUUID, GroupMember> GroupMembers;
        public Dictionary<uint, Avatar> AvatarList = new Dictionary<uint,Avatar>();
		public Dictionary<LLUUID, AvatarAppearancePacket> Appearances = new Dictionary<LLUUID, AvatarAppearancePacket>();
		public Dictionary<string, Command> Commands = new Dictionary<string,Command>();
		public bool Running = true;
        public string MasterName = String.Empty;
        public LLUUID MasterKey = LLUUID.Zero;
		public ClientManager ClientManager;
        public int regionX;
        public int regionY;

        //internal libsecondlife.InventorySystem.InventoryFolder currentDirectory;

        private LLQuaternion bodyRotation = LLQuaternion.Identity;
        private LLVector3 forward = new LLVector3(0, 0.9999f, 0);
        private LLVector3 left = new LLVector3(0.9999f, 0, 0);
        private LLVector3 up = new LLVector3(0, 0, 0.9999f);
        private System.Timers.Timer updateTimer;

        // FIXME: get rid of these when they are moved to the main lib
        public AssetManager NewAssetManager;
        public AppearanceManager NewAppearanceManager;

        /// <summary>
        /// 
        /// </summary>
        public TestClient(ClientManager manager)
        {
			ClientManager = manager;

            NewAssetManager = new AssetManager(this);
            NewAppearanceManager = new AppearanceManager(this, NewAssetManager);

            updateTimer = new System.Timers.Timer(1000);
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);

            RegisterAllCommands(Assembly.GetExecutingAssembly());

            Settings.DEBUG = true;
            Settings.STORE_LAND_PATCHES = true;
            Settings.ALWAYS_REQUEST_OBJECTS = true;

            Network.RegisterCallback(PacketType.AgentDataUpdate, new NetworkManager.PacketCallback(AgentDataUpdateHandler));

            Objects.OnNewPrim += new ObjectManager.NewPrimCallback(Objects_OnNewPrim);
            Objects.OnObjectUpdated += new ObjectManager.ObjectUpdatedCallback(Objects_OnObjectUpdated);
            Objects.OnObjectKilled += new ObjectManager.KillObjectCallback(Objects_OnObjectKilled);
			Objects.OnNewAvatar += new ObjectManager.NewAvatarCallback(Objects_OnNewAvatar);
            Self.OnInstantMessage += new MainAvatar.InstantMessageCallback(Self_OnInstantMessage);
            Groups.OnGroupMembers += new GroupManager.GroupMembersCallback(GroupMembersHandler);
            Inventory.OnInventoryObjectReceived += new InventoryManager.InventoryObjectReceived(Inventory_OnInventoryObjectReceived);

            Network.RegisterCallback(PacketType.AvatarAppearance, new NetworkManager.PacketCallback(AvatarAppearanceHandler));
            
            updateTimer.Start();
        }

        public void RegisterAllCommands(Assembly assembly)
        {
            foreach (Type t in assembly.GetTypes())
            {
                try
                {
                    if (t.IsSubclassOf(typeof(Command)))
                    {
                        ConstructorInfo info = t.GetConstructor(new Type[] { typeof(TestClient) });
                        Command command = (Command)info.Invoke(new object[] { this });
                        RegisterCommand(command);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void RegisterCommand(Command command)
        {
			command.Client = this;
			if (!Commands.ContainsKey(command.Name.ToLower()))
			{
                Commands.Add(command.Name.ToLower(), command);
			}
        }

        //breaks up large responses to deal with the max IM size
        private void SendResponseIM(SecondLife client, LLUUID fromAgentID, string data, LLUUID imSessionID)
        {
            for ( int i = 0 ; i < data.Length ; i += 1024 ) {
                int y;
                if ((i + 1023) > data.Length)
                {
                    y = data.Length - i;
                }
                else
                {
                    y = 1023;
                }
                string message = data.Substring(i, y);
                client.Self.InstantMessage(fromAgentID, message, imSessionID);
            }
        }

		public void DoCommand(string cmd, LLUUID fromAgentID, LLUUID imSessionID)
        {
			string[] tokens = Parsing.ParseArguments(cmd);

            if (tokens.Length == 0)
                return;
				
			string firstToken = tokens[0].ToLower();

            // "all balance" will send the balance command to all currently logged in bots
			if (firstToken == "all" && tokens.Length > 1)
			{
			    cmd = String.Empty;

			    // Reserialize all of the arguments except for "all"
			    for (int i = 1; i < tokens.Length; i++)
			    {
			        cmd += tokens[i] + " ";
			    }

			    ClientManager.DoCommandAll(cmd, fromAgentID, imSessionID);

			    return;
			}

            if (Commands.ContainsKey(firstToken))
            {
                string[] args = new string[tokens.Length - 1];
                Array.Copy(tokens, 1, args, 0, args.Length);
                string response = Commands[firstToken].Execute(args, fromAgentID);

                if (response.Length > 0)
                {
                    Console.WriteLine(response);

                    if (fromAgentID != null && Network.Connected)
                    {
                        // IMs don't like \r\n line endings, clean them up first
                        response = response.Replace("\r", "");
                        SendResponseIM(this, fromAgentID, response, imSessionID);
                    }
                }
            }
        }

        private void updateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (Command c in Commands.Values)
                if (c.Active)
                    c.Think();
        }

        private void AgentDataUpdateHandler(Packet packet, Simulator sim)
        {
            AgentDataUpdatePacket p = (AgentDataUpdatePacket)packet;
            if (p.AgentData.AgentID == sim.Client.Network.AgentID)
            {
                Console.WriteLine("Got the group ID for " + sim.Client.ToString() + ", requesting group members...");
                GroupID = p.AgentData.ActiveGroupID;

                sim.Client.Groups.BeginGetGroupMembers(GroupID);
            }
        }

        private void GroupMembersHandler(Dictionary<LLUUID, GroupMember> members)
        {
            Console.WriteLine("Got " + members.Count + " group members.");
            GroupMembers = members;
        }

        private void Objects_OnObjectKilled(Simulator simulator, uint objectID)
        {
            lock (SimPrims)
            {
                if (SimPrims.ContainsKey(simulator) && SimPrims[simulator].ContainsKey(objectID))
                    SimPrims[simulator].Remove(objectID);
            }

			lock (AvatarList)
			{
			    if (AvatarList.ContainsKey(objectID))
			        AvatarList.Remove(objectID);
			}
        }

        private void Objects_OnObjectUpdated(Simulator simulator, ObjectUpdate update, ulong regionHandle, ushort timeDilation)
        {
            if (update.Avatar)
            {
                lock (AvatarList)
                {
                    // TODO: We really need a solid avatar and object tracker in Utilities to use here
                    if (AvatarList.ContainsKey(update.LocalID))
                    {
                        AvatarList[update.LocalID].CollisionPlane = update.CollisionPlane;
                        AvatarList[update.LocalID].Position = update.Position;
                        AvatarList[update.LocalID].Velocity = update.Velocity;
                        AvatarList[update.LocalID].Acceleration = update.Acceleration;
                        AvatarList[update.LocalID].Rotation = update.Rotation;
                        AvatarList[update.LocalID].AngularVelocity = update.AngularVelocity;
                        AvatarList[update.LocalID].Textures = update.Textures;
                    }
                }
            }
            else
            {
                lock (SimPrims)
                {
                    if (SimPrims.ContainsKey(simulator) && SimPrims[simulator].ContainsKey(update.LocalID))
                    {
                        SimPrims[simulator][update.LocalID].Position = update.Position;
                        SimPrims[simulator][update.LocalID].Velocity = update.Velocity;
                        SimPrims[simulator][update.LocalID].Acceleration = update.Acceleration;
                        SimPrims[simulator][update.LocalID].Rotation = update.Rotation;
                        SimPrims[simulator][update.LocalID].AngularVelocity = update.AngularVelocity;
                        SimPrims[simulator][update.LocalID].Textures = update.Textures;
                    }
                }
            }
        }

        private void Objects_OnNewPrim(Simulator simulator, Primitive prim, ulong regionHandle, ushort timeDilation)
        {
            lock (SimPrims)
            {
                if (!SimPrims.ContainsKey(simulator))
                {
                    SimPrims[simulator] = new Dictionary<uint, Primitive>(10000);
                }

                SimPrims[simulator][prim.LocalID] = prim;
            }

            if ((prim.Flags & LLObject.ObjectFlags.CreateSelected) != 0 && OnPrimCreated != null)
            {
                OnPrimCreated(simulator, prim);
            }
        }

		private void Objects_OnNewAvatar(Simulator simulator, Avatar avatar, ulong regionHandle, ushort timeDilation)
		{
		    lock (AvatarList)
		    {
		        AvatarList[avatar.LocalID] = avatar;
		    }
		}

        private void AvatarAppearanceHandler(Packet packet, Simulator simulator)
        {
            AvatarAppearancePacket appearance = (AvatarAppearancePacket)packet;

            lock (Appearances) Appearances[appearance.Sender.ID] = appearance;
        }

        private void Self_OnInstantMessage(LLUUID fromAgentID, string fromAgentName, LLUUID toAgentID,
            uint parentEstateID, LLUUID regionID, LLVector3 position, MainAvatar.InstantMessageDialog dialog,
            bool groupIM, LLUUID imSessionID, DateTime timestamp, string message,
            MainAvatar.InstantMessageOnline offline, byte[] binaryBucket, Simulator simulator)
        {
            if (MasterKey != LLUUID.Zero)
            {
                if (fromAgentID != MasterKey)
                {
                    // Received an IM from someone that is not the bot's master, ignore
                    Console.WriteLine("<IM ({0})> {1} (not master): {2} (@{3}:{4})", dialog, fromAgentName, message,
                        regionID, position);
                    return;
                }
            }
            else if (GroupMembers != null && !GroupMembers.ContainsKey(fromAgentID))
            {
                // Received an IM from someone outside the bot's group, ignore
                Console.WriteLine("<IM ({0})> {1} (not in group): {2} (@{3}:{4})", dialog, fromAgentName,
                    message, regionID, position);
                return;
            }

            // Received an IM from someone that is authenticated
            Console.WriteLine("<IM ({0})> {1}: {2} (@{3}:{4})", dialog, fromAgentName, message, regionID, position);

            if (dialog == MainAvatar.InstantMessageDialog.RequestTeleport)
            {
                Console.WriteLine("Accepting teleport lure.");
                Self.TeleportLureRespond(fromAgentID, true);
            }
            else if (
                dialog == MainAvatar.InstantMessageDialog.MessageFromAgent ||
                dialog == MainAvatar.InstantMessageDialog.MessageFromObject)
            {
                DoCommand(message, fromAgentID, imSessionID);
            }
        }

        private bool Inventory_OnInventoryObjectReceived(LLUUID fromAgentID, string fromAgentName,
            uint parentEstateID, LLUUID regionID, LLVector3 position, DateTime timestamp, AssetType type,
            LLUUID objectID, bool fromTask)
        {
            if (MasterKey != LLUUID.Zero)
            {
                if (fromAgentID != MasterKey)
                    return false;
            }
            else if (GroupMembers != null && !GroupMembers.ContainsKey(fromAgentID))
            {
                return false;
            }

            return true;
        }
	}
}

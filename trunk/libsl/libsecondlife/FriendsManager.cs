/*
 * Copyright (c) 2007, Second Life Reverse Engineering Team
 * All rights reserved.
 *
 * - Redistribution and use in source and binary forms, with or without 
 *   modification, are permitted provided that the following conditions are met:
 *
 * - Redistributions of source code must retain the above copyright notice, this
 *   list of conditions and the following disclaimer.
 * - Neither the name of the Second Life Reverse Engineering Team nor the names 
 *   of its contributors may be used to endorse or promote products derived from
 *   this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE 
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Text;
using libsecondlife.Packets;

namespace libsecondlife
{
    /// <summary>
    /// This class is used to add and remove avatars from your friends list and to manage their permission.  
    /// </summary>

    public class FriendsManager
    {
        [Flags]
        public enum RightsFlags : int
        {
            /// <summary>The avatar has no rights</summary>
            None = 0,
            /// <summary>The avatar can see the online status of the target avatar</summary>
            CanSeeOnline = 1,
            /// <summary>The avatar can see the location of the target avatar on the map</summary>
            CanSeeOnMap = 2,
            /// <summary>The avatar can modify the ojects of the target avatar </summary>
            CanModifyObjects = 4
        }

        /// <summary>
        /// This class holds information about an avatar in the friends list.  There are two ways 
        /// to interface to this class.  The first is through the set of boolean properties.  This is the typical
        /// way clients of this class will use it.  The second interface is through two bitmap properties.  While 
        /// the bitmap interface is public, it is intended for use the libsecondlife framework.
        /// </summary>
        public class FriendInfo
        {
            private LLUUID m_id;
            private string m_name;
            private bool m_isOnline;
            private bool m_canSeeMeOnline;
            private bool m_canSeeMeOnMap;
            private bool m_canModifyMyObjects;
            private bool m_canSeeThemOnline;
            private bool m_canSeeThemOnMap;
            private bool m_canModifyTheirObjects;

            /// <summary>
            /// Used by the libsecondlife framework when building the initial list of friends
            /// at login time.  This constructor should not be called by consummer of this class.
            /// </summary>
            /// <param name="id">System ID of the avatar being prepesented</param>
            /// <param name="theirRights">Rights the friend has to see you online and to modify your objects</param>
            /// <param name="myRights">Rights you have to see your friend online and to modify their objects</param>
            public FriendInfo(LLUUID id, RightsFlags theirRights, RightsFlags myRights)
            {
                m_id = id;
                m_canSeeMeOnline = (theirRights & RightsFlags.CanSeeOnline) != 0;
                m_canSeeMeOnMap = (theirRights & RightsFlags.CanSeeOnMap) != 0;
                m_canModifyMyObjects = (theirRights & RightsFlags.CanModifyObjects) != 0;

                m_canSeeThemOnline = (myRights & RightsFlags.CanSeeOnline) != 0;
                m_canSeeThemOnMap = (myRights & RightsFlags.CanSeeOnMap) != 0;
                m_canModifyTheirObjects = (myRights & RightsFlags.CanModifyObjects) != 0;
            }

            /// <summary>
            /// System ID of the avatar
            /// </summary>
            public LLUUID UUID { get { return m_id; } }

            /// <summary>
            /// full name of the avatar
            /// </summary>
            public string Name
            {
                get { return m_name; }
                set { m_name = value; }
            }

            /// <summary>
            /// True if the avatar is online
            /// </summary>
            public bool IsOnline
            {
                get { return m_isOnline; }
                set { m_isOnline = value; }
            }

            /// <summary>
            /// True if the friend can see if I am online
            /// </summary>
            public bool CanSeeMeOnline
            {
                get { return m_canSeeMeOnline; }
                set
                {
                    m_canSeeMeOnline = value;

                    // if I can't see them online, then I can't see them on the map
                    if (!m_canSeeMeOnline)
                        m_canSeeMeOnMap = false;
                }
            }

            /// <summary>
            /// True if the friend can see me on the map 
            /// </summary>
            public bool CanSeeMeOnMap
            {
                get { return m_canSeeMeOnMap; }
                set
                {
                    // if I can't see them online, then I can't see them on the map
                    if (m_canSeeMeOnline)
                        m_canSeeMeOnMap = value;
                }
            }

            /// <summary>
            /// True if the freind can modify my objects
            /// </summary>
            public bool CanModifyMyObjects
            {
                get { return m_canModifyMyObjects; }
                set { m_canModifyMyObjects = value; }
            }

            /// <summary>
            /// True if I can see if my friend is online
            /// </summary>
            public bool CanSeeThemOnline { get { return m_canSeeThemOnline; } }

            /// <summary>
            /// True if I can see if my friend is on the map
            /// </summary>
            public bool CanSeeThemOnMap { get { return m_canSeeThemOnMap; } }

            /// <summary>
            /// True if I can modify my friend's objects
            /// </summary>
            public bool CanModifyTheirObjects { get { return m_canModifyTheirObjects; } }

            /// <summary>
            /// My friend's rights represented as bitmapped flags
            /// </summary>
            public RightsFlags TheirRightsFlags
            {
                get
                {
                    RightsFlags results = RightsFlags.None;
                    if (m_canSeeMeOnline)
                        results |= RightsFlags.CanSeeOnline;
                    if (m_canSeeMeOnMap)
                        results |= RightsFlags.CanSeeOnMap;
                    if (m_canModifyMyObjects)
                        results |= RightsFlags.CanModifyObjects;

                    return results;
                }
                set
                {
                    m_canSeeMeOnline = (value & RightsFlags.CanSeeOnline) != 0;
                    m_canSeeMeOnMap = (value & RightsFlags.CanSeeOnMap) != 0;
                    m_canModifyMyObjects = (value & RightsFlags.CanModifyObjects) != 0;
                }
            }

            /// <summary>
            /// My rights represented as bitmapped flags
            /// </summary>
            public RightsFlags MyRightsFlags
            {
                get
                {
                    RightsFlags results = RightsFlags.None;
                    if (m_canSeeThemOnline)
                        results |= RightsFlags.CanSeeOnline;
                    if (m_canSeeThemOnMap)
                        results |= RightsFlags.CanSeeOnMap;
                    if (m_canModifyTheirObjects)
                        results |= RightsFlags.CanModifyObjects;

                    return results;
                }
                set
                {
                    m_canSeeThemOnline = (value & RightsFlags.CanSeeOnline) != 0;
                    m_canSeeThemOnMap = (value & RightsFlags.CanSeeOnMap) != 0;
                    m_canModifyTheirObjects = (value & RightsFlags.CanModifyObjects) != 0;
                }
            }

            /// <summary>
            /// This class represented as a string.
            /// </summary>
            /// <returns>A string reprentation of both my rights and my friend's righs</returns>
            public override string ToString()
            {
                return String.Format("{0} (Their Rights: {1}, My Rights: {2})", m_name, TheirRightsFlags, 
                    MyRightsFlags);
            }
        }

        /// <summary>
        /// Triggered when an avatar in your friends list comes online
        /// </summary>
        /// <param name="friend"> System ID of the avatar</param>
        public delegate void FriendOnlineEvent(FriendInfo friend);

        /// <summary>
        /// Triggered when an avatar in your friends list goes offline
        /// </summary>
        /// <param name="friend"> System ID of the avatar</param>
        public delegate void FriendOfflineEvent(FriendInfo friend);

        /// <summary>
        /// Triggered in response to a call to the GrantRighs() method, or when a friend changes your rights
        /// </summary>
        /// <param name="friend"> System ID of the avatar you changed the right of</param>
        public delegate void FriendRightsEvent(FriendInfo friend);

        /// <summary>
        /// Triggered when someone offers you friendship
        /// </summary>
        /// <param name="agentID">System ID of the agent offering friendship</param>
        /// <param name="agentName">full name of the agent offereing friendship</param>
        /// <param name="IMSessionID">session ID need when accepting/declining the offer</param>
        /// <returns>Return true to accept the friendship, false to deny it</returns>
        public delegate void FriendshipOfferedEvent(LLUUID agentID, string agentName, LLUUID imSessionID);

        /// <summary>
        /// Trigger when your friendship offer has been excepted
        /// </summary>
        /// <param name="agentID">System ID of the avatar who accepted your friendship offer</param>
        /// <param name="agentName">Full name of the avatar who accepted your friendship offer</param>
        /// <param name="accepted">Whether the friendship request was accepted or declined</param>
        public delegate void FriendshipResponseEvent(LLUUID agentID, string agentName, bool accepted);

        public event FriendOnlineEvent OnFriendOnline;
        public event FriendOfflineEvent OnFriendOffline;
        public event FriendRightsEvent OnFriendRights;
        public event FriendshipOfferedEvent OnFriendshipOffered;
        public event FriendshipResponseEvent OnFriendshipResponse;


        private SecondLife Client;
        private Dictionary<LLUUID, FriendInfo> _Friends = new Dictionary<LLUUID, FriendInfo>();
        private Dictionary<LLUUID, LLUUID> _Requests = new Dictionary<LLUUID, LLUUID>();

        /// <summary>
        /// This constructor is intened to for use only the the libsecondlife framework
        /// </summary>
        /// <param name="client"></param>
        public FriendsManager(SecondLife client)
        {
            Client = client;

            Client.Network.OnConnected += new NetworkManager.ConnectedCallback(Network_OnConnect);
            Client.Avatars.OnAvatarNames += new AvatarManager.AvatarNamesCallback(Avatars_OnAvatarNames);
            Client.Self.OnInstantMessage += new MainAvatar.InstantMessageCallback(MainAvatar_InstantMessage);

            Client.Network.RegisterCallback(PacketType.OnlineNotification, OnlineNotificationHandler);
            Client.Network.RegisterCallback(PacketType.OfflineNotification, OfflineNotificationHandler);
            Client.Network.RegisterCallback(PacketType.ChangeUserRights, ChangeUserRightsHandler);
        }


        /// <summary>
        /// Get a list of all the friends we are currently aware of
        /// </summary>
        /// <remarks>
        /// This function performs a shallow copy from the internal dictionary
        /// in FriendsManager. Avoid calling it multiple times when it is not 
        /// necessary to as it can be expensive memory-wise
        /// </remarks>
        public List<FriendInfo> FriendsList()
        {
            List<FriendInfo> friends = new List<FriendInfo>();

            lock (_Friends)
            {
                foreach (FriendInfo info in _Friends.Values)
                    friends.Add(info);
            }

            return friends;
        }

        /// <summary>
        /// Dictionary of unanswered friendship offers
        /// </summary>
        public Dictionary<LLUUID, LLUUID> PendingOffers()
        {
            Dictionary<LLUUID, LLUUID> requests = new Dictionary<LLUUID,LLUUID>();

            lock (_Requests)
            {
                foreach(KeyValuePair<LLUUID, LLUUID> req in _Requests)
                    requests.Add(req.Key, req.Value);
            }

            return requests;
        }

        /// <summary>
        /// Accept a friendship request
        /// </summary>
        /// <param name="imSessionID">imSessionID of the friendship request message</param>
        public void AcceptFriendship(LLUUID fromAgentID, LLUUID imSessionID)
        {
            LLUUID callingCardFolder = Client.Inventory.FindFolderForType(AssetType.CallingCard);

            AcceptFriendshipPacket request = new AcceptFriendshipPacket();
            request.AgentData.AgentID = Client.Network.AgentID;
            request.AgentData.SessionID = Client.Network.SessionID;
            request.TransactionBlock.TransactionID = imSessionID;
            request.FolderData = new AcceptFriendshipPacket.FolderDataBlock[1];
            request.FolderData[0] = new AcceptFriendshipPacket.FolderDataBlock();
            request.FolderData[0].FolderID = callingCardFolder;

            Client.Network.SendPacket(request);

            FriendInfo friend = new FriendInfo(fromAgentID, RightsFlags.CanSeeOnline,
                RightsFlags.CanSeeOnline);
            lock (_Friends) _Friends.Add(friend.UUID, friend);
            lock (_Requests) { if (_Requests.ContainsKey(fromAgentID)) _Requests.Remove(fromAgentID); }

            Client.Avatars.RequestAvatarName(fromAgentID);
        }

        /// <summary>
        /// Decline a friendship request
        /// </summary>
        /// <param name="imSessionID">imSessionID of the friendship request message</param>
        public void DeclineFriendship(LLUUID fromAgentID, LLUUID imSessionID)
        {
            DeclineFriendshipPacket request = new DeclineFriendshipPacket();
            request.AgentData.AgentID = Client.Network.AgentID;
            request.AgentData.SessionID = Client.Network.SessionID;
            request.TransactionBlock.TransactionID = imSessionID;
            Client.Network.SendPacket(request);

            lock (_Requests) { if (_Requests.ContainsKey(fromAgentID)) _Requests.Remove(fromAgentID); }
        }

        /// <summary>
        /// Offer friendship to an avatar.
        /// </summary>
        /// <param name="agentID">System ID of the avatar you are offering friendship to</param>
        public void OfferFriendship(LLUUID agentID)
        {
            // HACK: folder id stored as "message"
            LLUUID callingCardFolder = Client.Inventory.FindFolderForType(AssetType.CallingCard);
            Client.Self.InstantMessage(Client.ToString(),
                agentID,
                callingCardFolder.ToString(),
                LLUUID.Random(),
                MainAvatar.InstantMessageDialog.FriendshipOffered,
                MainAvatar.InstantMessageOnline.Online,
                Client.Self.Position,
                Client.Network.CurrentSim.ID,
                new byte[0]);
        }


        /// <summary>
        /// Terminate a friendship with an avatar
        /// </summary>
        /// <param name="agentID">System ID of the avatar you are terminating the friendship with</param>
        public void TerminateFriendship(LLUUID agentID)
        {
            if (_Friends.ContainsKey(agentID))
            {
                TerminateFriendshipPacket request = new TerminateFriendshipPacket();
                request.AgentData.AgentID = Client.Network.AgentID;
                request.AgentData.SessionID = Client.Network.SessionID;
                request.ExBlock.OtherID = agentID;

                Client.Network.SendPacket(request);
            }
        }


        /// <summary>
        /// Change the rights of a friend avatar.  To use this routine, first change the right of the
        /// avatar stored in the item property.
        /// </summary>
        /// <param name="agentID">System ID of the avatar you are changing the rights of</param>
        public void GrantRights(LLUUID agentID)
        {
            GrantUserRightsPacket request = new GrantUserRightsPacket();
            request.AgentData.AgentID = Client.Network.AgentID;
            request.AgentData.SessionID = Client.Network.SessionID;
            request.Rights = new GrantUserRightsPacket.RightsBlock[1];
            request.Rights[0] = new GrantUserRightsPacket.RightsBlock();
            request.Rights[0].AgentRelated = agentID;
            request.Rights[0].RelatedRights = (int)(_Friends[agentID].TheirRightsFlags);

            Client.Network.SendPacket(request);
        }


        /// <summary>
        /// Adds a friend. Intended for use by the libsecondlife framework to build the  
        /// initial list of friends from the buddy-list in the login reply XML
        /// </summary>
        /// <param name="agentID">ID of the agent being added to the list of friends</param>
        /// <param name="theirRights">rights the friend has</param>
        /// <param name="myRights">rights you have</param>
        internal void AddFriend(LLUUID agentID, RightsFlags theirRights, RightsFlags myRights)
        {
            lock (_Friends)
            {
                if (!_Friends.ContainsKey(agentID))
                {
                    FriendInfo friend = new FriendInfo(agentID, theirRights, myRights);
                    _Friends[agentID] = friend;
                }
            }
        }


        /// <summary>
        /// Called when a connection to the SL server is established.  The list of friend avatars 
        /// is populated from XML returned by the login server.  That list contains the avatar's id 
        /// and right, but no names.  Here is where those names are requested.
        /// </summary>
        /// <param name="sender"></param>
        private void Network_OnConnect(object sender)
        {
            List<LLUUID> names = new List<LLUUID>();

            if ( _Friends.Count > 0 )
            {
                lock (_Friends)
                {
                    foreach (KeyValuePair<LLUUID, FriendInfo> kvp in _Friends)
                    {
                        if (String.IsNullOrEmpty(kvp.Value.Name))
                            names.Add(kvp.Key);
                    }
                }

                Client.Avatars.RequestAvatarNames(names);
            }
        }


        /// <summary>
        /// This handles the asynchronous response of a RequestAvatarNames call.
        /// </summary>
        /// <param name="names">names cooresponding to the the list of IDs sent the the RequestAvatarNames call.</param>
        private void Avatars_OnAvatarNames(Dictionary<LLUUID, string> names)
        {
            lock (_Friends)
            {
                foreach (KeyValuePair<LLUUID, string> kvp in names)
                {
                    if (_Friends.ContainsKey(kvp.Key))
                        _Friends[kvp.Key].Name = names[kvp.Key];
                }
            }
        }


        /// <summary>
        /// Handle notifications sent when a friends has come online.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        private void OnlineNotificationHandler(Packet packet, Simulator simulator)
        {
            if (packet.Type == PacketType.OnlineNotification)
            {
                OnlineNotificationPacket notification = ((OnlineNotificationPacket)packet);

                foreach (OnlineNotificationPacket.AgentBlockBlock block in notification.AgentBlock)
                {
                    FriendInfo friend;

                    lock (_Friends)
                    {
                        if (!_Friends.ContainsKey(block.AgentID))
                        {
                            friend = new FriendInfo(block.AgentID, RightsFlags.CanSeeOnline,
                                RightsFlags.CanSeeOnline);
                            _Friends.Add(block.AgentID, friend);
                        }
                        else
                        {
                            friend = _Friends[block.AgentID];
                        }
                    }

                    bool doNotify = !friend.IsOnline;
                    friend.IsOnline = true;

                    if (OnFriendOnline != null && doNotify)
                    {
                        try { OnFriendOnline(friend); }
                        catch (Exception e) { Client.Log(e.ToString(), Helpers.LogLevel.Error); }
                    }
                }
            }
        }


        /// <summary>
        /// Handle notifications sent when a friends has gone offline.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        private void OfflineNotificationHandler(Packet packet, Simulator simulator)
        {
            if (packet.Type == PacketType.OfflineNotification)
            {
                OfflineNotificationPacket notification = ((OfflineNotificationPacket)packet);

                foreach (OfflineNotificationPacket.AgentBlockBlock block in notification.AgentBlock)
                {
                    FriendInfo friend;

                    lock (_Friends)
                    {
                        if (!_Friends.ContainsKey(block.AgentID))
                            _Friends.Add(block.AgentID, new FriendInfo(block.AgentID, RightsFlags.CanSeeOnline, RightsFlags.CanSeeOnline));

                        friend = _Friends[block.AgentID];
                        friend.IsOnline = false;
                    }

                    if (OnFriendOffline != null)
                    {
                        try { OnFriendOffline(friend); }
                        catch (Exception e) { Client.Log(e.ToString(), Helpers.LogLevel.Error); }
                    }
                }
            }
        }


        /// <summary>
        /// Handle notifications sent when a friend rights change.  This notification is also received
        /// when my own rights change.
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="simulator"></param>
        private void ChangeUserRightsHandler(Packet packet, Simulator simulator)
        {
            if (packet.Type == PacketType.ChangeUserRights)
            {
                FriendInfo friend;
                ChangeUserRightsPacket rights = (ChangeUserRightsPacket)packet;

                foreach (ChangeUserRightsPacket.RightsBlock block in rights.Rights)
                {
                    RightsFlags newRights = (RightsFlags)block.RelatedRights;
                    if (_Friends.TryGetValue(block.AgentRelated, out friend))
                    {
                        friend.TheirRightsFlags = newRights;
                        if (OnFriendRights != null)
                        {
                            try { OnFriendRights(friend); }
                            catch (Exception e) { Client.Log(e.ToString(), Helpers.LogLevel.Error); }
                        }
                    }
                    else if (block.AgentRelated == Client.Self.ID)
                    {
                        if (_Friends.TryGetValue(rights.AgentData.AgentID, out friend))
                        {
                            friend.MyRightsFlags = newRights;
                            if (OnFriendRights != null)
                            {
                                try { OnFriendRights(friend); }
                                catch (Exception e) { Client.Log(e.ToString(), Helpers.LogLevel.Error); }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Handles relevant messages from the server encapsulated in instant messages.
        /// </summary>
        /// <param name="fromAgentID"></param>
        /// <param name="fromAgentName"></param>
        /// <param name="toAgentID"></param>
        /// <param name="parentEstateID"></param>
        /// <param name="regionID"></param>
        /// <param name="position"></param>
        /// <param name="dialog"></param>
        /// <param name="groupIM"></param>
        /// <param name="imSessionID"></param>
        /// <param name="timestamp"></param>
        /// <param name="message"></param>
        /// <param name="offline"></param>
        /// <param name="binaryBucket"></param>
        /// <param name="simulator"></param>
        private void MainAvatar_InstantMessage(LLUUID fromAgentID, string fromAgentName,
            LLUUID toAgentID, uint parentEstateID, LLUUID regionID, LLVector3 position,
            MainAvatar.InstantMessageDialog dialog, bool groupIM, LLUUID imSessionID,
            DateTime timestamp, string message,
            MainAvatar.InstantMessageOnline offline, byte[] binaryBucket, Simulator simulator)
        {
            if (dialog == MainAvatar.InstantMessageDialog.FriendshipOffered)
            {
                if (OnFriendshipOffered != null)
                {
                    lock (_Requests)
                    {
                        if (_Requests.ContainsKey(fromAgentID)) _Requests[fromAgentID] = imSessionID;
                        else _Requests.Add(fromAgentID, imSessionID);
                    }
                    try { OnFriendshipOffered(fromAgentID, fromAgentName, imSessionID); }
                    catch (Exception e) { Client.Log(e.ToString(), Helpers.LogLevel.Error); }
                }
            }
            else if (dialog == MainAvatar.InstantMessageDialog.FriendshipAccepted)
            {
                FriendInfo friend = new FriendInfo(fromAgentID, RightsFlags.CanSeeOnline, RightsFlags.CanSeeOnline);
                friend.Name = fromAgentName;
                lock (_Friends) _Friends[friend.UUID] = friend;

                if (OnFriendshipResponse != null)
                {
                    try { OnFriendshipResponse(fromAgentID, fromAgentName, true); }
                    catch (Exception e) { Client.Log(e.ToString(), Helpers.LogLevel.Error); }
                }
            }
            else if (dialog == MainAvatar.InstantMessageDialog.FriendshipDeclined)
            {
                if (OnFriendshipResponse != null)
                {
                    try { OnFriendshipResponse(fromAgentID, fromAgentName, false); }
                    catch (Exception e) { Client.Log(e.ToString(), Helpers.LogLevel.Error); }
                }
            }
        }
    }
}

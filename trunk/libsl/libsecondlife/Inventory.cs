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

namespace libsecondlife
{
    /// <summary>
    /// Responsible for maintaining inventory structure. Inventory constructs nodes
    /// and manages node children as is necessary to maintain a coherant hirarchy.
    /// Other classes should not manipulate or create InventoryNodes explicitly. When
    /// A node's parent changes (when a folder is moved, for example) simply pass
    /// Inventory the updated InventoryFolder and it will make the appropriate changes
    /// to its internal representation.
    /// </summary>
    public class Inventory
    {
        /// <summary>
        /// Delegate to use for the OnInventoryObjectUpdated event.
        /// </summary>
        /// <param name="oldObject">The state of the InventoryObject before the update occured.</param>
        /// <param name="newObject">The state of the InventoryObject after the update occured.</param>
        public delegate void InventoryObjectUpdated(InventoryBase oldObject, InventoryBase newObject);

        /// <summary>
        /// Called when an InventoryObject's state is changed.
        /// </summary>
        public event InventoryObjectUpdated OnInventoryObjectUpdated;


        /// <summary>
        /// Delegate to use for the OnInventoryObjectRemoved event.
        /// </summary>
        /// <param name="obj">The InventoryObject that was removed.</param>
        public delegate void InventoryObjectRemoved(InventoryBase obj);


        /// <summary>
        /// Called when an item or folder is removed from inventory.
        /// </summary>
        public event InventoryObjectRemoved OnInventoryObjectRemoved;

        /// <summary>
        /// 
        /// </summary>
        public readonly InventoryFolder RootFolder;
        /// <summary>
        /// 
        /// </summary>
        public readonly InventoryNode RootNode;

        private SecondLife Client;
        private InventoryManager Manager;
        private Dictionary<LLUUID, InventoryNode> Items;

        /// <summary>
        /// By using the bracket operator on this class, the program can get the 
        /// InventoryObject designated by the specified uuid. If the value for the corresponding
        /// UUID is null, the call is equivelant to a call to <code>RemoveNodeFor(this[uuid])</code>.
        /// If the value is non-null, it is equivelant to a call to <code>UpdateNodeFor(value)</code>,
        /// the uuid parameter is ignored.
        /// </summary>
        /// <param name="uuid">The UUID of the InventoryObject to get or set, ignored if set to non-null value.</param>
        /// <returns>The InventoryObject corresponding to <code>uuid</code>.</returns>
        public InventoryBase this[LLUUID uuid]
        {
            get
            {
                InventoryNode node = Items[uuid];
                return node.Data;
            }
            set
            {
                if (value != null)
                {
                    // what if value.UUID != uuid? :-O
                    // should we check for this?
                    UpdateNodeFor(value);
                }
                else
                {
                    InventoryNode node;
                    if (Items.TryGetValue(uuid, out node))
                    {
                        RemoveNodeFor(node.Data);
                    }
                }
            }
        }

        public Inventory(SecondLife client, InventoryManager manager, InventoryFolder rootFolder)
        {
            Client = client;
            Manager = manager;
            RootFolder = rootFolder;
            RootNode = new InventoryNode(rootFolder);
            Items = new Dictionary<LLUUID, InventoryNode>();
            Items[rootFolder.UUID] = RootNode;
        }


        public List<InventoryBase> GetContents(InventoryFolder folder)
        {
            return GetContents(folder.UUID);
        }

        /// <summary>
        /// Returns the contents of the specified folder.
        /// </summary>
        /// <param name="folder">A folder's UUID.</param>
        /// <returns>The contents of the folder corresponding to <code>folder</code>.</returns>
        /// <exception cref="InventoryException">When <code>folder</code> does not exist in the inventory.</exception>
        public List<InventoryBase> GetContents(LLUUID folder)
        {
            InventoryNode folderNode;
            if (!Items.TryGetValue(folder, out folderNode))
                throw new InventoryException("Unknown folder: " + folder);
            lock (folderNode.Nodes.SyncRoot)
            {
                List<InventoryBase> contents = new List<InventoryBase>(folderNode.Nodes.Count);
                foreach (InventoryNode node in folderNode.Nodes.Values)
                {
                    contents.Add(node.Data);
                }
                return contents;
            }
        }


        /// <summary>
        /// Updates the state of the InventoryNode and inventory data structure that
        /// is responsible for the InventoryObject. If the item was previously not added to inventory,
        /// it adds the item, and updates structure accordingly. If it was, it updates the 
        /// InventoryNode, changing the parent node if <code>item.parentUUID</code> does 
        /// not match <code>node.Parent.Data.UUID</code>. 
        /// 
        /// You can not set the inventory root folder using this method.
        /// </summary>
        /// <param name="item">The InventoryObject to store.</param>
        public void UpdateNodeFor(InventoryBase item)
        {
            lock (Items)
            {
                InventoryNode itemParent = null;
                if (item.ParentUUID != LLUUID.Zero && !Items.TryGetValue(item.ParentUUID, out itemParent))
                {
                    // OK, we have no data on the parent, let's create a fake one.
                    InventoryFolder fakeParent = new InventoryFolder(item.ParentUUID);
                    fakeParent.DescendentCount = 1; // Dear god, please forgive me.
                    itemParent = new InventoryNode(fakeParent);
                    Items[item.ParentUUID] = itemParent;
                    // Unfortunately, this breaks the nice unified tree
                    // while we're waiting for the parent's data to come in.
                    // As soon as we get the parent, the tree repairs itself.
                    Client.DebugLog("Attempting to update inventory child of " +
                        item.ParentUUID.ToStringHyphenated() +
                        " when we have no local reference to that folder");

                    if (Client.Settings.FETCH_MISSING_INVENTORY)
                    {
                        // Fetch the parent
                        List<LLUUID> fetchreq = new List<LLUUID>(1);
                        fetchreq.Add(item.ParentUUID);
                        Manager.FetchInventory(fetchreq);
                    }
                }

                InventoryNode itemNode;
                if (Items.TryGetValue(item.UUID, out itemNode)) // We're updating.
                {
                    InventoryNode oldParent = itemNode.Parent;
                    // Handle parent change
                    if (oldParent != null && itemParent.Data.UUID != oldParent.Data.UUID)
                    {
                        lock (oldParent.Nodes.SyncRoot)
                            oldParent.Nodes.Remove(item.UUID);

                        lock (itemParent.Nodes.SyncRoot)
                            itemParent.Nodes[item.UUID] = itemNode;
                    }

                    itemNode.Parent = itemParent;

                    if (item != itemNode.Data)
                        FireOnInventoryObjectUpdated(itemNode.Data, item);

                    itemNode.Data = item;
                }
                else // We're adding.
                {
                    if (item.ParentUUID == LLUUID.Zero)
                    {
                        Client.Log("UpdateNodeFor(): Cannot add the root folder", Helpers.LogLevel.Warning);
                        return;
                    }

                    itemNode = new InventoryNode(item, itemParent);
                    Items.Add(item.UUID, itemNode);
                }
            }
        }

        /// <summary>
        /// Removes the InventoryObject and all related node data from Inventory.
        /// </summary>
        /// <param name="item">The InventoryObject to remove.</param>
        public void RemoveNodeFor(InventoryBase item)
        {
            lock (Items)
            {
                InventoryNode node;
                if (Items.TryGetValue(item.UUID, out node))
                {
                    if (node.Parent != null)
                        lock (node.Parent.Nodes.SyncRoot)
                            node.Parent.Nodes.Remove(item.UUID);
                    Items.Remove(item.UUID);
                    FireOnInventoryObjectRemoved(item);
                }

                // In case there's a new parent:
                InventoryNode newParent;
                if (Items.TryGetValue(item.ParentUUID, out newParent))
                {
                    lock (newParent.Nodes.SyncRoot)
                        newParent.Nodes.Remove(item.UUID);
                }
            }
        }

        /// <summary>
        /// Used to find out if Inventory contains the InventoryObject
        /// specified by <code>uuid</code>.
        /// </summary>
        /// <param name="uuid">The LLUUID to check.</param>
        /// <returns>true if inventory contains uuid, false otherwise</returns>
        public bool Contains(LLUUID uuid)
        {
            return Items.ContainsKey(uuid);
        }

        public bool Contains(InventoryBase obj)
        {
            return Contains(obj.UUID);
        }

        #region Event Firing
        protected void FireOnInventoryObjectUpdated(InventoryBase oldObject, InventoryBase newObject)
        {
            if (OnInventoryObjectUpdated != null)
                OnInventoryObjectUpdated(oldObject, newObject);
        }
        protected void FireOnInventoryObjectRemoved(InventoryBase obj)
        {
            if (OnInventoryObjectRemoved != null)
                OnInventoryObjectRemoved(obj);
        }
        #endregion

    }

    /// <summary>
    /// A rudimentary Exception subclass, so exceptions thrown by the Inventory class
    /// can be easily identified and caught.
    /// </summary>
    public class InventoryException : Exception
    {
        public InventoryException(string message)
            : base(message) { }
    }
}

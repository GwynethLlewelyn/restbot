/*--------------------------------------------------------------------------------
 LICENSE:
	 This file is part of the RESTBot Project.
 
	 RESTbot is free software; you can redistribute it and/or modify it under
	 the terms of the Affero General Public License Version 1 (March 2002)
 
	 RESTBot is distributed in the hope that it will be useful,
	 but WITHOUT ANY WARRANTY; without even the implied warranty of
	 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.	See the
	 Affero General Public License for more details.

	 You should have received a copy of the Affero General Public License
	 along with this program (see ./LICENSING) If this is missing, please 
	 contact alpha.zaius[at]gmail[dot]com and refer to 
	 <http://www.gnu.org/licenses/agpl.html> for now.
	 
	 Further changes by Gwyneth Llewelyn

 COPYRIGHT: 
	 RESTBot Codebase (c) 2007-2008 PLEIADES CONSULTING, INC
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

// inventory functions; based on TestClient.exe code
namespace RESTBot
{
	// recursively list inventory, starting from folder ID. If no folder ID given, start from root
	public class ListInventoryPlugin : StatefulPlugin
	{
		private UUID session;
		private Inventory Inventory;
		private InventoryManager Manager;
		
		public ListInventoryPlugin()
		{
			MethodName = "list_inventory";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}
		
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID folderID;
			DebugUtilities.WriteDebug("LI - Entering folder key parser");
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key"))
				{
					DebugUtilities.WriteDebug("LI - Attempting to parse from POST");
					check = UUID.TryParse(Parameters["key"].ToString().Replace("_"," "), out folderID);
					DebugUtilities.WriteDebug("LI - Succesfully parsed POST");
				}
				else
				{
					folderID = UUID.Zero; // start with root folder
					check = true;
				}

				if (check)	// means that we have a correctly parsed key OR no key
							//  which is fine too (attempts root folder)
				{
					DebugUtilities.WriteDebug("List Inventory - Entering loop");
								
					Manager = b.Client.Inventory;
					Inventory = Manager.Store;
	
					StringBuilder response = new StringBuilder();
					
					InventoryFolder startFolder = new InventoryFolder(folderID);
					
					if (folderID == UUID.Zero)
						startFolder = Inventory.RootFolder;

					PrintFolder(b, startFolder, response);
				
					DebugUtilities.WriteDebug("List Inventory - Complete");
					
					return "<inventory>" + response + "</inventory>\n";
				}
				else 
				{
					return "<error>parsekey</error>";
				}
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>" + e.Message + "</error>";
			}
		}

		private void PrintFolder(RestBot b, InventoryFolder f, StringBuilder result)
		{	  
			List<InventoryBase> contents = Manager.FolderContents(f.UUID, b.Client.Self.AgentID,
				true, true, InventorySortOrder.ByName, 3000);

			if (contents != null)
			{
				foreach (InventoryBase i in contents)
				{
					result.AppendFormat("<item><name>{0}</name><itemid>{1}</itemid></item>", i.Name, i.UUID);
					if (i is InventoryFolder)
					{
						InventoryFolder folder = (InventoryFolder)i;
						PrintFolder(b, folder, result);
					}
				}
			}
		}

	}
	
	// List inventory item; params are (inventory) item ID
	public class ListItemPlugin : StatefulPlugin
	{
		private UUID session;
		
		public ListItemPlugin()
		{
			MethodName = "list_item";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}
		
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID itemID;
			DebugUtilities.WriteDebug("List Item - Entering key parser");
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key"))
				{
					DebugUtilities.WriteDebug("LI - Attempting to parse from POST");
					check = UUID.TryParse(Parameters["key"].ToString().Replace("_"," "), out itemID);
					DebugUtilities.WriteDebug("LI - Succesfully parsed POST");
				}
				else
				{
					return "<error>parsekey</error>";
				}

				if (check)	// means that we have a correctly parsed key	
				{
					DebugUtilities.WriteDebug("List Item - Fetching " + itemID.ToString());
						
					InventoryItem oneItem;
						
					oneItem = b.Client.Inventory.FetchItem(itemID, b.Client.Self.AgentID, 5000);
					// to-do: catch timeout explicitly
					
					if (oneItem == null)
					{
						return "<error>item " + itemID.ToString() + " not found</error>\n";
					}
					
					string response;
					
					response  = "<AssetUUID>" + oneItem.AssetUUID.ToString() + "</AssetUUID>";
					response += "<PermissionsOwner>" + PermMaskString(oneItem.Permissions.OwnerMask) + "</PermissionsOwner>";
					response += "<PermissionsGroup>" + PermMaskString(oneItem.Permissions.GroupMask) + "</PermissionsGroup>";					response += "<AssetType>" + oneItem.AssetType.ToString() + "</AssetType>";
					response += "<InventoryType>" + oneItem.InventoryType.ToString() + "</InventoryType>";
					response += "<CreatorID>" + oneItem.CreatorID.ToString() + "</CreatorID>";
					response += "<Description>" + oneItem.Description.ToString() + "</Description>";
					response += "<GroupID>" + oneItem.GroupID.ToString() + "</GroupID>";
					response += "<GroupOwned>" + oneItem.GroupOwned.ToString() + "</GroupOwned>";
					response += "<SalePrice>" + oneItem.SalePrice.ToString() + "</SalePrice>";
					response += "<SaleType>" + oneItem.SaleType.ToString() + "</SaleType>";
					response += "<Flags>" + oneItem.Flags.ToString() + "</Flags>";
					response += "<CreationDate>" + oneItem.CreationDate.ToString() + "</CreationDate>";
					response += "<LastOwnerID>" + oneItem.LastOwnerID.ToString() + "</LastOwnerID>";
						
					return "<item>" + response + "</item>\n";
				}
				else 
				{
					return "<error>parsekey</error>";
				}
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>" + e.Message + "</error>";
			}
		}

		/// <summary>
		/// Returns a 3-character summary of the PermissionMask
		/// CMT if the mask allows copy, mod and transfer
		/// -MT if it disallows copy
		/// --T if it only allows transfer
		/// --- if it disallows everything
		/// </summary>
		/// <param name="mask"></param>
		/// <returns></returns>
		private static string PermMaskString(PermissionMask mask)
		{
			string str = "";
			if (((uint)mask | (uint)PermissionMask.Copy) == (uint)PermissionMask.Copy)
				str += "C";
			else
				str += "-";
			if (((uint)mask | (uint)PermissionMask.Modify) == (uint)PermissionMask.Modify)
				str += "M";
			else
				str += "-";
			if (((uint)mask | (uint)PermissionMask.Transfer) == (uint)PermissionMask.Transfer)
				str += "T";
			else
				str += "-";
			return str;
		}
	} // end give_inventory
	
		// Give inventory item; params are (inventory) itemID and avatarKey
	public class GiveItemPlugin : StatefulPlugin
	{
		private UUID session;
		
		public GiveItemPlugin()
		{
			MethodName = "give_item";
		}
		
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}
		
		public override string Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID itemID, avatarKey;
			InventoryManager Manager;
        
			DebugUtilities.WriteDebug("Give Item - Entering key parser");
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("itemID"))
				{
					DebugUtilities.WriteDebug("GI - Attempting to parse from POST");
					check = UUID.TryParse(Parameters["itemID"].ToString().Replace("_"," "), out itemID);
					
					
					if (check)
					{
						check = UUID.TryParse(Parameters["avatarKey"].ToString().Replace("_"," "), out avatarKey);
						DebugUtilities.WriteDebug("GI - Succesfully parsed POST");
					}
					else
					{
						return "<error>parsekey itemID</error>";
					}
				}
				else
				{
					return "<error>parsekey</error>";
				}

				if (check)	// means that we have a correctly parsed key	
				{
					DebugUtilities.WriteDebug("Give Item " + itemID.ToString() + " to avatar" + avatarKey.ToString());
					
					// Extract item information from inventory
					InventoryItem oneItem;
						
					oneItem = b.Client.Inventory.FetchItem(itemID, b.Client.Self.AgentID, 5000);
					// to-do: catch timeout explicitly
					
					if (oneItem == null)
					{
						return "<error>item " + itemID.ToString() + " not found</error>\n";
					}
					
					// attempt to send it to the avatar
					
					Manager = b.Client.Inventory;
            							
					Manager.GiveItem(oneItem.UUID, oneItem.Name, oneItem.AssetType, avatarKey, false);
						
					return "<item><name>" + oneItem.Name + "</name><assetType>" + oneItem.AssetType + "</assetType><itemID>" + itemID.ToString() + "</itemID><avatarKey>" + avatarKey.ToString() + "</avatarKey></item>\n";
				}
				else 
				{
					return "<error>parsekey avatarKey</error>";
				}
			}
			catch ( Exception e )
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>" + e.Message + "</error>";
			}
		}
	} // end give item
}
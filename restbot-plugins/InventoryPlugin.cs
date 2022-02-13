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
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using OpenMetaverse;
using OpenMetaverse.Assets;
using OpenMetaverse.Packets;

// inventory functions; based on TestClient.exe code
namespace RESTBot
{
	// recursively list inventory, starting from folder ID. If no folder ID given, start from root
	public class ListInventoryPlugin : StatefulPlugin
	{
		private UUID session;

		private Inventory? Inventory; // may be null if avatar has no inventory

		private InventoryManager? Manager; // may be null as well...

		public ListInventoryPlugin()
		{
			MethodName = "list_inventory";
		}

		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}

		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID folderID;
			DebugUtilities.WriteDebug("LI - Entering folder key parser");
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key"))
				{
					DebugUtilities.WriteDebug("LI - Attempting to parse from POST");
					check =
						UUID
							.TryParse(Parameters["key"].ToString().Replace("_", " "),
							out folderID);
					DebugUtilities.WriteDebug("LI - Succesfully parsed POST");
				}
				else
				{
					folderID = UUID.Zero; // start with root folder
					check = true;
				}

				if (
					check // means that we have a correctly parsed key OR no key
				)
				//  which is fine too (attempts root folder)
				{
					DebugUtilities.WriteDebug("List Inventory - Entering loop");

					Manager = b.Client.Inventory;
					Inventory = Manager.Store;

					StringBuilder response = new StringBuilder();

					InventoryFolder startFolder = new InventoryFolder(folderID);

					if (folderID == UUID.Zero) startFolder = Inventory.RootFolder;

					PrintFolder (b, startFolder, response);

					DebugUtilities.WriteDebug("List Inventory - Complete");

					return "<inventory>" + response + "</inventory>\n";
				}
				else
				{
					return "<error>parsekey</error>";
				}
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>" + e.Message + "</error>";
			}
		}

		private void PrintFolder(RestBot b, InventoryFolder f, StringBuilder result)
		{
			List<InventoryBase>? contents =
				Manager?
					.FolderContents(f.UUID,
					b.Client.Self.AgentID,
					true,
					true,
					InventorySortOrder.ByName,
					3000);

			if (contents != null)
			{
				foreach (InventoryBase i in contents)
				{
					result
						.AppendFormat("<item><name>{0}</name><itemid>{1}</itemid></item>",
						i.Name,
						i.UUID);
					if (i is InventoryFolder)
					{
						InventoryFolder folder = (InventoryFolder) i;
						PrintFolder (b, folder, result);
					}
				}
			}
		}
	}

	/// <summary>
	/// List inventory item; params are (inventory) item ID
	/// </summary>
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

		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID itemID;
			DebugUtilities.WriteDebug("List Item - Entering key parser");
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("key"))
				{
					DebugUtilities.WriteDebug("LI - Attempting to parse from POST");
					check =
						UUID
							.TryParse(Parameters["key"].ToString().Replace("_", " "),
							out itemID);
					DebugUtilities.WriteDebug("LI - Succesfully parsed POST");
				}
				else
				{
					return "<error>parsekey</error>";
				}

				if (
					check // means that we have a correctly parsed key
				)
				{
					DebugUtilities
						.WriteDebug("List Item - Fetching " + itemID.ToString());

					InventoryItem oneItem;

					oneItem =
						b.Client.Inventory.FetchItem(itemID, b.Client.Self.AgentID, 5000);

					// to-do: catch timeout explicitly
					if (oneItem == null)
					{
						return "<error>item " + itemID.ToString() + " not found</error>\n";
					}

					string response;

					response =
						"<AssetUUID>" + oneItem.AssetUUID.ToString() + "</AssetUUID>";
					response +=
						"<PermissionsOwner>" +
						PermMaskString(oneItem.Permissions.OwnerMask) +
						"</PermissionsOwner>";
					response +=
						"<PermissionsGroup>" +
						PermMaskString(oneItem.Permissions.GroupMask) +
						"</PermissionsGroup>";
					response +=
						"<AssetType>" + oneItem.AssetType.ToString() + "</AssetType>";
					response +=
						"<InventoryType>" +
						oneItem.InventoryType.ToString() +
						"</InventoryType>";
					response +=
						"<CreatorID>" + oneItem.CreatorID.ToString() + "</CreatorID>";
					response +=
						"<Description>" + oneItem.Description.ToString() + "</Description>";
					response += "<GroupID>" + oneItem.GroupID.ToString() + "</GroupID>";
					response +=
						"<GroupOwned>" + oneItem.GroupOwned.ToString() + "</GroupOwned>";
					response +=
						"<SalePrice>" + oneItem.SalePrice.ToString() + "</SalePrice>";
					response +=
						"<SaleType>" + oneItem.SaleType.ToString() + "</SaleType>";
					response += "<Flags>" + oneItem.Flags.ToString() + "</Flags>";
					response +=
						"<CreationDate>" +
						oneItem.CreationDate.ToString() +
						"</CreationDate>";
					response +=
						"<LastOwnerID>" + oneItem.LastOwnerID.ToString() + "</LastOwnerID>";

					return "<item>" + response + "</item>\n";
				}
				else
				{
					return "<error>parsekey</error>";
				}
			}
			catch (Exception e)
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
			if (
				((uint) mask | (uint) PermissionMask.Copy) == (uint) PermissionMask.Copy
			)
				str += "C";
			else
				str += "-";
			if (
				((uint) mask | (uint) PermissionMask.Modify) ==
				(uint) PermissionMask.Modify
			)
				str += "M";
			else
				str += "-";
			if (
				((uint) mask | (uint) PermissionMask.Transfer) ==
				(uint) PermissionMask.Transfer
			)
				str += "T";
			else
				str += "-";
			return str;
		}
	} // end give_inventory

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

		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID
				itemID,
				avatarKey;
			InventoryManager Manager;

			DebugUtilities.WriteDebug("Give Item - Entering key parser");
			try
			{
				bool check = false;
				if (Parameters.ContainsKey("itemID"))
				{
					DebugUtilities.WriteDebug("GI - Attempting to parse from POST");
					check =
						UUID
							.TryParse(Parameters["itemID"].ToString().Replace("_", " "),
							out itemID);

					if (check)
					{
						check =
							UUID
								.TryParse(Parameters["avatarKey"].ToString().Replace("_", " "),
								out avatarKey);
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

				if (
					check // means that we have a correctly parsed key
				)
				{
					DebugUtilities
						.WriteDebug("Give Item " +
						itemID.ToString() +
						" to avatar" +
						avatarKey.ToString());

					// Extract item information from inventory
					InventoryItem oneItem;

					oneItem =
						b.Client.Inventory.FetchItem(itemID, b.Client.Self.AgentID, 5000);

					// to-do: catch timeout explicitly
					if (oneItem == null)
					{
						return "<error>item " + itemID.ToString() + " not found</error>\n";
					}

					// attempt to send it to the avatar
					Manager = b.Client.Inventory;

					Manager
						.GiveItem(oneItem.UUID,
						oneItem.Name,
						oneItem.AssetType,
						avatarKey,
						false);

					return "<item><name>" +
					oneItem.Name +
					"</name><assetType>" +
					oneItem.AssetType +
					"</assetType><itemID>" +
					itemID.ToString() +
					"</itemID><avatarKey>" +
					avatarKey.ToString() +
					"</avatarKey></item>\n";
				}
				else
				{
					return "<error>parsekey avatarKey</error>";
				}
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>" + e.Message + "</error>";
			}
		}
	} // end give item

	/// <summary>
	/// Class to create notecards; params are notecard name, notecard (textual data) and optionally a key (item UUID) to embed an item inside the notecard
	/// </summary>
	/// <param name="name"></param>
	/// <param name="notecard"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public class CreateNotecardPlugin : StatefulPlugin
	{
		private UUID session;

		const int NOTECARD_CREATE_TIMEOUT = 1000 * 10;

		const int NOTECARD_FETCH_TIMEOUT = 1000 * 10;

		const int INVENTORY_FETCH_TIMEOUT = 1000 * 10;

		public CreateNotecardPlugin()
		{
			MethodName = "create_notecard";
		}

		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug(session + " " + MethodName + " startup");
		}

		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			UUID embedItemID = UUID.Zero;
			string
				notecardName,
				notecardData;

			DebugUtilities.WriteDebug("Create Notecard - Entering key parser");
			try
			{
				// item ID to embed is optional; handle later
				if (Parameters.ContainsKey("key"))
				{
					UUID
						.TryParse(Parameters["key"].ToString().Replace("_", " "),
						out embedItemID);
				}

				// notecard data is required!
				if (Parameters.ContainsKey("notecard"))
				{
					DebugUtilities
						.WriteDebug("CN - Attempting to parse notecard data from POST");
					notecardData = Parameters["notecard"];
				}
				else
				{
					return "<error>notecard text data not found</error>";
				}

				// notecard name is optional, we'll assign a random name
				if (Parameters.ContainsKey("name"))
				{
					DebugUtilities
						.WriteDebug("CN - Attempting to parse notecard name from POST");
					notecardName = Parameters["name"];

					DebugUtilities.WriteDebug("CN - Succesfully parsed POST");
				}
				else
				{
					notecardName = "(no name)";
				}

				UUID
					notecardItemID = UUID.Zero,
					notecardAssetID = UUID.Zero;
				bool
					success = false,
					finalUploadSuccess = false;
				string message = String.Empty;
				AutoResetEvent notecardEvent = new AutoResetEvent(false);

				DebugUtilities
					.WriteDebug("Notecard data ('" +
					notecardName +
					"') found: '" +
					notecardData +
					"'");


#region Notecard asset data

				AssetNotecard notecard = new AssetNotecard();
				notecard.BodyText = notecardData;

				// Item embedding
				if (embedItemID != UUID.Zero)
				{
					// Try to fetch the inventory item
					InventoryItem? item = FetchItem(b, embedItemID);
					if (item != null)
					{
						notecard.EmbeddedItems = new List<InventoryItem> { item };
						notecard.BodyText += (char) 0xdbc0 + (char) 0xdc00;
					}
					else
					{
						return "Failed to fetch inventory item " + embedItemID;
					}
				}

				notecard.Encode();


#endregion Notecard asset data


				b
					.Client
					.Inventory
					.RequestCreateItem(b
						.Client
						.Inventory
						.FindFolderForType(AssetType.Notecard),
					notecardName,
					notecardName + " created by LibreMetaverse RESTbot " + DateTime.Now,
					AssetType.Notecard,
					UUID.Random(),
					InventoryType.Notecard,
					PermissionMask.All,
					delegate (bool createSuccess, InventoryItem item)
					{
						if (createSuccess)
						{
#region Upload an empty notecard asset first

							AutoResetEvent emptyNoteEvent = new AutoResetEvent(false);
							AssetNotecard empty = new AssetNotecard();
							empty.BodyText = "\n";
							empty.Encode();

							b
								.Client
								.Inventory
								.RequestUploadNotecardAsset(empty.AssetData,
								item.UUID,
								delegate (
									bool uploadSuccess,
									string status,
									UUID itemID,
									UUID assetID)
								{
									notecardItemID = itemID;
									notecardAssetID = assetID;
									success = uploadSuccess;
									message = status ?? "Unknown error uploading notecard asset";
									emptyNoteEvent.Set();
								});

							emptyNoteEvent.WaitOne(NOTECARD_CREATE_TIMEOUT, false);


#endregion Upload an empty notecard asset first


							if (success)
							{
								// Upload the actual notecard asset
								b
									.Client
									.Inventory
									.RequestUploadNotecardAsset(notecard.AssetData,
									item.UUID,
									delegate (
										bool uploadSuccess,
										string status,
										UUID itemID,
										UUID assetID)
									{
										notecardItemID = itemID;
										notecardAssetID = assetID;
										finalUploadSuccess = uploadSuccess;
										message =
											status ?? "Unknown error uploading notecard asset";
										notecardEvent.Set();
									});
							}
							else
							{
								notecardEvent.Set();
							}
						}
						else
						{
							message = "Notecard item creation failed";
							notecardEvent.Set();
						}
					}); // end delegate // end RequestCreateItem

				notecardEvent.WaitOne(NOTECARD_CREATE_TIMEOUT, false);

				// DebugUtilities.WriteDebug("Notecard possibly created, ItemID " + notecardItemID + " AssetID " + notecardAssetID + " Content: '" + DownloadNotecard(b, notecardItemID, notecardAssetID) + "'");
				if (finalUploadSuccess)
				{
					DebugUtilities
						.WriteDebug("Notecard successfully created, ItemID " +
						notecardItemID +
						" AssetID " +
						notecardAssetID +
						" Content: '" +
						DownloadNotecard(b, notecardItemID, notecardAssetID) +
						"'");
					return "<notecard><ItemID>" +
					notecardItemID +
					"</ItemID><AssetID>" +
					notecardAssetID +
					"</AssetID><name>" +
					notecardName +
					"</name></notecard>";
				}
				else
				{
					return "<error>Notecard creation failed: " + message + "</error>";
				}
			} // end try
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return "<error>" + e.Message + "</error>";
			}
		}

		// Accessory functions
		/// <summary>
		/// Fetch an inventory item
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="itemID">UUID of inventory item to fetch</param>
		/// <returns>An InventoryItem object if it exists, null if not</returns>
		/// <remarks>C# 8+ is stricter when returning nulls, thus the <c>InventoryItem?</c> method type.</remarks>
		private InventoryItem? FetchItem(RestBot b, UUID itemID)
		{
			InventoryItem? fetchItem = null;
			AutoResetEvent fetchItemEvent = new AutoResetEvent(false);

			EventHandler<ItemReceivedEventArgs> itemReceivedCallback =
				delegate (object? sender, ItemReceivedEventArgs e)
				{
					if (e.Item.UUID == itemID)
					{
						fetchItem = e.Item;
						fetchItemEvent.Set();
					}
				};

			b.Client.Inventory.ItemReceived += itemReceivedCallback;

			b.Client.Inventory.RequestFetchInventory(itemID, b.Client.Self.AgentID);

			fetchItemEvent.WaitOne(INVENTORY_FETCH_TIMEOUT, false);

			b.Client.Inventory.ItemReceived -= itemReceivedCallback;

			return fetchItem;
		}

		/// <summary>
		/// Download a notecard.
		/// </summary>
		/// <param name="b">RESTbot object</param>
		/// <param name="itemID">ID of the notecard in inventory</param>
		/// <param name="assetID">Asset ID of the notecard</param>
		private string DownloadNotecard(RestBot b, UUID itemID, UUID assetID)
		{
			AutoResetEvent assetDownloadEvent = new AutoResetEvent(false);
			byte[]? notecardData = null;
			string error = "Timeout";

			b
				.Client
				.Assets
				.RequestInventoryAsset(assetID,
				itemID,
				UUID.Zero,
				b.Client.Self.AgentID,
				AssetType.Notecard,
				true,
				UUID.Zero,
				delegate (AssetDownload transfer, Asset asset)
				{
					if (transfer.Success)
					{
						notecardData = transfer.AssetData;
					}
					else
					{
						error = transfer.Status.ToString();
					}

					assetDownloadEvent.Set();
				});

			assetDownloadEvent.WaitOne(NOTECARD_FETCH_TIMEOUT, false);

			if (notecardData != null)
				return Encoding.UTF8.GetString(notecardData);
			else
				return "Error downloading notecard asset: " + error;
		}
	} // end create notecard
} // end namespace

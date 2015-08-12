# Introduction #

The original instructions are minimalistic, they just deal with establishing a connection and exiting a bot.

Except for establishing a connection, the _original_ REST API is as following:

Call ` http://your-host-where-RESTbot-lives.com:9080/<command_name>/<session_id> `

while on the POST data you put the actual parameters

Establishing a connection will give you the ` <session_id> `

# Establishing a Connection #

using cURL:

` curl http://localhost:9080/establish_session/<RESTbot password> -d first=<BotFirstName> -d last=<BotLastName> -d pass=<md5'ed bot password> `

RESTbot password: the default is simply `pass`.

This will return a _session identification key_.

# Exiting #

` curl http://localhost:9080/exit/<session_id> `

where session\_id is the session identification key received on the previous call.

Returns:

```
<restbot><disposed>true</disposed></restbot>
```

# Avatar Plugin #

## avatar\_name ##

Parameters: key

` curl http://localhost:9080/avatar_key/<session_id>/ -d key=d2cdf457-5027-4887-abd8-573c62a85226 `

Returns:

```
<restbot><name>Gwyneth Llewelyn</name>
</restbot>
```

## avatar\_key ##

Parameters: name

` curl http://localhost:9080/avatar_key/<session_id>/ -d name=Gwyneth%20Llewelyn `

Returns:

```
<restbot><key>d2cdf457-5027-4887-abd8-573c62a85226</key>
</restbot>
```

## avatar\_online ##

Parameters: key

` curl http://localhost:9080/avatar_online/<session_id>/ -d key=d2cdf457-5027-4887-abd8-573c62a85226 `

Returns:

```
<restbot><online>False</online>
</restbot>
```

## avatar\_profile ##

Parameters: key

` curl http://localhost:9080/avatar_profile/<session_id>/ -d key=d2cdf457-5027-4887-abd8-573c62a85226 `

Returns:

```
<restbot><profile>
<publish>True</publish>
<firstlife>
<text>In RL I work for Beta Technologies: http://betatechnologies.info

What should I say more? I'm 40 and in a stable relationship, currently living in Portugal, at Europe's westernmost tip.
</text>
<image>0488b909-b5d7-dd58-fe96-600a801d6c6f</image>
</firstlife>
<partner>00000000-0000-0000-0000-000000000000</partner>
<born>7/31/2004</born>
<about>I'm accused by immersionists of being an augmentist; and augmentists believe I'm an immersionist. But I'm really just a virtual girl in a virtual world, enjoying this space just like you :)

Second Life mirrors the real life. The good and the bad of it.

Email me at gwyneth.llewelyn@gwynethllewelyn.net

... and in real life, I'm the European Operations Manager for Beta Technologies.

Joaz Janus: A rose by any other name would render so slowly

Distorted image? Well, I'm using a viewer that likes it!</about>
<charter>Resident</charter>
<profileimage>71416a9e-25a2-6e95-f96f-31bfb88ea7bc</profileimage>
<mature>False</mature>
<identified>True</identified>
<transacted>True</transacted>
<url>http://gwynethllewelyn.net/</url>
</profile>
</restbot>
```

## avatar\_groups ##

Parameters: key

` curl http://localhost:9080/avatar_profile/<session_id>/ -d key=d2cdf457-5027-4887-abd8-573c62a85226 `

Returns:

```
<restbot><groups>
<group>
<name>Fashion Consolidated</name>
<key>0726f59a-ca0c-9e73-7c1e-075c5242eb5b</key>
<title>FashConnoisseur</title>
<notices>True</notices>
<powers>SendNotices, ReceiveNotices, VoteOnProposal</powers>
<insignia>349d1efa-176d-9995-a3f2-ffd4f61cc0e3</insignia>
</group>
<group>
<name>Neufreistadt</name>
<key>1436766e-2212-1062-fe69-f43b0095c890</key>
<title>Neufreistadter</title>
<notices>True</notices>
<powers>18446744073709551615</powers>
<insignia>31c58113-f7e7-f282-acf8-6265acab8285</insignia>
</group>
<group>
...
</group>
</groups>
</restbot>
```

# Reaper Plugin #

## reaper\_info ##

Function unknown. Seems to have no parameters.

Apparently is tied to an internal garbage collector _or_ deals with specific applications for Pleiades.

# Stats Plugin #

## dilation ##

` curl http://localhost:9080/dilation/<session_id> `

Returns:

```
<restbot><dilation>0,9987054</dilation>
</restbot>
```

where the value is the current sim's dilation.

## sim\_stat ##

` curl http://localhost:9080/sim_stat/<session_id> `

Returns:

```
<restbot><stats>
<dilation>0,9964779</dilation>
<inbps>459</inbps>
<outbps>210</outbps>
<resentout>0</resentout>
<resentin>0</resentin>
<queue>0</queue>
<fps>45</fps>
<physfps>44,90754</physfps>
<agentupdates>2</agentupdates>
<objects>82</objects>
<scriptedobjects>2</scriptedobjects>
<agents>2</agents>
<childagents>0</childagents>
<activescripts>20</activescripts>
<lslips>0</lslips>
<inpps>31</inpps>
<outpps>29</outpps>
<pendingdownloads>0</pendingdownloads>
<pendinguploads>0</pendinguploads>
<virtualsize>0</virtualsize>
<residentsize>0</residentsize>
<pendinglocaluploads>0</pendinglocaluploads>
<unackedbytes>106</unackedbytes>
<time>
<frame>22,24652</frame>
<image>0,009069572</image>
<physics>0,2268104</physics>
<script>0,1629531</script>
<other>0,1085776</other>
</time>
</stats>
</restbot>
```

# New Functionality #

The commands below are new and manually added.

# Groups Plugin #

## group\_key\_activate ##

Used to set the avatar to an active group, using the group UUID. Returns active group and, in parenthesis, the group's role that is shown on the title.

Can be set to NULL key (00000000-0000-0000-0000-000000000000) to unset group.

` curl http://localhost:9080/group_key_activate/<session_id> -d key=150829ee-a295-4ef6-3965-9f5c77ac3b52 `

Returns:

```
<restbot><active>Beta Technologies (Beta Technologies)</active>
</restbot>
```

## group\_name\_activate ##

Used to set the avatar to an active group, using the group name. Returns active group and, in parenthesis, the group's role that is shown on the title.


` curl http://localhost:9080/group_name_activate/<session_id> -d name=Beta%20Technologies `

Returns:

```
<restbot><active>Beta Technologies (Beta Technologies)</active>
</restbot>
```

## group\_im ##

Sends messages in Group IM Chat (on any group the 'bot has joined). Returns just if the message was sent or not.

Parameters are the UUID key for the group and a text message (urlencoded)

` curl http://localhost:9080/group_im/<session_id> -d key=<group UUID> -d message="This%20is%20a%20test" `

Returns:

```
<restbot><message>message sent</message></restbot>
```

# Inventory Plugin #

## list\_inventory ##

Returns the whole inventory of this avatar. With an empty key, it starts from the root folder; add a folder key to just list that folder instead

Parameters: key <folder UUID> for the folder where it should start

` curl http://localhost:9080/list_inventory/<session_id> -d key=<folder UUID>`

Returns:

```
<restbot><inventory><item><name>...</name><itemid>...</itemid></item>...</inventory></restbot>
```

## list\_item ##

Returns information from a single item in the inventory of this avatar.

Parameters: key <key UUID> for the item ID

` curl http://localhost:9080/list_item/<session_id> -d key=<item ID>`

Returns:

```
<restbot><item><AssetUUID>251ff15a-25c5-ebd7-ea13-c45e4e520f23</AssetUUID><PermissionsOwner>---</PermissionsOwner><PermissionsGroup>---</PermissionsGroup><AssetType>Texture</AssetType><InventoryType>Texture</InventoryType><CreatorID>d2cdf457-5027-4887-abd8-573c62a85226</CreatorID><Description>(No Description)</Description><GroupID>00000000-0000-0000-0000-000000000000</GroupID><GroupOwned>False</GroupOwned><SalePrice>10</SalePrice><SaleType>Not</SaleType><Flags>0</Flags><CreationDate>19-06-2009 18:20:35</CreationDate><LastOwnerID>00000000-0000-0000-0000-000000000000</LastOwnerID></item>
```

## give\_item ##

Transfers a single item from the inventory of this avatar to another avatar.

Parameters: itemID <key UUID> for the item ID to give

avatarKey <avatar UUID> for the recipient's avatar UIID

` curl http://localhost:9080/list_item/<session_id> -d itemID=<item ID> -d avatarKey=<avatar UUID>`

Returns:

```
<restbot><item><name>BetaTech_OV_RGB</name><assetType>Texture</assetType><itemID>6e89002b-6788-ecc5-c314-95f795b08cd8</itemID><avatarKey>d2cdf457-5027-4887-abd8-573c62a85226</avatarKey></item>
</restbot>
```

## create\_notecard ##

Creates a notecard on the 'bot's inventory with name, text, and optionally an attachment (for some reasons the attachment seems to be broken)

Parameters: name (name for the notecard; if empty, a default name will be selected)

notecard (text for the notecard's content; not tested with UTF-8 and special formatting yet)

key (itemID for the attachment)

` curl http://localhost:9080/create_notecard/<session_id> -d name=<string> -d notecard=<string> -d key=<item UUID> `

Returns:

```
<restbot><notecard><ItemID>f2890f40-44ba-d123-0709-ec73ca42f237</ItemID><AssetID>093dea8c-8231-8515-decc-0f13ee0abada</AssetID><name>Notecard name</name></notecard></restbot>
```

# Movement Plugin #

## location ##

Shows the current simulator name and actual position of the 'bot.

Parameters: none

` curl http://localhost:9080/location/<session_id> `

Returns:

```
<restbot><location><CurrentSim>Beta Technologies (216.82.45.92:13000)</CurrentSim><Position><103.0242, 211.2815, 30.03234></Position></restbot>
```

## goto ##

Teleports 'bot to a specific simulator and a position (x,y,z) inside that sim. Position coordinates are floats.

Parameters: sim <a text string> for the simulator name (spaces should work fine if properly quoted)

x, y, z <each is a float> Position inside the simulator to teleport to

` curl http://localhost:9080/goto/<session_id> -d sim=<string> -d x=<float> -d y=<float> -d z=<float> `

Returns:

```
<restbot><teleport>Beta Technologies</teleport></restbot>
```

# Prims Plugin #

Contributed by jonafree

## nearby\_prims ##

Gets a list of primitives in the neighbourhood of the 'bot, in a certain radius.

Parameters: type (it will do a partial search on the prim name; leave empty to retrieve all prims; it's case-sensitive)

radius (in meters around the avatar)

` curl http://localhost:9080/nearby_prims/<session_id> -d type=<string> -d radius=<float> `

Returns:

```
<restbot>
  <nearby_prims>
    <prim><name>{0}</name><pos>{1},{2},{3}</pos></prim>
  <nearby_prims>
</restbot>
```
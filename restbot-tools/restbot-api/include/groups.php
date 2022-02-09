<?php
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
--------------------------------------------------------------------------------*/
require_once "db.php";
require_once "funktions.php";
/*
function groupList($key) {
	global $debug;
	$result = rest("get_groups", "");
	if ( $result == null ) {
		logMessage('sl', 0, "Error retrieving group list for", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	$return = "";
	foreach ($xml->groups->group as $group) {
		$return += $group->key . "," . $group->name . ",";
	}
	return $return;
}*/

function groupInfo($key)
{
	global $debug;
	$result = rest("get_group_profile", "group=$key");
	if ($result == null) {
		logMessage("sl", 0, "Error retrieving group profile for $key", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	return $xml->groupprofile->name .
		"," .
		$xml->groupprofile->insignia .
		"," .
		$xml->groupprofile->maturepublish .
		"," .
		$xml->groupprofile->charter;
}

?>

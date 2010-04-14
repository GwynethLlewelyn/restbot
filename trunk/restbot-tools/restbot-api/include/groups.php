<?php
/*--------------------------------------------------------------------------------
 LICENSE:
     This file is part of the RESTBot Project.
 
     RESTbot is free software; you can redistribute it and/or modify it under
     the terms of the Affero General Public License Version 1 (March 2002)
 
     RESTBot is distributed in the hope that it will be useful,
     but WITHOUT ANY WARRANTY; without even the implied warranty of
     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
     Affero General Public License for more details.

     You should have received a copy of the Affero General Public License
     along with this program (see ./LICENSING) If this is missing, please 
     contact alpha.zaius[at]gmail[dot]com and refer to 
     <http://www.gnu.org/licenses/agpl.html> for now.

 COPYRIGHT: 
     RESTBot Codebase (c) 2007-2008 PLEIADES CONSULTING, INC
--------------------------------------------------------------------------------*/
require_once 'db.php';
require_once 'funktions.php';
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

function groupInfo($key) {
	global $debug;
	$result = rest("get_group_profile", "group=$key");
	if ( $result == null ) {
		logMessage('sl', 0, "Error retrieving group profile for $key", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	return  $xml->groupprofile->name . "," . $xml->groupprofile->insignia . "," . $xml->groupprofile->maturepublish . "," . $xml->groupprofile->charter;
}
	
?>

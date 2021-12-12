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
require_once 'db.php';
require_once 'funktions.php';

function avatarName($key) {
	global $debug;
	$cached = getFromCache('key2name', $key);
	if ( $cached['value'] != null ) {
		logMessage('rest', 3, 'Returning cache entry (' . ( time() - $cached['timestamp']). ')');
		return $cached['value'];
	} else {
		$avatarname = getAvatarName($key);
		if ( $avatarname == null ) {
			logMessage('rest', 3, 'Response not received.');
			$cached = getForceFromCache('regionhandle', $sim);
			if ( $cached != null ) {
				logMessage('db', 1, 'Returning old cache entry (' . ( time() - $cached['timestamp'])
				. ')');
				return $cached['value'];
			} else {
				logMessage('rest', 1, 'Failed to lookup avatar name for ' . $key);
				return null;
			}
		} else if ( $avatarname == "0" ) {
			logMessage('rest', 3, "Avatar name not found");
			return 0;
		} else {
			if ( existsInCache('key2name', $key) ) {
				updateInCache('key2name', $key, $avatarname);
			} else {
				putInCache('key2name', $key, $avatarname);
			}
			return $avatarname;
		}
	}
}
function avatarKey($name) {
	global $debug;
	$cached = getFromCache('name2key', $name);
	if ( $cached['value'] != null ) {
		return $cached['value'];
	} else {
		$avatarkey = getAvatarKey($name);
		if ( $avatarkey != null ) {
			if ( existsInCache('name2key', $name) ) {
				updateInCache('name2key', $name, $avatarkey);
			} else {
				putInCache('name2key', $name, $avatarkey);
			}
			return $avatarkey;
		} else {
			$cached = getForceFromCache('name2key', $name);
			if ( $cached['value'] == null ) {
				logMessage('rest', 3, 'Response not received.');
				return null;
			} else if ( $cached['value'] == 0 ) {
				logMessage('db', 1, 'Failed to lookup avatar name for ' . $key);
				return null;
			} else {
				logMessage('db', 1, 'Returning old cache entry (' . ( time() - $cached['timestamp']) . ')');
				return $cached['value'];
			}
		}
	}
}
function avatarProfilePic($key) {
	$imagekey = getAvatarProfilePic($key);
	return $imagekey;
}

function avatarGroupList($key) {
	return getAvatarGroupList($key);
}

function getAvatarName($avkey) {
	$result = rest("avatar_name", "key=$avkey");
	if ( $result == null ) {
		logMessage('sl', 0, "Error looking up Avatar Name for $avkey", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	return $xml->name;
}

function getAvatarKey($avatarname) {
	$result = rest("avatar_key", "name=$avatarname");
	if ( $result == null ) {
    logMessage('sl', 0, "Error looking up Avatar Key for $avatarname", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	if ( $xml->key != "00000000000000000000000000000000" ) {
		return $xml->key;
	} else {
		logMessage('sl', 1, "$avatarname does not exist", null, null);
		return "0";
	}
}

function getAvatarProfilePic($key) {
	$result = rest("avatar_profile", "key=$key");
	if ( $result == null ) {
		logMessage('sl', 0, "Error looking up profile for $key", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	return $xml->profile->profileimage;
}

function getAvatarGroupList($key) {
	$result = rest("avatar_groups", "key=$key");
	if ( $result == null ) {
		logMessage('sl', 0, "Error looking up profile for $key", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	$return = "";
	foreach ( $xml->groups->group as $group) {
		$return = $return . $group->name . "," . friendlyUUID($group->key) .",";
	}
	return $return;
}
function getAvatarGroupListDetailed($key) {
	$result = rest("avatar_groups", "key=$key");
	if ( $result == null ) {
		logMessage('sl', 0, "Error looking up profile for $key", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	$return = "";
	foreach ( $xml->groups->group as $group) {
		$return = $return . $group->key . "," . $group->name . "," . $group->title . "," . $group->notices . "," . $group->powers . "," . friendlyUUID($group->insignia) . ",";
	}
	return $return;
}
?>

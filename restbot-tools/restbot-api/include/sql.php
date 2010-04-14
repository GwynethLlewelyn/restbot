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

function restbotFromDB()
{
	$request = doQuery("SELECT restbots.id, restbots.hostname, restbots.session FROM restbots WHERE restbots.lock = 0 ORDER BY RAND() DESC LIMIT 1");
	if ( $request->numRows() != 1 ) {
		return null;
	} else {
		$session = $request->fetchRow(DB_FETCHMODE_ASSOC);
		return array( 'session' => $session['session'], 'hostname' => $session['hostname'], 'id' => $session['id'] );
	}
}
function restbotRemoveByID($botid)
{
	doQuery("DELETE FROM restbots WHERE restbots.id = $botid");
}
function restbotRemoveBySession($session)
{
	doQuery("DELETE FROM restbots WHERE restbots.session = '$session'");
}
function restbotAddToDB($session, $hostname)
{
	doQuery("INSERT INTO restbots VALUES(null, '$session','$hostname', 0, null)");
	return getSingle("SELECT restbots.id FROM restbots WHERE restbots.session = '$session'");
}
function sessionAlreadyExists($session)
{
	$bots = doQuery("SELECT restbots.id FROM restbots WHERE restbots.session = '$session'");
	if ( $bots->numRows() == 1 ) {
		return true;
	} else {
		return false;
	}
}
function lockBot($session)
{
	global $debug;
	if ( getSingle("SELECT restbots.lock FROM restbots WHERE restbots.session = '$session'") == 1 ) {
		return false;
	}
	if ( $debug ) { logMessage('rest', 3, "Locking session $session"); }
	if ( doUpdate("UPDATE restbots SET restbots.lock = 1, restbots.timestamp = null WHERE restbots.session = '$session'") == 1 ) {
		return true;
	} else {
		return false;
	}
}
function releaseBot($session)
{
	global $debug;
	if ( $debug ) { logMessage('rest', 3, "Unlocking session $session"); }
	doQuery("UPDATE restbots SET restbots.lock = 0 WHERE restbots.session = '$session'");
}
function restbotIsLocked($session)
{
	$locked = getSingle("SELECT restbots.lock FROM restbots WHERE restbots.session = '$session'");
	if ( $locked == 1 ) {
		return true;
	} else {
		return false;
	}
}

//////
function getFromCache($type, $key)
{
	global $cache_expire;
	$cache = getForceFromCache($type, $key);
	if ( ( ( time() - $cache['timestamp'] ) > $cache_expire ) || $cache == null) {
		return null;
	}
	return array( 'value' => $cache['value'] );
}
function getForceFromCache($type, $key)
{
	$result = doQuery("SELECT UNIX_TIMESTAMP(cache.time) AS timestamp, value, id FROM cache WHERE cache.key = '$key' AND cache.type = '$type'");
	if ( $result->numRows() == 0 ) {
		return null;
	}
	$cache = $result->fetchRow(DB_FETCHMODE_ASSOC);
	return array( 'value' => $cache['value'], 'timestamp' => $cache['timestamp'] );
}
function removeFromCache($type, $key)
{
	if ( $debug ) { logMessage('db', 3, "Removing $key from $type cache"); }
	doQuery("DELETE FROM cache WHERE cache.type = '$type' AND cache.key = '$key'");
}
function updateInCache($type, $key, $value)
{
	if ( $debug ) { logMessage('db', 3, "Updating $key in $type cache with $value"); }
	doQuery("UPDATE cache SET cache.value = '$value', cache.time = null WHERE cache.type = '$type' AND cache.key = '$key'");
}
function putInCache($type, $key, $value)
{
	if ( $debug ) { logMessagE('db', 3, "Inserting $key into $type cache with $value"); }
	doQuery("INSERT INTO cache VALUES(null, null, '$type', '$key', '$value')");
}
function existsInCache($type, $key)
{
	$q = getSingle("SELECT cache.id FROM cache WHERE cache.type = '$type' AND cache.key = '$key'");
	if ( $q == null ) {
		return false;
	} else {
		return true;
	}
}

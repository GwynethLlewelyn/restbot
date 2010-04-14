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
require_once 'DB.php';

function getConn() {
	global $dburi;
	$conn =& DB::connect($dburi);
	if ( DB::isError($conn)) {
		logMessage('db', 0,  $conn->getMessage() , null, null);
		genPipeError('db');
	} else {
		return $conn;
	}
}

function getSingle($sql) {
	global $debugdb;
	if ( $debugdb ) {
		logMessage('db', 3, "SQL - " . $sql);
	}
	$return = 0;
	$conn = getConn();
	$result = $conn->query($sql);
	if ( DB::isError($result)) {
		logMessage('db', 0, "Query Error " .  $result->getMessage() , null, null);
		genPipeError('db');
	}
	if ( $result->numRows() > 1 ) {
		return "MULTIPLE";
	}
	$row = $result->fetchRow();
	if ( count($row) == 1 ) {
		$return = $row[0];
	}
	return $return;
}

function doQuery($sql) {
	global $debugdb;
	if ( $debugdb ) {
		logMessage('db', 3, "SQL - " . $sql);
	}
	$conn = getConn();
	$result = $conn->query($sql);
	if ( DB::isError($result)) {
		logMessage('db', 0, "Query Error " .  $result->getMessage() , null, null);
		genPipeError('db');
	}
	return $result;
}

function doUpdate($sql) {
	global $debugdb;
	if ( $debugdb ) {
		logMessage('db', 3, "SQL - " . $sql);
	}
	$conn = getConn();
	$result = $conn->query($sql);
	if ( DB::isError($result)) {
		logMessage('db', 0, "Query Error " .  $result->getMessage() , null, null);
		genPipeError('db');
	}
	return $conn->affectedrows();
}


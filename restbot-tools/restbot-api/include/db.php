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
require_once "DB.php";

function getConn()
{
	global $dburi;
	$conn = &DB::connect($dburi);
	if (DB::isError($conn)) {
		logMessage("db", 0, $conn->getMessage(), null, null);
		genPipeError("db");
	} else {
		return $conn;
	}
}

function getSingle($sql)
{
	global $debugdb;
	if ($debugdb) {
		logMessage("db", 3, "SQL - " . $sql);
	}
	$return = 0;
	$conn = getConn();
	$result = $conn->query($sql);
	if (DB::isError($result)) {
		logMessage("db", 0, "Query Error " . $result->getMessage(), null, null);
		genPipeError("db");
	}
	if ($result->numRows() > 1) {
		return "MULTIPLE";
	}
	$row = $result->fetchRow();
	if (count($row) == 1) {
		$return = $row[0];
	}
	return $return;
}

function doQuery($sql)
{
	global $debugdb;
	if ($debugdb) {
		logMessage("db", 3, "SQL - " . $sql);
	}
	$conn = getConn();
	$result = $conn->query($sql);
	if (DB::isError($result)) {
		logMessage("db", 0, "Query Error " . $result->getMessage(), null, null);
		genPipeError("db");
	}
	return $result;
}

function doUpdate($sql)
{
	global $debugdb;
	if ($debugdb) {
		logMessage("db", 3, "SQL - " . $sql);
	}
	$conn = getConn();
	$result = $conn->query($sql);
	if (DB::isError($result)) {
		logMessage("db", 0, "Query Error " . $result->getMessage(), null, null);
		genPipeError("db");
	}
	return $conn->affectedrows();
}

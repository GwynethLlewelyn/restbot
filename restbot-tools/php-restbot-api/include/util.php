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

function regionHandle($sim)
{
//	global $debug;	// if not used, don't declare it! (gwyneth 20220422)
//	$cached = getFromCache("regionhandle", $key);	// this was on the original code; typo? (gwyneth 20220422)
	$cached = getFromCache("regionhandle", $sim);
	if ($cached["value"] != null) {
		return $cached["value"];
	} else {
		$handle = getRegionHandle($sim);
		if ($handle == null) {
			logMessage("rest", 3, "Response not received.");
			$cached = getForceFromCache("regionhandle", $sim);
			if ($cached != null) {
				logMessage(
					"db",
					1,
					"Returning old cache entry (" . (time() - $cached["timestamp"]) . ")"
				);
				return $cached["value"];
			} else {
				return null;
			}
		} elseif ($handle == 0) {
			return "0";
		} else {
			if (existsInCache("regionhandle", $sim)) {
				updateInCache("regionhandle", $sim, $handle);
			} else {
				putInCache("regionhandle", $sim, $handle);
			}
			return $handle;
		}
	}
}

function getRegionHandle($sim)
{
	$result = rest("region_handle", "region=$sim");
	if ($result == null) {
		logMessage("sl", 0, "Error looking up region handle for $sim", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	return $xml->handle;
}
function regionMap($sim)
{
	// global $debug; // if not used, don't include it (gwyneth 20220422)
//	$cached = getFromCache("regionmap", $key);	// probably a typo? (gwyneth 20220422)
	$cached = getFromCache("regionmap", $sim);
	if ($cached["value"] != null) {
		return $cached["value"];
	} else {
		$handle = getRegionMap($sim);
		if ($handle == null) {
			logMessage("rest", 3, "Response not received.");
			$cached = getForceFromCache("regionmap", $sim);
			if ($cached != null) {
				logMessage(
					"db",
					1,
					"Returning old cache entry (" . (time() - $cached["timestamp"]) . ")"
				);
				return $cached["value"];
			} else {
				return null;
			}
		} else {
			if (existsInCache("regionmap", $sim)) {
				updateInCache("regionmap", $sim, $handle);
			} else {
				putInCache("regionmap", $sim, $handle);
			}
			return $handle;
		}
	}
}

function getRegionMap($sim)
{
	$result = rest("region_image", "region=" . urlencode($sim));
	if ($result == null) {
		logMessage("sl", 0, "Error looking up region handle for $sim", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	return $xml->image;
}

?>

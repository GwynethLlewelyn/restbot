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

function regionHandle($sim) {
	global $debug;
	$cached = getFromCache('regionhandle', $key);
	if ( $cached['value'] != null ) {
		return $cached['value'];
	} else {
		$handle = getRegionHandle($sim);
		if ( $handle == null ) {
			logMessage('rest', 3, 'Response not received.');
			$cached = getForceFromCache('regionhandle', $sim);
			if ( $cached != null ) {
				logMessage('db', 1, 'Returning old cache entry (' . ( time() - $cached['timestamp']) . ')');
				return $cached['value'];
			} else {
				return null;
			}
		} else if ( $handle == 0 ) {
			return "0";
		} else {
			if ( existsInCache('regionhandle', $sim) ) {
				updateInCache('regionhandle', $sim, $handle);
			} else {
				putInCache('regionhandle', $sim, $handle);
			}
			return $handle;
		}
	}
}

function getRegionHandle($sim) {
	$result = rest("region_handle", "region=$sim");
	if ( $result == null ) {
		logMessage('sl', 0, "Error looking up region handle for $sim", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	return $xml->handle;
}
function regionMap($sim) {
	global $debug;
	$cached = getFromCache('regionmap', $key);
	if ( $cached['value'] != null ) {
		return $cached['value'];
	} else {
		$handle = getRegionMap($sim);
		if ( $handle == null ) {
			logMessage('rest', 3, 'Response not received.');
			$cached = getForceFromCache('regionmap', $sim);
			if ( $cached != null ) {
				logMessage('db', 1, 'Returning old cache entry (' . ( time() - $cached['timestamp']) . ')');
				return $cached['value'];
			} else {
				return null;
			}
		} else {
			if ( existsInCache('regionmap', $sim) ) {
				updateInCache('regionmap', $sim, $handle);
			} else {
				putInCache('regionmap', $sim, $handle);
			}
			return $handle;
		}
	}
}

function getRegionMap($sim) {
	$result = rest("region_image", "region=" . urlencode($sim));
	if ( $result == null ) {
		logMessage('sl', 0, "Error looking up region handle for $sim", null, null);
		return null;
	}
	$xml = new SimpleXMLElement($result);
	return $xml->image;
}


?>

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

function getGridStatus() {
	$url = "http://www.secondlife.com/status";
	if ( !@($web = file($url) ) ) {
		logMessage('sl', 0, 'Error looking up grid status', null, null);
		return "Unknown";
	}
	if ( ! $web ) {
		logMessage('sl', 0, 'Error looking up grid status', null, null);
		return "Unknown";
	}
	$data = implode("", $web);
	$findme = 'Open';
	preg_match_all ("/<h3>([^`]*?)<\/h3>/", $data, $matches);
	$result = $matches[0][0];
	//Force a fake "SL Grid is closed result for testing
	//$result = "Second life is offline";
	$pos = strpos($result, $findme);
	if ($pos == FALSE) {
		return "Closed";
	} else {
		return "Open";
	}
}
function gridStatus() {
	$gridStatus;
	$result = doQuery("SELECT UNIX_TIMESTAMP(state.time) AS timestamp, value, id FROM state WHERE state.key = 'gridStatus'");
	if ( $result->numRows() == 1 ) {
		$state = $result->fetchRow(DB_FETCHMODE_ASSOC);
		if ( ( time() - $state['timestamp'] ) > 300 ) {
			$gridStatus = getGridStatus();
#			print "Update " . $gridStatus;
			if ( $gridStatus != $state['value'] && $gridStatus != null ) {
				doQuery("UPDATE state SET time=null, value = '" . $gridStatus . "' WHERE id = " . $state['id']);
			}
		} else {
#			print "Cached " . $state['value'];
			$gridStatus = $state['value'];
		}
	} else {
		$gridStatus = getGridStatus();
		if ( $gridStatus != null ) {
#		print "New " . $gridStatus;
			doQuery("DELETE FROM state WHERE state.key = 'gridStatus'");
			doQuery("INSERT INTO state VALUES(null, null, null, 'gridStatus', '$gridStatus')");
		}
	}
	return $gridStatus;
}
?>

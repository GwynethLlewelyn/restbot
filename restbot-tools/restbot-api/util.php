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
require_once "include/funktions.php";
require_once "include/config.php";

require_once "include/avatars.php";
require_once "include/util.php";

global $ownername;
global $authuser;
if (
	$_SERVER["HTTP_X_SECONDLIFE_OWNER_NAME"] == $ownername ||
	$_REQUEST["psk"] == $authuser
) {
	$authorized = true;
}

$command = $_REQUEST["command"];

if ($command == "getkey") {
	if (!$authorized) {
		genPipeError("auth");
	}
	if (isset($_REQUEST["name"])) {
		if ($_REQUEST["base64"] == 1) {
			$name = base64_decode($_REQUEST["name"]);
		} else {
			$name = urldecode($_REQUEST["name"]);
		}
	} else {
		genPipeError("param");
	}
	$resp = avatarKey($name);
	if ($resp == null) {
		genPipeError("lookup");
	} elseif ($resp == "0") {
		genPipeError("found");
	} else {
		genPipeResponse($resp);
	}
} elseif ($command == "getname") {
	if (!$authorized) {
		genPipeError("auth");
	}
	if (isset($_REQUEST["key"])) {
		if ($_REQUEST["base64"] == 1) {
			$key = base64_decode($_REQUEST["key"]);
		} else {
			$key = urldecode($_REQUEST["key"]);
		}
	} else {
		genPipeError("param");
	}
	$resp = avatarName($key);
	if ($resp == null) {
		genPipeError("lookup");
	} elseif ($resp == "0") {
		genPipeError("found");
	} else {
		genPipeResponse($resp);
	}
} elseif ($command == "gethandle") {
	if (!$authorized) {
		genPipeError("auth");
	}
	if (isset($_REQUEST["sim"])) {
		if ($_REQUEST["base64"] == 1) {
			$sim = base64_decode($_REQUEST["sim"]);
		} else {
			$sim = urldecode($_REQUEST["sim"]);
		}
	} else {
		genPipeError("param");
	}
	$resp = regionHandle($sim);
	if ($resp == null) {
		genPipeError("lookup");
	} elseif ($resp == "0") {
		genPipeError("found");
	} else {
		genPipeResponse($resp);
	}
} elseif ($command == "getmap") {
	if (!$authorized) {
		genPipeError("auth");
	}
	if (isset($_REQUEST["sim"])) {
		if ($_REQUEST["base64"] == 1) {
			$sim = base64_decode($_REQUEST["sim"]);
		} else {
			$sim = urldecode($_REQUEST["sim"]);
		}
	} else {
		genPipeError("param");
	}
	$resp = regionMap($sim);
	if ($resp == null) {
		genPipeError("lookup");
	} elseif ($resp == "00000000000000000000000000000000") {
		genPipeResponse("3ab7e2fa-9572-ef36-1a30-d855dbea4f92");
	} else {
		genPipeResponse(friendlyUUID($resp));
	}
} else {
	genPipeError("param");
}

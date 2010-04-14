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
require_once 'include/funktions.php';
require_once 'include/config.php';

require_once 'include/avatars.php';
require_once 'include/util.php';

global $ownername ; global $authuser;
if ( 
	( $_SERVER['HTTP_X_SECONDLIFE_OWNER_NAME'] == $ownername ) ||
	( $_REQUEST['psk'] == $authuser ) 
	) {
	$authorized = true;
}

$command = $_REQUEST['command'];

if ( $command == "getkey" ) {
	if ( !$authorized ) {
		genPipeError('auth');
	}
	if ( isset($_REQUEST['name']) ) {
		if ( $_REQUEST['base64'] == 1 ) {
			$name = base64_decode($_REQUEST['name']);
		} else {
			$name = urldecode($_REQUEST['name']);
		}
	} else {
		genPipeError('param');
	}
	$resp = avatarKey($name);
	if ( $resp == null ) {
		genPipeError("lookup");
	} else if ( $resp == "0" ) {
		genPipeError("found");
	} else {
		genPipeResponse($resp);
	}
} else if ( $command == "getname" ) {
	if ( !$authorized) {
		genPipeError('auth');
	}
	if ( isset($_REQUEST['key']) ) {
		if ( $_REQUEST['base64'] == 1 ) {
			$key = base64_decode($_REQUEST['key']);
		} else {
			$key = urldecode($_REQUEST['key']);
		}
	} else {
		genPipeError('param');
	}
	$resp = avatarName($key);
	if ( $resp == null ) {
		genPipeError("lookup");
	} else if ( $resp == "0" ) {
		genPipeError("found");
	} else {
		genPipeResponse($resp);
	}
} else if ( $command == "gethandle" ) {
	if ( !$authorized ) {
		genPipeError('auth');
	}
	if ( isset($_REQUEST['sim']) ) {
		if ( $_REQUEST['base64'] == 1 ) {
			$sim = base64_decode($_REQUEST['sim']);
		} else {
			$sim = urldecode($_REQUEST['sim']);
		}
	} else {
		genPipeError('param');
	}
	$resp = regionHandle($sim);
	if ( $resp == null ) {
		genPipeError("lookup");
	} else if ( $resp == "0" ) {
		genPipeError("found");
	} else {
		genPipeResponse($resp);
	}
} else if ( $command == "getmap" ) {
	if ( !$authorized ) {
		genPipeError('auth');
	}
	if ( isset($_REQUEST['sim']) ) {
		if ( $_REQUEST['base64'] == 1 ) {
			$sim = base64_decode($_REQUEST['sim']);
		} else {
			$sim = urldecode($_REQUEST['sim']);
		}
	} else {
		genPipeError('param');
	}
	$resp = regionMap($sim);
	if ( $resp == null ) {
		genPipeError("lookup");
	} else if ( $resp == "00000000000000000000000000000000" ) {
		genPipeResponse("3ab7e2fa-9572-ef36-1a30-d855dbea4f92");
	} else {
		genPipeResponse(friendlyUUID($resp));
	}
} else {
 genPipeError('param');
}

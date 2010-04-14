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

if ( $command == "profilepic" ) {
	if ( !$authorized ) {
		genPipeError('auth');
	}
	if ( ! isset($_REQUEST['key']) ) {
		genPipeError('param');
	}
	$key = avatarProfilePic($_REQUEST['key']);
	if ( $key == null ) {
		genPipeError('lookup');
	} else {
		genPipeResponse(friendlyUUID($key));
	}
} else if ( $command == "grouplist" ) {
	if ( !$authorized ) {
		genPipeError('auth');
	}
	if ( ! isset($_REQUEST['key']) ) {
		genPipeError('param');
	}
	$list = avatarGroupList($_REQUEST['key']);
	genPipeResponse($list);

} else {
 genPipeError('param');
}

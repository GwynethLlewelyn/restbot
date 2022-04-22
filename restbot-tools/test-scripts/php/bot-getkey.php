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

// Don't forget to change th $url to reflect your configuration!

if ($argv[1] == null || $argv[2] == null || $argv[3] == null) {
	print "Usage " . $argv[0] . " session firstname lastname\n";
	exit(1);
}
$url = "http://127.0.0.1:9080/avatar_key/" . $argv[1] . "/";
$ch = curl_init($url);

curl_setopt($ch, CURLOPT_POST, true);
curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
curl_setopt($ch, CURLOPT_POSTFIELDS, "name=" . $argv[2] . " " . $argv[3]);
$stuff = curl_exec($ch);
curl_close($ch);
if (empty($stuff)) {
	print "Nothing returned from server\n";
	exit(2);
}
$xml = new SimpleXMLElement($stuff);
print $xml->key . "\n";

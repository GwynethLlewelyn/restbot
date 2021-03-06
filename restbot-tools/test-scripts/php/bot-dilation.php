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
if ( $argv[1] == null ) {
	print "Usage bot-dilation.php session\n";
	end ;
}
$url = "http://lumo.eghetto.ca:9080/dilation/" . $argv[1] . "/";
$ch = curl_init($url);

curl_setopt($ch, CURLOPT_POST, TRUE);
curl_setopt($ch,CURLOPT_RETURNTRANSFER,1);
$stuff = curl_exec($ch);
curl_close($ch);
if ( empty($stuff) ) {
	print "Nothing returned from server\n";
	end;
}
#print "$stuff";
$xml = new SimpleXMLElement($stuff);
print $xml->dilation . "\n";

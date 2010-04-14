//--------------------------------------------------------------------------------
// LICENSE:
//     This file is part of the RESTBot Project.
// 
//     RESTbot is free software; you can redistribute it and/or modify it under
//     the terms of the Affero General Public License Version 1 (March 2002)
// 
//     RESTBot is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     Affero General Public License for more details.
//
//     You should have received a copy of the Affero General Public License
//     along with this program (see ./LICENSING) If this is missing, please 
//     contact alpha.zaius[at]gmail[dot]com and refer to 
//     <http://www.gnu.org/licenses/agpl.html> for now.
//
// COPYRIGHT: 
//     RESTBot Codebase (c) 2007-2008 PLEIADES CONSULTING, INC
//--------------------------------------------------------------------------------

string API = "http://racketberries.pleiades.ca:9080/restbot/util.php?psk=lolwhut";

key nameLookupID = NULL_KEY;

default
{
    touch_start(integer total_number)
    {
        if ( nameLookupID == NULL_KEY ) {
            nameLookupID = llHTTPRequest(API + "&command=getname&key=" + (string) llDetectedKey(0), [], "");
        } else {
            llSay(0, "In use");
        }
    }
    
    http_response(key request_id, integer status, list metadata, string body) {
        //llWhisper(0, (string) status + " " + body);
        if ( request_id == nameLookupID ) {
            if ( llGetSubString(body, 0, 1) == "OK" ) {
                llSay(0, "Hello thar " + llGetSubString(body, 2, -1));
            }
            nameLookupID = NULL_KEY;
        }
    }
}

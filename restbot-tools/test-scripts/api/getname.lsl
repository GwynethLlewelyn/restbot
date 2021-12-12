//--------------------------------------------------------------------------------
// LICENSE:
// This file is part of the RESTBot Project.
//
// Copyright (C) 2007-2008 PLEIADES CONSULTING, INC
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//--------------------------------------------------------------------------------

string API = "http://localhost:9080/restbot/util.php?psk=lolwhut";

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

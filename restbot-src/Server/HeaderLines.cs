/*--------------------------------------------------------------------------------
 FILE INFORMATION:
     Name: HeaderLines.cs [./restbot-src/Server/HeaderLines.cs]
     Description: Contains a class that allows easy parsing (or creation)
                  of individual header lines in a HTTP request/response.

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
using System;
using System.Collections.Generic;
using System.Text;

namespace RESTBot.Server
{
    public class HeaderLine
    {
        private string _key = "";
        private string _value = "";

        #region Get/Set Code
        public string Key
        {
            get
            {
                return _key;
            }
        }

        public string Value
        {
            get
            {
                return _value;
            }
        }
        #endregion

        public override string ToString()
        {
            return Key + ": " + Value;
        }

        //Constructors!
        public HeaderLine(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public HeaderLine(string entire_line)
        {
            entire_line = entire_line.Trim();
            string[] split = entire_line.Split(':');
            if (split.Length > 2)
            {
                string second_part = "";
                for (int i = 1; i < split.Length; ++i)
                {
                    second_part += split[i];
                }
                string[] new_split = new string[2];
                new_split[0] = split[0];
                new_split[1] = second_part;
                split = new_split;
            }
            else if (split.Length < 2)
            {
                DebugUtilities.WriteWarning("Could not parse header line! (" + entire_line + ")");
                //no exception needed, just a warning
                return;
            }
	    //DebugUtilities.WriteDebug("key=[" + split[0] + "] value=[" + split[1] + "]");
            _key = split[0].Trim();
            _value = split[1].Trim();
	    //DebugUtilities.WriteDebug("[" + _value + "]");
        }
    }
}

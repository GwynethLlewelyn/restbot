/*--------------------------------------------------------------------------------
 FILE INFORMATION:
     Name: Program.cs [./restbot-src/Server/HeaderParser.cs]
     Description: Handles entire header sections of a HTTP request for processing 
                  into individual HeaderLines

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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Web;
using RESTBot;

namespace RESTBot.Server
{
    public class RequestHeaders
    {
        public HeaderRequestLine RequestLine;
        public List<HeaderLine> HeaderLines;
        public string Hostname = "";

        public RequestHeaders(string headers_section, string host_name)
        {
            Hostname = host_name;
            headers_section.Trim();

            string[] split_up = headers_section.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            //first line is the request line

            if (split_up.Length < 1) throw new Exception("Uhm, Theres supposed to be something in the headers");

            string request_line = split_up[0];
            RequestLine = new HeaderRequestLine(request_line);

            int length = split_up.Length;
            HeaderLines = new List<HeaderLine>();
            for (int i = 1; i < length; ++i)
            {
                HeaderLines.Add(new HeaderLine(split_up[i]));
            }
            DebugUtilities.WriteDebug("HTTP request has " + HeaderLines.Count + " headers");
        }
    }

    public class HeaderRequestLine
    {
        private string _method = ""; //GET/POST/etc
        private string _path = ""; //For parsing REST commands
        private string _http_version = ""; //Something that might be usefull, eg. HTTP/1.1

        public string Method
        {
            get
            {
                return _method;
            }
        }

        public string Path
        {
            get
            {
                return _path;
            }
        }

        public string HttpVersion
        {
            get
            {
                return _http_version;
            }
        }

        public HeaderRequestLine(string method, string path, string http_version)
        {
            _method = method;
            _path = path;
            _http_version = http_version;
        }

        public HeaderRequestLine(string entire_line)
        {
            string[] split = entire_line.Trim().Split(' ');
            if (split.Length != 3)
            {
                //this is more serious than a header line, this is the request line batch!
                DebugUtilities.WriteError("Could not parse request line");
                throw new Exception("Could not parse request line", new Exception("Line has " + (split.Length - 1) + " spaces instead of 2 [requestline]"));
            }

            //ok, we got three entries
            _method = split[0].ToUpper().Trim();
            _path = split[1];
            _http_version = split[2].ToUpper().Trim();

            DebugUtilities.WriteDebug("Request Line Parsed: method=" + _method + "; path=" + _path + "; version=" + _http_version);
        }
    }
}
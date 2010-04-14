/*--------------------------------------------------------------------------------
 FILE INFORMATION:
     Name: HeaderConstructor.cs [./restbot-src/Server/HeaderConstructor.cs]
     Description: Contains methods used to create headers for an HTTP response

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

    public class ResponseStatus
    {
        public int StatusCode;
        public string StatusReason;

        public ResponseStatus(int code, string reason)
        {
            StatusCode = code;
            StatusReason = reason;
        }

        public override string ToString()
        {
            return StatusCode + " " + StatusReason;
        }
    }

    public class HeaderResponseLine
    {
        private ResponseStatus _status = new ResponseStatus(200, "OK");
        private string _http_version = ""; //Something that might be usefull, eg. HTTP/1.1

        public ResponseStatus Status
        {
            get
            {
                return _status;
            }
        }

        public string HttpVersion
        {
            get
            {
                return _http_version;
            }
        }

        private void CreateHeaderResponseLine(string http_version, ResponseStatus status)
        {
            _http_version = http_version;
            _status = status;
        }

        public HeaderResponseLine(string http_version, ResponseStatus status)
        {
            CreateHeaderResponseLine(http_version,status);
        }

        public HeaderResponseLine(string http_version, int status_code, string status_reason)
        {
            ResponseStatus status = new ResponseStatus(status_code, status_reason);
            CreateHeaderResponseLine(http_version, status);
        }

        public override string ToString()
        {
            return _http_version + " " + _status.ToString();
        }
    }
    public class ResponseHeaders
    {
        public HeaderResponseLine ResponseLine;
        public List<HeaderLine> HeaderLines;

        private void CreateResponseHeaders(int status, string status_response, string content_type)
        {
            ResponseLine = new HeaderResponseLine("HTTP/1.1", new ResponseStatus(status, status_response)); //some defaults
            HeaderLines = new List<HeaderLine>();


            HeaderLines.Add(new HeaderLine("Date", DateTime.Now.ToUniversalTime().ToLongDateString()));
            HeaderLines.Add(new HeaderLine("Server", "RestBotRV/0.1"));
            HeaderLines.Add(new HeaderLine("Content-type", content_type));
        }

        private ResponseHeaders(int status, string status_response, string content_type)
        {
            CreateResponseHeaders(status, status_response, content_type);
        }
        public ResponseHeaders(int status, string status_response)
        {
            CreateResponseHeaders(status, status_response, "text/xml");
        }
        
        public override string ToString()
        {
            string headers = ResponseLine.ToString() + "\r\n"; //initial header response line
            foreach(HeaderLine line in HeaderLines)
            {
                headers += line.ToString() + "\r\n";
            }
            headers += "\r\n"; //finish up the headers.. a \r\n\r\n signifies a change between headers and the body
            return headers;
        }
    }
}

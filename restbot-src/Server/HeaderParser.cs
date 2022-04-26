/*--------------------------------------------------------------------------------
	FILE INFORMATION:
    Name: HeaderParser.cs [./restbot-src/Server/HeaderParser.cs]
    Description: Handles entire header sections of a HTTP request for processing
                 into individual HeaderLines

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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
using RESTBot;

namespace RESTBot.Server
{
	/// <summary>Extract the actual request and all the other headers from a HTTP request</summary>
	/// <remarks>Who was insane enough to reinvent the wheel?! Microsoft already
	/// provides everything and the kitchen sink, and is (probably) future-proof
	/// (gwyneth 20220425)</remarks>
	public class RequestHeaders
	{
		public HeaderRequestLine RequestLine;

		public List<HeaderLine> HeaderLines;

		/// <value>FQDN host name</value>
		public string Hostname = "";

		/// <summary>Constructor</summary>
		/// <param name="headers_section">All lines from the HTTP stream captured so far</param>
		/// <param name="host_name">FQDN host name, extracted from the socket stream and reverse-DNSified</param>
		public RequestHeaders(string headers_section, string host_name)
		{
			Hostname = host_name;
			headers_section.Trim();

			string[] split_up =
				headers_section
					.Split(new string[] { "\r\n" },
					StringSplitOptions.RemoveEmptyEntries);

			//first line is the request line
			if (split_up.Length < 1)
				throw new Exception("Uhm, Theres supposed to be something in the headers");

			string request_line = split_up[0];
			RequestLine = new HeaderRequestLine(request_line);

			int length = split_up.Length;
			HeaderLines = new List<HeaderLine>();
			for (int i = 1; i < length; ++i)
			{
				HeaderLines.Add(new HeaderLine(split_up[i]));
			}
			DebugUtilities
				.WriteDebug($"HTTP request has {HeaderLines.Count} headers");
		}
	}

	/// <summary>Represents one valid header, received from the request.</summary>
	public class HeaderRequestLine
	{
		private string _method = ""; //GET/POST/etc

		private string _path = ""; //For parsing REST commands

		private string _http_version = ""; //Something that might be usefull, eg. HTTP/1.1

		/// <summary>Returns GET/PUT/POST/DELETE... etc.</summary>
		public string Method
		{
			get
			{
				return _method;
			}
		}

		/// <summary>Path part of the URL</summary>
		public string Path
		{
			get
			{
				return _path;
			}
		}

		/// <summary>HTTP Version of this request</summary>
		/// <remarks>Very likely, just HTTP/1.0 or HTTP/1.1 are supported...</remarks>
		public string HttpVersion
		{
			get
			{
				return _http_version;
			}
		}

		/// <summary>Constructor (parsed line)</summary>
		public HeaderRequestLine(string method, string path, string http_version)
		{
			_method = method;
			_path = path;
			_http_version = http_version;
		}

		/// <summary>Constructor (overloaded, for a single raw line)</summary>
		public HeaderRequestLine(string entire_line)
		{
			string[] split = entire_line.Trim().Split(' ');
			if (split.Length != 3)
			{
				//this is more serious than a header line, this is the request line batch!
				DebugUtilities.WriteError("Could not parse request line");
				throw new Exception("Could not parse request line",
					new Exception($"Line has {(split.Length - 1)} spaces instead of 2 [requestline]"));
			}

			//ok, we got three entries
			_method = split[0].ToUpper().Trim();
			_path = split[1];
			_http_version = split[2].ToUpper().Trim();

			DebugUtilities
				.WriteDebug($"Request Line Parsed: method={_method}; path={_path}; version={_http_version}");
		}
	}
} // namespace RESTBot.Server

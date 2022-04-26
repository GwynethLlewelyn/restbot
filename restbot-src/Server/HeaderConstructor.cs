/*--------------------------------------------------------------------------------
	FILE INFORMATION:
    Name: HeaderConstructor.cs [./restbot-src/Server/HeaderConstructor.cs]
    Description: Contains methods used to create headers for an HTTP response

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
using System.Collections.Generic;
using System.Text;

namespace RESTBot.Server
{
	/// <summary>
	/// Generates a response header line with HTTP status code and given reason for that status.
	/// </summary>
	/// <remarks>I believe this will only cover the most basic HTTP codes, as some require bodies as well (gwyneth 20220425)</remarks>
	public class ResponseStatus
	{
		/// <value>A valid HTTP status code, e.g. 100, 200, 404, 503, etc.</value>
		public int StatusCode;

		/// <value>Reason for given status code.</value>
		/// <remarks>Traditionally, these are (semi-)standard and could be a lookup table (gwyneth 20220425)</remarks>
		public string StatusReason;

		/// <summary>Constructor</summary>
		/// <param name="code">A valid HTTP code, e.g. 100, 200, 404, 503, etc.</param>
		/// <param name="reason">Reason given for the above status code</param>
		/// <remarks>So-called "reason" is almost standard these days and should not be overriden... (gwyneth 20220425)</remarks>
		public ResponseStatus(int code, string reason)
		{
			StatusCode = code;
			StatusReason = reason;
		}

		/// <summary>Converts internal representation of a ResponseStatus to a string.</summary>
		/// <returns>The converted string, which will at least be a single, empty space character...</returns>
		public override string ToString()
		{
			return $"{StatusCode} {StatusReason}";
		}
	}

	/// <summary>
	/// Class to generate the properly-formatted response header, including status code.
	/// </summary>
	public class HeaderResponseLine
	{
		private ResponseStatus _status = new ResponseStatus(200, "OK");

		private string _http_version = ""; //Something that might be usefull, eg. HTTP/1.1

		/// <summary>Returns the status of the request, which is a ResponseStatus object</summary>
		public ResponseStatus Status
		{
			get
			{
				return _status;
			}
		}

		/// <summary>Returns the HTTP version</summary>
		/// <remarks>I believe that only 1.0 or 1.1 will make sense here... (gwyneth 20220425)</remarks>
		public string HttpVersion
		{
			get
			{
				return _http_version;
			}
		}

		private void CreateHeaderResponseLine(
			string http_version,
			ResponseStatus status
		)
		{
			_http_version = http_version;
			_status = status;
		}

		/// <summary>
		/// Creates a header response line, from the status code and a previously created ResponseStatus object.
		/// </summary>
		public HeaderResponseLine(string http_version, ResponseStatus status)
		{
			CreateHeaderResponseLine(http_version, status);
		}

		/// <summary>
		/// Overloaded version including a string with a reason for the status, which will be
		/// used to (internally) create a ResponseStatus object.
		/// </summary>
		public HeaderResponseLine(
			string http_version,
			int status_code,
			string status_reason
		)
		{
			ResponseStatus status = new ResponseStatus(status_code, status_reason);
			CreateHeaderResponseLine(http_version, status);
		}

		/// <summary>Converts the HTTP version and the current status (code + reason) to a string</summary>
		/// <returns>Converted string (at least one space character will be returned)</returns>
		public override string ToString()
		{
			return $"{_http_version} {_status.ToString()}";
		}
	}

	/// <summary>Class to generate properly-formatted response headers</summary>
	/// <remarks>Assumes that the response will be XML; note that we might have JSON
	/// as an option in the future, which will require a few overloaded methods here
	/// (gwyneth 20220425)</remarks>
	public class ResponseHeaders
	{
		/// The following _may_ be null...
		public HeaderResponseLine? ResponseLine;

		public List<HeaderLine>? HeaderLines;

		private void CreateResponseHeaders(
			int status,
			string status_response,
			string content_type
		)
		{
			ResponseLine =
				new HeaderResponseLine("HTTP/1.1",
					new ResponseStatus(status, status_response)); //some defaults
			HeaderLines = new List<HeaderLine>();

			HeaderLines
				.Add(new HeaderLine("Date",
					DateTime.Now.ToUniversalTime().ToLongDateString()));
			HeaderLines.Add(new HeaderLine("Server", "RestBotRV/0.1"));
			HeaderLines.Add(new HeaderLine("Content-type", content_type));
		}

		private ResponseHeaders(
			int status,
			string status_response,
			string content_type
		)
		{
			CreateResponseHeaders(status, status_response, content_type);
		}

		/// <summary>Creates the full reponse headers for a XML reply</summary>
		public ResponseHeaders(int status, string status_response)
		{
			CreateResponseHeaders(status, status_response, "text/xml");
		}

		/// <summary>Converts a response/header line to a string representation</summary>
		/// <returns>Converted string, or just a line break if reponse/headers missing</returns>
		public override string ToString()
		{
			// Do we have any headers at all? (gwyneth 20220213)
			if (ResponseLine != null && HeaderLines != null)
			{
				string headers = ResponseLine.ToString() + "\r\n"; //initial header response line
				foreach (HeaderLine line in HeaderLines)
				{
					headers += line.ToString() + "\r\n";
				}
				headers += "\r\n"; //finish up the headers.. a \r\n\r\n signifies a change between headers and the body
				return headers;
			}
			return "\r\n";
		}
	}
}

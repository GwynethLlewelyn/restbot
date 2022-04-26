/*--------------------------------------------------------------------------------
	FILE INFORMATION:
    Name: Server.cs [./restbot-src/Server/Server.cs]
    Description: This class handles an individual http request, passes it on to a restbot
                 (if applicable) and responds with a response in xml form.

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
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RESTBot.Server
{
	/// <summary>Lower-level connections to special scenarios</summary>
	public partial class Router
	{
		/// <summary>deals with methods of the router class</summary>
		private void AcceptClientThread(IAsyncResult result)
		{
			TcpClient client = _listener.EndAcceptTcpClient(result);
			_processed_connection.Set();

			EndPoint? endpoint = client.Client.RemoteEndPoint; // we put it right here to make things easier later on (gwyneth 20220107)

			string ip = String.Empty;

			// more stupid checks to deal with the many, many ways this can become null... (gwyneth 20220214)
			if (endpoint != null)
			{
				string? endpointToString = endpoint.ToString();
				if (endpointToString != null)
				{
					ip = endpointToString.Split(':')[0];
					DebugUtilities
						.WriteInfo($"Processing Connection from {IPAddress.Parse(((IPEndPoint) endpoint).Address.ToString())} (ip: {ip})!"); // new syntax, since this is now nullable (gwyneth 20220207)
				}
				else
				{
					DebugUtilities.WriteError("No IP address received (?) or couldn't parse IP address for connection");
				}
			}

			NetworkStream stream = client.GetStream();

			DebugUtilities.WriteSpecial("Reading Stream");
			string request = "";
			byte[] buffer = new byte[512];
			int i = 0;
			do
			{
				i = stream.Read(buffer, 0, buffer.Length); //add the next set of data into the buffer..
				DebugUtilities.WriteSpecial("Read chunk from the stream");
				request += Encoding.UTF8.GetString(buffer, 0, i); //append it to the overall request
			}
			while (stream.DataAvailable); //and repeat :)

			DebugUtilities
				.WriteInfo($"Got request, totalling {request.Length} characters");
			string[] split =
				request
					.Split(new string[] { "\r\n\r\n" },
					StringSplitOptions.RemoveEmptyEntries);

			string hostname = "unknown";	// if we can't resolve IP address to a hostname...
			try
			{
				DebugUtilities.WriteDebug("ip: " + ip ?? "nothing");	// may be an empty string (gwyneth 20220214)
				if (ip != null)
				{
					IPHostEntry host = Dns.GetHostEntry(IPAddress.Parse(ip));
					hostname = host.HostName;
				}
				DebugUtilities.WriteDebug($"ENDPOINT HOSTNAME: {hostname}");
			}
			catch
			{
				DebugUtilities
					.WriteWarning("Could not parse ip address to get the hostname (ipv6?) -  hostname is set as 'unknown'");
			}

			/// <value>get request headers into the appropriate class</value>
			RequestHeaders _request_headers = new RequestHeaders(split[0], hostname);

			string body = "";

			/// <value>
			/// For some reason, the RESTbot HTTP server takes pleasure in waiting for 100-continue,
			/// so we set a flag here.
			/// </value>
			bool foundExpectContinue = false;
			foreach (HeaderLine line in _request_headers.HeaderLines)
			{
				if (line.ToString() == "Expect: 100-continue")
				{
					foundExpectContinue = true;
					DebugUtilities.WriteSpecial("Found 100 continue!");
				}
			}

			if (foundExpectContinue)
			{
				try
				{
					ResponseHeaders continue_response =
						new ResponseHeaders(100, "Continue");
					byte[] byte_continue_response =
						System.Text.Encoding.UTF8.GetBytes(continue_response.ToString());

					//send the 100 continue message and then go back to the above.
					DebugUtilities.WriteSpecial("Writing 100 continue response");
					stream.Write(byte_continue_response, 0, byte_continue_response.Length);
					DebugUtilities.WriteSpecial($"Finished writing - {byte_continue_response.Length} bytes total sent");

					request = "";
					buffer = new byte[512];
					/// <value>stream chunk counter</value>
					i = 0;
					if (stream.DataAvailable)
					{
						DebugUtilities.WriteSpecial("DATA AVALIABLE!!");
					}
					else
					{
						DebugUtilities.WriteWarning("NO DATA AVALIABLE? Hurr");
					}
					do
					{
						i = stream.Read(buffer, 0, buffer.Length); //add the next set of data into the buffer..
						DebugUtilities.WriteSpecial("Read continued chunk from the stream");
						request += Encoding.UTF8.GetString(buffer, 0, i); //append it to the overall request
					}
					while (stream.DataAvailable); //and repeat :)

					DebugUtilities.WriteInfo($"Got continued request, totalling {request.Length} characters");
					DebugUtilities.WriteDebug($"Here's what I got: {request}");
					body = request;
				}
				catch (Exception e)
				{
					DebugUtilities.WriteError("An error occured while trying to talk to the client (100 expectation response): " + e.Message);
				}
			}
			else
			{
				if (split.Length > 1)
				{
					body = split[1];
				}
			}
			/// <value>Status message to return to the client that made the request</value>
			/// <remarks>Likely XML-formatted (there are very few exceptions)</remarks>
			string to_return = Program.DoProcessing(_request_headers, body);
			// The next line is DELIBERATELY not using interpolated strings, because there might have
			// been some issues with those. (gwyneth 20220426)
			to_return = "<restbot>" + to_return + "</restbot>";
			// commented out until I figure out how to write a DebugUtilities.WriteTrace() method. (gwyneth 20220426)
			// DebugUtilities.WriteDebug($"What I should return to the client: {to_return}");

			ResponseHeaders response_headers = new ResponseHeaders(200, "OK");
			string response = response_headers.ToString() + to_return;

			try
			{
				/// <value>stream of bytes to send over the HTTP stream as response</value>
				/// <remarks>TODO(gwyneth): This comes as XML, but we might wish to convert it first to JSON or
				/// something else. Note: this would require changing the headers, too</remarks>
				byte[] the_buffer = System.Text.Encoding.UTF8.GetBytes(response);
				response = ""; //unset for the hell of it
				stream.Write(the_buffer, 0, the_buffer.Length);
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError("Could not write to the network stream, error was: " + e.Message);
				//see below
			}

			try
			{
				stream.Close();
				client.Close();
			}
			catch
			{
				DebugUtilities.WriteWarning("An error occured while closing the stream");
				// ignore, sometimes the connection was closed by the client
				// if it's to be ignored, I've downgraded it to a warning. (gwyneth 20220426)
			}
		}
	}
}

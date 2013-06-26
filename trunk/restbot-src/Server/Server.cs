/*--------------------------------------------------------------------------------
 FILE INFORMATION:
     Name: Program.cs [./restbot-src/Server/Server.cs]
     Description: This class handles an individual http request, passes it on to a restbot
                  (if applicable) and responds with a response in xml form.

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
using System.Net.Sockets;
using System.Net;

namespace RESTBot.Server
{
    public partial class Router
    {
        
        private void AcceptClientThread(IAsyncResult result)
        {
            TcpClient client = _listener.EndAcceptTcpClient(result);
            _proccessed_connection.Set();

            DebugUtilities.WriteInfo("Processing Connection from " + client.Client.RemoteEndPoint.ToString() + "!");
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

            DebugUtilities.WriteInfo("Got request, totalling " + request.Length + " characters");
            string[] split = request.Split(new string[] { "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            string hostname = "unknown";
            try
            {
                EndPoint endpoint = client.Client.RemoteEndPoint;
                DebugUtilities.WriteDebug("ip: " + endpoint.ToString());
                string ip = (endpoint.ToString().Split(':'))[0];
                IPHostEntry host = Dns.GetHostEntry(IPAddress.Parse(ip));
                hostname = host.HostName;
                DebugUtilities.WriteDebug("ENDPOINT HOSTNAME: " + hostname);
            }
            catch
            {                
                DebugUtilities.WriteWarning("Could not parse ip address to get the hostname (ipv6?) -  hostname is set as 'unknown'");
            }
	
            RequestHeaders x = new RequestHeaders(split[0], hostname);

            string body = "";

	        bool foundExpectContinue = false;
	        foreach(HeaderLine line in x.HeaderLines)
	        {
	    	    if(line.ToString() == "Expect: 100-continue")
		        {
			        foundExpectContinue = true;
                    DebugUtilities.WriteSpecial("Found 100 continue!");
		        }
	        }
    	    
	        if(foundExpectContinue)
	        {
	    	    try
		        {
			        ResponseHeaders continue_response = new ResponseHeaders(100, "Continue");
			        byte[] byte_continue_response = System.Text.Encoding.UTF8.GetBytes(continue_response.ToString());
        			
			        //send the 100 continue message and then go back to the above.
			        DebugUtilities.WriteSpecial("Writing 100 continue response");
			        stream.Write(byte_continue_response,0,byte_continue_response.Length);
			        DebugUtilities.WriteSpecial("Finished writing - " + byte_continue_response.Length + " bytes total sent");
                    
                    request = "";
                    buffer = new byte[512];
                    i = 0;
                    if (stream.DataAvailable) DebugUtilities.WriteSpecial("DATA AVALIABLE!!");
                    else DebugUtilities.WriteWarning("NO DATA AVALIABLE? Hurr");

                    do
                    {
                        i = stream.Read(buffer, 0, buffer.Length); //add the next set of data into the buffer..
                        DebugUtilities.WriteSpecial("Read continued chunk from the stream");
                        request += Encoding.UTF8.GetString(buffer, 0, i); //append it to the overall request
                    }
                    while (stream.DataAvailable); //and repeat :)

                    DebugUtilities.WriteInfo("Got continued request, totalling " + request.Length + " characters");

                    DebugUtilities.WriteDebug("Heres what I got: " + request);
                    body = request;
		        }
		        catch
		        {
			        DebugUtilities.WriteError("An error occured while trying to talk to the client (100 expectation response)");
		        }
	        }
            else if (split.Length > 1) body = split[1];

            string to_return = Program.DoProcessing(x, body);
            to_return = "<restbot>" + to_return + "</restbot>";
            DebugUtilities.WriteDebug("What I should return to the client: " + to_return);

            ResponseHeaders response_headers = new ResponseHeaders(200, "OK");
            string response = response_headers.ToString() + to_return;

            try
            {
                byte[] the_buffer = System.Text.Encoding.UTF8.GetBytes(response);
                response = ""; //unset for the hell of it
                stream.Write(the_buffer, 0, the_buffer.Length);
            }
            catch
            {
                DebugUtilities.WriteError("Could not write to the network stream!");
                //see below
            }

            try
            {
                stream.Close();
                client.Close();
            }
            catch
            {
                DebugUtilities.WriteError("An error occured while closing the stream");
                //ignore, sometimes the connection was closed by the client
            }
        }
    }
}

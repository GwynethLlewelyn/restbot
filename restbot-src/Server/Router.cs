/*--------------------------------------------------------------------------------
	FILE INFORMATION:
    Name: Router.cs [./restbot-src/Server/Router.cs]
    Description: The class in this file handles all http requests and passes them to
                 an indivual server (defined in Server.cs)

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
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace RESTBot.Server
{
		/// <summary>HTTP router for incoming REST requests</summary>
    public partial class Router
    {
        private IPAddress _bounded_ip;
        private int _port;
        private TcpListener _listener;
        private Thread _router_thread;
        private ManualResetEvent _proccessed_connection = new ManualResetEvent(false);
        public bool StillRunning;

				/// <summary>Constructor</summary>
        public Router(IPAddress IP, int Port)
        {
            _bounded_ip = IP;
            _port = Port;

            try
            {
                DebugUtilities.WriteInfo("Starting HTTP server...");

                _listener = new TcpListener(_bounded_ip, _port);
                _listener.Start();
            }
            catch (Exception e)
            {
                throw new Exception("Could not bind to the specified IP address", e);
            }

            DebugUtilities.WriteDebug("Router was able to bind to specified ip/port combination.. starting thread");

            ManualResetEvent waitingForStart = new ManualResetEvent(false);
            try
            {
                _router_thread = new Thread(new ParameterizedThreadStart(RunListener));
                _router_thread.Start(waitingForStart);
                DebugUtilities.WriteDebug("Waiting for thread to initialize");
                if (!waitingForStart.WaitOne(15000, true))
                {
                    throw new Exception("Timeout exceeded on the router thread start (15s)");
                }
                else
                {
                    DebugUtilities.WriteDebug("Router thread started without a problem, so far");
                }
            }
            catch (Exception e)
            {
                throw new Exception("Could not start the server routing thread!", e);
            }
            DebugUtilities.WriteDebug("If you see this message, all was good when initializing the router");
        }

        private void RunListener(object? ResetTrigger)
        {
						if (ResetTrigger == null)
						{
								DebugUtilities.WriteWarning("RunListener was passed a null ResetTrigger");
								return;
						}
            DebugUtilities.WriteDebug("Router thread started successfully");

            ManualResetEvent trigger = (ManualResetEvent)ResetTrigger;
            StillRunning = true;
            trigger.Set();
            do
            {
                _proccessed_connection.Reset();
                try {
                	_listener.BeginAcceptTcpClient(new AsyncCallback(AcceptClientThread), _listener); // in server_thread.cs
                }
                catch (Exception e)
                {
                	DebugUtilities.WriteError("Failed to listen to client thread: " + e.Message);
                }
                _proccessed_connection.WaitOne();
            } while (StillRunning);
            _listener.Stop();
        }
    }
}

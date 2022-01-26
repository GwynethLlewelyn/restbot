/*--------------------------------------------------------------------------------
	FILE INFORMATION:
    Name: RestPlugin.cs [./restbot-src/RestPlugin.cs]
    Description: This file defines the prototypes used in any restbot plugin

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
using System.Net;

namespace RESTBot
{
    /// <summary>
    /// A base class for REST plugins
    /// </summary>
    public abstract class RestPlugin
    {
        /// <summary>
        /// The name of the method. Should be set in the constructor.
        /// </summary>
        public string MethodName = "unknown";	// cannot be null! (gwyneth 20220126)

        /// <summary>
        /// Process the request through this method
        /// </summary>
        /// <param name="b">The RestBot that is doing the processing</param>
        /// <param name="Parameters">QueryString and POST parameters</param>
        /// <returns>XML output</returns>
        public abstract string Process(RestBot b, Dictionary<string, string> Parameters);
    }

    /// <summary>
    /// A base class for stateful plugins, ie those requiring actions on events from a specific instance of libsecondlife
    /// or RestBot
    /// </summary>
    public abstract class StatefulPlugin
    {
        /// <summary>
        /// The name of the method. Should be set in the constructor.
        /// </summary>
        public string MethodName = "unknown";	// making sure it's never null. (gwyneth 20220126)

        /// <summary>
        /// An optionally overridable method for setting up events and callbacks from a RestBot
        /// </summary>
        /// <param name="bot"></param>
        public virtual void Initialize(RestBot bot)
        {
        }

        /// <summary>
        /// Process the request through this method
        /// </summary>
        /// <param name="b">The RestBot that is doing the processing</param>
        /// <param name="Parameters">QueryString and POST parameters</param>
        /// <returns>XML output</returns>
        public abstract string Process(RestBot b, Dictionary<string, string> Parameters);

        // Indicates that the current plugin is actively running.
        public bool Active;

        // Implement to perform actions that require updates over time.
        public virtual void Think() {
        }

    }
}

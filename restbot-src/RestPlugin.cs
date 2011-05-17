/*--------------------------------------------------------------------------------
 FILE INFORMATION:
     Name: RestPlugin.cs [./restbot-src/RestPlugin.cs]
     Description: This file defines the prototypes used in any restbot plugin
 
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
        public string MethodName;

        /// <summary>
        /// Process the request through this method
        /// </summary>
        /// <param name="b">The RestBot that is doing the processing</param>
        /// <param name="Paramaters">QueryString and POST parameters</param>
        /// <returns>XML output</returns>
        public abstract string Process(RestBot b, Dictionary<string, string> Paramaters);
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
        public string MethodName;

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
        /// <param name="Paramaters">QueryString and POST parameters</param>
        /// <returns>XML output</returns>
        public abstract string Process(RestBot b, Dictionary<string, string> Paramaters);

        // Indicates that the current plugin is actively running.
        public bool Active;

        // Implement to perform actions that require updates over time.
        public virtual void Think() {
        }

    }
}

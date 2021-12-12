/*--------------------------------------------------------------------------------
 FILE INFORMATION:
     Name: DebugUtilities.cs [./restbot-src/DebugUtilities.cs]
     Description: This file contains methods for any type of debug outputs

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
#define STARTUP_DEBUG //turn this on if you want any debug messages to be outputted before the configuration file is set

using System;
using log4net;
using log4net.Config;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace RESTBot
{
	public static class DebugUtilities
	{
		//Define a new logger for RestBot
		private static readonly ILog restbotLog = LogManager.GetLogger(typeof(RestBot));

        private static void Output(string message, ConsoleColor color)
        {
                Console.ForegroundColor = color;
                Console.WriteLine(message); //Add any formatting here, in the future
                Console.ForegroundColor = ConsoleColor.Gray; //revert to the default color
        }

        /// <summary>
        /// Outputs text in the normal white color.
        /// </summary>
        /// <param name="message">What to be outputted to console</param>
        /// <example>DebugUtilities.WriteInfo("Listening on 127.0.0.1 on port 80");</example>
        /// <remarks>This only outputs colored version on console! Will prepend "[INFO] " to message</remarks>
        public static void WriteInfo(string message)
        {
			restbotLog.Info(message);
            Output("[INFO] " + message, ConsoleColor.White);
        }
        /// <summary>
        /// Outputs text in a gray color.
        /// </summary>
        /// <param name="message">What to be outputted to console</param>
        /// <example>DebugUtilities.WriteDebug("Request recieved - 2048 bytes in buffer");</example>
        /// <remarks>This only outputs colored version on console! Will prepend "[DEBUG] " to message</remarks>
        public static void WriteDebug(string message)
        {
            try
            {
                if (Program.config.debug.restbotDebug)
					restbotLog.Debug(message);
                    Output("[DEBUG] " + message, ConsoleColor.Gray);
            }
            catch
            {
                //well, ok.. the restbotDebug setting wasnt set yet.. lets just do whatever the define tells us to do
#if STARTUP_DEBUG
				restbotLog.Debug(message);
                Output("[DEBUG] " + message, ConsoleColor.Gray);
#endif
            }
        }

        /// <summary>
        /// Outputs text in a yellow color.
        /// </summary>
        /// <param name="message">What to be outputted to console</param>
        /// <example>DebugUtilities.WriteWarning("Could not find custom configuration file, using default");</example>
        /// <remarks>This only outputs colored version on console! Will prepend "[WARN] " to message</remarks>
        public static void WriteWarning(string message)
        {
			restbotLog.Warn(message);
            Output("[WARN] " + message, ConsoleColor.Yellow);
        }

        /// <summary>
        /// Outputs text in a red color.
        /// </summary>
        /// <param name="message">What to be outputted to console</param>
        /// <example>DebugUtilities.WriteError("Socket could not be established, quitting");</example>
        /// <remarks>This only outputs colored version on console! Will prepend "[ERROR] " to message</remarks>
        public static void WriteError(string message)
        {
			restbotLog.Error(message);
            Output("[ERROR] " + message, ConsoleColor.Red);
        }

        /// <summary>
        /// Outputs text in a blue color. Well, it would if log4net had extra logging levels.
        /// </summary>
        /// <param name="message">What to be outputted to console</param>
        /// <example>DebugUtilities.WriteSpecial("Starting login method!");</example>
        /// <remarks>This only outputs colored version on console! Will prepend "[SPEC] " to message. This type of method should be used temporarly when developing specific blogs of code</remarks>
        public static void WriteSpecial(string message)
        {
			restbotLog.Info(message);
            Output("[SPEC] " + message, ConsoleColor.Blue);
        }
    }
}

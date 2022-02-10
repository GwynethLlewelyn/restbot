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
//#define STARTUP_DEBUG //turn this on if you want any debug messages to be outputted before the configuration file is set
//Note: this now gets set on the csproj file using DefineConstants (gwyneth 20220109).
using System;
using log4net;
using log4net.Config;
#if VERBOSE_MESSAGES
/// <see><href="https://stackoverflow.com/a/12556789/1035977">StackOverflow</see>
/// <see><href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/attributes/caller-information">Microsoft Documentation</see>
using System.Runtime.CompilerServices;
using System.IO; // for Path.GetFileName()
#endif


[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace RESTBot
{
	/// <summary>
	/// Defines a few common (console) logging methods
	/// </summary>
	public static class DebugUtilities
	{
		/// <summary>
		/// Define a new logger for RestBot.
		/// </summary>
		private static readonly ILog
			restbotLog = LogManager.GetLogger(typeof (RestBot));

		/// <summary>
		/// Generic outputting method, used internally.
		/// </summary>
		/// <param name="message">What to be outputted to console</param>
		/// <param name="ConsoleColor">Color to use for this message (ConsoleColor.Gray is default)</param>
		/// <example>DebugUtilities.WriteInfo("Listening on 127.0.0.1 on port 80");</example>
		private static void Output(
			string message,
			ConsoleColor color = ConsoleColor.Gray
		)
		{
			Console.ForegroundColor = color;
			Console.WriteLine (message); //Add any formatting here, in the future
			Console.ForegroundColor = ConsoleColor.Gray; //revert to the default color
		}
#endif

		// Note: code duplication is unavoidable, since these CompilerServices functions always
		// refer to the _caller_; if we simply place everything inside Output() (or even a
		// special function just for that purpose), we'll get the file & line number of *this*
		// file, which is not what we want! (gwyneth 20220109)
		/// <summary>
		/// Outputs text in the normal white color.
		/// </summary>
		/// <param name="message">What to be outputted to console</param>
#if VERBOSE_MESSAGES
		/// <param name="[CallerMemberName]">Calling method</param>
		/// <param name="[CallerFilePath]">Path for the file calling the method</param>
		/// <param name="[CallerLineNumber]">Line number of the method calling this</param>
		/// <remarks>When VERBOSE_MESSAGES is set, we get additional info on who called this function, great to track down messages with the same name called from different places (gwyneth 20220109)</remarks>
		/// <example>DebugUtilities.WriteInfo("Listening on 127.0.0.1 on port 80");</example>
		/// <remarks>This only outputs colored version on console! Will prepend "[INFO] " to message</remarks>
		public static void WriteInfo(
			string message,
#if VERBOSE_MESSAGES
			[CallerMemberName] string callingMethod = "",
			[CallerFilePath] string callingFilePath = "",
			[CallerLineNumber] int callingFileLineNumber = 0
		)
#endif
		{
#if VERBOSE_MESSAGES
			message =
				message +
				"; from " +
				callingMethod +
				"(" +
				Path.GetFileName(callingFilePath) +
				":" +
				callingFileLineNumber +
				")";
#endif

			restbotLog.Info (message);
			Output("[INFO] " + message, ConsoleColor.White);
		}
#endif

		/// <summary>
		/// Outputs text in a gray color.
		/// </summary>
		/// <param name="message">What to be outputted to console</param>
#if VERBOSE_MESSAGES
		/// <param name="[CallerMemberName]">Calling method</param>
		/// <param name="[CallerFilePath]">Path for the file calling the method</param>
		/// <param name="[CallerLineNumber]">Line number of the method calling this</param>
		/// <remarks>When VERBOSE_MESSAGES is set, we get additional info on who called this function, great to track down messages with the same name called from different places (gwyneth 20220109)</remarks>
		/// <example>DebugUtilities.WriteDebug("Request recieved - 2048 bytes in buffer");</example>
		/// <remarks>This only outputs colored version on console! Will prepend "[DEBUG] " to message</remarks>
		public static void WriteDebug(
			string message,
#if VERBOSE_MESSAGES
			[CallerMemberName] string callingMethod = "",
			[CallerFilePath] string callingFilePath = "",
			[CallerLineNumber] int callingFileLineNumber = 0
		)
#endif
		{
#if VERBOSE_MESSAGES
			message =
				message +
				"; from " +
				callingMethod +
				"(" +
				Path.GetFileName(callingFilePath) +
				":" +
				callingFileLineNumber +
				")";
#endif

			try
			{
				if (
					Program.config != null &&
					Program.config.debug != null &&
					Program.config.debug.restbotDebug != false
				) restbotLog.Debug(message);
				Output("[DEBUG] " + message, ConsoleColor.Gray);
			}
			catch
			{
				//well, ok.. the restbotDebug setting wasnt set yet.. lets just do whatever the define tells us to do
				//Note: STARTUP_DEBUG is now _also_ defined in the csproj! (gwyneth 20220109)
#if STARTUP_DEBUG
				restbotLog.Debug (message);
				Output("[DEBUG] " + message, ConsoleColor.Gray);
#endif
			}
		}
#endif

		/// <summary>
		/// Outputs text in a yellow color.
		/// </summary>
		/// <param name="message">What to be outputted to console</param>
#if VERBOSE_MESSAGES
		/// <param name="[CallerMemberName]">Calling method</param>
		/// <param name="[CallerFilePath]">Path for the file calling the method</param>
		/// <param name="[CallerLineNumber]">Line number of the method calling this</param>
		/// <remarks>When VERBOSE_MESSAGES is set, we get additional info on who called this function, great to track down messages with the same name called from different places (gwyneth 20220109)</remarks>
		/// <example>DebugUtilities.WriteWarning("Could not find custom configuration file, using default");</example>
		/// <remarks>This only outputs colored version on console! Will prepend "[WARN] " to message</remarks>
		public static void WriteWarning(
			string message,
#if VERBOSE_MESSAGES
			[CallerMemberName] string callingMethod = "",
			[CallerFilePath] string callingFilePath = "",
			[CallerLineNumber] int callingFileLineNumber = 0
		)
#endif
		{
#if VERBOSE_MESSAGES
			message =
				message +
				"; from " +
				callingMethod +
				"(" +
				Path.GetFileName(callingFilePath) +
				":" +
				callingFileLineNumber +
				")";
#endif

			restbotLog.Warn (message);
			Output("[WARN] " + message, ConsoleColor.Yellow);
		}
#endif

		/// <summary>
		/// Outputs text in a red color.
		/// </summary>
		/// <param name="message">What to be outputted to console</param>
#if VERBOSE_MESSAGES
		/// <param name="[CallerMemberName]">Calling method</param>
		/// <param name="[CallerFilePath]">Path for the file calling the method</param>
		/// <param name="[CallerLineNumber]">Line number of the method calling this</param>
		/// <remarks>When VERBOSE_MESSAGES is set, we get additional info on who called this function, great to track down messages with the same name called from different places (gwyneth 20220109)</remarks>
		/// <example>DebugUtilities.WriteError("Socket could not be established, quitting");</example>
		/// <remarks>This only outputs colored version on console! Will prepend "[ERROR] " to message</remarks>
		public static void WriteError(
			string message,
#if VERBOSE_MESSAGES
			[CallerMemberName] string callingMethod = "",
			[CallerFilePath] string callingFilePath = "",
			[CallerLineNumber] int callingFileLineNumber = 0
		)
#endif
		{
#if VERBOSE_MESSAGES
			message =
				message +
				"; from " +
				callingMethod +
				"(" +
				Path.GetFileName(callingFilePath) +
				":" +
				callingFileLineNumber +
				")";
#endif

			restbotLog.Error (message);
			Output("[ERROR] " + message, ConsoleColor.Red);
		}
#endif

		/// <summary>
		/// Outputs text in a blue color. Well, it would if log4net had extra logging levels.
		/// </summary>
		/// <param name="message">What to be outputted to console</param>
#if VERBOSE_MESSAGES
		/// <param name="[CallerMemberName]">Calling method</param>
		/// <param name="[CallerFilePath]">Path for the file calling the method</param>
		/// <param name="[CallerLineNumber]">Line number of the method calling this</param>
		/// <remarks>When VERBOSE_MESSAGES is set, we get additional info on who called this function, great to track down messages with the same name called from different places (gwyneth 20220109)</remarks>
		/// <example>DebugUtilities.WriteSpecial("Starting login method!");</example>
		/// <remarks>This only outputs colored version on console! Will prepend "[SPEC] " to message. This type of method should be used temporarly when developing specific blogs of code</remarks>
		public static void WriteSpecial(
			string message,
#if VERBOSE_MESSAGES
			[CallerMemberName] string callingMethod = "",
			[CallerFilePath] string callingFilePath = "",
			[CallerLineNumber] int callingFileLineNumber = 0
		)
#endif
		{
#if VERBOSE_MESSAGES
			message =
				message +
				"; from " +
				callingMethod +
				"(" +
				Path.GetFileName(callingFilePath) +
				":" +
				callingFileLineNumber +
				")";
#endif

			restbotLog.Info (message);
			Output("[SPEC] " + message, ConsoleColor.Blue);
		}
	}
}

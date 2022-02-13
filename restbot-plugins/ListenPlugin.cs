/*--------------------------------------------------------------------------------
FILE INFORMATION:
	Name: ListenPlugin.cs [./restbot-plugins/ListenPlugin.cs]
	Description: Listens to chat, and, if matched, calls URL for callback (gwyneth 20220213).

LICENSE:
	This file is part of the RESTBot Project.

	Copyright (C) 2007-2008 PLEIADES CONSULTING, INC; (C) 2021,2022 Gwyneth Llewelyn

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

	----------------------------------------------------------------------------

	Author: Gwyneth Llewelyn <gwyneth.llewelyn@gwynethllewelyn.net>
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Utilities;

namespace RESTBot
{
	/// <summary>
	/// Watches for chat, and, if found, calls URL with registered callback(s).
	/// </summary>
	public class ListenPlugin : StatefulPlugin
	{
		/// <summary>System-wide registry for backend chat callbacks</summary>
		static private Dictionary<UUID, String> ListenCallbacks;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public ListenPlugin()
		{
			MethodName = "listen";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		public override void Initialize(RestBot bot)
		{
			ListenCallbacks = new Dictionary<UUID, String>();

			DebugUtilities.WriteDebug($"Listen - unknown yet");

			base.Initialize(bot);
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing at least a session name and an URL for callback</param>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			return "<listen>Not implemented yet</listen>";
		}
	} // end class
} //end namespace
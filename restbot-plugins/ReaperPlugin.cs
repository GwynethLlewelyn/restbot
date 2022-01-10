/*--------------------------------------------------------------------------------
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
using OpenMetaverse;
using OpenMetaverse.Packets;
using System.Net;
using System.Threading;
using System.Timers;

// avatar info functions
namespace RESTBot
{
	/// <summary>
	/// This SEEMS to clean up stalled sessions, i.e. bots that haven't logged out properly
	/// and are now in 'zombie' state (gwyneth 20220109)
	/// </summary>
  public static class Reaper
  {
    /// <summary>
    /// Session timeout timespan
    /// </summary>
    static readonly TimeSpan SESSION_TIMEOUT = new TimeSpan(1, 0, 0); // one hour (h, m, s)
    /// <summary>
    /// interval between reaper sweeps in ms
    /// </summary>
    const double REAPER_INTERVAL = 10000;

    private static bool hasAlreadyStarted = false;

		/// <summary>Running timer for killing bot sessions</summary>
    public static System.Timers.Timer? ReaperTimer;	// weirdly, this can be null?...

		/// <summary>
		/// Reaper Plugin Initialisation.
		/// </summary>
    public static void Init()
    {
      if (!hasAlreadyStarted)
      {
        hasAlreadyStarted = true;
        ReaperTimer = new System.Timers.Timer();
        ReaperTimer.Interval = REAPER_INTERVAL;
        ReaperTimer.Elapsed += new ElapsedEventHandler(Reaper_Elapsed);
        ReaperTimer.Start();
      }
    }

    static void Reaper_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
    {
      List<UUID> kc = new List<UUID>();
      foreach (UUID key in Program.Sessions.Keys) //why not just lock() ? :P
      {
        kc.Add(key);
      }
      //Do reaping
      foreach (UUID key in kc)
      {
        if (Program.Sessions.ContainsKey(key))
        {
          DateTime t = DateTime.Now;
          TimeSpan ts = t.Subtract(Program.Sessions[key].LastAccessed);
          if (ts > SESSION_TIMEOUT)
          {
            DebugUtilities.WriteWarning("Session expiring, " + key.ToString() + ", timespan=" + ts.ToString());
            Program.DisposeSession(key);
          }
        }
      }
    }
  }

	/// <summary>Settimg up main class as a Stateful Plugin</summary>
  public class ReaperPlugin : StatefulPlugin
  {
    private UUID session;

		/// <summary>Constructor</summary>
    public ReaperPlugin()
    {
      MethodName = "reaper_info";
    }

		/// <summary>Initialisation</summary>
    public override void Initialize(RestBot bot)
    {
      session = bot.sessionid;
      DebugUtilities.WriteDebug(session + " REAPER startup");
      Reaper.Init(); //start up the reaper if we havent already (the check to see if we have is in this function)
    }

		/// <summary>Whatever needs to get processed... apparently. nothing</summary>
    public override string Process(RestBot b, Dictionary<string, string> Paramaters)
    {
      return "<error>notprocessed</error>";
    }
  }
}

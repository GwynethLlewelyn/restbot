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

  Further changes & development by Gwyneth Llewelyn
--------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using OpenMetaverse;
using OpenMetaverse.Packets;

/// <summary>movement functions; based on TestClient.exe code</summary>
namespace RESTBot
{
	/// <summary>show current location</summary>
	public class CurrentLocationPlugin : StatefulPlugin
	{
		private UUID session;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public CurrentLocationPlugin()
		{
			MethodName = "location";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">Unused; refers to the _current_ bot</param>
		/// <returns>XML-encoded region + avatar/bot position, if found; error otherwise</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			try
			{
				return $"<{MethodName}><CurrentSim>{b.Client.Network.CurrentSim.ToString()}</CurrentSim><Position>{b.Client.Self.SimPosition.X},{b.Client.Self.SimPosition.Y},{b.Client.Self.SimPosition.Z}</Position></{MethodName}>";
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{MethodName}: {e.Message}</error>";
			}
		}
	} // end location

	/// <summary>Move to location; parameters are sim, x, y, z</summary>
	/// <remarks>This is essentially a _teleport_ to the location (gwyneth 20220303)</remarks>
	public class GotoPlugin : StatefulPlugin
	{
		private UUID session;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public GotoPlugin()
		{
			MethodName = "goto";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the region name (<c>sim</c>) and <c>x, y, z</c> positioning data</param>
		/// <returns>XML-encoded region name and avatar desired position</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			try
			{
				string sim = "";
				float
					x = 128.0f,
					y = 128.0f,
					z = 30.0f;
				bool check = true;

				if (Parameters.ContainsKey("sim"))
				{
					sim = Parameters["sim"].ToString();
				}
				else
					check = false;

				if (Parameters.ContainsKey("x"))
				{
					check &= float.TryParse(Parameters["x"], out x);
				}
				else
					check = false;

				if (Parameters.ContainsKey("y"))
				{
					check &= float.TryParse(Parameters["y"], out y);
				}
				else
					check = false;

				if (Parameters.ContainsKey("z"))
				{
					check &= float.TryParse(Parameters["z"], out z);
				}
				else
					check = false;

				if (!check)
				{
					return $"<error>{MethodName} parameters have to be simulator name, x, y, z</error>";
				}
				// debug: calculate destination vector, show data, just to make sure it's correct (gwyneth 20220411)
				Vector3 teleportPoint = new Vector3(x, y, z);
				DebugUtilities.WriteDebug($"attempting teleport to ({x}), ({y}), ({z}) - vector: {teleportPoint.ToString()}");

				if (b.Client.Self.Teleport(sim, teleportPoint))
					if (b.Client.Network.CurrentSim != null)
					{
						return String
						.Format("<teleport><CurrentSim>{0}</CurrentSim><Position>{1},{2},{3}</Position></teleport>",
						b.Client.Network.CurrentSim.ToString(),
						b.Client.Self.SimPosition.X,
						b.Client.Self.SimPosition.Y,
						b.Client.Self.SimPosition.Z);
					}
					else
					{
						return $"<error>Teleport failed, no sim handle found: {b.Client.Self.TeleportMessage}</error>";
					}
				else
					return $"<error>Teleport failed: {b.Client.Self.TeleportMessage}</error>";
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{MethodName}: {e.Message}</error>";
			}
		}
	} // end goto

	/// <summary>move to location; parameters are x, y, z, distance, run</summary>
	public class MoveToPlugin : StatefulPlugin
	{
		private UUID session;

		private Vector3 goalPos;

		const float DISTANCE_BUFFER = 7.0f;

		private RestBot? me; // may be null, who knows why...

		private float prevDistance;

		/// <summary>Start with 20 attempts to reach destination, abort if destination not reached.</summary>
		/// <remarks>(gwyneth 20220303)</remarks>
		private int attempts = 20;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public MoveToPlugin()
		{
			MethodName = "moveto";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			me = bot;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		public override void Think()
		{
			if (Active && me != null && attempts > 0)
			{
				float distance = 0.0f;
				distance = Vector3.Distance(goalPos, me.Client.Self.SimPosition);

				DebugUtilities
					.WriteDebug($"My pos = {me.Client.Self.SimPosition} Goal: {goalPos} and distance is: {distance}  (attempts left: {attempts})");
				if (distance < DISTANCE_BUFFER)
				{
					DebugUtilities
						.WriteDebug($"I am close to my goal pos: {goalPos.X}, {goalPos.Y}, {goalPos.Z}");
					me.Client.Self.AutoPilotCancel();
					DebugUtilities.WriteSpecial("Cancel Autopilot");
					me.Client.Self.Movement.TurnToward(goalPos, true);
					Active = false;
					attempts = 0;
				}
				else
				{
					attempts--;
				}
			}
			else if (attempts <= 0)
			{
				DebugUtilities.WriteDebug("{MethodName}: No more attempts left; aborting...");
				Active = false;
			}
			base.Think();
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing the </param>
		/// <returns>XML-encoded avatar name, if found</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			uint
				regionX,
				regionY;
			Utils
				.LongToUInts(b.Client.Network.CurrentSim.Handle,
				out regionX,
				out regionY);

			try
			{
				double
					x = 0.0f,
					y = 0.0f,
					z = 0.0f,
					distance = 0.0f;
				bool check = true;
				/// <summary>Selects between running (true) or walking (false)</summary>
				bool run = false;

				if (Parameters.ContainsKey("x"))
				{
					check &= double.TryParse(Parameters["x"], out x);
					DebugUtilities.WriteDebug($"{MethodName} Parse: {Parameters["x"]} into {x}");
				}
				else
					check = false;

				if (Parameters.ContainsKey("y"))
				{
					check &= double.TryParse(Parameters["y"], out y);
				}
				else
					check = false;

				if (Parameters.ContainsKey("z"))
				{
					check &= double.TryParse(Parameters["z"], out z);
				}
				else
					check = false;

				if (Parameters.ContainsKey("distance"))
				{
					check &= double.TryParse(Parameters["distance"], out distance);
				}

				if (Parameters.ContainsKey("run"))
				{
					check &= bool.TryParse(Parameters["run"], out run);
				}

				if (!check)
				{
					return $"<error>{MethodName} parameters have to be x, y, z, [distance, run]</error>";
				}

				if (run)
				{
					b.Client.Self.Movement.AlwaysRun = true;
				}
				else
				{
					b.Client.Self.Movement.AlwaysRun = false;
				}
				goalPos = new Vector3((float) x, (float) y, (float) z);
				b.Client.Self.Movement.TurnToward(goalPos, false);

				// Check for null and, if so, abort with error (gwyneth 20220213)
				if (me == null)
				{
					DebugUtilities.WriteError("'me' was null!");
					return $"<error>{MethodName}: 'me' was null</error>";
				}

				prevDistance = Vector3.Distance(goalPos, me.Client.Self.SimPosition);

				// Convert the local coordinates to global ones by adding the region handle parts to x and y
				b
					.Client
					.Self
					.AutoPilot(goalPos.X + regionX, goalPos.Y + regionY, goalPos.Z);
				Active = true;
				attempts = 20;	// reset attempts, or else they'll be stuck at zero... (gwyneth 20220304)
				while (Active)
				{
					Thread.Sleep(1 * 1000);
				}
				DebugUtilities.WriteSpecial($"End {MethodName} Thread!");
				return $"<{MethodName}>{b.Client.Self.GlobalPosition.X},{b.Client.Self.GlobalPosition.Y},{b.Client.Self.GlobalPosition.Z}</{MethodName}>";
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{MethodName}: {e.Message}</error>";
			}
		}
	} // end moveto

	/// <summary>
	/// Move to avatar location; parameters are the name of the destination avatar.
	/// </summary>
	/// <remarks>Note that AvatarPositionPlugin just _returns_ the position of the target avatar,
	/// but doesn't actually _move_ to it (gwyneth 20220120)</remarks>
	public class MoveToAvatarPlugin : StatefulPlugin
	{
		private UUID session;

		/// <summary>given destination, as a <c>(x,y,z)</c> vector</summary>
		private Vector3 goalPos;

		const float DISTANCE_BUFFER = 7.0f;

		private RestBot? me; // may be null...

		private float prevDistance;

		private Avatar? target; // may be null...

		/// <summary>Region coordinates</summary>
		private uint regionX, regionY;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public MoveToAvatarPlugin()
		{
			MethodName = "moveto-avatar";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			me = bot;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		public override void Think()
		{
			if (Active && me != null && target != null)
			{
				float distance = 0.0f;
				goalPos = target.Position;
				me
					.Client
					.Self
					.AutoPilot(goalPos.X + regionX, goalPos.Y + regionY, goalPos.Z);
				distance = Vector3.Distance(goalPos, me.Client.Self.SimPosition);
				if (distance > 30)
				{
					me.Client.Self.Movement.AlwaysRun = true;
				}
				else
				{
					me.Client.Self.Movement.AlwaysRun = false;
				}
				DebugUtilities.WriteDebug($"My pos = {me.Client.Self.SimPosition} Goal: {goalPos} and distance is: {distance}");
				if (distance < DISTANCE_BUFFER)
				{
					DebugUtilities.WriteDebug($"I am close to my goal pos: <{goalPos.X}, {goalPos.Y}, {goalPos.Z}");
					me.Client.Self.AutoPilotCancel();
					DebugUtilities.WriteSpecial("Cancel Autopilot");
					me.Client.Self.Movement.TurnToward(goalPos, true);
					Active = false;
				}
			}
			base.Think();
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot.</param>
		/// <param name="Parameters">A dictionary containing the destination <c>avatar</c> name
		/// as well as if the bot should run (<c>run</c>, default false).</param>
		/// <returns>XML-encoded position<c>x, y, z</c></returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			Utils
				.LongToUInts(b.Client.Network.CurrentSim.Handle,
				out regionX,
				out regionY);

			try
			{
				double
					x = 0.0f,
					y = 0.0f,
					z = 0.0f;
				bool check = true;
				bool run = false;
				string avatarName = String.Empty;

				if (Parameters.ContainsKey("avatar"))
				{
					avatarName = Parameters["avatar"].ToString().Replace("+", " ");
				}
				else
					check = false;

				// added it here, but I'm not sure if it's correct... (gwyneth 20220409)
				if (Parameters.ContainsKey("run"))
				{
					check &= bool.TryParse(Parameters["run"], out run);
				}

				if (!check)
				{
					return $"<error>{MethodName} parameters have to be avatar name to follow</error>";
				}

				if (run)
				{
					b.Client.Self.Movement.AlwaysRun = true;
				}
				else
				{
					b.Client.Self.Movement.AlwaysRun = false;
				}

				lock (b.Client.Network.Simulators)
				{
					for (int i = 0; i < b.Client.Network.Simulators.Count; i++)
					{
						DebugUtilities
							.WriteDebug($"Found {b.Client.Network.Simulators[i].ObjectsAvatars.Count} Avatar(s)");
						target =
							b
								.Client
								.Network
								.Simulators[i]
								.ObjectsAvatars
								.Find(delegate (Avatar avatar)
								{
									DebugUtilities.WriteDebug($"Found avatar: {avatar.Name}");
									return avatar.Name == avatarName;
								});

						if (target != null)
						{
							goalPos = target.Position;
						}
						else
						{
							DebugUtilities
								.WriteError($"{MethodName} - Error finding position of '{avatarName}'");
							return $"<error>{MethodName} error finding position of '{avatarName}'</error>";
						}
					}
				}

				goalPos = new Vector3((float) x, (float) y, (float) z);
				b.Client.Self.Movement.TurnToward(goalPos, false);

				// C# 8+ is stricter with nulls; if 'me' is null, we got a problem,
				// because there is no way to calculate the distance; so we simply set it to zero
				// in that case; this may play havoc with the algorithm, though. (gwyneth 20220213)
				if (me != null)
				{
					prevDistance = Vector3.Distance(goalPos, me.Client.Self.SimPosition);
				}
				else
				{
					prevDistance = 0;
				}
				if (prevDistance > 30)
				{
					b.Client.Self.Movement.AlwaysRun = true;
				}
				else
				{
					b.Client.Self.Movement.AlwaysRun = false;
				}

				// Convert the local coordinates to global ones by adding the region handle parts to x and y
				b
					.Client
					.Self
					.AutoPilot(goalPos.X + regionX, goalPos.Y + regionY, goalPos.Z);
				Active = true;

				while (Active)
				{
					Thread.Sleep(1 * 1000);
				}
				DebugUtilities.WriteSpecial($"End {MethodName} Thread!");
				return $"<{MethodName}>{b.Client.Self.GlobalPosition.X},{b.Client.Self.GlobalPosition.Y},{b.Client.Self.GlobalPosition.Z}</{MethodName}>";
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{MethodName}: {e.Message}</error>";
			}
		}
	} // end movetoavatar

	/// <summary>Follow an avatar; parameters are:</summary>
	/// <remarks><para>The summary line was abruptly cut and left unfinished... (gwyneth 20220303)</para>
	/// <para>But there seems to be only one parameter, <c>target</c>, the avatar UUID to follow.
	/// (gwyneth 20220409)</para></remarks>
	public class FollowPlugin : StatefulPlugin
	{
		private UUID session;

		const float DISTANCE_BUFFER = 3.0f;

		private RestBot? me;

		uint targetLocalID = 0;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public FollowPlugin()
		{
			MethodName = "follow";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			me = bot;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		public override void Think()
		{
			if (Active && me != null)
			{
				// Find the target position
				lock (me.Client.Network.Simulators)
				{
					for (int i = 0; i < me.Client.Network.Simulators.Count; i++)
					{
						Avatar targetAv;

						if (
							me
								.Client
								.Network
								.Simulators[i]
								.ObjectsAvatars
								.TryGetValue(targetLocalID, out targetAv)
						)
						{
							float distance = 0.0f;

							if (
								me.Client.Network.Simulators[i] == me.Client.Network.CurrentSim
							)
							{
								distance =
									Vector3
										.Distance(targetAv.Position, me.Client.Self.SimPosition);
							}
							else
							{
								// FIXME: Calculate global distances
							}

							if (distance > DISTANCE_BUFFER)
							{
								uint
									regionX,
									regionY;
								Utils
									.LongToUInts(me.Client.Network.Simulators[i].Handle,
									out regionX,
									out regionY);

								double xTarget =
									(double) targetAv.Position.X + (double) regionX;
								double yTarget =
									(double) targetAv.Position.Y + (double) regionY;
								double zTarget = targetAv.Position.Z - 2f;

								Logger
									.DebugLog($"[Autopilot] {distance} meters away from the target, starting autopilot to <{xTarget},{yTarget},{zTarget}>", me.Client);

								me.Client.Self.AutoPilot(xTarget, yTarget, zTarget);
							}
							else
							{
								// We are in range of the target and moving, stop moving
								me.Client.Self.AutoPilotCancel();
							}
						}
					}
				}
			}
			base.Think();
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot.</param>
		/// <param name="Parameters">A dictionary containing the name of the <c>target</c> avatar
		/// to follow; empty or <c>off</c> means stop following the avatar.</param>
		/// <returns>XML-encoded position<c>x, y, z</c></returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			uint
				regionX,
				regionY;
			Utils
				.LongToUInts(b.Client.Network.CurrentSim.Handle,
				out regionX,
				out regionY);

			try
			{
				string target = String.Empty;

				if (Parameters.ContainsKey("target"))
				{
					target =
						Parameters["target"]
							.ToString()
							.Replace("%20", " ")
							.Replace("+", " ");
				}
				else
				{
					return $"<error>{MethodName}: missing argument for target</error>";
				}

				if (target.Length == 0 || target == "off")
				{
					Active = false;
					targetLocalID = 0;
					b.Client.Self.AutoPilotCancel();
					return $"<{MethodName}>off</{MethodName}>";
				}
				else
				{
					if (Follow(target))
						return $"<{MethodName}>on</{MethodName}>";
					else
						return $"<error>cannot follow {target}</error>";
				}
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{MethodName}: {e.Message}</error>";
			}
		}

		bool Follow(string name)
		{
			if (me != null)
			{
				lock (me.Client.Network.Simulators)
				{
					for (int i = 0; i < me.Client.Network.Simulators.Count; i++)
					{
						Avatar target =
							me
								.Client
								.Network
								.Simulators[i]
								.ObjectsAvatars
								.Find(delegate (Avatar avatar)
								{
									return avatar.Name == name;
								});

						if (target != null)
						{
							targetLocalID = target.LocalID;
							Active = true;
							return true;
						}
					}
				}
			}

			if (Active && me != null)
			{
				me.Client.Self.AutoPilotCancel();
				Active = false;
			}

			return false;
		} // end Follow()
	} // end FollowPlugin


	// new plugin for sitting on prim
	/// <summary>
	/// Sit On (Prim); parameter is UUID of object to sit on
	/// </summary>
	/// <since>8.1.5</since>
	/// <remarks>RESTful interface to the SitOn command on LibreMetaverse's own SitOn</remarks>
	public class SitOnPlugin : StatefulPlugin
	{
		private UUID session;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public SitOnPlugin()
		{
			MethodName = "siton";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot.</param>
		/// <param name="Parameters">A dictionary containing the destination prim <c>target</c>.
		/// Optionally, <c>force=true</c> can be given to skip the prim detection.
		/// </param>
		/// <returns>XML-encoded status of sit attempt</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			/// <summary>UUID for the prim/object that we intend the bot to sit on</summary>
			UUID sitTargetID = UUID.Zero;

			DebugUtilities.WriteDebug($"{b.sessionid} {MethodName} - Searching for prim to sit on");
			try
			{
				bool check = false;
				/// <summary>optionally, we can skip prim search and force sitting on UUID</summary>
				bool force = false;

				if (Parameters.ContainsKey("target"))
				{
					check =
						UUID
							.TryParse(Parameters["target"].ToString().Replace("_", " "),
							out sitTargetID);
				}

				if (!check)
				{
					return $"<error>{MethodName} no sit target specified</error>";
				}

				// optional
				if (Parameters.ContainsKey("force"))
				{
					check = bool.TryParse(Parameters["force"], out force);
				}

				if (!check)
				{
					return $"<error>{MethodName} force sit attempted with wrong setting; only true/false are allowed</error>";
				}

				// If we get to this point means that we have a correctly parsed key for the target prim
				DebugUtilities.WriteDebug($"{b.sessionid} {MethodName} - Trying to sit on {sitTargetID.ToString()}...");

				// If not forcing, we'll search for a prim with this UUID in the 'bot interest list
				// and retrieve the information from that prim. (gwyneth 20220410)
				if (!force)
				{
					Primitive targetPrim = b.Client.Network.CurrentSim.ObjectsPrimitives.Find(
						prim => prim.ID == sitTargetID
					);

					if (targetPrim != null)
					{
						b.Client.Self.RequestSit(targetPrim.ID, Vector3.Zero);
						b.Client.Self.Sit();
						return $"<{MethodName}>sitting on \"{targetPrim.Properties.Name}\" ({targetPrim.ID.ToString()} [Local ID: {targetPrim.LocalID}])</{MethodName}>";
					}

					return $"<error>{MethodName}: no prim with UUID {sitTargetID} found</error>";
				}
				else	// forcing to sit on a prim we KNOW that exists! (gwyneth 20220410)
				{
					b.Client.Self.RequestSit(sitTargetID, Vector3.Zero);
					b.Client.Self.Sit();
					return $"<{MethodName}>forced sitting on {sitTargetID.ToString()}</{MethodName}>";
				}

			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{MethodName}: {e.Message}</error>";
			}
		}
	} // end SitOnPlugin


	/// <summary>
	/// Stand (avatar will inconditionally stand up)
	/// </summary>
	/// <since>8.1.5</since>
	/// <remarks>RESTful interface to the Stand command on LibreMetaverse's own Stand</remarks>
	public class StandPlugin : StatefulPlugin
	{
		private UUID session;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public StandPlugin()
		{
			MethodName = "stand";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			session = bot.sessionid;
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot.</param>
		/// <param name="Parameters">void</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			try
			{
				b.Client.Self.Stand();
				return $"<{MethodName}>standing</{MethodName}>";
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{MethodName}: {e.Message}</error>";
			}
		}
	} // end StandPlugin

	/// Upcoming plugins: turnto (a point) and turnto_avatar (avatar UUID)
	/// me.Client.Self.Movement.TurnToward(Pos, true);

	/// <summary>Turn avatar towards a point; parameters are x, y, z</summary>
	public class TurnToPlugin : StatefulPlugin
	{
		private Vector3 goalPos;

		/// <summary>
		/// Sets the plugin name for the router.
		/// </summary>
		public TurnToPlugin()
		{
			MethodName = "turnto";
		}

		/// <summary>
		/// Initialises the plugin.
		/// </summary>
		/// <param name="bot">A currently active RestBot</param>
		/// <returns>void</returns>
		public override void Initialize(RestBot bot)
		{
			DebugUtilities.WriteDebug($"{session} {MethodName} startup");
		}

		/// <summary>
		/// Handler event for this plugin.
		/// </summary>
		/// <param name="b">A currently active RestBot</param>
		/// <param name="Parameters">A dictionary containing (x,y,z) coordinates for
		/// the point to turn to.</param>
		/// <returns>Avatar's current position and rotation</returns>
		public override string
		Process(RestBot b, Dictionary<string, string> Parameters)
		{
			// uint
			// 	regionX,
			// 	regionY;
			// Utils
			// 	.LongToUInts(b.Client.Network.CurrentSim.Handle,
			// 	out regionX,
			// 	out regionY);

			try
			{
				double
					x = 0.0f,
					y = 0.0f,
					z = 0.0f;
				bool check = true;

				if (Parameters.ContainsKey("x"))
				{
					check &= double.TryParse(Parameters["x"], out x);
					DebugUtilities.WriteDebug($"{MethodName} Parse: {Parameters["x"]} into {x}");
				}
				else
					check = false;

				if (Parameters.ContainsKey("y"))
				{
					check &= double.TryParse(Parameters["y"], out y);
				}
				else
					check = false;

				if (Parameters.ContainsKey("z"))
				{
					check &= double.TryParse(Parameters["z"], out z);
				}
				else
					check = false;

				if (!check)
				{
					return $"<error>{MethodName} parameters have to be x, y, z</error>";
				}

				goalPos = new Vector3((float) x, (float) y, (float) z);
				b.Client.Self.Movement.TurnToward(goalPos, false);

				return $"<{MethodName}><location></location><rotation>{b.Client.Self.GlobalPosition.X},{b.Client.Self.GlobalPosition.Y},{b.Client.Self.GlobalPosition.Z}</location><rotation>{b.Client.Self.SimRotation.X},{b.Client.Self.SimRotation.Y},{b.Client.Self.SimRotation.Z},{b.Client.Self.SimRotation.W}</rotation></{MethodName}>";
			}
			catch (Exception e)
			{
				DebugUtilities.WriteError(e.Message);
				return $"<error>{MethodName}: {e.Message}</error>";
			}
		} // end turnto process
	} // end turnto plugin

} // end namespace RESTBot

using System;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Shared.Data.Databases;
using Sabine.Shared.Network;

namespace Sabine.Shared
{
	/// <summary>
	/// Global accessor for the application settings.
	/// </summary>
	public static class Game
	{
		/// <summary>
		/// The packet version to use.
		/// </summary>
		public static int Version { get; set; } = Versions.Alpha;

		/// <summary>
		/// Returns the servers current tick time, indicating how many
		/// milliseconds have passed since a certain time.
		/// </summary>
		/// <returns></returns>
		public static int GetTick()
			=> Environment.TickCount;

		/// <summary>
		/// True for the standard 2011-11-02 client wire profile used by the
		/// NostalRO reference environment. This profile is chronologically
		/// newer than S2500 but intentionally uses the earlier actor layouts.
		/// </summary>
		public static ProtocolProfile Protocol => ProtocolProfiles.Default;
		public static bool IsGoro => Version == Versions.Goro20080910;
		public static bool IsNostalRo20111102 => Version == Versions.NostalRo20111102;
		public static bool IsNostalRo20120307 => Version == Versions.NostalRo20120307;
		public static bool IsNostalRo => IsNostalRo20111102 || IsNostalRo20120307;

		/// <summary>True only for Sabine's grouped modern profile.</summary>
		public static bool IsSabine => Version == Versions.Sabine;

		/// <summary>
		/// The active server's loaded data, registered during
		/// <see cref="Server.LoadData"/>. Used by the script-facing
		/// feature accessors (<see cref="IsRenewal"/>) so they can be
		/// called from Sabine.Shared without taking a hard dependency
		/// on a particular server type.
		/// </summary>
		public static SabineData CurrentData { get; set; }

		/// <summary>
		/// The gameplay era resolved from <c>features.txt</c>. Defaults
		/// to <see cref="Era.Classic"/> when no data is loaded.
		/// </summary>
		public static Era Era
			=> CurrentData?.Features.GetEra() ?? Era.Classic;

		/// <summary>
		/// True when the Renewal feature flag is enabled in
		/// <c>features.txt</c>. Mirrors rAthena's <c>checkre()</c>.
		/// Returns false if data has not been loaded yet.
		/// </summary>
		public static bool IsRenewal => Era == Era.Renewal;

		/// <summary>
		/// True when the era is rAthena PRE-RE.
		/// </summary>
		public static bool IsPreRenewal => Era == Era.PreRenewal;

		/// <summary>
		/// True when the era is eAthena classic. Default.
		/// </summary>
		public static bool IsClassic => Era == Era.Classic;
	}

	/// <summary>
	/// A list of known packet versions.
	/// </summary>
	public static class Versions
	{
		// TODO: Just decided that custom version numbers would be a good
		//   idea, and now I'm confronted with the problem of what to name
		//   known versions, after I found that EP3 jRO and EP3 iRO don't
		//   have he same packet table. Granted, the difference is minor,
		//   but there is one, so... sub-episode versions? 3.1, 3.2, etc?
		//   Meh. Not ideal. I'll leave it like this for now, but the
		//   known versions should probably be the literal clients, like
		//   "euRO_20040512". The purpose of this class is to be able to
		//   easily reference clients you know after all.

		/// <summary>
		/// Sabine packet version 100, based on iRO Alpha 2001-08-30.
		/// </summary>
		public const int Alpha = 100;

		/// <summary>
		/// Sabine packet version 190, based on iRO Beta1 2001-12-14.
		/// </summary>
		public const int S190 = 190;

		/// <summary>
		/// Sabine packet version 200, based on iRO Beta 1 2002-02-20.
		/// </summary>
		public const int Beta1 = 200;

		/// <summary>
		/// Sabine packet version 300, based on jRO Beta 2 2002-08-09.
		/// </summary>
		public const int Beta2 = 300;

		/// <summary>
		/// Sabine packet version 350, based on iRO EP4 2003-04-30.
		/// </summary>
		public const int S350 = 350;

		/// <summary>
		/// Sabine packet version 400, based on jRO EP3 2003-05-27.
		/// </summary>
		public const int S400 = 400;

		/// <summary>
		/// Sabine packet version 500, based on iRO EP6 2003-10-31.
		/// </summary>
		public const int S500 = 500;

		/// <summary>
		/// Sabine packet version 600, based on euRO EP5 2004-05-12.
		/// </summary>
		public const int S600 = 600;

		/// <summary>
		/// Sabine packet version 700, based on iRO EP8 2004-08-03.
		/// </summary>
		public const int S700 = 700;

		/// <summary>
		/// Sabine packet version 800, based on bRO EP8 2004-12-28.
		/// </summary>
		public const int S800 = 800;

		/// <summary>
		/// Sabine packet version 900, based on bRO EP9 2006-01-31.
		/// </summary>
		public const int S900 = 900;

		/// <summary>
		/// Sabine packet version 2000, based on euRO EP10.4 2007-03-05.
		/// </summary>
		public const int S2000 = 2000;

		/// <summary>
		/// Sabine packet version 2500, based on kRO Sak EP12 2008-09-10.
		/// </summary>
		public const int S2500 = 2500;

		/// <summary>
		/// Standard RO wire profile pinned to PACKETVER 20111102. Its
		/// packet layouts are loaded after S2500 but before modern Sabine
		/// profiles, so date-specific actor packets can be selected without
		/// changing the historical version comparisons elsewhere.
		/// </summary>
		public const int NostalRo20111102 = 20111102;

		/// <summary>goro's plain Sakexe/rAthena 2008-09-10 profile.</summary>
		public const int Goro20080910 = 20080910;

		/// <summary>NostalRO's canonical 2012-03-07 profile.</summary>
		public const int NostalRo20120307 = 20120307;

		/// <summary>
		/// Modern client anchor: 2018-03-07bRagexeRE, the last build
		/// before kRO disabled packet obfuscation. Used as a regression
		/// fixture for the obfuscation/shuffle infrastructure.
		/// </summary>
		public const int RagexeRE20180307 = 18307;

		/// <summary>
		/// rAthena's default modern PACKETVER (2021-11-03 RagexeRE).
		/// Phase 1 target. Obfuscation is disabled at this version
		/// (post-2018 clients send opcodes in the clear).
		/// </summary>
		public const int Ragexe20211103 = 21103;

		/// <summary>
		/// Sabine-native protocol targeting the open-midgard custom
		/// client. Not pinned to any historical Ragnarok client —
		/// wire opcodes are renumbered into a clean grouped layout
		/// (0x00xx auth, 0x01xx char, 0x02xx CZ, 0x04xx+ ZC) on top
		/// of the S2500 effective packet bodies. Opt-in: set
		/// <c>packet_version</c> in <c>user\conf\version.conf</c> to
		/// switch a deployment onto this profile. The value sits far
		/// above the historical timeline so existing
		/// <c>Game.Version &gt;= Versions.X</c> checks (e.g. v4 actor
		/// entry, v3 item lists) naturally pick the modern variant.
		/// </summary>
		public const int Sabine = 50000;

		/// <summary>
		/// roBrowser / rAthena standard wire-ID protocol.
		/// Stacks body sizes from Alpha through S2500, then remaps
		/// all wire opcodes to the standard RO client IDs (0x0064+)
		/// used by rAthena, roBrowser (RBL), and AppleClient.
		/// Use this to connect standard RO clients or tools that
		/// expect the rAthena wire surface.
		/// </summary>
		public const int RBL = 60000;
	}

	/// <summary>
	/// Sizes of various elements in packets.
	/// </summary>
	public static class Sizes
	{
		/// <summary>
		/// Size of fixed-sized character name strings.
		/// </summary>
		public static int CharacterNames => Game.Version < Versions.Beta1 ? 16 : 24;

		/// <summary>
		/// Size of fixed-sized item name strings.
		/// </summary>
		public static int ItemNames => Game.Version < Versions.Beta1 ? 16 : 24;

		/// <summary>
		/// Size of fixed-sized username strings.
		/// </summary>
		public static int Usernames => Game.Version < Versions.Beta1 ? 16 : 24;

		/// <summary>
		/// Size of fixed-sized party name strings.
		/// </summary>
		public static int PartyNames => Game.Version < Versions.Beta1 ? 16 : 24;

		/// <summary>
		/// Size of fixed-sized skill name strings.
		/// </summary>
		public static int SkillNames => 24;
	}
}

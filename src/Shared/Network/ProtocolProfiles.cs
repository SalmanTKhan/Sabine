using System.Collections.Generic;
using Sabine.Shared.Const;

namespace Sabine.Shared.Network
{
	/// <summary>
	/// Registry of known <see cref="ProtocolProfile"/>s.
	/// </summary>
	/// <remarks>
	/// One entry per Sabine PACKETVER plus reserved slots for
	/// roadmap targets (Ragexe 20211103, open-midgard PV 23) so
	/// the IDs are stable from day one. Profiles for clients not
	/// yet implemented carry empty obfuscation/shuffle data; their
	/// transport flags will flip on in Phase 1 when the keys and
	/// shuffle maps are ported.
	/// </remarks>
	public static class ProtocolProfiles
	{
		private static readonly Dictionary<int, ProtocolProfile> ByVersion = new();
		private static readonly Dictionary<string, ProtocolProfile> ById = new();

		static ProtocolProfiles()
		{
			Register(new ProtocolProfile("alpha", Versions.Alpha, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("s190", Versions.S190, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("beta1", Versions.Beta1, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("beta2", Versions.Beta2, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("s350-iro-ep4", Versions.S350, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("s400-jro-ep3", Versions.S400, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("s500-iro-ep6", Versions.S500, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("s600-euro-ep5", Versions.S600, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("s700-iro-ep8", Versions.S700, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("s800-bro-ep8", Versions.S800, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("s900-bro-ep9", Versions.S900, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("s2000-euro-ep10", Versions.S2000, Era.Classic, ProtocolFeatures.None));
			Register(new ProtocolProfile("s2500-kro-sak-ep12", Versions.S2500, Era.Classic, ProtocolFeatures.CashShop));

			// Modern anchors. The 2018-03-07 RagexeRE keys come from
			// rAthena's clif_obfuscation.hpp and serve as the last
			// build before obfuscation was retired client-side. The
			// 2021-11-03 default for rAthena ships with no
			// obfuscation per the post-2018 short-circuit.
			Register(new ProtocolProfile(
				"ragexe-re-20180307",
				Versions.RagexeRE20180307,
				Era.PreRenewal,
				ProtocolFeatures.Rodex | ProtocolFeatures.CashShop | ProtocolFeatures.Achievements | ProtocolFeatures.Clan | ProtocolFeatures.BuyingStore,
				new ObfuscationKeys(0x47DA10EBu, 0x4B922CCFu, 0x765C5055u)));

			Register(new ProtocolProfile(
				"ragexe-20211103",
				Versions.Ragexe20211103,
				Era.Renewal,
				ProtocolFeatures.Rodex | ProtocolFeatures.CashShop | ProtocolFeatures.Achievements | ProtocolFeatures.Clan | ProtocolFeatures.BuyingStore | ProtocolFeatures.EquipmentSwitch | ProtocolFeatures.ItemOptions));

			// Sabine-native profile for the open-midgard custom client.
			// Inherits S2500 effective packet bodies but renumbers wire
			// IDs into a clean grouped layout. No obfuscation.
			Register(new ProtocolProfile("sabine", Versions.Sabine, Era.Classic, ProtocolFeatures.CashShop));
		}

		/// <summary>
		/// Registers a profile, overwriting any existing entry with
		/// the same id or version.
		/// </summary>
		public static void Register(ProtocolProfile profile)
		{
			ByVersion[profile.Version] = profile;
			ById[profile.Id] = profile;
		}

		/// <summary>
		/// Returns the profile whose <see cref="ProtocolProfile.Version"/>
		/// matches, or null if none is registered.
		/// </summary>
		public static ProtocolProfile GetByVersion(int version)
			=> ByVersion.TryGetValue(version, out var profile) ? profile : null;

		/// <summary>
		/// Returns the profile by its stable id (e.g. "alpha"),
		/// or null if none is registered.
		/// </summary>
		public static ProtocolProfile GetById(string id)
			=> ById.TryGetValue(id, out var profile) ? profile : null;

		/// <summary>
		/// Returns the profile that matches the running server's
		/// configured <see cref="Game.Version"/>, with the era
		/// overlaid from <see cref="Game.Era"/>. Falls back to the
		/// closest registered version less than or equal to the
		/// requested one (mirrors how <see cref="PacketTable"/>
		/// stacks tables).
		/// </summary>
		public static ProtocolProfile Default
		{
			get
			{
				if (!ByVersion.TryGetValue(Game.Version, out var profile))
					profile = ResolveClosest(Game.Version);

				if (profile == null)
					return null;

				// Era is sourced from features.txt and may differ
				// from the profile's static era. Prefer the runtime
				// era so renewal toggles work without re-registering.
				if (profile.Era == Game.Era)
					return profile;

				return new ProtocolProfile(
					profile.Id,
					profile.Version,
					Game.Era,
					profile.Features,
					profile.ObfuscationKeys,
					profile.ShuffleMap);
			}
		}

		private static ProtocolProfile ResolveClosest(int version)
		{
			ProtocolProfile best = null;
			foreach (var profile in ByVersion.Values)
			{
				if (profile.Version > version)
					continue;

				if (best == null || profile.Version > best.Version)
					best = profile;
			}
			return best;
		}
	}
}

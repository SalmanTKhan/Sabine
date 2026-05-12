using System.Collections.Generic;
using Sabine.Shared.Const;

namespace Sabine.Shared.Network
{
	/// <summary>
	/// Describes a client protocol variant: which packet table to use,
	/// which era's gameplay rules apply, and which transport-layer
	/// transforms (obfuscation, shuffle) the client expects.
	/// </summary>
	/// <remarks>
	/// A profile is the spine that ties together everything era-specific
	/// in one place. Phase 0 introduces the type and a registry; existing
	/// code paths keep using the static <see cref="PacketTable"/> driven
	/// by <see cref="Game.Version"/>. Later phases use
	/// <see cref="Connection.Profile"/> to switch obfuscation and shuffle
	/// per-connection so e.g. a 20211103 Ragexe and an open-midgard
	/// PACKETVER 23 client can share a server.
	/// </remarks>
	public sealed class ProtocolProfile
	{
		/// <summary>
		/// Stable identifier (e.g. "alpha", "beta1", "ragexe-20211103").
		/// </summary>
		public string Id { get; }

		/// <summary>
		/// PACKETVER value from <see cref="Versions"/>. Selects the
		/// packet table.
		/// </summary>
		public int Version { get; }

		/// <summary>
		/// Gameplay era. Drives formula switches (HP/SP, refine,
		/// SC math) independently of PACKETVER.
		/// </summary>
		public Era Era { get; }

		/// <summary>
		/// Three-key XOR obfuscation tuple (rAthena
		/// <c>clif_obfuscation.hpp</c>). Null when the client does
		/// not obfuscate packet IDs (pre-2011-08-17 and open-source
		/// clients like open-midgard).
		/// </summary>
		public ObfuscationKeys? ObfuscationKeys { get; }

		/// <summary>
		/// Per-PACKETVER opcode remap (rAthena
		/// <c>clif_shuffle.hpp</c>). Null when no shuffle applies.
		/// </summary>
		public IReadOnlyDictionary<int, int> ShuffleMap { get; }

		/// <summary>
		/// True when the framing layer must apply
		/// <see cref="ObfuscationKeys"/> on every outbound packet ID.
		/// </summary>
		public bool UsesObfuscation => this.ObfuscationKeys.HasValue;

		/// <summary>
		/// True when the framing layer must remap opcodes via
		/// <see cref="ShuffleMap"/>.
		/// </summary>
		public bool UsesShuffle => this.ShuffleMap != null && this.ShuffleMap.Count > 0;

		/// <summary>
		/// Capability flags for top-level systems. Used to gate
		/// feature code without spreading version comparisons.
		/// </summary>
		public ProtocolFeatures Features { get; }

		public ProtocolProfile(
			string id,
			int version,
			Era era,
			ProtocolFeatures features,
			ObfuscationKeys? obfuscationKeys = null,
			IReadOnlyDictionary<int, int> shuffleMap = null)
		{
			this.Id = id;
			this.Version = version;
			this.Era = era;
			this.Features = features;
			this.ObfuscationKeys = obfuscationKeys;
			this.ShuffleMap = shuffleMap;
		}
	}

	/// <summary>
	/// Three-key XOR seed used by rAthena-compatible clients
	/// from PACKETVER 20110817 onwards. Each outbound opcode is
	/// transformed by mixing the keys per packet.
	/// </summary>
	public readonly struct ObfuscationKeys
	{
		public uint Key1 { get; }
		public uint Key2 { get; }
		public uint Key3 { get; }

		public ObfuscationKeys(uint key1, uint key2, uint key3)
		{
			this.Key1 = key1;
			this.Key2 = key2;
			this.Key3 = key3;
		}
	}

	/// <summary>
	/// Capability flags for top-level systems. Phase 0 declares the
	/// shape; later phases flip flags on as features land.
	/// </summary>
	[System.Flags]
	public enum ProtocolFeatures
	{
		None = 0,
		Rodex = 1 << 0,
		CashShop = 1 << 1,
		Achievements = 1 << 2,
		Clan = 1 << 3,
		Roulette = 1 << 4,
		BuyingStore = 1 << 5,
		EquipmentSwitch = 1 << 6,
		ItemOptions = 1 << 7,
	}
}

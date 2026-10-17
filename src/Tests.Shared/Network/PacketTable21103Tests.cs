using Sabine.Shared;
using Sabine.Shared.Network;
using Xunit;

namespace Tests.Sabine.Shared.Network
{
	/// <summary>
	/// Smoke tests for the PACKETVER 20211103 (rAthena modern default)
	/// table overlay. Verifies the post-2018 CZ shuffle from
	/// rAthena clif_shuffle.hpp resolves correctly.
	/// </summary>
	[Collection("PacketTable")]
	public class PacketTable21103Tests
	{
		[Fact]
		public void Walk_to_xy_resolves_to_request_move()
		{
			using var _ = new PacketTableScope(Versions.Ragexe20211103);

			Assert.Equal(0x035F, PacketTable.ToNetwork(Op.CZ_REQUEST_MOVE));
			Assert.Equal(5, PacketTable.GetSize(0x035F));
			Assert.Equal(Op.CZ_REQUEST_MOVE, PacketTable.ToHost(0x035F));
		}

		[Fact]
		public void Want_to_connection_uses_re_2021_size()
		{
			using var _ = new PacketTableScope(Versions.Ragexe20211103);

			// Length carve-out: PACKETVER_RE >= 20211103 makes
			// CZ_ENTER 23 bytes (rAthena clif_shuffle.hpp:4744).
			Assert.Equal(0x0436, PacketTable.ToNetwork(Op.CZ_ENTER));
			Assert.Equal(23, PacketTable.GetSize(0x0436));
		}

		[Fact]
		public void Sak_era_placeholders_are_retired()
		{
			using var _ = new PacketTableScope(Versions.Ragexe20211103);

			// 0x035F-0x0368 must resolve to real CZ ops, not the
			// UNK_03XX placeholders S2500 leaves behind.
			Assert.Equal(Op.CZ_REQUEST_MOVE, PacketTable.ToHost(0x035F));
			Assert.Equal(Op.CZ_REQUEST_TIME, PacketTable.ToHost(0x0360));
			Assert.Equal(Op.CZ_CHANGE_DIRECTION, PacketTable.ToHost(0x0361));
			Assert.Equal(Op.CZ_ITEM_PICKUP, PacketTable.ToHost(0x0362));
			Assert.Equal(Op.CZ_ITEM_THROW, PacketTable.ToHost(0x0363));
			Assert.Equal(Op.CZ_REQNAME_BYGID, PacketTable.ToHost(0x0368));
			Assert.Equal(6, PacketTable.GetSize(0x0368));
		}

		[Fact]
		public void Action_request_and_use_skill_remap_to_post_2018_block()
		{
			using var _ = new PacketTableScope(Versions.Ragexe20211103);

			Assert.Equal(0x0437, PacketTable.ToNetwork(Op.CZ_REQUEST_ACT));
			Assert.Equal(7, PacketTable.GetSize(0x0437));

			Assert.Equal(0x0438, PacketTable.ToNetwork(Op.CZ_USE_SKILL));
			Assert.Equal(10, PacketTable.GetSize(0x0438));
		}

		/// <summary>
		/// Switches Game.Version + reloads the static PacketTable
		/// for one test, then restores it on dispose so other
		/// tests see the prior table state.
		/// </summary>
		private sealed class PacketTableScope : System.IDisposable
		{
			private readonly int _previousVersion;

			public PacketTableScope(int version)
			{
				_previousVersion = Game.Version;
				Game.Version = version;
				PacketTable.Load();
			}

			public void Dispose()
			{
				Game.Version = _previousVersion;
				PacketTable.Load();
			}
		}
	}
}

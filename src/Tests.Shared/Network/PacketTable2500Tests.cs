using Sabine.Shared;
using Sabine.Shared.Network;
using Xunit;

namespace Tests.Sabine.Shared.Network
{
	/// <summary>
	/// Smoke tests for the PACKETVER 2500 (kRO Sak EP12,
	/// 2008-09-10aSakexe) table overlay. Pins the wire shape that
	/// open-midgard's PV23 client expects so client interop does not
	/// regress silently.
	/// </summary>
	[Collection("PacketTable")]
	public class PacketTable2500Tests
	{
		[Fact]
		public void Want_to_connection_uses_cz_enter2()
		{
			using var _ = new PacketTableScope(Versions.S2500);

			Assert.Equal(0x0436, PacketTable.ToNetwork(Op.CZ_ENTER));
			Assert.Equal(19, PacketTable.GetSize(0x0436));
			Assert.Equal(Op.CZ_ENTER, PacketTable.ToHost(0x0436));
		}

		[Fact]
		public void Action_skill_item_remap_to_ep12_block()
		{
			using var _ = new PacketTableScope(Versions.S2500);

			Assert.Equal(0x0437, PacketTable.ToNetwork(Op.CZ_REQUEST_ACT));
			Assert.Equal(7, PacketTable.GetSize(0x0437));

			Assert.Equal(0x0438, PacketTable.ToNetwork(Op.CZ_USE_SKILL));
			Assert.Equal(10, PacketTable.GetSize(0x0438));

			Assert.Equal(0x0439, PacketTable.ToNetwork(Op.CZ_USE_ITEM));
			Assert.Equal(8, PacketTable.GetSize(0x0439));
		}

		[Fact]
		public void Walk_keeps_legacy_opcode_and_size()
		{
			using var _ = new PacketTableScope(Versions.S2500);

			// PV22 and PV23 both leave walktoxy at 0x00A7/8 — only the
			// connect/action/skill/item opcodes move into the 0x043X
			// block at EP12.
			Assert.Equal(0x00A7, PacketTable.ToNetwork(Op.CZ_REQUEST_MOVE));
			Assert.Equal(8, PacketTable.GetSize(0x00A7));
		}

		[Fact]
		public void Accept_enter_uses_ep12_opcode_and_size()
		{
			using var _ = new PacketTableScope(Versions.S2500);

			Assert.Equal(0x02EB, PacketTable.ToNetwork(Op.ZC_ACCEPT_ENTER));
			Assert.Equal(13, PacketTable.GetSize(0x02EB));
		}

		[Fact]
		public void Stand_entry_npc_resized_for_ep12()
		{
			using var _ = new PacketTableScope(Versions.S2500);

			// ZC_NOTIFY_STANDENTRY_NPC is a separate packet that keeps
			// its EP3-era wire ID at S2500 but is resized to 41 bytes.
			// (Base ZC_NOTIFY_STANDENTRY is asserted separately under
			// the consolidated actor-entry trio test below.)
			Assert.Equal(41, PacketTable.GetSize(PacketTable.ToNetwork(Op.ZC_NOTIFY_STANDENTRY_NPC)));
		}

		[Fact]
		public void Actor_entry_consolidated_to_base_at_ep12_wire_slots()
		{
			using var _ = new PacketTableScope(Versions.S2500);

			// After variant consolidation the v4 wire slots (0x02EE/
			// 0x02ED/0x02EC) and PV23 body sizes are owned by the base
			// ZC_NOTIFY_*ENTRY ops directly — STANDENTRY4/NEWENTRY4/
			// MOVEENTRY4 are no longer registered on the wire.
			Assert.Equal(0x02EE, PacketTable.ToNetwork(Op.ZC_NOTIFY_STANDENTRY));
			Assert.Equal(60, PacketTable.GetSize(0x02EE));

			Assert.Equal(0x02ED, PacketTable.ToNetwork(Op.ZC_NOTIFY_NEWENTRY));
			Assert.Equal(59, PacketTable.GetSize(0x02ED));

			Assert.Equal(0x02EC, PacketTable.ToNetwork(Op.ZC_NOTIFY_MOVEENTRY));
			Assert.Equal(67, PacketTable.GetSize(0x02EC));
		}

		[Fact]
		public void Storage_and_pickup_block_matches_eathena_pv23()
		{
			using var _ = new PacketTableScope(Versions.S2500);

			// eA db/packet_db.txt PV22 lines 1009-1010 + PV15 line 676:
			// at PV22+ the 0x00F5/0x00F7 slots are repurposed for
			// takeitem and movefromkafra; closekafra moved to 0x0193
			// at PV15. Without this pin the EP12 client's takeitem
			// (0x00F5/8) routes into the storage handler.
			Assert.Equal(0x00F5, PacketTable.ToNetwork(Op.CZ_ITEM_PICKUP));
			Assert.Equal(8, PacketTable.GetSize(0x00F5));
			Assert.Equal(Op.CZ_ITEM_PICKUP, PacketTable.ToHost(0x00F5));

			Assert.Equal(0x00F7, PacketTable.ToNetwork(Op.CZ_MOVE_ITEM_FROM_STORE_TO_BODY));
			Assert.Equal(22, PacketTable.GetSize(0x00F7));
			Assert.Equal(Op.CZ_MOVE_ITEM_FROM_STORE_TO_BODY, PacketTable.ToHost(0x00F7));

			Assert.Equal(0x0193, PacketTable.ToNetwork(Op.CZ_CLOSE_STORE));
			Assert.Equal(2, PacketTable.GetSize(0x0193));
			Assert.Equal(Op.CZ_CLOSE_STORE, PacketTable.ToHost(0x0193));
		}

		[Fact]
		public void Ep12_block_opcodes_resolve_to_real_ops()
		{
			using var _ = new PacketTableScope(Versions.S2500);

			// Negative regression: 0x0436-0x0439 must be the real
			// CZ ops after S2500 Redefine runs, not UNK_04XX
			// placeholders the upper-range Register loop reserves.
			Assert.Equal(Op.CZ_ENTER, PacketTable.ToHost(0x0436));
			Assert.Equal(Op.CZ_REQUEST_ACT, PacketTable.ToHost(0x0437));
			Assert.Equal(Op.CZ_USE_SKILL, PacketTable.ToHost(0x0438));
			Assert.Equal(Op.CZ_USE_ITEM, PacketTable.ToHost(0x0439));
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

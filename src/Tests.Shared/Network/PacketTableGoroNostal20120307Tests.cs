using Sabine.Shared;
using Sabine.Shared.Network;
using Xunit;

namespace Tests.Sabine.Shared.Network
{
	[Collection("PacketTable")]
	public class PacketTableGoroNostal20120307Tests
	{
		[Fact]
		public void Goro_profile_matches_plain_sakexe_packetdb()
		{
			using var _ = new Scope(Versions.Goro20080910);

			Assert.Equal("goro-20080910", ProtocolProfiles.Default.Id);
			Assert.False(PacketTable.IsRegistered(Op.ZC_AID));

			Assert.Equal(0x006D, PacketTable.ToNetwork(Op.HC_ACCEPT_MAKECHAR));
			Assert.Equal(110, PacketTable.GetSize(0x006D));
			Assert.Equal(0x0436, PacketTable.ToNetwork(Op.CZ_ENTER));
			Assert.Equal(0x0437, PacketTable.ToNetwork(Op.CZ_REQUEST_ACT));
			Assert.Equal(0x0438, PacketTable.ToNetwork(Op.CZ_USE_SKILL));
			Assert.Equal(0x0439, PacketTable.ToNetwork(Op.CZ_USE_ITEM));
			Assert.Equal(0x0089, PacketTable.ToNetwork(Op.CZ_REQUEST_TIME));
			Assert.Equal(0x008C, PacketTable.ToNetwork(Op.CZ_REQNAME));
			Assert.Equal(0x00A7, PacketTable.ToNetwork(Op.CZ_REQUEST_MOVE));
			Assert.Equal(0x0085, PacketTable.ToNetwork(Op.CZ_CHANGE_DIRECTION));
			Assert.Equal(0x00F5, PacketTable.ToNetwork(Op.CZ_ITEM_PICKUP));
			Assert.Equal(0x0116, PacketTable.ToNetwork(Op.CZ_ITEM_THROW));
			Assert.Equal(0x0113, PacketTable.ToNetwork(Op.CZ_USE_SKILL_TOGROUND));
			Assert.Equal(0x007E, PacketTable.ToNetwork(Op.CZ_USE_SKILL_TOGROUND_WITHTALKBOX));
			Assert.Equal(0x01FB, PacketTable.ToNetwork(Op.CH_DELETE_CHAR));

			Assert.Equal(19, PacketTable.GetSize(0x0436));
			Assert.Equal(7, PacketTable.GetSize(0x0437));
			Assert.Equal(10, PacketTable.GetSize(0x0438));
			Assert.Equal(8, PacketTable.GetSize(0x0439));
			Assert.Equal(8, PacketTable.GetSize(0x0089));
			Assert.Equal(11, PacketTable.GetSize(0x008C));
			Assert.Equal(8, PacketTable.GetSize(0x00A7));
			Assert.Equal(11, PacketTable.GetSize(0x0085));
			Assert.Equal(8, PacketTable.GetSize(0x00F5));
			Assert.Equal(10, PacketTable.GetSize(0x0116));
			Assert.Equal(22, PacketTable.GetSize(0x0113));
			Assert.Equal(102, PacketTable.GetSize(0x007E));
			Assert.Equal(56, PacketTable.GetSize(0x01FB));

			Assert.Equal(0x02EB, PacketTable.ToNetwork(Op.ZC_ACCEPT_ENTER));
			Assert.Equal(13, PacketTable.GetSize(0x02EB));
			Assert.Equal(0x0078, PacketTable.ToNetwork(Op.ZC_NOTIFY_STANDENTRY));
			Assert.Equal(0x0079, PacketTable.ToNetwork(Op.ZC_NOTIFY_NEWENTRY));
			Assert.Equal(0x007A, PacketTable.ToNetwork(Op.ZC_NOTIFY_ACTENTRY));
			Assert.Equal(0x007B, PacketTable.ToNetwork(Op.ZC_NOTIFY_MOVEENTRY));
			Assert.Equal(0x007C, PacketTable.ToNetwork(Op.ZC_NOTIFY_STANDENTRY_NPC));
			Assert.Equal(55, PacketTable.GetSize(0x0078));
			Assert.Equal(53, PacketTable.GetSize(0x0079));
			Assert.Equal(58, PacketTable.GetSize(0x007A));
			Assert.Equal(60, PacketTable.GetSize(0x007B));
			Assert.Equal(42, PacketTable.GetSize(0x007C));

			Assert.Equal(0x02E8, PacketTable.ToNetwork(Op.ZC_NORMAL_ITEMLIST));
			Assert.Equal(0x02D0, PacketTable.ToNetwork(Op.ZC_EQUIPMENT_ITEMLIST));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(0x02E8));
			Assert.Equal(PacketTable.Dynamic, PacketTable.GetSize(0x02D0));
			Assert.Equal(0x00BC, PacketTable.ToNetwork(Op.ZC_STATUS_CHANGE_ACK));
			Assert.Equal(0x00BD, PacketTable.ToNetwork(Op.ZC_STATUS));
			Assert.Equal(0x00BE, PacketTable.ToNetwork(Op.ZC_STATUS_CHANGE));
			Assert.Equal(44, PacketTable.GetSize(0x00BD));
			Assert.Equal(0x010E, PacketTable.ToNetwork(Op.ZC_SKILLINFO_UPDATE));
			Assert.Equal(0x010F, PacketTable.ToNetwork(Op.ZC_SKILLINFO_LIST));
			Assert.Equal(0x0111, PacketTable.ToNetwork(Op.ZC_ADD_SKILL));
			Assert.Equal(0x0114, PacketTable.ToNetwork(Op.ZC_NOTIFY_SKILL));
			Assert.Equal(31, PacketTable.GetSize(0x0114));

			Assert.Equal(Op.CZ_REQUEST_ACT, PacketTable.ToHost(0x0190));
			Assert.Equal(19, PacketTable.GetSize(0x0190));
			Assert.Equal(19, PacketTable.GetSize(0x00E9));
		}

		[Fact]
		public void Goro_overlay_does_not_leak_between_profiles()
		{
			using (new Scope(Versions.Goro20080910))
			{
				Assert.False(PacketTable.IsRegistered(Op.ZC_AID));
				Assert.Equal(0x0437, PacketTable.ToNetwork(Op.CZ_REQUEST_ACT));
			}

			using (new Scope(Versions.Sabine))
			{
				Assert.True(PacketTable.IsRegistered(Op.ZC_AID));
				Assert.NotEqual(0x0437, PacketTable.ToNetwork(Op.CZ_REQUEST_ACT));
			}

			using (new Scope(Versions.NostalRo20111102))
			{
				Assert.True(PacketTable.IsRegistered(Op.ZC_AID));
				Assert.NotEqual(0x0437, PacketTable.ToNetwork(Op.CZ_REQUEST_ACT));
			}

			using (new Scope(Versions.Alpha))
			{
				Assert.NotEqual(0x0078, PacketTable.ToNetwork(Op.ZC_NOTIFY_STANDENTRY));
				Assert.NotEqual(0x02EB, PacketTable.ToNetwork(Op.ZC_ACCEPT_ENTER));
			}
		}

		[Fact]
		public void Nostal_20120307_uses_date_specific_client_packets()
		{
			using var _ = new Scope(Versions.NostalRo20120307);

			Assert.Equal("nostalro-20120307", ProtocolProfiles.Default.Id);
			Assert.Equal(0x086A, PacketTable.ToNetwork(Op.CZ_ENTER));
			Assert.Equal(0x0437, PacketTable.ToNetwork(Op.CZ_REQUEST_MOVE));
			Assert.Equal(0x0885, PacketTable.ToNetwork(Op.CZ_REQUEST_ACT));
			Assert.Equal(0x0887, PacketTable.ToNetwork(Op.CZ_REQUEST_TIME));
			Assert.Equal(0x0890, PacketTable.ToNetwork(Op.CZ_CHANGE_DIRECTION));
			Assert.Equal(0x0970, PacketTable.ToNetwork(Op.CH_MAKE_CHAR));
			Assert.Equal(31, PacketTable.GetSize(0x0970));
			Assert.Equal(Op.CH_DELETE_CHAR3_RESERVED, PacketTable.ToHost(0x0827));
			Assert.Equal(Op.CH_DELETE_CHAR3, PacketTable.ToHost(0x0829));
			Assert.Equal(Op.CH_DELETE_CHAR3_CANCEL, PacketTable.ToHost(0x082B));
			Assert.Equal(Op.CZ_REQUEST_MOVE, PacketTable.ToHost(0x0437));
		}

		private sealed class Scope : System.IDisposable
		{
			private readonly int _previous;
			public Scope(int version)
			{
				_previous = Game.Version;
				Game.Version = version;
				PacketTable.Load();
			}
			public void Dispose()
			{
				Game.Version = _previous;
				PacketTable.Load();
			}
		}
	}
}

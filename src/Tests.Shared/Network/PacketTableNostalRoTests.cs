using System;
using System.Linq;
using Sabine.Shared;
using Sabine.Shared.Network;
using Xunit;

namespace Tests.Sabine.Shared.Network
{
	[Collection("PacketTable")]
	public sealed class PacketTableNostalRoTests
	{
		[Fact]
		public void Actor_and_movement_packets_match_the_20111102_wire_table()
		{
			using var _ = new PacketTableScope(Versions.NostalRo20111102);

			AssertPacket(Op.ZC_NOTIFY_STANDENTRY, 0x0078, 55);
			AssertPacket(Op.ZC_NOTIFY_NEWENTRY, 0x0079, 53);
			AssertPacket(Op.ZC_NOTIFY_MOVEENTRY, 0x007B, 60);
			AssertPacket(Op.ZC_NOTIFY_VANISH, 0x0080, 7);
			AssertPacket(Op.ZC_NOTIFY_MOVE, 0x0086, 16);
			AssertPacket(Op.ZC_NOTIFY_PLAYERMOVE, 0x0087, 12);
			AssertPacket(Op.ZC_STOPMOVE, 0x0088, 10);
			AssertPacket(Op.ZC_NOTIFY_ACT, 0x008A, 29);
			AssertPacket(Op.ZC_ACK_REQNAME, 0x0095, 30);
			AssertPacket(Op.ZC_CHANGE_DIRECTION, 0x009C, 9);
			AssertPacket(Op.CZ_CANCEL_LOCKON, 0x0118, 2);
			AssertPacket(Op.CZ_REQUEST_MOVE, 0x0364, 5);
			AssertPacket(Op.CZ_CHANGE_DIRECTION, 0x0366, 5);
			AssertPacket(Op.CZ_REQUEST_ACT, 0x08AA, 7);
			AssertPacket(Op.CZ_REQNAME, 0x088A, 6);
		}

		[Fact]
		public void Nostal_profile_has_no_duplicate_wire_ids_and_does_not_load_modern_bodies()
		{
			using var _ = new PacketTableScope(Versions.NostalRo20111102);

			var actorOps = new[]
			{
				Op.ZC_NOTIFY_STANDENTRY, Op.ZC_NOTIFY_NEWENTRY, Op.ZC_NOTIFY_MOVEENTRY,
				Op.ZC_NOTIFY_VANISH, Op.ZC_NOTIFY_MOVE, Op.ZC_NOTIFY_PLAYERMOVE,
				Op.ZC_STOPMOVE, Op.ZC_NOTIFY_ACT, Op.ZC_ACK_REQNAME, Op.ZC_CHANGE_DIRECTION,
				Op.CZ_CANCEL_LOCKON, Op.CZ_REQUEST_MOVE, Op.CZ_CHANGE_DIRECTION,
				Op.CZ_REQUEST_ACT, Op.CZ_REQNAME
			};
			Assert.Equal(actorOps.Length, actorOps.Select(PacketTable.ToNetwork).Distinct().Count());
			Assert.Equal(19, PacketTable.GetSize(0x083C));
			Assert.Equal(11, PacketTable.GetSize(0x0073));
			Assert.False(Game.IsSabine);
			Assert.True(Game.IsNostalRo);
		}

		private static void AssertPacket(Op op, int wireId, int size)
		{
			Assert.Equal(wireId, PacketTable.ToNetwork(op));
			Assert.Equal(op, PacketTable.ToHost(wireId));
			Assert.Equal(size, PacketTable.GetSize(wireId));
		}

		private sealed class PacketTableScope : IDisposable
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

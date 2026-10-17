using System;
using System.Linq;
using Sabine.Shared;
using Sabine.Shared.Network;
using Xunit;

namespace Tests.Sabine.Shared.Network
{
	[Collection("PacketTable")]
	public class PacketTableEarlyClientTests
	{
		[Fact]
		public void Alpha_party_packets_use_16_character_name_shapes()
		{
			using var _ = new PacketTableScope(Versions.Alpha);

			AssertPacket(Op.ZC_ACK_REQ_JOIN_GROUP, 0x0098, 19);
			AssertPacket(Op.ZC_REQ_JOIN_GROUP, 0x0099, 22);
			AssertPacket(Op.CZ_REQ_EXPEL_GROUP_MEMBER, 0x009C, 22);
			AssertPacket(Op.ZC_ADD_MEMBER_TO_GROUP, 0x009D, 63);
			AssertPacket(Op.ZC_DELETE_MEMBER_FROM_GROUP, 0x009E, 23);
		}

		[Fact]
		public void Alpha_includes_skill_upgrade_packet()
		{
			using var _ = new PacketTableScope(Versions.Alpha);

			AssertPacket(Op.CZ_UPGRADE_SKILLLEVEL, 0x00AB, 4);
		}

		[Fact]
		public void Beta_190_restores_24_character_party_shapes_after_insert()
		{
			using var _ = new PacketTableScope(Versions.S190);

			AssertPacket(Op.ZC_ACK_REQ_JOIN_GROUP, 0x0099, 27);
			AssertPacket(Op.ZC_REQ_JOIN_GROUP, 0x009A, 30);
			AssertPacket(Op.CZ_REQ_EXPEL_GROUP_MEMBER, 0x009D, 30);
			AssertPacket(Op.ZC_ADD_MEMBER_TO_GROUP, 0x009E, 79);
			AssertPacket(Op.ZC_DELETE_MEMBER_FROM_GROUP, 0x009F, 31);
			AssertPacket(Op.CZ_UPGRADE_SKILLLEVEL, 0x00AC, 4);
		}

		[Fact]
		public void Beta1_200_keeps_groupinfo_insertions_and_party_shapes()
		{
			using var _ = new PacketTableScope(Versions.Beta1);

			AssertPacket(Op.ZC_GROUPINFO_CHANGE, 0x009D, 6);
			AssertPacket(Op.CZ_CHANGE_GROUPEXPOPTION, 0x009E, 6);
			AssertPacket(Op.CZ_REQ_EXPEL_GROUP_MEMBER, 0x009F, 30);
			AssertPacket(Op.ZC_ADD_MEMBER_TO_GROUP, 0x00A0, 79);
			AssertPacket(Op.ZC_DELETE_MEMBER_FROM_GROUP, 0x00A1, 31);
			AssertPacket(Op.CZ_UPGRADE_SKILLLEVEL, 0x00AE, 4);
		}

		[Fact]
		public void Early_client_tables_do_not_have_duplicate_wire_ids()
		{
			foreach (var version in new[] { Versions.Alpha, Versions.S190, Versions.Beta1 })
			{
				using (new PacketTableScope(version))
				{
					var duplicate = PacketTable.GetTable()
						.GroupBy(static entry => entry.OpNetwork)
						.FirstOrDefault(static group => group.Count() > 1);

					Assert.Null(duplicate);
				}
			}
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

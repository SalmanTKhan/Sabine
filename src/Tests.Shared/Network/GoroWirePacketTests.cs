using System;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.Network.Helpers;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;
using Xunit;

namespace Tests.Sabine.Shared.Network
{
	[Collection("PacketTable")]
	public class GoroWirePacketTests
	{
		[Fact]
		public void Legacy_actor_serializers_keep_rathena_record_lengths_and_offsets()
		{
			using var _ = new PacketTableScope(Versions.Goro20080910);
			var actor = new TestCharacter
			{
				Position = new Position(123, 456),
				Direction = Direction.East,
				HeadTurn = HeadTurn.Left,
				HairId = 11,
				WeaponLook = 22,
				HeadBottomLook = 33,
				HeadTopLook = 44,
				HeadMiddleLook = 55,
			};
			actor.Parameters.Speed = 777;

			using (var packet = Packet.Rent(Op.ZC_NOTIFY_STANDENTRY))
			{
				packet.AddStandEntry(actor);
				var body = packet.Build();
				Assert.Equal(53, body.Length);
				Assert.Equal(1, body[0]); // objecttype; header is added by transport
				Assert.Equal(actor.Handle, BitConverter.ToInt32(body, 1));
				Assert.Equal(777, BitConverter.ToInt16(body, 5));
				Assert.Equal(actor.IdentityId.ToNetwork(), BitConverter.ToInt16(body, 13));
			}

			using (var packet = Packet.Rent(Op.ZC_NOTIFY_NEWENTRY))
			{
				packet.AddNewEntry(actor);
				var body = packet.Build();
				Assert.Equal(51, body.Length);
				Assert.Equal(actor.Handle, BitConverter.ToInt32(body, 0));
				Assert.Equal(actor.IdentityId.ToNetwork(), BitConverter.ToInt16(body, 12));
			}

			using (var packet = Packet.Rent(Op.ZC_NOTIFY_MOVEENTRY))
			{
				packet.AddMoveEntry(actor, new Position(100, 200), new Position(123, 456));
				var body = packet.Build();
				Assert.Equal(58, body.Length);
				Assert.Equal(actor.Handle, BitConverter.ToInt32(body, 0));
				Assert.Equal(777, BitConverter.ToInt16(body, 4));
			}
		}

		[Fact]
		public void Legacy_npc_spawn_serializer_is_the_42_byte_rathena_shape()
		{
			using var _ = new PacketTableScope(Versions.Goro20080910);
			var actor = new TestCharacter();
			actor.Parameters.Speed = 400;

			using var packet = Packet.Rent(Op.ZC_NOTIFY_STANDENTRY_NPC);
			packet.AddStandEntryNpc(actor);
			var body = packet.Build();

			Assert.Equal(40, body.Length);
			Assert.Equal(1, body[0]);
			Assert.Equal(actor.Handle, BitConverter.ToInt32(body, 1));
			Assert.Equal(actor.IdentityId.ToNetwork(), BitConverter.ToInt16(body, 19));
			Assert.Equal(0, body[33]); // PK mode
		}

		[Fact]
		public void Skill_info_update_uses_the_11_byte_legacy_record()
		{
			using var _ = new PacketTableScope(Versions.Goro20080910);
			using var packet = Send.BuildSkillInfoUpdatePacket(
				SkillId.MG_FIREBOLT, 3, 12, 5, true);
			var body = packet.Build();

			Assert.Equal(9, body.Length); // 11 bytes including the opcode
			Assert.Equal((int)SkillId.MG_FIREBOLT, BitConverter.ToInt16(body, 0));
			Assert.Equal(3, BitConverter.ToInt16(body, 2));
			Assert.Equal(12, BitConverter.ToInt16(body, 4));
			Assert.Equal(5, BitConverter.ToInt16(body, 6));
			Assert.Equal(1, body[8]);
		}

		private sealed class TestCharacter : Character
		{
			public TestCharacter()
			{
				Handle = 0x11223344;
				IdentityId = (IdentityId)0x1234;
				Parameters = new TestParameters();
			}

			public override int Handle { get; protected set; }
			public override IdentityId IdentityId { get; protected set; }
			public override void Warp(Location location) { }
		}

		private sealed class TestParameters : Parameters
		{
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

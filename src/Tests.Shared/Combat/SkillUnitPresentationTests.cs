using System;
using System.Text;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Maps.SkillUnits;
using Xunit;

namespace Tests.Sabine.Shared.Combat
{
	public class SkillUnitPresentationTests
	{
		static SkillUnitPresentationTests()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		[Theory]
		[InlineData(SkillId.MG_SAFETYWALL, SkillUnitId.SafetyWall)]
		[InlineData(SkillId.MG_FIREWALL, SkillUnitId.FireWall)]
		[InlineData(SkillId.AL_PNEUMA, SkillUnitId.Pneuma)]
		[InlineData(SkillId.HT_ANKLESNARE, SkillUnitId.AnkleSnare)]
		[InlineData(SkillId.BD_LULLABY, SkillUnitId.Lullaby)]
		[InlineData(SkillId.HW_GRAVITATION, SkillUnitId.Gravitation)]
		public void Skill_ids_resolve_to_rbl_unit_ids(SkillId skillId, SkillUnitId expected)
		{
			Assert.Equal(expected, SkillUnit.ResolveUnitId(skillId));
		}

		[Fact]
		public void Modern_skill_entry_uses_the_fixed_16_byte_unit_layout()
		{
			using var packet = Send.BuildSkillEntryPacket(
				true,
				700000001,
				1234,
				SkillUnitId.FireWall,
				136,
				324);

			var body = packet.Build();
			Assert.Equal(14, body.Length); // plus two-byte opcode on the wire
			Assert.Equal(700000001, BitConverter.ToInt32(body, 0));
			Assert.Equal(1234, BitConverter.ToInt32(body, 4));
			Assert.Equal(136, BitConverter.ToInt16(body, 8));
			Assert.Equal(324, BitConverter.ToInt16(body, 10));
			Assert.Equal((byte)SkillUnitId.FireWall, body[12]);
			Assert.Equal(1, body[13]);
		}

		[Fact]
		public void Beta1_skill_entry_keeps_the_11_byte_legacy_layout()
		{
			using var packet = Send.BuildSkillEntryPacket(
				false,
				700000001,
				1234,
				SkillUnitId.Pneuma,
				55,
				66);

			var body = packet.Build();
			Assert.Equal(9, body.Length); // plus two-byte opcode on the wire
			Assert.Equal(55, BitConverter.ToInt16(body, 4));
			Assert.Equal(66, BitConverter.ToInt16(body, 6));
			Assert.Equal((byte)SkillUnitId.Pneuma, body[8]);
		}
	}
}

using System;
using System.Text;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Xunit;

namespace Tests.Sabine.Shared.Network
{
	public class SkillDamagePacketTests
	{
		static SkillDamagePacketTests()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}

		[Theory]
		[InlineData(false, 29, 26)]
		[InlineData(true, 31, 28)]
		public void NotifySkill_serializes_the_requested_multi_hit_count(bool useV2, int expectedLength, int countOffset)
		{
			using var packet = Send.BuildNotifySkillPacket(
				useV2,
				SkillId.MG_LIGHTNINGBOLT,
				123,
				456,
				789,
				800,
				1000,
				240,
				4,
				4,
				ActionType.Skill);

			var body = packet.Build();
			Assert.Equal(expectedLength, body.Length);
			Assert.Equal(800, BitConverter.ToInt32(body, 14));
			Assert.Equal(4, BitConverter.ToInt16(body, countOffset));
		}

		[Fact]
		public void NotifySkill_clamps_invalid_hit_count_to_one()
		{
			using var packet = Send.BuildNotifySkillPacket(
				true,
				SkillId.MG_FIREBOLT,
				1,
				2,
				3,
				4,
				5,
				6,
				7,
				0,
				ActionType.Skill);

			Assert.Equal(1, BitConverter.ToInt16(packet.Build(), 28));
		}
	}
}

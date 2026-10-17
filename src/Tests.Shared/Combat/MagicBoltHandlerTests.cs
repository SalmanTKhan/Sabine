using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Skills.Handlers.Magician;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;
using Xunit;

namespace Tests.Sabine.Shared.Combat
{
	public class MagicBoltHandlerTests
	{
		private sealed class TestCharacter : Character
		{
			public TestCharacter()
			{
				this.Parameters = new Parameters<TestCharacter>(this);
			}

			public override int Handle { get; protected set; }
			public override IdentityId IdentityId { get; protected set; }
			public override void Warp(Location location) { }
		}

		[Theory]
		[InlineData(1, 1)]
		[InlineData(2, 2)]
		[InlineData(5, 5)]
		[InlineData(10, 10)]
		[InlineData(0, 1)]
		[InlineData(99, 10)]
		public void Bolt_context_uses_one_hit_and_one_matk_ratio_per_level(int level, int expectedHits)
		{
			var caster = new TestCharacter();
			var target = new TestCharacter();

			var context = MagicBoltHandler.CreateAttackContext(
				caster,
				target,
				SkillId.MG_FIREBOLT,
				level,
				ElementType.Fire);

			Assert.Equal(expectedHits, context.HitCount);
			Assert.Equal((float)expectedHits, context.SkillRatio);
			Assert.Equal(SkillId.MG_FIREBOLT, context.SkillId);
			Assert.Equal(ElementType.Fire, context.AttackElement);
			Assert.Equal(AttackKind.Magic, context.Kind);
		}

		[Theory]
		[InlineData(1)]
		[InlineData(5)]
		[InlineData(10)]
		public void Bolt_context_calculates_nonzero_aggregated_multi_hit_damage(int level)
		{
			var caster = new TestCharacter();
			var target = new TestCharacter();
			var context = MagicBoltHandler.CreateAttackContext(
				caster,
				target,
				SkillId.MG_LIGHTNINGBOLT,
				level,
				ElementType.Wind);
			context.AlwaysHits = true;

			var result = BattleCalculator.Calc(context);

			Assert.False(result.IsMiss);
			Assert.Equal(level, result.HitCount);
			Assert.True(result.DamagePerHit > 0);
			Assert.Equal(result.DamagePerHit * level, result.Damage);
		}

		[Fact]
		public void Bolt_impact_uses_the_raw_attack_motion_delay()
		{
			var caster = new TestCharacter();
			caster.Parameters.AttackMotionDelay = 800;

			Assert.Equal(800, MagicBoltHandler.ResolveImpactDelayMs(caster));
		}
	}
}

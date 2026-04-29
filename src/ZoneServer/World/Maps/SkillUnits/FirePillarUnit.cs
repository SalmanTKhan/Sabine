using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// WZ_FIREPILLAR cell. Two-stage: first ~20s as a "primed" pillar
	/// that does nothing, then triggers when an enemy steps onto it,
	/// dealing Fire magic in a small splash. Approximated here as a
	/// single-trigger Fire hit on touch.
	/// </summary>
	public class FirePillarUnit : SkillUnit
	{
		public FirePillarUnit(Character owner, Position position, int level)
			: base(owner, position, SkillId.WZ_FIREPILLAR, level, TimeSpan.FromSeconds(30))
		{
			this.HitsRemaining = 1;
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == null || !this.IsEnemy(entrant)) return;

			var ctx = new AttackContext(this.Owner, entrant)
			{
				SkillId = SkillId.WZ_FIREPILLAR,
				SkillLevel = this.SkillLevel,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f + 0.50f * this.SkillLevel,
				AttackElement = ElementType.Fire,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				entrant.TakeDamage(result.Damage, this.Owner);
				Send.ZC_NOTIFY_SKILL(this.Owner, entrant.Handle, SkillId.WZ_FIREPILLAR, this.SkillLevel, result.Damage, 0, 0, ActionType.Skill);
			}

			if (this.HitsRemaining > 0)
				this.HitsRemaining--;
		}
	}
}

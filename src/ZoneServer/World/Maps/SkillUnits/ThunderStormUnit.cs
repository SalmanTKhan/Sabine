using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// MG_THUNDERSTORM cell — Wind magic damage on touch. The handler
	/// spawns several of these in a cross / box around the target so an
	/// enemy in the AOE can be hit by multiple cells in quick succession.
	/// </summary>
	public class ThunderStormUnit : SkillUnit
	{
		private static readonly TimeSpan HitInterval = TimeSpan.FromMilliseconds(250);
		private DateTime _nextHitAt = DateTime.MinValue;

		public ThunderStormUnit(Character owner, Position position, int level)
			: base(owner, position, SkillId.MG_THUNDERSTORM, level, TimeSpan.FromSeconds(3))
		{
			this.HitsRemaining = 1 + level / 2;
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == this.Owner) return;
			if (!this.IsEnemy(entrant)) return;
			if (entrant.IsHidden) return;

			var now = DateTime.Now;
			if (now < _nextHitAt) return;
			_nextHitAt = now + HitInterval;

			var ctx = new AttackContext(this.Owner, entrant)
			{
				SkillId = SkillId.MG_THUNDERSTORM,
				SkillLevel = this.SkillLevel,
				Kind = AttackKind.Magic,
				SkillRatio = 0.8f + 0.4f * this.SkillLevel,
				AttackElement = ElementType.Wind,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				entrant.TakeDamage(result.Damage, this.Owner);
				Send.ZC_NOTIFY_SKILL(this.Owner, entrant.Handle, SkillId.MG_THUNDERSTORM, this.SkillLevel, result.Damage, 0, 0, ActionType.Skill);
			}

			if (this.HitsRemaining > 0)
				this.HitsRemaining--;
		}
	}
}

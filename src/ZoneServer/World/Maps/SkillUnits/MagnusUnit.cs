using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// PR_MAGNUS cell. Periodic Holy magic damage to Undead/Demon
	/// enemies standing on the unit. Lifetime ≈ 0.5s * level.
	/// </summary>
	public class MagnusUnit : SkillUnit
	{
		private static readonly TimeSpan HitInterval = TimeSpan.FromSeconds(1);
		private DateTime _nextHitAt = DateTime.MinValue;

		public MagnusUnit(Character owner, Position position, int level)
			: base(owner, position, SkillId.PR_MAGNUS, level, TimeSpan.FromMilliseconds(500 * level + 1500))
		{
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == null || entrant == this.Owner) return;
			if (!this.IsEnemy(entrant)) return;
			if (!IsUndeadOrDemon(entrant)) return;

			var now = DateTime.Now;
			if (now < _nextHitAt) return;
			_nextHitAt = now + HitInterval;

			var ctx = new AttackContext(this.Owner, entrant)
			{
				SkillId = SkillId.PR_MAGNUS,
				SkillLevel = this.SkillLevel,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f + 0.20f * this.SkillLevel,
				AttackElement = ElementType.Holy,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				entrant.TakeDamage(result.Damage, this.Owner);
				Send.ZC_NOTIFY_SKILL(this.Owner, entrant.Handle, SkillId.PR_MAGNUS, this.SkillLevel, result.Damage, 0, 0, ActionType.Skill);
			}
		}

		private static bool IsUndeadOrDemon(Character target)
		{
			if (target is not Monster monster) return false;
			return monster.Data.Element == ElementType.Undead
				|| monster.Data.Race == RaceType.Undead
				|| monster.Data.Race == RaceType.Demon;
		}
	}
}

using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// MG_FIREWALL — deals magic Fire damage to enemies who walk onto
	/// the cell. Each hit consumes one charge; classic Fire Wall is
	/// 3+level hits with a short duration.
	/// </summary>
	public class FireWallUnit : SkillUnit
	{
		// Throttle to avoid hitting the same target twice in the same
		// tick window. eAthena uses ~300ms per hit.
		private static readonly TimeSpan HitInterval = TimeSpan.FromMilliseconds(300);
		private DateTime _nextHitAt = DateTime.MinValue;

		public FireWallUnit(Character owner, Position position, int level)
			: base(owner, position, SkillId.MG_FIREWALL, level, TimeSpan.FromSeconds(4 + level))
		{
			this.HitsRemaining = 3 + level;
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
				SkillId = SkillId.MG_FIREWALL,
				SkillLevel = this.SkillLevel,
				Kind = AttackKind.Magic,
				SkillRatio = 0.5f,
				AttackElement = ElementType.Fire,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				entrant.TakeDamage(result.Damage, this.Owner);
				Send.ZC_NOTIFY_SKILL(this.Owner, entrant.Handle, SkillId.MG_FIREWALL, this.SkillLevel, result.Damage, 0, 0, ActionType.Skill);
			}

			if (this.HitsRemaining > 0)
				this.HitsRemaining--;
		}
	}
}

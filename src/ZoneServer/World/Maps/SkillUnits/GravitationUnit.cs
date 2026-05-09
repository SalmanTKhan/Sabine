using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// HW_GRAVITATION cell — fixed neutral damage of 200 + 200*level on
	/// enemies inside the AoE every second. eAthena battle.c:2796 sets
	/// the damage as <c>md.damage = 200 + 200*skill_lv</c> (BF_MISC,
	/// no flinch). Skips DEF / size / element entirely. Lifetime
	/// follows skill_cast_db.txt:815 (5s + 1s/level).
	/// </summary>
	public class GravitationUnit : SkillUnit
	{
		private static readonly TimeSpan HitInterval = TimeSpan.FromSeconds(1);
		private DateTime _nextHitAt = DateTime.MinValue;

		public GravitationUnit(Character owner, Position position, int level, TimeSpan duration)
			: base(owner, position, SkillId.HW_GRAVITATION, level, duration)
		{
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == null || entrant == this.Owner) return;
			if (!this.IsEnemy(entrant)) return;

			var now = DateTime.Now;
			if (now < _nextHitAt) return;
			_nextHitAt = now + HitInterval;

			var damage = 200 + 200 * this.SkillLevel;
			entrant.TakeDamage(damage, this.Owner);
			Send.ZC_NOTIFY_SKILL(this.Owner, entrant.Handle, SkillId.HW_GRAVITATION, this.SkillLevel, damage, 0, 0, ActionType.Skill);
		}
	}
}

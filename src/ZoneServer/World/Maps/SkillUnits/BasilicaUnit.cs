using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// HP_BASILICA — refuses physical attacks (melee or ranged) on
	/// any character standing in the cells. Magic still lands;
	/// eAthena classic Basilica only blocks weapon damage. Doesn't
	/// deplete on hit; the time-based expiry handles removal.
	/// Friendly entrants pick up the <see cref="StatusId.Basilica"/>
	/// marker via <see cref="OnTouch"/> so future "caster can't move"
	/// work has a flag to read.
	/// </summary>
	public class BasilicaUnit : SkillUnit
	{
		private static readonly TimeSpan StatusBleed = TimeSpan.FromSeconds(5);

		public BasilicaUnit(Character owner, Position position, int level, TimeSpan duration)
			: base(owner, position, SkillId.HP_BASILICA, level, duration)
		{
		}

		public override bool BlocksAttack(AttackContext ctx, bool isRanged)
		{
			return ctx.Kind == AttackKind.Physical;
		}

		public override void OnAttackBlocked(AttackContext ctx)
		{
			// Time-based; no hit counter to decrement.
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == null) return;
			if (entrant.IsHostileTo(this.Owner)) return;
			if (entrant.StatusEffects.Has(StatusId.Basilica)) return;

			entrant.StatusEffects.Start(StatusId.Basilica, this.SkillLevel, StatusBleed, this.Owner);
		}
	}
}

using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// Persistent song / dance / ensemble aura. Applies a status to
	/// allies (or enemies, depending on the skill) standing inside
	/// the unit's footprint. The status carries the actual stat
	/// modifications. Lifetime ≈ 30s by default.
	/// </summary>
	public class SongUnit : SkillUnit
	{
		public StatusId AppliedStatus { get; }
		public bool AffectsEnemies { get; }

		public SongUnit(Character owner, Position position, SkillId skillId, int level, StatusId applied, bool affectsEnemies = false)
			: base(owner, position, skillId, level, TimeSpan.FromSeconds(60))
		{
			this.AppliedStatus = applied;
			this.AffectsEnemies = affectsEnemies;
		}

		// Per-tick the song unit fires OnTouch on every character
		// standing on the cell. If the bearer already has the status,
		// extend its duration instead of restarting it — restarting
		// would re-run OnEnd/OnStart and spam the status-icon broadcast,
		// pinging the stat-bonus cache every tick.
		private static readonly TimeSpan TickWindow = TimeSpan.FromSeconds(5);

		public override void OnTouch(Character entrant)
		{
			if (entrant == null) return;
			var hostile = entrant.IsHostileTo(this.Owner);
			if (this.AffectsEnemies != hostile) return;

			if (entrant.StatusEffects.Refresh(this.AppliedStatus, TickWindow))
				return;

			entrant.StatusEffects.Start(this.AppliedStatus, this.SkillLevel, TickWindow, this.Owner);
		}
	}
}

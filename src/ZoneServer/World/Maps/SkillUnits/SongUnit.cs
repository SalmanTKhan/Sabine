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

		public override void OnTouch(Character entrant)
		{
			if (entrant == null) return;
			var hostile = entrant.IsHostileTo(this.Owner);
			if (this.AffectsEnemies != hostile) return;
			if (entrant.StatusEffects.Has(this.AppliedStatus)) return;

			entrant.StatusEffects.Start(this.AppliedStatus, this.SkillLevel, TimeSpan.FromSeconds(5), this.Owner);
		}
	}
}

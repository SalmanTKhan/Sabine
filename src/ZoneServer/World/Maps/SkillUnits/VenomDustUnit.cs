using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// AS_VENOMDUST cell. Applies Poison to enemies that step on it.
	/// Persists ~30s.
	/// </summary>
	public class VenomDustUnit : SkillUnit
	{
		public VenomDustUnit(Character owner, Position position, int level)
			: base(owner, position, SkillId.AS_VENOMDUST, level, TimeSpan.FromSeconds(30))
		{
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == null || !this.IsEnemy(entrant)) return;
			if (entrant.StatusEffects.Has(StatusId.Poison)) return;

			entrant.StatusEffects.Start(StatusId.Poison, this.SkillLevel, TimeSpan.FromSeconds(30 + 10 * this.SkillLevel), this.Owner);
		}
	}
}

using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// WZ_QUAGMIRE cell. Applies the Quagmire status (movement /
	/// ASPD / AGI / DEX debuff) to enemies that step on it. Renewal
	/// adjusts the magnitudes; the status flag itself is the same.
	/// </summary>
	public class QuagmireUnit : SkillUnit
	{
		public QuagmireUnit(Character owner, Position position, int level)
			: base(owner, position, SkillId.WZ_QUAGMIRE, level, TimeSpan.FromSeconds(20))
		{
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == null || !this.IsEnemy(entrant)) return;
			if (entrant.StatusEffects.Has(StatusId.Quagmire)) return;

			var duration = TimeSpan.FromSeconds(10 + 5 * this.SkillLevel);
			entrant.StatusEffects.Start(StatusId.Quagmire, this.SkillLevel, duration, this.Owner);
		}
	}
}

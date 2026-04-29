using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// Persistent elemental field unit (Volcano, Deluge, Violent
	/// Gale, Magnetic Earth). Applies a status to friendlies who
	/// stand on its cells. The status itself is what carries the
	/// stat modifications (ATK / MDEF / Speed).
	/// </summary>
	public class ElementalFieldUnit : SkillUnit
	{
		public StatusId AppliedStatus { get; }

		public ElementalFieldUnit(Character owner, Position position, SkillId skillId, int level, StatusId applied)
			: base(owner, position, skillId, level, TimeSpan.FromSeconds(60))
		{
			this.AppliedStatus = applied;
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == null) return;
			if (entrant.IsHostileTo(this.Owner)) return;
			if (entrant.StatusEffects.Has(this.AppliedStatus)) return;

			entrant.StatusEffects.Start(this.AppliedStatus, this.SkillLevel, TimeSpan.FromSeconds(5), this.Owner);
		}
	}
}

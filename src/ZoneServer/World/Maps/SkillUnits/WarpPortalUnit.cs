using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// AL_WARP — a portal that teleports anyone who walks onto it to
	/// the caster's save point. Consumed after a fixed number of uses
	/// or when the duration expires.
	/// </summary>
	public class WarpPortalUnit : SkillUnit
	{
		public Location Destination { get; }

		public WarpPortalUnit(Character owner, Position position, int level, Location destination)
			: base(owner, position, SkillId.AL_WARP, level, TimeSpan.FromSeconds(60))
		{
			this.Destination = destination;
			this.HitsRemaining = 8;
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == null) return;
			if (entrant.IsDead) return;

			// Mirrors eAthena: the portal teleports anyone — caster,
			// party members, or hostile players. Monsters are skipped
			// so they don't follow players through.
			if (entrant is Monster) return;

			entrant.Warp(this.Destination);

			if (this.HitsRemaining > 0)
				this.HitsRemaining--;
		}
	}
}

using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// WZ_ICEWALL cell. Blocks line of sight and movement. Has HP
	/// scaled with level; hits against the wall reduce its HP and
	/// remove it when depleted. v1 models the HP via HitsRemaining.
	/// </summary>
	public class IceWallUnit : SkillUnit
	{
		public IceWallUnit(Character owner, Position position, int level)
			: base(owner, position, SkillId.WZ_ICEWALL, level, TimeSpan.FromSeconds(60 + 20 * level))
		{
			this.HitsRemaining = 50 + 20 * level;
		}

		public override bool BlocksAttack(AttackContext ctx, bool isRanged)
		{
			// Ice Wall absorbs ranged attacks aimed past it; it does
			// not block melee that resolves on adjacent cells.
			return isRanged;
		}
	}
}

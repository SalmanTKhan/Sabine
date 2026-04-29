using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// MG_SAFETYWALL — absorbs the next N melee physical attacks against
	/// units standing on its cell. Hit count scales with skill level.
	/// </summary>
	public class SafetyWallUnit : SkillUnit
	{
		public SafetyWallUnit(Character owner, Position position, int level)
			: base(owner, position, SkillId.MG_SAFETYWALL, level, TimeSpan.FromSeconds(20))
		{
			// eAthena classic: 2 + level*2 hits, 35 second duration at lvl 10.
			this.HitsRemaining = 2 + level * 2;
			this.ExpireTime = DateTime.Now + TimeSpan.FromSeconds(20 + level);
		}

		public override bool BlocksAttack(AttackContext ctx, bool isRanged)
		{
			if (ctx.Kind != AttackKind.Physical) return false;
			if (isRanged) return false;
			return true;
		}
	}
}

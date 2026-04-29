using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// AL_PNEUMA — blocks ranged physical attacks against units standing
	/// on its cell. Time-based, doesn't deplete on hits.
	/// </summary>
	public class PneumaUnit : SkillUnit
	{
		public PneumaUnit(Character owner, Position position, int level)
			: base(owner, position, SkillId.AL_PNEUMA, level, TimeSpan.FromSeconds(8))
		{
		}

		public override bool BlocksAttack(AttackContext ctx, bool isRanged)
		{
			if (ctx.Kind != AttackKind.Physical) return false;
			if (!isRanged) return false;
			return true;
		}

		public override void OnAttackBlocked(AttackContext ctx)
		{
			// Pneuma doesn't consume on hit; let the timer handle removal.
		}
	}
}

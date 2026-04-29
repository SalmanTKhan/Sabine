namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Consulted by <see cref="BattleCalculator"/> to query ground-based
	/// damage blockers (Pneuma against ranged physical, Safety Wall
	/// against the next melee hit, etc.). Scans
	/// <c>target.Map.GetSkillUnitsAt(target.Position)</c> for active
	/// units and asks each whether the incoming attack should be blocked.
	/// </summary>
	internal static class GroundEffectGuard
	{
		public static bool IsBlocked(AttackContext ctx, bool isRanged)
		{
			var map = ctx.Target?.Map;
			if (map == null)
				return false;

			var units = map.GetSkillUnitsAt(ctx.Target.Position);
			if (units == null || units.Count == 0)
				return false;

			foreach (var unit in units)
			{
				if (unit.IsExpired)
					continue;

				if (unit.BlocksAttack(ctx, isRanged))
				{
					unit.OnAttackBlocked(ctx);
					return true;
				}
			}

			return false;
		}
	}
}

using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_CP_ARMOR / WEAPON / HELM / SHIELD aren't broken out as a
	/// single SC in eAthena; CR_FULLPROTECTION sets all four item-flag
	/// guards in parallel. Sabine collapses them under a single
	/// <see cref="StatusId.FullProtection"/> marker. The on-hit
	/// equip-break guard lives in
	/// <see cref="Sabine.Zone.Battle.Listeners.EquipBreakListener"/>:
	/// while this status is active on the target, the WS_MELTDOWN
	/// equip-break roll skips all four equip slots, mirroring eAthena's
	/// pc_skill_isequip_break early-out. Pure flag handler here.
	/// </summary>
	[StatusEffectHandler(StatusId.FullProtection)]
	public class FullProtectionStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}

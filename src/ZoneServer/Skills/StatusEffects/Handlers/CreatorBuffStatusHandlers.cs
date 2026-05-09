using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects.Handlers
{
	/// <summary>
	/// SC_CP_ARMOR / WEAPON / HELM / SHIELD aren't broken out as a
	/// single SC in eAthena; CR_FULLPROTECTION sets all four item-flag
	/// guards in parallel. Sabine collapses them under a single
	/// <see cref="StatusId.FullProtection"/> marker until the on-hit
	/// equip-break listener exists. Pure flag handler today.
	/// </summary>
	[StatusEffectHandler(StatusId.FullProtection)]
	public class FullProtectionStatusHandler : IStatusEffectHandler
	{
		public void OnStart(Character target, StatusEffect effect) { }
		public void OnEnd(Character target, StatusEffect effect) { }
	}
}

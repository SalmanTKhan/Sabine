using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Blacksmith
{
	[SkillHandler(SkillId.BS_ADRENALINE)]
	public class AdrenalineRushHandler : BuffSkillHandler
	{
		// eAthena BS_ADRENALINE: ASPD buff for Axe / Mace wielders.
		// 150s baseline, party members benefit at reduced rate.
		protected override StatusId StatusId => StatusId.AdrenalineRush;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(150);

		// status.c:4818-4820 fails the SC_ADRENALINE start when the wielded
		// weapon doesn't match BS_ADRENALINE's allowed types (Axe / Mace).
		// Enforce it at cast time so the player gets a proper WrongWeapon
		// fail packet instead of a silently wasted SP cost.
		public override void Handle(UseSkillParams parameters)
		{
			if (parameters.Character is PlayerCharacter pc && !IsAxeOrMaceEquipped(pc))
			{
				Send.ZC_ACK_TOUSESKILL(pc, SkillId.BS_ADRENALINE, SkillUseResult.Fail, SkillFailType.WrongWeapon, SkillFailReason.SkillFailed);
				return;
			}

			base.Handle(parameters);
		}

		private static bool IsAxeOrMaceEquipped(PlayerCharacter pc)
		{
			var weapon = pc.Inventory.RightHand?.Data.GetWeaponType() ?? WeaponType.Unknown;
			return weapon == WeaponType.OneHandedAxe
				|| weapon == WeaponType.TwoHandedAxe
				|| weapon == WeaponType.Mace;
		}
	}

	[SkillHandler(SkillId.BS_WEAPONPERFECT)]
	public class WeaponPerfectionHandler : BuffSkillHandler
	{
		// eAthena BS_WEAPONPERFECT: 10s/level — bypasses weapon-vs-size
		// penalty.
		protected override StatusId StatusId => StatusId.WeaponPerfection;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(10 * level);
	}

	[SkillHandler(SkillId.BS_OVERTHRUST)]
	public class OverThrustHandler : BuffSkillHandler
	{
		// eAthena BS_OVERTHRUST: +5% ATK per level for 180s.
		protected override StatusId StatusId => StatusId.OverThrust;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(180);
	}

	[SkillHandler(SkillId.BS_MAXIMIZE)]
	public class MaximizePowerHandler : BuffSkillHandler
	{
		// eAthena BS_MAXIMIZE: forces max-roll damage. 10s/level,
		// drains 1 SP/sec.
		protected override StatusId StatusId => StatusId.PowerMaximize;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(10 * level);
	}
}

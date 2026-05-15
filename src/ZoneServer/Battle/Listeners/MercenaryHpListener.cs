using Sabine.Shared;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle.Listeners
{
	/// <summary>
	/// Mirrors a mercenary's HP changes onto the owner's merc panel via
	/// <c>ZC_MER_PAR_CHANGE</c>. rA emits this every time the merc takes
	/// damage or heals so the panel HP bar tracks combat without the
	/// owner needing to refresh manually.
	/// </summary>
	public static class MercenaryHpListener
	{
		// rAthena mmo.h SP_HP = 5. Only the per-stat update varId — the
		// owner-side ZC_MER_PAR_CHANGE handler doesn't carry maxhp here
		// (that comes from ZC_MER_INIT and rarely changes mid-contract).
		private const short SP_HP = 5;

		public static void Subscribe()
		{
			CombatEvents.CharacterDamaged += OnCharacterDamaged;
			CombatEvents.CharacterHealed += OnCharacterHealed;
		}

		private static void OnCharacterDamaged(Character target, int amount, Character attacker, int remainingHp)
		{
			if (Game.Version < Versions.S2500) return;
			if (target is not Mercenary merc) return;

			Send.ZC_MER_PAR_CHANGE(merc.Owner, SP_HP, remainingHp);
		}

		private static void OnCharacterHealed(Character target, int amount)
		{
			if (Game.Version < Versions.S2500) return;
			if (target is not Mercenary merc) return;

			Send.ZC_MER_PAR_CHANGE(merc.Owner, SP_HP, merc.Parameters.Hp);
		}
	}
}

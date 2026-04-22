using System;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Magician
{
	public abstract class MagicBoltHandler : ISkillHandler
	{
		public async Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return;
			}

			// All bolt spells are multi-hit based on level.
			// Example: Level 1-2 = 1 hit, 3-4 = 2 hits, etc. up to 5 hits.
			var hitCount = (skill.Level + 1) / 2;

			// Base MATK is 100%. Each bolt does one hit of MATK.
			var damage = caster.Parameters.MagicAttack;

			// Send the main skill usage packet to start animation/casting.
			Send.ZC_USE_SKILL(caster as PlayerCharacter, skill.Id, skill.Level, target.Handle, true);

			for (var i = 0; i < hitCount; i++)
			{
				// TODO: Damage should have an element and be reduced by MDEF.
				targetCharacter.TakeDamage(damage, caster);

				// In a real scenario, there would be a delay between hits.
				await Task.Delay(100);
			}
		}
	}

	[SkillHandler(SkillId.MG_FIREBOLT)]
	public class FireBoltHandler : MagicBoltHandler { }

	[SkillHandler(SkillId.MG_COLDBOLT)]
	public class ColdBoltHandler : MagicBoltHandler
	{
		// TODO: Add chance to freeze target.
	}

	[SkillHandler(SkillId.MG_LIGHTNINGBOLT)]
	public class LightningBoltHandler : MagicBoltHandler { }
}
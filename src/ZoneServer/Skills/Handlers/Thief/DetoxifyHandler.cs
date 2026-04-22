using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Thief
{
	[SkillHandler(SkillId.TF_DETOXIFY)]
	public class DetoxifyHandler : ISkillHandler
	{
		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target is not Character targetCharacter)
			{
				return Task.CompletedTask;
			}
			
			// TODO: Remove 'Poison' status effect from the target.

			Send.ZC_NOTIFY_SKILL(caster, target.Handle, skill.Id, skill.Level, 0, 0, 0, ActionType.Skill);

			if (caster is PlayerCharacter pc)
			{
				pc.ServerMessage("Detoxify is not fully implemented yet.");
			}
			
			return Task.CompletedTask;
		}
	}
}
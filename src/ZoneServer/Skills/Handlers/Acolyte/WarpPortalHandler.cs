using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_WARP)]
	public class WarpPortalHandler : ISkillHandler
	{
		// Blue Gemstone item id (eAthena db: 717).
		private const int BlueGemstoneId = 717;

		public Task HandleAsync(Character caster, Character target, Skill skill)
		{
			if (target == null) return Task.CompletedTask;
			if (caster is not PlayerCharacter pc) return Task.CompletedTask;

			// v1: all skill levels warp to the caster's save point.
			// Multi-memo (levels 2-4) is future work.
			var dest = pc.SaveLocation;
			if (dest.MapId == 0)
			{
				pc.ServerMessage("You don't have a save point set.");
				return Task.CompletedTask;
			}

			var gem = pc.Inventory.GetItems(static i => i.ClassId == BlueGemstoneId).FirstOrDefault();
			if (gem == null)
			{
				pc.ServerMessage("You need a Blue Gemstone.");
				return Task.CompletedTask;
			}
			pc.Inventory.DecrementItem(gem, 1);

			var unit = new Sabine.Zone.World.Maps.SkillUnits.WarpPortalUnit(caster, target.Position, skill.Level, dest);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, skill.Level, target.Position.X, target.Position.Y, 0);
			return Task.CompletedTask;
		}
	}
}

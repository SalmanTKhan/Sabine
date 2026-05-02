using System.Linq;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_WARP)]
	public class WarpPortalHandler : IGroundSkillHandler
	{
		// Blue Gemstone item id (eAthena db: 717).
		private const int BlueGemstoneId = 717;

		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var pos = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;

			if (caster is not PlayerCharacter pc) return;

			// v1: all skill levels warp to the caster's save point.
			// Multi-memo (levels 2-4) is future work.
			var dest = pc.SaveLocation;
			if (dest.MapId == 0)
			{
				pc.ServerMessage("You don't have a save point set.");
				return;
			}

			var gem = pc.Inventory.GetItems(static i => i.ClassId == BlueGemstoneId).FirstOrDefault();
			if (gem == null)
			{
				pc.ServerMessage("You need a Blue Gemstone.");
				return;
			}
			pc.Inventory.DecrementItem(gem, 1);

			var unit = new Sabine.Zone.World.Maps.SkillUnits.WarpPortalUnit(caster, pos, level, dest);
			caster.Map.AddSkillUnit(unit);

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, pos.X, pos.Y, 0);
		}
	}
}

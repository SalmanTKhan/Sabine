using System;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.World.Maps.SkillUnits;

namespace Sabine.Zone.Skills.Handlers.HighPriest
{
	/// <summary>
	/// HP_ASSUMPTIO — halves physical damage taken on the target
	/// (eAthena status.c SC_ASSUMPTIO; the 50% reduction lives in
	/// the damage pipeline so the handler stays marker-only here).
	/// Duration: 20s/level. Targetable on allies; the BuffSkillHandler
	/// base routes the status onto parameters.Target.
	/// </summary>
	[SkillHandler(SkillId.HP_ASSUMPTIO)]
	public class AssumptioHandler : BuffSkillHandler
	{
		protected override StatusId StatusId => StatusId.Assumptio;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromSeconds(20 * level);
	}

	/// <summary>
	/// HP_BASILICA — spawns a 5×5 grid of <see cref="BasilicaUnit"/>
	/// cells around the target position. Standers (friendlies) are
	/// shielded from physical damage; magic still lands. Mirrors the
	/// Sage-field 5×5 spawn idiom from <c>SageFieldPlacement</c>.
	/// Duration: 30s + 30s/level (eAthena classic skill_cast_db).
	/// </summary>
	[SkillHandler(SkillId.HP_BASILICA)]
	public class BasilicaHandler : IGroundSkillHandler
	{
		public void Handle(UseGroundSkillParams parameters)
		{
			var caster = parameters.Character;
			var center = parameters.TargetPosition;
			var skill = parameters.Skill;
			var level = parameters.SkillLevel;
			var duration = TimeSpan.FromSeconds(30 + 30 * level);

			for (var dx = -2; dx <= 2; dx++)
			{
				for (var dy = -2; dy <= 2; dy++)
				{
					var pos = new Sabine.Shared.World.Position((short)(center.X + dx), (short)(center.Y + dy));
					caster.Map.AddSkillUnit(new BasilicaUnit(caster, pos, level, duration));
				}
			}

			Send.ZC_NOTIFY_GROUNDSKILL(caster, skill.Id, caster.Handle, level, center.X, center.Y, 0);
		}
	}
}

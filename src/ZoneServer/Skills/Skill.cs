using System;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Shared.Data.Databases;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills
{
	public class Skill
	{
		public Character Owner { get; }
		public SkillId Id { get; }
		public int Level { get; set; }
		public SkillData Data { get; }

		/// <summary>
		/// Gets the skill's permanence type.
		/// </summary>
		public SkillPerm Perm { get; }

		public Skill(Character owner, SkillId skillId, int level, SkillPerm perm)
		{
			this.Owner = owner;
			this.Id = skillId;
			this.Level = level;
			this.Perm = perm;
			this.Data = SabineData.Skills.Find(skillId) ?? throw new ArgumentException($"Unknown skill '{skillId}'.");
		}

		public int GetSpCost() => this.Data.Costs.Sp?[this.Level - 1] ?? 0;
		public int GetCastTime() => this.Data.Cast.CastTime?[this.Level - 1] ?? 0;
		public int GetCooldown() => this.Data.Cast.Cooldown?[this.Level - 1] ?? 0;
		public int GetAfterCastActDelay() => this.Data.Cast.AfterCastActDelay?[this.Level - 1] ?? 0;
	}
}

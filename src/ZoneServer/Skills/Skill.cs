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

		public Skill(Character owner, SkillId skillId, int level)
		{
			this.Owner = owner;
			this.Id = skillId;
			this.Level = level;
			this.Data = SabineData.Skills.Find(skillId) ?? throw new ArgumentException($"Unknown skill '{skillId}'.");
		}

		public int GetSpCost() => this.Data.Costs.Sp[this.Level - 1];
		public int GetCastTime() => this.Data.CastTime[this.Level - 1];
		public int GetCooldown() => this.Data.Cooldown[this.Level - 1];
		public int GetAfterCastActDelay() => this.Data.AfterCastActDelay[this.Level - 1];
	}
}

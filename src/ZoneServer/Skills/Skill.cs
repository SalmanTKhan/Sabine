using System;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Zone.Network;
using Sabine.Zone.World.Entities;

namespace Sabine.Zone.Skills
{
	public class Skill
	{
		public Character Owner { get; }
		public Character Character => this.Owner;
		public SkillId Id { get; }
		public int Level { get; set; }
		public SkillData Data { get; }

		/// <summary>
		/// Gets the skill's permanence type.
		/// </summary>
		public SkillPerm Perm { get; }

		public int Range => this.Data.GetRange(this.Level);
		public int SpCost => this.Data.GetSpCost(this.Level);
		public int HpCost => this.Data.GetHpCost(this.Level);
		public bool CanBeLeveled => this.Level < this.Data.MaxLevel;

		public Skill(Character owner, SkillId skillId, int level)
			: this(owner, skillId, level, SkillPerm.Permanent)
		{
		}

		public Skill(Character owner, SkillId skillId, int level, SkillPerm perm)
		{
			if (!ZoneServer.Instance.Data.Skills.TryFind(skillId, out var data))
				throw new ArgumentException($"Unknown skill '{skillId}'.");

			this.Owner = owner;
			this.Id = skillId;
			this.Level = level;
			this.Perm = perm;
			this.Data = data;
		}

		public int GetSpCost() => this.Data.Costs.Sp?[this.Level - 1] ?? 0;
		public int GetCastTime() => this.Data.Cast.CastTime?[this.Level - 1] ?? 0;
		public int GetCooldown() => this.Data.Cast.Cooldown?[this.Level - 1] ?? 0;
		public int GetAfterCastActDelay() => this.Data.Cast.AfterCastActDelay?[this.Level - 1] ?? 0;

		public void LevelUp()
		{
			this.Level = Math.Min(this.Level + 1, this.Data.MaxLevel);

			if (this.Owner is PlayerCharacter player)
				Send.ZC_SKILLINFO_UPDATE(player, this);
		}
	}
}

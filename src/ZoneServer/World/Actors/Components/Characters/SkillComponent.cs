using System;
using System.Collections.Generic;
using System.Linq;
using Sabine.Shared.Const;
using Sabine.Zone.Network;
using Sabine.Zone.Skills;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	/// <summary>
	/// A component that manages a character's skills.
	/// </summary>
	/// <param name="character"></param>
	public class SkillComponent(Character character) : ICharacterComponent
	{
		private readonly Dictionary<SkillId, Skill> _skills = new();

		public int Count => _skills.Count;

		/// <summary>
		/// Returns the character this component belongs to.
		/// </summary>
		public Character Character { get; } = character;

		/// <summary>
		/// Returns the player character this component belongs to, if any.
		/// </summary>
		public PlayerCharacter Player => this.Character as PlayerCharacter;

		/// <summary>
		/// Returns the level of the given skill or 0 if it doesn't exist.
		/// </summary>
		public int GetLevel(SkillId skillId)
		{
			lock (_skills)
				return _skills.TryGetValue(skillId, out var skill) ? skill.Level : 0;
		}

		/// <summary>
		/// Returns true if the character has the given skill.
		/// </summary>
		public bool Has(SkillId skillId)
			=> this.GetLevel(skillId) > 0;

		/// <summary>
		/// Returns true if the character has the given skill on at least
		/// the given level.
		/// </summary>
		/// <param name="skillId"></param>
		/// <param name="minLevel"></param>
		/// <returns></returns>
		public bool Has(SkillId skillId, int minLevel)
		{
			lock (_skills)
			{
				if (_skills.TryGetValue(skillId, out var skill))
					return skill.Level >= minLevel;
			}

			return false;
		}

		/// <summary>
		/// Adds the given skill without updating the character's client.
		/// </summary>
		/// <param name="skill"></param>
		public void AddSilent(Skill skill)
		{
			lock (_skills)
				_skills[skill.Id] = skill;
		}

		/// <summary>
		/// Adds the given skill without updating the client.
		/// </summary>
		public void AddInit(SkillId skillId, int level, SkillPerm perm)
		{
			lock (_skills)
			{
				if (_skills.TryGetValue(skillId, out var existingSkill))
				{
					existingSkill.Level = Math.Max(existingSkill.Level, level);
					return;
				}

				_skills.Add(skillId, new Skill(this.Character, skillId, level, perm));
			}
		}

		/// <summary>
		/// Adds the given skill and updates the character's client.
		/// </summary>
		/// <remarks>
		/// If the skill already exists, it gets replaced and updated
		/// instead of added.
		/// </remarks>
		/// <param name="skill"></param>
		public void Add(Skill skill)
		{
			var updated = false;

			lock (_skills)
			{
				if (_skills.ContainsKey(skill.Id))
					updated = true;

				_skills[skill.Id] = skill;
			}

			if (this.Character is PlayerCharacter pc)
			{
				if (!updated)
					Send.ZC_ADD_SKILL(pc, skill);
				else
					Send.ZC_SKILLINFO_UPDATE(pc, skill);
			}
		}

		/// <summary>
		/// Adds the given skill and updates the client.
		/// </summary>
		public void Add(SkillId skillId, int level, SkillPerm perm)
		{
			var updated = false;
			Skill skill;

			lock (_skills)
			{
				if (_skills.TryGetValue(skillId, out var existingSkill))
				{
					updated = true;
					existingSkill.Level = Math.Max(existingSkill.Level, level);
					skill = existingSkill;
				}
				else
				{
					skill = new Skill(this.Character, skillId, level, perm);
					_skills.Add(skillId, skill);
				}
			}

			if (this.Player != null)
			{
				if (!updated)
					Send.ZC_ADD_SKILL(this.Player, skill);
				else
					Send.ZC_SKILLINFO_UPDATE(this.Player, skill);
			}
		}

		/// <summary>
		/// Adds the skill with the given level if the character doesn't
		/// already have it on at least that level. Doesn't update client.
		/// </summary>
		/// <param name="skillId"></param>
		/// <param name="level"></param>
		public void AddSilent(SkillId skillId, int level)
		{
			if (this.Has(skillId, level))
				return;

			this.AddSilent(new Skill(this.Character, skillId, level));
		}

		/// <summary>
		/// Adds the skill with the given level if the character doesn't
		/// already have it on at least that level.
		/// </summary>
		/// <param name="skillId"></param>
		/// <param name="level"></param>
		public void Add(SkillId skillId, int level)
		{
			if (this.Has(skillId, level))
				return;

			this.Add(new Skill(this.Character, skillId, level));
		}

		/// <summary>
		/// Removes the given skill.
		/// </summary>
		/// <param name="skillId"></param>
		public void Remove(SkillId skillId)
		{
			lock (_skills)
				_skills.Remove(skillId);

			//if (this.Character is PlayerCharacter pc)
			//	Send.ZC_REMOVE_SKILL(pc, skillId);
		}

		/// <summary>
		/// Returns the skill with the given id via out. Returns false if
		/// the skill didn't exist.
		/// </summary>
		/// <param name="skillId"></param>
		/// <param name="skill"></param>
		/// <returns></returns>
		public bool TryGet(SkillId skillId, out Skill skill)
		{
			lock (_skills)
				return _skills.TryGetValue(skillId, out skill);
		}

		/// <summary>
		/// Returns a list of all skills.
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<Skill> GetAll()
			=> _skills.Values.ToList();

		/// <summary>
		/// Sends the full list of skills to the client.
		/// </summary>
		public void RefreshClient()
		{
			if (this.Player == null)
				return;

			Send.ZC_SKILLINFO_LIST(this.Player, _skills.Values.ToList());
		}

		/// <summary>
		/// Updates the component.
		/// </summary>
		/// <param name="elapsed"></param>
		public void Update(TimeSpan elapsed)
		{
		}

		/// <summary>
		/// Returns true if the given skill can currently be leveled up.
		/// </summary>
		public bool CanUpgrade(SkillId skillId)
		{
			if (this.Player == null)
				return false;

			if (!ZoneServer.Instance.Data.SkillTrees.TryFind(this.Player.JobId, out var treeData))
				return false;

			var skillData = treeData.Skills.FirstOrDefault(a => a.SkillId == skillId);
			if (skillData == null)
				return false;

			if (this.GetLevel(skillId) >= skillData.MaxLevel)
				return false;

			if (this.Player.Parameters.SkillPoints < 1)
				return false;

			foreach (var prerequisite in skillData.Prerequisites)
			{
				if (!this.Has(prerequisite.SkillId, prerequisite.MinLevel))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Adds the skills the character should have access to, based on
		/// their skill tree data.
		/// </summary>
		/// <remarks>
		/// This method should be called whenever skills may change, in
		/// case a prerequisite was newly met.
		/// </remarks>
		public void UpdateClassSkills()
		{
			if (this.Character is not PlayerCharacter player)
				return;

			if (!ZoneServer.Instance.Data.SkillTrees.TryFind(player.JobId, out var treeData))
				return;

			foreach (var skill in treeData.Skills)
			{
				var prerequisitesMet = true;

				foreach (var prerequisite in skill.Prerequisites)
				{
					if (!this.Has(prerequisite.SkillId, prerequisite.MinLevel))
					{
						prerequisitesMet = false;
						break;
					}
				}

				// We need to make sure not to send new skills before the
				// client is finished loading, or it will lock up. As
				// such, calling Add instead of AddSilent in all cases is
				// fine for the moment, since we add class skills only on
				// skill level up, job change, and on PlayerReady, all of
				// which happen after the loading screen. Should we need
				// to add skills earlier, we need a conditional AddSilent.

				if (prerequisitesMet)
					this.Add(skill.SkillId, 0);
			}
		}
	}
}

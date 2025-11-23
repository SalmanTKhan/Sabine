using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Zone.Network;
using Sabine.Zone.Skills;

namespace Sabine.Zone.World.Entities.Components.Characters
{
	/// <summary>
	/// Manages a player character's skills.
	/// </summary>
	public class SkillComponent : ICharacterComponent
	{
		private readonly Dictionary<SkillId, Skill> _skills = new();

		public int Count => _skills.Count;

		/// <summary>
		/// Returns the character this component belongs to.
		/// </summary>
		public Character Character => this.Player;

		/// <summary>
		/// Returns the player character this component belongs to.
		/// </summary>
		public PlayerCharacter Player { get; }

		/// <summary>
		/// Creates a new SkillComponent.
		/// </summary>
		/// <param name="player"></param>
		public SkillComponent(PlayerCharacter player)
		{
			this.Player = player;
		}

		/// <summary>
		/// Does nothing.
		/// </summary>
		/// <param name="elapsed"></param>
		public void Update(TimeSpan elapsed)
		{
		}

		/// <summary>
		/// Adds a skill to the character's skill list or updates its level.
		/// </summary>
		/// <param name="id">The ID of the skill to add.</param>
		/// <param name="level">The level of the skill.</param>
		/// <param name="perm">The permanence of the skill.</param>
		public void Add(SkillId id, int level, SkillPerm perm)
		{
			if (_skills.TryGetValue(id, out var existingSkill))
			{
				// Skill already known, update level if necessary
				if (existingSkill.Level < level)
				{
					existingSkill.Level = level;
				}
			}
			else
			{
				// New skill
				var newSkill = new Skill(this.Character, id, level, perm);
				_skills.Add(id, newSkill);
			}

			// Client needs to be updated with the full skill list.
			this.RefreshClient();
		}

		/// <summary>
		/// Adds a skill during character loading, without notifying the client.
		/// </summary>
		/// <param name="id">The ID of the skill to add.</param>
		/// <param name="level">The level of the skill.</param>
		/// <param name="perm">The permanence of the skill.</param>
		internal void AddInit(SkillId id, int level, SkillPerm perm)
		{
			if (_skills.TryGetValue(id, out var existingSkill))
			{
				if (existingSkill.Level < level)
				{
					existingSkill.Level = level;
				}
			}
			else
			{
				var newSkill = new Skill(this.Character, id, level, perm);
				_skills.Add(id, newSkill);
			}
		}

		/// <summary>
		/// Gets the level of a specific skill.
		/// </summary>
		/// <param name="id">The ID of the skill.</param>
		/// <returns>The level of the skill, or 0 if not known.</returns>
		public int GetLevel(SkillId id)
		{
			return _skills.TryGetValue(id, out var skill) ? skill.Level : 0;
		}

		/// <summary>
		/// Checks if the character has a specific skill.
		/// </summary>
		/// <param name="id">The ID of the skill.</param>
		/// <returns>True if the skill is known, false otherwise.</returns>
		public bool Has(SkillId id)
		{
			return _skills.ContainsKey(id);
		}

		/// <summary>
		/// Returns all skills the character has learned.
		/// </summary>
		/// <returns></returns>
		public IList<Skill> GetAll()
		{
			return _skills.Values.ToList();
		}

		/// <summary>
		/// Sends the full list of skills to the client.
		/// </summary>
		public void RefreshClient()
		{
			Send.ZC_SKILLINFO_LIST(this.Player, _skills.Values.ToList());
		}

		/// <summary>
		/// Checks if a skill can be upgraded by the character.
		/// </summary>
		/// <param name="skillId"></param>
		/// <returns>True if the skill can be upgraded, otherwise false.</returns>
		public bool CanUpgrade(SkillId skillId)
		{
			var currentLevel = this.GetLevel(skillId);
			var skillTreeEntry = SabineData.SkillTree.FindByJobAndSkill(this.Player.JobId, skillId);

			// Check if the job can learn this skill
			if (skillTreeEntry == null)
				return false;

			// Check if the skill is already at the max level for this job
			if (currentLevel >= skillTreeEntry.MaxLevel)
				return false;

			// Check if the player has enough skill points
			if (this.Player.Parameters.SkillPoints < 1)
				return false;

			// Check all prerequisites
			foreach (var prereq in skillTreeEntry.Prerequisites)
			{
				if (this.GetLevel(prereq.SkillId) < prereq.RequiredLevel)
					return false;
			}

			return true;
		}

		internal bool TryGet(SkillId skillId, out Skill skill)
		{
			lock (_skills)
			{
				return _skills.TryGetValue(skillId, out skill);
			}
		}

		/// <summary>
		/// Attempts to use a skill. Handles checks, cast time, and execution.
		/// </summary>
		public async Task Use(SkillId skillId, int level, IEntity target)
		{
			if (!this.TryGet(skillId, out var skill))
				return;

			// 1. Validation
			if (this.Player.IsDead || this.Player.IsCasting)
				return;

			// TODO: Validate SP, Ammo, Weapon Type, Range, Line of Sight here.
			var spCost = skill.Data.GetSpCost(level);
			if (this.Player.Parameters.Sp < spCost)
				return; // Optionally send "Not enough SP" packet

			var handler = SkillHandlerManager.GetHandler(skillId);
			if (handler == null)
			{
				this.Player.ServerMessage($"Skill {skillId} is not implemented yet.");
				return;
			}

			// 2. Cast Time Calculation
			// Dex reduces cast time: CastTime * (1 - (Dex / 150))
			var baseCastTime = 0;
			if (skill.Data.Cast.CastTime != null)
				baseCastTime = skill.Data.Cast.CastTime[Math.Max(0, level - 1)];
			var castTime = (int)(baseCastTime * (1 - (this.Player.Parameters.Dex / 150f)));
			castTime = Math.Max(0, castTime);

			// 3. Execution Flow
			if (castTime > 0)
			{
				// Notify client to show cast bar
				var targetId = target?.Handle ?? this.Player.Handle;
				Send.ZC_USESKILL_ACK(this.Player, this.Player.Handle, targetId, skillId, level, castTime);

				var token = this.Player.RegisterCast();

				try
				{
					await Task.Delay(castTime, token);
				}
				catch (TaskCanceledException)
				{
					// Cast was interrupted
					return;
				}
				finally
				{
					this.Player.FinishCasting();
				}
			}

			// 4. Consume Resources
			this.Player.Parameters.Modify(ParameterType.Sp, -spCost);

			// 5. Execute Logic
			await handler.HandleAsync(this.Player, target, skill);

			// 6. Post-Cast Delays (Cooldowns / After-cast delay)
			// TODO: Implement global cooldown/skill-specific cooldown tracking
		}
	}
}

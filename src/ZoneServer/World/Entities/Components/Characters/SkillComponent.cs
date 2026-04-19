using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
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
		public Character Character { get; }

		/// <summary>
		/// Returns the player character this component belongs to, if any.
		/// </summary>
		public PlayerCharacter Player => this.Character as PlayerCharacter;

		/// <summary>
		/// Creates a new SkillComponent.
		/// </summary>
		/// <param name="character"></param>
		public SkillComponent(Character character)
		{
			this.Character = character;
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
		/// Adds a skill to the character's skill list or updates it.
		/// </summary>
		/// <param name="skill"></param>
		public void Add(Skill skill)
		{
			lock (_skills)
			{
				if (_skills.TryGetValue(skill.Id, out var existingSkill))
				{
					existingSkill.Level = Math.Max(existingSkill.Level, skill.Level);
				}
				else
				{
					_skills.Add(skill.Id, skill);
				}
			}

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
		public int GetLevel(SkillId skillId)
		{
			return _skills.TryGetValue(skillId, out var skill) ? skill.Level : 0;
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
			if (this.Player == null)
				return;

			Send.ZC_SKILLINFO_LIST(this.Player, _skills.Values.ToList());
		}

		/// <summary>
		/// Checks if a skill can be upgraded by the character.
		/// </summary>
		/// <param name="skillId"></param>
		/// <returns>True if the skill can be upgraded, otherwise false.</returns>
		public bool CanUpgrade(SkillId skillId)
		{
			if (this.Player == null)
				return false;

			var currentLevel = this.GetLevel(skillId);
			var skillTreeEntry = ZoneServer.Instance.Data.SkillTree.FindByJobAndSkill(this.Player.JobId, skillId);

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
		/// Checks if the character can use a skill right now.
		/// </summary>
		private bool CanUseSkill(Skill skill, int level, IEntity target, out string errorMessage)
		{
			errorMessage = null;

			// Dead characters can't use skills
			if (this.Player.IsDead)
			{
				errorMessage = "You cannot use skills while dead.";
				return false;
			}

			// Can't use skills while sitting (except sitting-allowed skills)
			//if (this.Player.State == CharacterState.Sitting && !skill.Data.CanUseWhileSitting)
			if (this.Player.State == CharacterState.Sitting)
			{
				errorMessage = "You must stand up to use this skill.";
				return false;
			}

			// Can't use skills while already casting (unless it's instant)
			if (this.Player.IsCasting && skill.GetCastTime() > 0)
			{
				errorMessage = "You are already casting a skill.";
				return false;
			}

			// Check SP cost
			var spCost = skill.GetSpCost();
			if (this.Player.Parameters.Sp < spCost)
			{
				errorMessage = "Not enough SP.";
				return false;
			}

			// TODO: Add more checks:
			// - Weapon type requirement
			// - Ammo requirement
			// - Range check
			// - Line of sight check
			// - Skill-specific requirements

			return true;
		}

		/// <summary>
		/// Attempts to use a skill. Handles checks, cast time, and execution.
		/// </summary>
		public async Task Use(SkillId skillId, int level, IEntity target)
		{
			if (this.Player == null)
				return;

			if (!this.TryGet(skillId, out var skill))
				return;

			// 1. Pre-cast validation
			if (!this.CanUseSkill(skill, level, target, out var errorMessage))
			{
				if (!string.IsNullOrEmpty(errorMessage))
					this.Player.ServerMessage(errorMessage);
				return;
			}

			var handler = SkillHandlerManager.GetHandler(skillId);
			if (handler == null)
			{
				this.Player.ServerMessage($"Skill {skillId} is not implemented yet.");
				return;
			}

			// 2. Cancel any ongoing actions
			// Note: This should stop attacking, but not movement (player can cast while moving in some games)
			// Adjust based on your game's design
			this.Player.StopAttacking();

			// 3. Calculate cast time with DEX reduction
			var baseCastTime = skill.GetCastTime();
			var castTime = (int)(baseCastTime * (1 - (this.Player.Parameters.Dex / 150f)));
			castTime = Math.Max(0, castTime);

			// 4. Handle casting period
			if (castTime > 0)
			{
				var targetId = target?.Handle ?? this.Player.Handle;
				Send.ZC_USESKILL_ACK(this.Player, this.Player.Handle, targetId, skillId, level, castTime);

				var token = this.Player.RegisterCast();

				try
				{
					await Task.Delay(castTime, token);
				}
				catch (TaskCanceledException)
				{
					// Cast was interrupted - this is normal, just return
					return;
				}
				finally
				{
					this.Player.FinishCasting();
				}
			}

			// 5. Final validation (target might have moved/died during cast)
			if (!this.CanUseSkill(skill, level, target, out errorMessage))
			{
				if (!string.IsNullOrEmpty(errorMessage))
					this.Player.ServerMessage(errorMessage);
				return;
			}

			// 6. Consume resources
			var spCost = skill.GetSpCost();
			this.Player.Parameters.Modify(ParameterType.Sp, -spCost);

			// 7. Execute skill logic
			try
			{
				await handler.HandleAsync(this.Player, target, skill);
			}
			catch (Exception ex)
			{
				this.Player.ServerMessage("An error occurred while using the skill.");
				Yggdrasil.Logging.Log.Error($"Error executing skill {skillId}: {ex}");
			}

			// 8. Post-cast delays
			var afterCastDelay = skill.GetAfterCastActDelay();
			if (afterCastDelay > 0)
			{
				// TODO: Implement global cooldown or skill-specific cooldown
				// This prevents the player from acting immediately after casting
			}

			var cooldown = skill.GetCooldown();
			if (cooldown > 0)
			{
				// TODO: Implement skill-specific cooldown tracking
			}
		}
	}
}

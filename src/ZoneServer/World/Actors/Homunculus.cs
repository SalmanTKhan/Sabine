using Sabine.Shared.Const;
using Sabine.Zone.World.Actors.Components.Characters;

namespace Sabine.Zone.World.Actors
{
	/// <summary>
	/// A homunculus companion. Lives on a map as a Monster-derived
	/// entity owned by a player. Wild monsters won't aggro on it
	/// (AggressiveAi scans for PlayerCharacter only); enemies of the
	/// owner are treated as hostile by the homunculus AI.
	/// </summary>
	public class Homunculus : Monster
	{
		/// <summary>
		/// Player who owns this homunculus. Never null for a live
		/// homunculus; the spawn pipeline sets this before the entity
		/// is added to a map.
		/// </summary>
		public PlayerCharacter Owner { get; }

		/// <summary>
		/// State component on the owner. Damage and rest sync through
		/// this so the data survives despawn.
		/// </summary>
		public HomunculusComponent State { get; }

		public HomunculusType Type => this.State.Type;

		public Homunculus(PlayerCharacter owner, HomunculusComponent state, IdentityId identityId)
			: base(identityId)
		{
			this.Owner = owner;
			this.State = state;
			this.Name = state.Name;

			// Homun's HP is owned by the component so it persists
			// across rest/dispatch. Sync into the entity at spawn.
			this.Parameters.HpMax = state.HpMax;
			this.Parameters.Hp = state.Hp;

			this.ApplyPassives();
			this.RecalculateStats();
		}

		/// <summary>
		/// Re-applies stat bonuses from learned passive skills to
		/// the live entity. Idempotent: bumps the persistent stat
		/// values directly so calling it again after a learn-up
		/// without re-spawn would double-apply. Use only via the
		/// constructor and <see cref="RefreshFromState"/>.
		/// </summary>
		public void ApplyPassives()
		{
			// HLIF_BRAIN_SURGERY (Lif): +SP cap, +INT.
			var brain = this.State.GetSkillLevel(SkillId.HLIF_BRAIN);
			if (brain > 0)
			{
				this.Parameters.SpMax += 10 * brain;
				this.Parameters.Int += 1 * brain;
			}

			// HAMI_SKIN (Amistr): +HP cap, +DEF.
			var skin = this.State.GetSkillLevel(SkillId.HAMI_SKIN);
			if (skin > 0)
			{
				this.Parameters.HpMax += 30 * skin;
				this.Parameters.Defense += 2 * skin;
			}

			// HVAN_INSTRUCT (Vanilmirth): +STR/+INT.
			var instruct = this.State.GetSkillLevel(SkillId.HVAN_INSTRUCT);
			if (instruct > 0)
			{
				this.Parameters.Str += 1 * instruct;
				this.Parameters.Int += 1 * instruct;
			}
		}

		/// <summary>
		/// Recomputes derived stats (ATK min/max) from base data,
		/// per-level growth, and current STR. Called after every
		/// stat-affecting change (level-up, learn-up, passive apply)
		/// so consumers like BattleCalculator see the right numbers.
		/// </summary>
		public void RecalculateStats()
		{
			var growth = HomunculusTypeData.GetGrowth(this.Type);
			var levelBonus = (this.State.Level - 1) * growth.AtkPerLevel;

			// Pre-renewal monster ATK ≈ base + STR. Add per-level
			// growth on top so leveling matters even without STR.
			this.Parameters.AttackMin = this.Data.AttackMin + levelBonus + this.Parameters.Str;
			this.Parameters.AttackMax = this.Data.AttackMax + levelBonus + this.Parameters.Str;
		}

		/// <summary>
		/// Wipes derived state and re-applies passives + recalc from
		/// scratch. Used when a passive is learned or ranked up so
		/// stacking from multiple ApplyPassives calls is avoided.
		/// </summary>
		public void RefreshFromState()
		{
			// Reset to base + level-driven progression, then re-apply
			// passives on top of the clean baseline.
			var growth = HomunculusTypeData.GetGrowth(this.Type);
			var levelBonus = this.State.Level - 1;

			this.Parameters.HpMax = this.State.HpMax;
			this.Parameters.SpMax = this.Data.Sp;
			this.Parameters.Defense = this.Data.Defense + levelBonus * growth.DefPerLevel;

			this.Parameters.Str = this.Data.Str + levelBonus * growth.StrPerLevel;
			this.Parameters.Agi = this.Data.Agi + levelBonus * growth.AgiPerLevel;
			this.Parameters.Vit = this.Data.Vit + levelBonus * growth.VitPerLevel;
			this.Parameters.Int = this.Data.Int + levelBonus * growth.IntPerLevel;
			this.Parameters.Dex = this.Data.Dex + levelBonus * growth.DexPerLevel;
			this.Parameters.Luk = this.Data.Luk + levelBonus * growth.LukPerLevel;

			this.ApplyPassives();
			this.RecalculateStats();

			// Cap current HP/SP to the new max.
			if (this.Parameters.Hp > this.Parameters.HpMax) this.Parameters.Hp = this.Parameters.HpMax;
			if (this.Parameters.Sp > this.Parameters.SpMax) this.Parameters.Sp = this.Parameters.SpMax;
		}

		internal override bool IsHostileTo(Character target)
		{
			if (target == null || target == this) return false;
			if (target == this.Owner) return false;
			// Other players are neutral; PvP is not in scope.
			if (target is PlayerCharacter) return false;
			// Other homunculi (also Monsters) are neutral too.
			if (target is Homunculus) return false;
			// Wild monsters: hostile.
			return target is Monster;
		}

		public override int TakeDamage(int amount, Character attacker)
		{
			var remaining = base.TakeDamage(amount, attacker);
			// Mirror the entity's HP back to the persistent state so
			// the next call/rest sees the correct value.
			this.State.SyncHp(this.Parameters.Hp);
			return remaining;
		}

		/// <summary>
		/// Casts one of the homunculus's skills at the given target.
		/// Reuses the player skill-handler registry: each homun skill
		/// has a regular [SkillHandler(SkillId.X)] handler keyed off
		/// the SkillId enum's homun range (8001+).
		/// </summary>
		public System.Threading.Tasks.Task CastSkill(SkillId skillId, Character target, int level = 0)
		{
			// Gate on the homun's learned skill level. AI-driven calls
			// pass level 0 to mean "use the homun's currently-known
			// level"; explicit player invocations may override.
			var known = this.State.GetSkillLevel(skillId);
			if (known <= 0) return System.Threading.Tasks.Task.CompletedTask;

			var castLevel = level > 0 ? System.Math.Min(level, known) : known;

			if (!ZoneServer.Instance.SkillHandlers.TryGetHandler<Sabine.Zone.Skills.Handlers.ITargetedSkillHandler>(skillId, out var handler))
				return System.Threading.Tasks.Task.CompletedTask;

			var skill = new Sabine.Zone.Skills.Skill(this, skillId, castLevel);
			handler.Handle(new Sabine.Zone.Skills.Handlers.UseSkillParams(this, target ?? this, skill, castLevel));
			return System.Threading.Tasks.Task.CompletedTask;
		}
	}
}

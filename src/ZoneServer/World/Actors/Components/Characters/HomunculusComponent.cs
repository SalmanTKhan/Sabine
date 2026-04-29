using System;
using System.Collections.Generic;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Actors.Components.Characters
{
	public enum HomunculusType
	{
		None = 0,
		Lif = 1,
		Amistr = 2,
		Filir = 3,
		Vanilmirth = 4,
	}

	public enum HomunculusState
	{
		Absent = 0,
		Active = 1,
		Resting = 2,
		Dead = 3,
	}

	/// <summary>
	/// Per-player homunculus state. v1 tracks bookkeeping (type,
	/// state, HP, intimacy, hunger) without spawning a tangible
	/// map entity; the AM_* skills mutate this state and emit chat
	/// messages so players can use the skill chain end-to-end.
	/// Spawning a real Monster-derived companion with its own AI
	/// is a follow-up and slots into <see cref="Spawn"/>.
	/// </summary>
	public class HomunculusComponent : ICharacterComponent
	{
		public Character Character { get; }

		public HomunculusType Type { get; private set; } = HomunculusType.None;
		public HomunculusState State { get; private set; } = HomunculusState.Absent;
		public string Name { get; private set; } = "";
		public int Level { get; private set; } = 1;
		public int Hp { get; private set; }
		public int HpMax { get; private set; }
		public int Intimacy { get; private set; } = 1;
		public int Hunger { get; private set; } = 50;

		public bool HasContract => this.Type != HomunculusType.None && this.State != HomunculusState.Absent;
		public bool IsActive => this.State == HomunculusState.Active;

		public HomunculusComponent(Character character)
		{
			this.Character = character;
		}

		/// <summary>
		/// Pushes a fresh property block to the owner if the live
		/// entity is up. Safe no-op when the entity is null or the
		/// packet isn't registered for this client version.
		/// </summary>
		private void NotifyOwner()
		{
			if (this.Entity == null) return;
			if (this.Character is PlayerCharacter pc)
				Sabine.Zone.Network.Send.ZC_PROPERTY_HOMUN(pc, this.Entity);
		}

		// eAthena classic: hunger drops 1 every 6 minutes while the
		// homunculus is active. Intimacy nudges down by 1 when hunger
		// hits 0 (starving) and up by 1 when hunger ≥ 75 at tick time.
		private static readonly TimeSpan HungerInterval = TimeSpan.FromMinutes(6);
		private TimeSpan _hungerAccumulator;

		public void Update(TimeSpan elapsed)
		{
			if (this.State != HomunculusState.Active) return;

			_hungerAccumulator += elapsed;
			while (_hungerAccumulator >= HungerInterval)
			{
				_hungerAccumulator -= HungerInterval;
				this.TickHunger();
			}
		}

		private void TickHunger()
		{
			this.Hunger = Math.Max(0, this.Hunger - 1);

			if (this.Hunger == 0)
			{
				this.Intimacy = Math.Max(0, this.Intimacy - 1);
				if (this.Intimacy == 0)
				{
					// Bond broken — homunculus runs away. Despawn the
					// entity if present and clear the contract.
					this.Entity?.Map?.RemoveNpc(this.Entity);
					this.Entity = null;
					this.Type = HomunculusType.None;
					this.State = HomunculusState.Absent;
				}
			}
			else if (this.Hunger >= 75)
			{
				this.Intimacy = Math.Min(1000, this.Intimacy + 1);
			}
		}

		/// <summary>
		/// Feeds the homunculus, raising hunger by the given amount
		/// (typically 11 per food item). Excess hunger above 100
		/// loses intimacy — over-feeding is punished in eAthena.
		/// </summary>
		public void Feed(int amount, int intimacyBonus = 1)
		{
			var newHunger = this.Hunger + amount;
			if (newHunger > 100)
			{
				this.Intimacy = Math.Max(0, this.Intimacy - 5);
				this.Hunger = 100;
			}
			else
			{
				this.Hunger = newHunger;
				this.Intimacy = Math.Min(1000, this.Intimacy + intimacyBonus);
			}
			this.NotifyOwner();
		}

		/// <summary>
		/// Resolves an item id against the type-specific food map and
		/// applies it. Returns true if the item is valid food for
		/// this homunculus and was consumed.
		/// </summary>
		public bool FeedItem(int itemId)
		{
			var entry = HomunculusTypeData.GetFood(this.Type, itemId);
			if (entry == null) return false;
			this.Feed(entry.Hunger, entry.IntimacyBonus);
			return true;
		}

		public void Create(HomunculusType type, string name)
		{
			this.Type = type;
			this.Name = name;
			this.Level = 1;
			this.HpMax = 150;
			this.Hp = this.HpMax;
			this.Intimacy = 1;
			this.Hunger = 50;
			this.State = HomunculusState.Resting;
		}

		/// <summary>
		/// Live entity reference while the homunculus is summoned;
		/// null while resting / dead. Set by the spawn service.
		/// </summary>
		public Homunculus Entity { get; internal set; }

		internal void MarkActive() => this.State = HomunculusState.Active;
		internal void MarkResting() => this.State = HomunculusState.Resting;
		internal void MarkDead() => this.State = HomunculusState.Dead;

		internal void SyncHp(int hp)
		{
			this.Hp = Math.Max(0, hp);
			if (this.Hp == 0) this.State = HomunculusState.Dead;
		}

		public void Heal(int amount)
		{
			if (this.Hp <= 0) return;
			this.Hp = Math.Min(this.HpMax, this.Hp + amount);
		}

		public void TakeDamage(int amount)
		{
			this.Hp = Math.Max(0, this.Hp - amount);
			if (this.Hp == 0)
				this.State = HomunculusState.Dead;
		}

		public bool Resurrect()
		{
			if (this.State != HomunculusState.Dead) return false;
			this.Hp = this.HpMax / 2;
			this.State = HomunculusState.Resting;
			return true;
		}

		// --- EXP / leveling ---------------------------------------------
		// Simple geometric curve: level N requires 50 * N² total EXP.
		// HP grows by +20 per level.

		public int Exp { get; private set; }
		public int SkillPoints { get; private set; }

		// SkillId → learned level. Saved/loaded as a single delimited
		// string in vars (eAthena packs this similarly).
		private readonly Dictionary<SkillId, int> _learnedSkills = new();

		public int ExpToNext(int level) => 50 * (level + 1) * (level + 1);

		public int GetSkillLevel(SkillId skillId)
			=> _learnedSkills.TryGetValue(skillId, out var lv) ? lv : 0;

		public bool KnowsSkill(SkillId skillId) => this.GetSkillLevel(skillId) > 0;

		/// <summary>
		/// Try to spend one skill point to raise the given homun
		/// skill by one level. Returns the new level on success or
		/// 0 on failure (no points, missing prereqs, max level, or
		/// the skill isn't on the type's tree).
		/// </summary>
		public int TryLearn(SkillId skillId)
		{
			if (this.SkillPoints <= 0) return 0;

			var entry = HomunculusTypeData.FindSkill(this.Type, skillId);
			if (entry == null) return 0;
			if (this.Level < entry.RequiredHomunLevel) return 0;
			if (entry.Prerequisite != SkillId.None
				&& this.GetSkillLevel(entry.Prerequisite) < entry.PrerequisiteLevel) return 0;

			var current = this.GetSkillLevel(skillId);
			if (current >= entry.MaxLevel) return 0;

			_learnedSkills[skillId] = current + 1;
			this.SkillPoints--;

			// Refresh live-entity stats so the new rank takes effect
			// without a re-summon. RefreshFromState rebuilds passives
			// from scratch to avoid double-stacking.
			this.Entity?.RefreshFromState();
			this.NotifyOwner();
			return current + 1;
		}

		public void GainExp(int amount)
		{
			if (this.Type == HomunculusType.None) return;
			if (amount <= 0) return;

			this.Exp += amount;

			var growth = HomunculusTypeData.GetGrowth(this.Type);
			var leveledUp = false;
			while (this.Level < 99 && this.Exp >= this.ExpToNext(this.Level))
			{
				this.Exp -= this.ExpToNext(this.Level);
				this.Level++;
				this.HpMax += growth.HpPerLevel;
				this.Hp = this.HpMax;
				this.SkillPoints++;
				leveledUp = true;
			}

			// Single recalc at the end captures the cumulative changes.
			if (leveledUp)
				this.Entity?.RefreshFromState();
			this.NotifyOwner();
		}

		// --- Persistence ---------------------------------------------------
		// Persisted as discrete keys on character vars rather than a
		// dedicated table so no schema migration is needed. Active
		// state is downgraded to Resting on save — recall is the
		// player's first action after re-login.

		private const string KeyType = "homun.type";
		private const string KeyState = "homun.state";
		private const string KeyName = "homun.name";
		private const string KeyLevel = "homun.level";
		private const string KeyHp = "homun.hp";
		private const string KeyHpMax = "homun.hpMax";
		private const string KeyIntimacy = "homun.intimacy";
		private const string KeyHunger = "homun.hunger";
		private const string KeyExp = "homun.exp";
		private const string KeySkillPoints = "homun.skillPoints";
		private const string KeySkills = "homun.skills";

		public void SaveTo(Sabine.Shared.Util.Variables vars)
		{
			vars.Set(KeyType, (int)this.Type);

			if (this.Type == HomunculusType.None)
				return;

			var stateToSave = this.State == HomunculusState.Active
				? HomunculusState.Resting
				: this.State;
			vars.Set(KeyState, (int)stateToSave);
			vars.Set(KeyName, this.Name);
			vars.Set(KeyLevel, this.Level);
			vars.Set(KeyHp, this.Hp);
			vars.Set(KeyHpMax, this.HpMax);
			vars.Set(KeyIntimacy, this.Intimacy);
			vars.Set(KeyHunger, this.Hunger);
			vars.Set(KeyExp, this.Exp);
			vars.Set(KeySkillPoints, this.SkillPoints);
			vars.Set(KeySkills, SerializeSkills());
		}

		private string SerializeSkills()
		{
			if (_learnedSkills.Count == 0) return "";
			var parts = new List<string>(_learnedSkills.Count);
			foreach (var kvp in _learnedSkills)
				parts.Add($"{(int)kvp.Key}:{kvp.Value}");
			return string.Join(",", parts);
		}

		public void LoadFrom(Sabine.Shared.Util.Variables vars)
		{
			var type = (HomunculusType)vars.GetInt(KeyType, 0);
			if (type == HomunculusType.None) return;

			this.Type = type;
			this.State = (HomunculusState)vars.GetInt(KeyState, (int)HomunculusState.Resting);
			this.Name = vars.GetString(KeyName, type.ToString());
			this.Level = vars.GetInt(KeyLevel, 1);
			this.HpMax = vars.GetInt(KeyHpMax, 150);
			this.Hp = vars.GetInt(KeyHp, this.HpMax);
			this.Intimacy = vars.GetInt(KeyIntimacy, 1);
			this.Hunger = vars.GetInt(KeyHunger, 50);
			this.Exp = vars.GetInt(KeyExp, 0);
			this.SkillPoints = vars.GetInt(KeySkillPoints, 0);
			DeserializeSkills(vars.GetString(KeySkills, ""));
		}

		private void DeserializeSkills(string packed)
		{
			_learnedSkills.Clear();
			if (string.IsNullOrEmpty(packed)) return;
			foreach (var part in packed.Split(','))
			{
				var pair = part.Split(':');
				if (pair.Length != 2) continue;
				if (int.TryParse(pair[0], out var sid) && int.TryParse(pair[1], out var lv) && lv > 0)
					_learnedSkills[(SkillId)sid] = lv;
			}
		}
	}
}

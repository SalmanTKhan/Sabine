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

		// Evolved forms (eAthena mob ids 6009-6012). Reached via
		// AM_BIOETHICS + intimacy >= 910 + Stone of Sage in classic.
		Lif2 = 5,
		Amistr2 = 6,
		Filir2 = 7,
		Vanilmirth2 = 8,

		// S-class mutations (eAthena mob ids 6048-6051). Renewal-era
		// addition; require an already-evolved homun at level 99 with
		// intimacy 1000. Each S-class is rolled within its base
		// family at mutate time.
		Eira = 9,        // Lif S-class
		Bayeri = 10,     // Amistr S-class
		Sera = 11,       // Filir S-class
		Dieter = 12,     // Vanilmirth S-class
	}

	/// <summary>
	/// Family groupings used by Evolve / Mutate to find the base→evo
	/// and evo→S-class transitions. eAthena hard-codes these in
	/// <c>homun_class2mapid</c>; we keep the same mapping.
	/// </summary>
	public static class HomunculusTypeExtensions
	{
		public static HomunculusType GetBaseFamily(this HomunculusType type) => type switch
		{
			HomunculusType.Lif or HomunculusType.Lif2 or HomunculusType.Eira => HomunculusType.Lif,
			HomunculusType.Amistr or HomunculusType.Amistr2 or HomunculusType.Bayeri => HomunculusType.Amistr,
			HomunculusType.Filir or HomunculusType.Filir2 or HomunculusType.Sera => HomunculusType.Filir,
			HomunculusType.Vanilmirth or HomunculusType.Vanilmirth2 or HomunculusType.Dieter => HomunculusType.Vanilmirth,
			_ => HomunculusType.None,
		};

		public static HomunculusType GetEvolvedForm(this HomunculusType type) => type switch
		{
			HomunculusType.Lif => HomunculusType.Lif2,
			HomunculusType.Amistr => HomunculusType.Amistr2,
			HomunculusType.Filir => HomunculusType.Filir2,
			HomunculusType.Vanilmirth => HomunculusType.Vanilmirth2,
			_ => HomunculusType.None,
		};

		public static HomunculusType GetSClassForm(this HomunculusType type) => type switch
		{
			HomunculusType.Lif2 => HomunculusType.Eira,
			HomunculusType.Amistr2 => HomunculusType.Bayeri,
			HomunculusType.Filir2 => HomunculusType.Sera,
			HomunculusType.Vanilmirth2 => HomunculusType.Dieter,
			_ => HomunculusType.None,
		};

		public static bool IsEvolved(this HomunculusType type)
			=> type is HomunculusType.Lif2 or HomunculusType.Amistr2 or HomunculusType.Filir2 or HomunculusType.Vanilmirth2;

		public static bool IsSClass(this HomunculusType type)
			=> type is HomunculusType.Eira or HomunculusType.Bayeri or HomunculusType.Sera or HomunculusType.Dieter;
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

		/// <summary>
		/// Persists a player-supplied name. Mirrors
		/// <c>CZ_RENAME_MER</c>. Empty / whitespace input is rejected.
		/// Survives re-login via the vars store below.
		/// </summary>
		public void Rename(string newName)
		{
			if (string.IsNullOrWhiteSpace(newName)) return;
			this.Name = newName;
		}

		// --- Evolution / Mutation ----------------------------------------
		// eAthena classic gates: Evolve needs intimacy ≥ 910 (Loyal
		// band) on a base form; Mutate needs an evolved form at level
		// 99 with intimacy 1000. Both consume a Stone of Sage (7619)
		// in eAthena but service-level item consumption is left to
		// callers — this component just owns the state transition.

		/// <summary>Minimum intimacy to attempt evolution (Loyal band).</summary>
		public const int EvolveIntimacyThreshold = 910;

		/// <summary>Minimum level to attempt S-class mutation.</summary>
		public const int MutateLevelThreshold = 99;

		/// <summary>Minimum intimacy to attempt S-class mutation.</summary>
		public const int MutateIntimacyThreshold = 1000;

		/// <summary>
		/// Promotes a base homunculus to its evolved form. The level,
		/// EXP, intimacy, and learned skills carry over; HpMax /
		/// stat profile rebuilds from the new growth curve. Returns
		/// the new type on success or <see cref="HomunculusType.None"/>
		/// when the gate fails (already evolved, wrong family,
		/// intimacy too low, dead, or no contract).
		/// </summary>
		public HomunculusType Evolve()
		{
			if (!this.HasContract) return HomunculusType.None;
			if (this.State == HomunculusState.Dead) return HomunculusType.None;
			if (this.Intimacy < EvolveIntimacyThreshold) return HomunculusType.None;

			var evolved = this.Type.GetEvolvedForm();
			if (evolved == HomunculusType.None) return HomunculusType.None;

			this.Type = evolved;
			this.ShuffleBaselineStats();
			this.Entity?.RefreshFromState();
			this.NotifyOwner();
			return evolved;
		}

		/// <summary>
		/// Mutates an evolved homunculus into its S-class form.
		/// Level/EXP/intimacy carry; HpMax rebuilds from the S-class
		/// growth curve. Returns the new type on success or
		/// <see cref="HomunculusType.None"/> when the gate fails.
		/// </summary>
		public HomunculusType Mutate()
		{
			if (!this.HasContract) return HomunculusType.None;
			if (this.State == HomunculusState.Dead) return HomunculusType.None;
			if (!this.Type.IsEvolved()) return HomunculusType.None;
			if (this.Level < MutateLevelThreshold) return HomunculusType.None;
			if (this.Intimacy < MutateIntimacyThreshold) return HomunculusType.None;

			var sclass = this.Type.GetSClassForm();
			if (sclass == HomunculusType.None) return HomunculusType.None;

			this.Type = sclass;
			this.ShuffleBaselineStats();
			this.Entity?.RefreshFromState();
			this.NotifyOwner();
			return sclass;
		}

		/// <summary>
		/// Re-rolls the HP baseline using the current type's growth
		/// curve. Used after Evolve / Mutate, and exposed standalone
		/// for the homshuffle command (rA classic stat-shuffle on a
		/// non-evolved homun is mostly nostalgia, but the GM chat
		/// command stub expects to call it).
		/// </summary>
		public void ShuffleBaselineStats()
		{
			var growth = HomunculusTypeData.GetGrowth(this.Type);
			var levelBonus = this.Level - 1;
			// Base HP scales with the new form's per-level growth. The
			// living-entity stat bump runs via RefreshFromState.
			this.HpMax = 150 + levelBonus * growth.HpPerLevel;
			this.Hp = this.HpMax;
		}

		/// <summary>
		/// Wipes the contract entirely — used by the dismiss path.
		/// Unlike <see cref="MarkResting"/>, this resets the type so
		/// <see cref="HasContract"/> goes back to false and the saved
		/// vars row goes empty on the next persist.
		/// </summary>
		public void Clear()
		{
			this.Entity?.Map?.RemoveNpc(this.Entity);
			this.Entity = null;
			this.Type = HomunculusType.None;
			this.State = HomunculusState.Absent;
			this.Name = string.Empty;
			this.Level = 1;
			this.Hp = 0;
			this.HpMax = 0;
			this.Intimacy = 0;
			this.Hunger = 0;
			this.Exp = 0;
			this.SkillPoints = 0;
			_learnedSkills.Clear();
		}

		internal void SyncHp(int hp)
		{
			this.Hp = Math.Max(0, hp);
			if (this.Hp == 0) this.State = HomunculusState.Dead;
		}

		/// <summary>
		/// GM/test setter for intimacy. Clamps to [0, 1000] (eAthena
		/// hard cap). Doesn't trigger the runaway path even at 0 —
		/// only the hunger tick does that, so admins can probe the
		/// edge without losing the contract.
		/// </summary>
		public void SetIntimacy(int value)
		{
			this.Intimacy = Math.Max(0, Math.Min(1000, value));
			this.NotifyOwner();
		}

		/// <summary>
		/// GM/test setter for hunger. Clamps to [0, 100].
		/// </summary>
		public void SetHunger(int value)
		{
			this.Hunger = Math.Max(0, Math.Min(100, value));
			this.NotifyOwner();
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
			var maxLevel = entry.MaxLevel + HomunculusTypeData.GetMaxLevelBonus(this.Type);
			if (current >= maxLevel) return 0;

			_learnedSkills[skillId] = current + 1;
			this.SkillPoints--;

			// Refresh live-entity stats so the new rank takes effect
			// without a re-summon. RefreshFromState rebuilds passives
			// from scratch to avoid double-stacking.
			this.Entity?.RefreshFromState();
			this.NotifyOwner();

			// Push the single-skill rank-up packet (rA clif_homskillup).
			// NotifyOwner already covers the property panel; this one
			// targets the skill-tree row specifically.
			if (this.Character is PlayerCharacter pc)
				Sabine.Zone.Network.Send.ZC_HOSKILLINFO_UPDATE(pc, skillId, current + 1);

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

		// --- DB-backed snapshot --------------------------------------------
		// Mirrors the `homunculus` SQL table. Lets ZoneDb persist the
		// state to a dedicated row instead of packing eleven keys into
		// vars_character. Active is still downgraded to Resting on
		// snapshot so the re-summon flow doesn't auto-spawn on login.

		/// <summary>
		/// Returns a snapshot of the current state for DB persistence.
		/// Returns <c>null</c> when the contract is empty — the caller
		/// uses that as the signal to delete any stored row.
		/// </summary>
		public HomunculusSnapshot ToSnapshot()
		{
			if (this.Type == HomunculusType.None) return null;
			var stateToSave = this.State == HomunculusState.Active
				? HomunculusState.Resting
				: this.State;
			return new HomunculusSnapshot
			{
				Type = (int)this.Type,
				State = (int)stateToSave,
				Name = this.Name,
				Level = this.Level,
				Hp = this.Hp,
				HpMax = this.HpMax,
				Intimacy = this.Intimacy,
				Hunger = this.Hunger,
				Exp = this.Exp,
				SkillPoints = this.SkillPoints,
				Skills = this.SerializeSkills(),
			};
		}

		/// <summary>
		/// Hydrates the component from a stored row. No-ops on null /
		/// empty type — the caller falls back to the vars-keys path
		/// in that case (during migration).
		/// </summary>
		public void LoadFromSnapshot(HomunculusSnapshot snapshot)
		{
			if (snapshot == null) return;
			var type = (HomunculusType)snapshot.Type;
			if (type == HomunculusType.None) return;

			this.Type = type;
			this.State = (HomunculusState)snapshot.State;
			this.Name = snapshot.Name ?? type.ToString();
			this.Level = snapshot.Level <= 0 ? 1 : snapshot.Level;
			this.HpMax = snapshot.HpMax <= 0 ? 150 : snapshot.HpMax;
			this.Hp = snapshot.Hp <= 0 ? this.HpMax : snapshot.Hp;
			this.Intimacy = snapshot.Intimacy;
			this.Hunger = snapshot.Hunger;
			this.Exp = snapshot.Exp;
			this.SkillPoints = snapshot.SkillPoints;
			this.DeserializeSkills(snapshot.Skills ?? string.Empty);
		}
	}

	/// <summary>
	/// Flat DTO matching one row of the <c>homunculus</c> SQL table.
	/// Used by ZoneDb to persist a contract without going through the
	/// vars store. Skills are packed in the same delimited format
	/// <see cref="HomunculusComponent"/> already uses for vars
	/// (<c>&lt;skillId&gt;:&lt;level&gt;,...</c>) so the two paths
	/// stay format-compatible during the migration.
	/// </summary>
	public class HomunculusSnapshot
	{
		public int Type { get; set; }
		public int State { get; set; }
		public string Name { get; set; } = string.Empty;
		public int Level { get; set; }
		public int Hp { get; set; }
		public int HpMax { get; set; }
		public int Intimacy { get; set; }
		public int Hunger { get; set; }
		public int Exp { get; set; }
		public int SkillPoints { get; set; }
		public string Skills { get; set; } = string.Empty;
	}
}

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Sabine.Shared.Const;
using Yggdrasil.Data.JSON;
using Yggdrasil.Extensions;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// One row from <c>system/data/mob_skills.txt</c> — a single skill
	/// entry attached to a mob class. Slimmer than eAthena's
	/// <c>db/mob_skill_db.txt</c> (which carries 19 columns including
	/// per-state, per-condition, val1–5, emotion, chat); v1 covers the
	/// essentials and the eA columns can grow in later passes without
	/// changing the loader shape — the omitted fields just default to
	/// "always-true while in combat".
	/// </summary>
	public class MobSkillEntry
	{
		/// <summary>Mob class id. Multiple entries per mob are allowed.</summary>
		public int MobId { get; set; }

		/// <summary>Skill to cast; routed through the standard handler registry.</summary>
		public SkillId SkillId { get; set; }

		/// <summary>Skill level for the cast.</summary>
		public int Level { get; set; } = 1;

		/// <summary>Per-100 chance to fire each AI tick while the condition holds.</summary>
		public int Rate { get; set; } = 50;

		/// <summary>Minimum milliseconds between casts of this skill on this mob.</summary>
		public int CooldownMs { get; set; } = 5000;

		/// <summary>Target mode: "target" / "self".</summary>
		public string Target { get; set; } = "target";
	}

	/// <summary>
	/// Database keyed by mob id; <c>GetForMob</c> returns the list of
	/// skills on that mob (empty list when none). The list is fed into
	/// the AI tick — order doesn't carry meaning, the rate roll picks
	/// independently per entry per tick.
	/// </summary>
	public class MobSkillDb : DatabaseJsonIndexed<int, List<MobSkillEntry>>
	{
		/// <inheritdoc/>
		protected override void ReadEntry(JObject entry)
		{
			entry.AssertNotMissing("mobId", "skillId");

			var data = new MobSkillEntry
			{
				MobId = entry.ReadInt("mobId"),
				SkillId = (SkillId)System.Enum.Parse(typeof(SkillId), entry.ReadString("skillId")),
				Level = entry.ReadInt("level", 1),
				Rate = entry.ReadInt("rate", 50),
				CooldownMs = entry.ReadInt("cooldownMs", 5000),
				Target = entry.ReadString("target", "target"),
			};

			if (!this.Entries.TryGetValue(data.MobId, out var list))
			{
				list = new List<MobSkillEntry>();
				this.AddOrReplace(data.MobId, list);
			}
			list.Add(data);
		}

		public IReadOnlyList<MobSkillEntry> GetForMob(int mobId)
		{
			if (this.Entries.TryGetValue(mobId, out var list))
				return list;
			return System.Array.Empty<MobSkillEntry>();
		}
	}
}

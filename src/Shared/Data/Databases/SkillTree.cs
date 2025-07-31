using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sabine.Shared.Const;
using Yggdrasil.Data.JSON;
using Yggdrasil.Extensions;

namespace Sabine.Shared.Data.Databases
{
	/// <summary>
	/// Represents a skill prerequisite.
	/// </summary>
	public class SkillPrerequisiteData
	{
		public SkillId SkillId { get; set; }
		public int RequiredLevel { get; set; }
	}

	/// <summary>
	/// Represents a skill's entry in a job's skill tree.
	/// </summary>
	public class SkillTreeData
	{
		public JobId JobId { get; set; }
		public SkillId SkillId { get; set; }
		public int MaxLevel { get; set; }
		public List<SkillPrerequisiteData> Prerequisites { get; set; } = new();
	}

	/// <summary>
	/// A skill tree database, outlining which skills jobs can learn and
	/// what the prerequisites are.
	/// </summary>
	public class SkillTreeDb : DatabaseJson<SkillTreeData>
	{
		/// <summary>
		/// Returns all skill tree entries for a given job.
		/// </summary>
		/// <param name="jobId"></param>
		/// <returns></returns>
		public List<SkillTreeData> FindByJob(JobId jobId)
			=> this.Entries.Where(e => e.JobId == jobId).ToList();

		/// <summary>
		/// Finds a specific skill tree entry for a job and skill ID.
		/// </summary>
		/// <param name="jobId"></param>
		/// <param name="skillId"></param>
		/// <returns></returns>
		public SkillTreeData FindByJobAndSkill(JobId jobId, SkillId skillId)
			=> this.Find(e => e.JobId == jobId && e.SkillId == skillId);

		/// <summary>
		/// Reads a single entry from the JSON database file.
		/// </summary>
		/// <param name="entry"></param>
		protected override void ReadEntry(JObject entry)
		{
			var versionMin = entry.ReadInt("versionMin", 0);
			var versionMax = entry.ReadInt("versionMax", int.MaxValue);

			if (Game.Version < versionMin || Game.Version > versionMax)
				return;

			entry.AssertNotMissing("jobId", "skillId", "maxLevel");

			var data = new SkillTreeData();

			data.JobId = ReadId<JobId>(entry["jobId"]);
			data.SkillId = ReadId<SkillId>(entry["skillId"]);
			data.MaxLevel = entry.ReadInt("maxLevel");

			if (entry.ContainsKey("prerequisites"))
			{
				foreach (var prereqEntry in entry.ForEachObject("prerequisites"))
				{
					prereqEntry.AssertNotMissing("skillId", "requiredLevel");

					var prereq = new SkillPrerequisiteData
					{
						SkillId = ReadId<SkillId>(prereqEntry["skillId"]),
						RequiredLevel = prereqEntry.ReadInt("requiredLevel")
					};

					data.Prerequisites.Add(prereq);
				}
			}

			this.Add(data);
		}

		/// <summary>
		/// Reads an ID value that can either be an integer or a string enum name.
		/// </summary>
		/// <typeparam name="TEnum"></typeparam>
		/// <param name="token"></param>
		/// <returns></returns>
		private TEnum ReadId<TEnum>(JToken token) where TEnum : struct, Enum
		{
			if (token.Type == JTokenType.Integer)
				return (TEnum)(object)token.Value<int>();

			if (token.Type == JTokenType.String)
				return Enum.Parse<TEnum>(token.Value<string>(), true);

			throw new FormatException($"Invalid token type for ID: {token.Type}");
		}
	}
}

using System;
using System.IO;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Yggdrasil.Data;
using Xunit;

namespace Tests.Sabine.Shared.Data
{
	[Collection("PacketTable")]
	public class VersionedSkillDataTests
	{
		[Fact]
		public void Alpha_skips_beta1_skills_and_skill_tree_groups()
		{
			var previousVersion = Game.Version;
			try
			{
				Game.Version = Versions.Alpha;
				var skills = new SkillDb();
				Load(skills, "[{ id: 1, skillId: 'NV_BASIC', name: 'Basic Skills', maxLevel: 9, versionMin: 200 }]");

				var jobs = CreateJobs();
				var trees = new SkillTreeDb(jobs, skills);
				Load(trees, "[{ job: 'Novice', versionMin: 200, skills: [{ id: 'NV_BASIC', maxLevel: 9 }] }]");

				Assert.Empty(skills.Entries);
				Assert.Empty(trees.Entries);
				Assert.Contains("NV_BASIC", skills.VersionDiagnostics.SkippedEntries[0]);
				Assert.Contains("job Novice", trees.VersionDiagnostics.SkippedEntries[0]);
			}
			finally
			{
				Game.Version = previousVersion;
			}
		}

		[Fact]
		public void Beta1_loads_nv_basic_and_its_skill_tree()
		{
			var previousVersion = Game.Version;
			try
			{
				Game.Version = Versions.Beta1;
				var skills = new SkillDb();
				Load(skills, "[{ id: 1, skillId: 'NV_BASIC', name: 'Basic Skills', maxLevel: 9, versionMin: 200 }]");

				var jobs = CreateJobs();
				var trees = new SkillTreeDb(jobs, skills);
				Load(trees, "[{ job: 'Novice', versionMin: 200, skills: [{ id: 'NV_BASIC', maxLevel: 9 }] }]");

				Assert.True(skills.TryFind(SkillId.NV_BASIC, out _));
				Assert.True(trees.TryFind(JobId.Novice, out var tree));
				Assert.Single(tree.Skills);
				Assert.Equal(SkillId.NV_BASIC, tree.Skills[0].SkillId);
			}
			finally
			{
				Game.Version = previousVersion;
			}
		}

		[Fact]
		public void Inactive_child_is_skipped_before_skill_lookup()
		{
			var previousVersion = Game.Version;
			try
			{
				Game.Version = Versions.Alpha;
				var skills = new SkillDb();
				var jobs = CreateJobs();
				var trees = new SkillTreeDb(jobs, skills);

				Load(trees, "[{ job: 'Novice', skills: [{ id: 'NV_BASIC', maxLevel: 9, versionMin: 200 }] }]");

				Assert.True(trees.TryFind(JobId.Novice, out var tree));
				Assert.Empty(tree.Skills);
			}
			finally
			{
				Game.Version = previousVersion;
			}
		}

		[Fact]
		public void Unknown_active_skill_still_fails_validation()
		{
			var previousVersion = Game.Version;
			try
			{
				Game.Version = Versions.Beta1;
				var trees = new SkillTreeDb(CreateJobs(), new SkillDb());

				Assert.Throws<DatabaseErrorException>(() =>
					Load(trees, "[{ job: 'Novice', skills: [{ id: 'NOT_A_SKILL', maxLevel: 1 }] }]")
				);
			}
			finally
			{
				Game.Version = previousVersion;
			}
		}

		private static JobDb CreateJobs()
		{
			var jobs = new JobDb();
			Load(jobs, "[{ id: 'Novice', name: 'Novice', modifiers: { weight: 0, hpFactor: 0, hpMultiplier: 1, spFactor: 0 }, weaponDelays: { bareHand: 1, dagger: 1, sword: 1, bow: 1, spear: 1, axe: 1, mace: 1, rod: 1 } }]");
			return jobs;
		}

		private static void Load(IDatabase database, string contents)
		{
			var path = Path.GetTempFileName();
			try
			{
				File.WriteAllText(path, contents);
				database.LoadFile(path);
			}
			finally
			{
				File.Delete(path);
			}
		}
	}
}

using System;
using Sabine.Shared.Const;

namespace Sabine.Zone.Skills.Handlers.Acolyte
{
	[SkillHandler(SkillId.AL_ANGELUS)]
	public class AngelusHandler : BuffSkillHandler
	{
		// eAthena skill_cast_db: 28,0,1000,0,60000:80000:...:240000
		private static readonly int[] DurationsMs = { 60000, 80000, 100000, 120000, 140000, 160000, 180000, 200000, 220000, 240000 };

		protected override StatusId StatusId => StatusId.Angelus;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromMilliseconds(DurationsMs[Math.Clamp(level - 1, 0, DurationsMs.Length - 1)]);
	}

	[SkillHandler(SkillId.AL_BLESSING)]
	public class BlessingHandler : BuffSkillHandler
	{
		// eAthena skill_cast_db: 29,1000,1000,0,60000:80000:...:240000
		private static readonly int[] DurationsMs = { 60000, 80000, 100000, 120000, 140000, 160000, 180000, 200000, 220000, 240000 };

		protected override StatusId StatusId => StatusId.Blessing;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromMilliseconds(DurationsMs[Math.Clamp(level - 1, 0, DurationsMs.Length - 1)]);
	}

	[SkillHandler(SkillId.AL_INCAGI)]
	public class IncreaseAgiHandler : BuffSkillHandler
	{
		// eAthena skill_cast_db: 34,0,0,0,60000:80000:...:240000
		private static readonly int[] DurationsMs = { 60000, 80000, 100000, 120000, 140000, 160000, 180000, 200000, 220000, 240000 };

		protected override StatusId StatusId => StatusId.IncreaseAgi;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromMilliseconds(DurationsMs[Math.Clamp(level - 1, 0, DurationsMs.Length - 1)]);
	}

	[SkillHandler(SkillId.AL_DECAGI)]
	public class DecreaseAgiHandler : BuffSkillHandler
	{
		// eAthena skill_cast_db: 33,500,3500,0,30000:60000:...:300000
		private static readonly int[] DurationsMs = { 30000, 60000, 90000, 120000, 150000, 180000, 210000, 240000, 270000, 300000 };

		protected override StatusId StatusId => StatusId.DecreaseAgi;
		protected override TimeSpan GetDuration(int level) => TimeSpan.FromMilliseconds(DurationsMs[Math.Clamp(level - 1, 0, DurationsMs.Length - 1)]);
	}
}

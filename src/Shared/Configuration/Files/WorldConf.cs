using Yggdrasil.Configuration;

namespace Sabine.Shared.Configuration.Files
{
	/// <summary>
	/// Represents world.conf.
	/// </summary>
	public class WorldConf : ConfFile
	{
		// exp.conf
		public int JobExpRate { get; set; }

		// items.conf
		public int ItemDropRate { get; set; }
		public int ItemDisappearTime { get; set; }

		// monsters.conf
		public DisplayMonsterHpType DisplayMonsterHp { get; set; }

		// party.conf
		public int MaxPartySize { get; set; }

		// skills.conf
		public bool CheckBasicSkills { get; set; }

		/// <summary>
		/// Loads the conf file and its options from the given path.
		/// </summary>
		public void Load(string filePath)
		{
			this.Require(filePath);

			this.JobExpRate = this.GetInt("job_exp_rate", 100);

			this.ItemDropRate = this.GetInt("item_drop_rate", 100);
			this.ItemDisappearTime = this.GetInt("item_disappear_time", 30);

			this.DisplayMonsterHp = (DisplayMonsterHpType)this.GetInt("display_monster_hp", (int)DisplayMonsterHpType.No);

			this.MaxPartySize = this.GetInt("max_party_size", 8);

			this.CheckBasicSkills = this.GetBool("check_basic_skills", true);
		}
	}

	public enum DisplayMonsterHpType
	{
		No,
		Percentage,
		Actual,
	}
}

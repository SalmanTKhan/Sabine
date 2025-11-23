using Yggdrasil.Configuration;

namespace Sabine.Shared.Configuration.Files
{
	/// <summary>
	/// Represents world.conf.
	/// </summary>
	public class WorldConf : ConfFile
	{
		public int ItemDropRate { get; set; }
		public int ItemDisappearTime { get; set; }
		public DisplayMonsterHpType DisplayMonsterHp { get; set; }
		public int MaxPartySize { get; set; }
		public bool BasicSkillCheck { get; set; }

		/// <summary>
		/// Loads the conf file and its options from the given path.
		/// </summary>
		public void Load(string filePath)
		{
			this.Require(filePath);

			this.ItemDropRate = this.GetInt("item_drop_rate", 100);
			this.ItemDisappearTime = this.GetInt("item_disappear_time", 30);
			this.DisplayMonsterHp = (DisplayMonsterHpType)this.GetInt("display_monster_hp", (int)DisplayMonsterHpType.No);
			this.MaxPartySize = this.GetInt("max_party_size", 8);
			this.BasicSkillCheck = this.GetBool("basic_skill_check", true);
		}
	}

	public enum DisplayMonsterHpType
	{
		No,
		Percentage,
		Actual,
	}
}

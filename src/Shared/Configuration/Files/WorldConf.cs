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
		public int PartyEvenShareBonus { get; set; }

		// skills.conf
		public bool CheckBasicSkills { get; set; }

		// items.conf — durability model
		// When true, retains the legacy behaviour of destroying an item
		// on a refine-break failure. When false (the default), the item
		// is force-unequipped and flagged as broken (Item.IsBroken),
		// preserving it in the player's inventory so BS_REPAIRWEAPON or
		// the @repair GM command can restore it.
		public bool RefineBreakDestroys { get; set; }

		// mail.conf — auto-purge schedule
		// Mail older than this many days is deleted from mail_messages
		// by MailManager's expiration tick. Set to 0 to disable purging
		// entirely (rows live forever). Defaults to 15 to mirror rA's
		// `mail_expire_days`.
		public int MailExpireDays { get; set; }

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
			this.PartyEvenShareBonus = this.GetInt("party_even_share_bonus", 0);

			this.CheckBasicSkills = this.GetBool("check_basic_skills", true);

			this.RefineBreakDestroys = this.GetBool("refine_break_destroys", false);

			this.MailExpireDays = this.GetInt("mail_expire_days", 15);
		}
	}

	public enum DisplayMonsterHpType
	{
		No,
		Percentage,
		Actual,
	}
}

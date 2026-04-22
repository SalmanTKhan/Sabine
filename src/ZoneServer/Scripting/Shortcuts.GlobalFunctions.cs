using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Zone.World.Actors;
using Yggdrasil.Logging;

namespace Sabine.Zone.Scripting
{
	/// <summary>
	/// A static class containing C# equivalents of rAthena's global script functions.
	/// These methods can be called from any script to perform common tasks.
	/// It's recommended to place this file in a shared location accessible by all scripts.
	/// </summary>
	public static partial class Shortcuts
	{
		#region Variable Management

		/// <summary>
		/// Clears all job quest-related player variables.
		/// Equivalent to rAthena's F_ClearJobVar.
		/// </summary>
		public static void ClearJobVar(PlayerCharacter player)
		{
			var varsToClear = new[]
			{
            // Misc
            "JBLVL", "FIRSTAID", "PLAYDEAD", "got_bandage", "got_novnametag",
            // First Class
            "job_acolyte_q", "job_acolyte_q2", "job_archer_q", "job_magician_q",
			"job_merchant_q", "job_merchant_q2", "job_merchant_q3",
			"job_sword_q", "SWTEST", "job_thief_q",
            // Super Novice
            "SUPNOV_Q",
            // 2-1 Jobs
            "ASSIN_Q", "ASSIN_Q2", "ASSIN_Q3", "BSMITH_Q", "BSMITH_Q2",
			"HNTR_Q", "HNTR_Q2", "KNIGHT_Q", "KNIGHT_Q2",
			"PRIEST_Q", "PRIEST_Q2", "PRIEST_Q3", "WIZ_Q", "WIZ_Q2",
            // 2-2 Jobs
            "ROGUE_Q", "ROGUE_Q2", "ALCH_Q", "ALCH_Q2", "CRUS_Q",
			"MONK_Q", "JOB_MONK_C", "SAGE_Q", "SAGE_Q2", "DANC_Q", "BARD_Q",
            // Extended
            "TAEK_Q", "TK_Q", "STGL_Q", "SOUL_Q", "GUNS_Q", "NINJ_Q"
		};

			foreach (var varName in varsToClear)
			{
				player.Vars.Perm.Set($"quest_{varName}", 0);
			}
		}

		/// <summary>
		/// Clears old or unused player variables.
		/// Equivalent to rAthena's F_ClearGarbage.
		/// </summary>
		public static void ClearGarbage(PlayerCharacter player)
		{
			const string VAR_TURTLE = "quest_TURTLE";
			const string VAR_MISC_QUEST = "quest_MISC_QUEST";

			if (player.Vars.Perm.GetInt(VAR_TURTLE, 0) == 20)
			{
				int miscQuest = player.Vars.Perm.GetInt(VAR_MISC_QUEST, 0);
				player.Vars.Perm.Set(VAR_MISC_QUEST, miscQuest | 65536);
			}

			if ((player.Vars.Perm.GetInt(VAR_MISC_QUEST, 0) & 65536) != 0)
			{
				player.Vars.Perm.Set(VAR_TURTLE, 0);
			}

			// Clear other deprecated variables
			player.Vars.Perm.Set("quest_ADV_QSK", 0);
			player.Vars.Perm.Set("quest_ADV_QSK2", 0);
			player.Vars.Perm.Set("quest_RES_SKILL", 0);
			player.Vars.Perm.Set("quest_wizard_m2", 0);

			// Old Novice variables
			for (int i = 0; i <= 5; i++) player.Vars.Perm.Set($"quest_NEW_MES_FLAG{i}", 0);
			for (int i = 0; i <= 1; i++) player.Vars.Perm.Set($"quest_NEW_LVUP{i}", 0);
			player.Vars.Perm.Set("quest_NEW_JOBLVUP", 0);

			// Old DTS variables
			player.Vars.Perm.Set("quest_dtseligible", 0);
			int currentMisc = player.Vars.Perm.GetInt(VAR_MISC_QUEST, 0);
			player.Vars.Perm.Set(VAR_MISC_QUEST, currentMisc & ~128);
		}

		#endregion

		#region Player and Job

		/// <summary>
		/// Changes the player's job. In Sabine, the distinction between
		/// normal, advanced, and baby classes is handled by the JobId enum.
		/// </summary>
		public static void JobChange(PlayerCharacter player, JobId newJob)
		{
			player.ChangeJob(newJob);
			Log.Info($"CLASS CHANGE: {player.Name} became a {newJob}.");
		}

		/// <summary>
		/// Checks if the player can open their storage.
		/// Assumes 'basicskillcheck()' corresponds to a server configuration.
		/// </summary>
		public static bool CanOpenStorage(PlayerCharacter player)
		{
			var basicSkillCheck = ZoneServer.Instance.Conf.World.BasicSkillCheck;
			if (!basicSkillCheck) return true;

			return !(player.Skills.GetLevel(SkillId.NV_BASIC) < 6 && player.Skills.GetLevel(SkillId.SU_BASIC_SKILL) < 1);
		}

		/// <summary>
		/// Checks if the player has learned all basic skills required for a job change.
		/// Assumes 'basicskillcheck()' corresponds to a server configuration.
		/// </summary>
		public static bool CanChangeJob(this PlayerCharacter player)
		{
			var basicSkillCheck = ZoneServer.Instance.Conf.World.BasicSkillCheck;
			if (!basicSkillCheck) return true;

			return player.Skills.GetLevel(SkillId.NV_BASIC) > 8;
		}

		#endregion

		#region Dialog and Text Helpers

		/// <summary>
		/// Returns a random argument from the provided list.
		/// </summary>
		public static T Rand<T>(params T[] args)
		{
			if (args == null || args.Length == 0) return default;
			return args[Random(0, args.Length)];
		}

		/// <summary>
		/// Returns a message based on the player's sex.
		/// </summary>
		public static string SexMes(PlayerCharacter player, string femaleMessage, string maleMessage)
		{
			return player.Sex == Sex.Female ? femaleMessage : maleMessage;
		}

		/// <summary>Returns a random "hello" message.</summary>
		public static string Hi() => Rand("Hi!", "Hello!", "Good day!", "How are you?", "Hello there.");

		/// <summary>Returns a random "goodbye" message.</summary>
		public static string Bye() => Rand("Bye. See you again.", "Later.", "Goodbye.", "Good luck!", "Have a nice day!", "Byebye!!!");

		/// <summary>
		/// Returns a fully formatted item name string, including refine, element, etc.
		/// </summary>
		public static string ItemName(int itemId, int element = 0, int vvs = 0, int refine = 0)
		{
			var sb = new StringBuilder();
			if (refine > 0) sb.Append($"+{refine} ");
			if (vvs > 0) sb.Append(vvs switch { 1 => "VS ", 2 => "VVS ", 3 => "VVVS ", _ => $"{vvs}xVS " });
			if (element > 0) sb.Append(element switch { 1 => "Ice ", 2 => "Earth ", 3 => "Fire ", 4 => "Wind ", _ => "Strange " });

			ZoneServer.Instance.Data.Items.TryFind(itemId, out var item);
			sb.Append(item?.Name ?? "Unknown Item");

			return $"^000090{sb}^000000";
		}

		#endregion

		#region Skill Management

		/// <summary>
		/// Saves the player's learned quest skills into permanent variables using bitmasks.
		/// </summary>
		public static void SaveQuestSkills(PlayerCharacter player)
		{
			uint advQsk = 0;
			uint advQsk2 = 0;

			for (var i = 0; i < 14; i++)
			{
				if (player.Skills.GetLevel((SkillId)(144 + i)) > 0)
					advQsk |= (1u << i);
			}
			for (var i = 0; i < 19; i++)
			{
				if (player.Skills.GetLevel((SkillId)(1001 + i)) > 0)
					advQsk2 |= (1u << i);
			}
			player.Vars.Perm.Set("quest_ADV_QSK", (int)advQsk);
			player.Vars.Perm.Set("quest_ADV_QSK2", (int)advQsk2);
		}

		/// <summary>Restores learned 1st class quest skills from saved variables.</summary>
		public static void Load1stSkills(PlayerCharacter player)
		{
			var advQsk = (uint)player.Vars.Perm.GetInt("quest_ADV_QSK", 0);
			for (var i = 0; i < 14; i++)
			{
				if ((advQsk & (1u << i)) != 0)
				{
					player.Skills.Add((SkillId)(144 + i), 1, SkillPerm.Permanent);
				}
			}
			player.Vars.Perm.Set("quest_ADV_QSK", 0); // Clear var
		}

		/// <summary>Restores learned 2nd class quest skills from saved variables.</summary>
		public static void Load2ndSkills(PlayerCharacter player)
		{
			uint advQsk2 = (uint)player.Vars.Perm.GetInt("quest_ADV_QSK2", 0);
			for (int i = 0; i < 19; i++)
			{
				if ((advQsk2 & (1u << i)) != 0)
				{
					player.Skills.Add((SkillId)(1001 + i), 1, SkillPerm.Permanent);
				}
			}
			player.Vars.Perm.Set("quest_ADV_QSK2", 0); // Clear var
		}

		/// <summary>Gives the player their appropriate Platinum Skills based on their class.</summary>
		public static void GetPlatinumSkills(PlayerCharacter player)
		{
			player.Skills.Add(SkillId.NV_FIRSTAID, 1, SkillPerm.Permanent);

			// This assumes player.JobData.BaseClass exists and maps to base JobIds.
			var baseClass = player.JobData.Id;

			switch (baseClass)
			{
				case JobId.Novice:
					if (player.JobId != JobId.SuperNovice) player.Skills.Add(SkillId.NV_TRICKDEAD, 1, SkillPerm.Permanent);
					break;
				case JobId.Swordman:
					player.Skills.Add(SkillId.SM_MOVINGRECOVERY, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.SM_FATALBLOW, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.SM_AUTOBERSERK, 1, SkillPerm.Permanent);
					break;
				case JobId.Mage:
					player.Skills.Add(SkillId.MG_ENERGYCOAT, 1, SkillPerm.Permanent);
					break;
				case JobId.Archer:
					player.Skills.Add(SkillId.AC_MAKINGARROW, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.AC_CHARGEARROW, 1, SkillPerm.Permanent);
					break;
				case JobId.Acolyte:
					player.Skills.Add(SkillId.AL_HOLYLIGHT, 1, SkillPerm.Permanent);
					break;
				case JobId.Merchant:
					player.Skills.Add(SkillId.MC_CARTREVOLUTION, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.MC_CHANGECART, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.MC_LOUD, 1, SkillPerm.Permanent);
					// if(PACKETVER >= 20150826) skill "MC_CARTDECORATE",1,SKILL_PERM; // Packet version check logic needed
					break;
				case JobId.Thief:
					player.Skills.Add(SkillId.TF_SPRINKLESAND, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.TF_BACKSLIDING, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.TF_PICKSTONE, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.TF_THROWSTONE, 1, SkillPerm.Permanent);
					break;
			}

			switch (player.JobId) // BaseJob in rAthena is the current job
			{
				case JobId.Knight: player.Skills.Add(SkillId.KN_CHARGEATK, 1, SkillPerm.Permanent); break;
				case JobId.Priest: player.Skills.Add(SkillId.PR_REDEMPTIO, 1, SkillPerm.Permanent); break;
				case JobId.Wizard: player.Skills.Add(SkillId.WZ_SIGHTRASHER, 1, SkillPerm.Permanent); break;
				case JobId.Blacksmith:
					player.Skills.Add(SkillId.BS_UNFAIRLYTRICK, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.BS_GREED, 1, SkillPerm.Permanent);
					break;
				case JobId.Hunter: player.Skills.Add(SkillId.HT_PHANTASMIC, 1, SkillPerm.Permanent); break;
				case JobId.Assassin:
					player.Skills.Add(SkillId.AS_SONICACCEL, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.AS_VENOMKNIFE, 1, SkillPerm.Permanent);
					break;
				case JobId.Crusader: player.Skills.Add(SkillId.CR_SHRINK, 1, SkillPerm.Permanent); break;
				case JobId.Monk:
					player.Skills.Add(SkillId.MO_KITRANSLATION, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.MO_BALKYOUNG, 1, SkillPerm.Permanent);
					break;
				case JobId.Sage:
					player.Skills.Add(SkillId.SA_CREATECON, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.SA_ELEMENTWATER, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.SA_ELEMENTGROUND, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.SA_ELEMENTFIRE, 1, SkillPerm.Permanent);
					player.Skills.Add(SkillId.SA_ELEMENTWIND, 1, SkillPerm.Permanent);
					break;
				case JobId.Rogue: player.Skills.Add(SkillId.RG_CLOSECONFINE, 1, SkillPerm.Permanent); break;
				case JobId.Alchemist: player.Skills.Add(SkillId.AM_BIOETHICS, 1, SkillPerm.Permanent); break;
				case JobId.Bard: player.Skills.Add(SkillId.BA_PANGVOICE, 1, SkillPerm.Permanent); break;
				case JobId.Dancer: player.Skills.Add(SkillId.DC_WINKCHARM, 1, SkillPerm.Permanent); break;
			}
		}

		#endregion

		#region Item and Equipment

		/// <summary>Returns the string name of a weapon type based on its view ID.</summary>
		public static string GetWeaponType(int weaponId)
		{
			if (!ZoneServer.Instance.Data.Items.TryFind(weaponId, out var itemData)) return "Unknown Weapon";

			return itemData.LookId switch
			{
				1 => "Dagger",
				2 => "One-handed Sword",
				3 => "Two-handed Sword",
				4 => "One-handed Spear",
				5 => "Two-handed Spear",
				6 => "One-handed Axe",
				7 => "Two-handed Axe",
				8 => "Mace",
				10 => "Staff",
				11 => "Bow",
				12 => "Knuckle",
				13 => "Instrument",
				14 => "Whip",
				15 => "Book",
				16 => "Katar",
				17 => "Revolver",
				18 => "Rifle",
				19 => "Gatling gun",
				20 => "Shotgun",
				21 => "Grenade Launcher",
				22 => "Shuriken",
				_ => "Unknown Weapon"
			};
		}

		/// <summary>Returns the string name of an armor type based on its equip location.</summary>
		public static string GetArmorType(int itemId)
		{
			if (!ZoneServer.Instance.Data.Items.TryFind(itemId, out var itemData)) return "Unknown Equip";

			return itemData.WearSlots switch
			{
				EquipSlots.HeadLow => "Lower Headgear",
				EquipSlots.RightHand => GetWeaponType(itemId),
				EquipSlots.Garment => "Garment",
				EquipSlots.AccessoryLeft or EquipSlots.AccessoryRight or EquipSlots.Accessories => "Accessory",
				EquipSlots.Body => "Armor",
				EquipSlots.LeftHand => "Shield",
				EquipSlots.Shoes => "Shoes",
				EquipSlots.HeadTop => "Upper Headgear",
				EquipSlots.HeadMid => "Middle Headgear",
				EquipSlots.CostumeHeadTop => "Costume Upper Headgear",
				EquipSlots.CostumeHeadMid => "Costume Middle Headgear",
				EquipSlots.CostumeHeadLow => "Costume Lower Headgear",
				EquipSlots.CostumeGarment => "Costume Garment",
				EquipSlots.Ammo => "Ammo",
				EquipSlots.ShadowArmor => "Shadow Armor",
				EquipSlots.ShadowWeapon => "Shadow Weapon",
				EquipSlots.ShadowShield => "Shadow Shield",
				EquipSlots.ShadowShoes => "Shadow Shoes",
				EquipSlots.ShadowAccRight or EquipSlots.ShadowAccLeft or EquipSlots.ShadowAccessories => "Shadow Accessory",
				_ => "Unknown Equip"
			};
		}

		/// <summary>Checks if an equipment swap hack occurred by comparing expected and actual item IDs.</summary>
		public static bool IsEquipIdHack(PlayerCharacter player, EquipSlots equipSlot, int expectedId)
		{
			var equippedItem = player.Inventory.GetEquip(equipSlot);
			var actualId = equippedItem?.Data.ClassId ?? 0;
			if (expectedId != actualId)
			{
				ZoneServer.Instance.Data.Items.TryFind(expectedId, out var expectedItem);
				Log.Warning($"Hack: Player {player.Name} tried to swap equip {expectedItem?.Name} for {equippedItem?.Data.Name}.");
				return true;
			}
			return false;
		}

		// F_IsEquipRefineHack and F_IsEquipCardHack would follow a similar pattern, accessing equippedItem.Refine, etc.

		/// <summary>Returns true if the card ID corresponds to an enchant/charm.</summary>
		public static bool IsCharm(int cardId)
		{
			return (cardId >= 4700 && cardId <= 4999) ||
				   (cardId >= 29000 && cardId <= 29689) ||
				   (cardId >= 310000 && cardId <= 311091);
		}

		#endregion

		#region Time and Formatting

		/// <summary>Formats a duration into a human-readable string (days, hours, minutes, seconds).</summary>
		public static string Time2Str(long futureUnixTimestamp)
		{
			long diff = futureUnixTimestamp - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			if (diff <= 0) return "0 seconds";

			var ts = TimeSpan.FromSeconds(diff);
			var parts = new List<string>();
			if (ts.Days > 0) parts.Add($"{ts.Days} day{(ts.Days > 1 ? "s" : "")}");
			if (ts.Hours > 0) parts.Add($"{ts.Hours} hour{(ts.Hours > 1 ? "s" : "")}");
			if (ts.Minutes > 0) parts.Add($"{ts.Minutes} minute{(ts.Minutes > 1 ? "s" : "")}");
			if (ts.Seconds > 0) parts.Add($"{ts.Seconds} second{(ts.Seconds > 1 ? "s" : "")}");

			return string.Join(", ", parts);
		}

		/// <summary>Returns a number with commas.</summary>
		public static string InsertComma(int number) => number.ToString("N0");

		/// <summary>Returns a number with its ordinal suffix (e.g., 1st, 2nd, 23rd).</summary>
		public static string GetNumSuffix(int num)
		{
			if (num <= 0) return num.ToString();
			switch (num % 100)
			{
				case 11: case 12: case 13: return $"{num}th";
			}
			return (num % 10) switch
			{
				1 => $"{num}st",
				2 => $"{num}nd",
				3 => $"{num}rd",
				_ => $"{num}th",
			};
		}

		#endregion

		#region Advanced Text Manipulation

		/// <summary>Returns 'a' or 'an' based on the following word.</summary>
		public static string GetArticle(string word)
		{
			string lword = word.ToLower();
			if (string.IsNullOrEmpty(lword)) return "a";

			if (!char.IsLetter(lword[0])) return "a";

			if (lword.Length == 1) return "aefhilmnorsx".Contains(lword[0]) ? "an" : "a";

			if (Regex.IsMatch(lword, "(euler|hour(?!i)|heir|honest|hono)")) return "an";
			if (Regex.IsMatch(lword, "^[^aeiouy]")) return "a";
			if (Regex.IsMatch(lword, "^e[uw]") || Regex.IsMatch(lword, "^onc?e\\b") || Regex.IsMatch(lword, "^uni([^nmd]|mo)") || Regex.IsMatch(lword, "^u[bcfhjkqrst][aeiou]")) return "a";
			if (Regex.IsMatch(lword, "^ut[th]")) return "an";
			if (Regex.IsMatch(lword, "^[aeiou]")) return "an";

			return "a";
		}

		/// <summary>Returns an article ('a' or 'an') followed by the word.</summary>
		public static string InsertArticle(string word, bool capitalize = false)
		{
			string article = GetArticle(word);
			if (capitalize) article = char.ToUpper(article[0]) + article.Substring(1);
			return $"{article} {word}";
		}

		/// <summary>Returns the plural form of a noun.</summary>
		public static string GetPlural(string noun, bool uppercase = false)
		{
			// This is a complex function. A full, perfect implementation would require a dedicated library.
			// This is a direct translation of the rAthena script logic.
			// Known exceptions
			var exceptions = new Dictionary<string, string>
		{
			{"fish", "fish"}, {"glasses", "glasses"}, {"sunglasses", "sunglasses"}, {"clothes", "clothes"},
			{"boots", "boots"}, {"shoes", "shoes"}, {"greaves", "greaves"}, {"sandals", "sandals"},
			{"wings", "wings"}, {"ears", "ears"},
			{"belief", "beliefs"}, {"cliff", "cliffs"}, {"chief", "chiefs"}, {"dwarf", "dwarfs"},
			{"grief", "griefs"}, {"gulf", "gulfs"}, {"proof", "proofs"}, {"roof", "roofs"}
		};

			string lowerNoun = noun.ToLower();
			if (exceptions.ContainsKey(lowerNoun)) return uppercase ? exceptions[lowerNoun].ToUpper() : exceptions[lowerNoun];

			string plural;
			if (Regex.IsMatch(lowerNoun, "(s|x|z|ch|sh)$"))
				plural = noun + "es";
			else if (Regex.IsMatch(lowerNoun, "[^aeiou]y$"))
				plural = noun.Substring(0, noun.Length - 1) + "ies";
			else if (lowerNoun.EndsWith("f"))
				plural = noun.Substring(0, noun.Length - 1) + "ves";
			else if (lowerNoun.EndsWith("fe"))
				plural = noun.Substring(0, noun.Length - 2) + "ves";
			else
				plural = noun + "s";

			return uppercase ? plural.ToUpper() : plural;
		}

		/// <summary>Returns a formatted string with the number and the correct plural form of the noun.</summary>
		public static string InsertPlural(int number, string noun, bool uppercase = false, string format = "{0} {1}")
		{
			string finalNoun = (number == 1) ? noun : GetPlural(noun, uppercase);
			if (uppercase) finalNoun = finalNoun.ToUpper();

			return string.Format(format, number, finalNoun);
		}

		#endregion
	}
}

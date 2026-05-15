using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Zone.Network;
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
			var basicSkillCheck = ZoneServer.Instance.Conf.World.CheckBasicSkills;
			if (!basicSkillCheck) return true;

			return !(player.Skills.GetLevel(SkillId.NV_BASIC) < 6 && player.Skills.GetLevel(SkillId.SU_BASIC_SKILL) < 1);
		}

		/// <summary>
		/// Opens the player's account storage window, sending the current
		/// item list. Equivalent to rAthena's <c>openstorage</c>.
		/// </summary>
		public static void OpenStorage(PlayerCharacter player)
		{
			player.OpenStorage();
		}

		/// <summary>
		/// Opens the player's guild storage window. The caller is
		/// responsible for verifying guild membership and the
		/// permission gate before invoking this -- the v1 gate is
		/// "guild master only". Equivalent to rAthena's
		/// <c>guildopenstorage</c>.
		/// </summary>
		public static void OpenGuildStorage(PlayerCharacter player)
		{
			player.OpenGuildStorage();
		}

		/// <summary>
		/// Opens the EP12 cash-shop window populated with the full
		/// <c>cash_shop.txt</c> catalog. Equivalent to eAthena's
		/// <c>cashshop</c> script directive.
		/// </summary>
		/// <remarks>
		/// Requires <c>Game.Version &gt;= S2500</c> — older clients
		/// have no cash-shop UI. The handler silently no-ops on
		/// earlier versions instead of throwing, so scripts can be
		/// shared across eras.
		/// </remarks>
		public static void OpenCashShop(PlayerCharacter player)
		{
			if (Sabine.Shared.Game.Version < Sabine.Shared.Versions.S2500)
				return;

			Send.ZC_PC_CASH_POINT_ITEMLIST(player, ZoneServer.Instance.Data.CashShop.Entries.Values);
		}

		/// <summary>
		/// Checks if the player has learned all basic skills required for a job change.
		/// Assumes 'basicskillcheck()' corresponds to a server configuration.
		/// </summary>
		public static bool CanChangeJob(this PlayerCharacter player)
		{
			var basicSkillCheck = ZoneServer.Instance.Conf.World.CheckBasicSkills;
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

		// instance_npcname / instance_id stubs were superseded by the
		// Phase-2 `Instance*` helpers in the
		// `eAthena script-API helpers` region at the bottom of this
		// file.

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

		#region eAthena script-API helpers

		// Plain helpers the converter (`SabineTools/convert_rathena.py`)
		// emits in place of the eAthena builtins it can't translate
		// inline. Each one is a single-line wrap over an existing
		// Sabine API; the goal is to take the converter out of the
		// "emit a typed `""`/`0` placeholder" business for these
		// builtins so generated scripts compile against real data
		// instead of comment-stub TODOs. Order in this region mirrors
		// the eAthena script API for easy cross-reference.

		/// <summary>
		/// eAthena `getitemname(<id>)`. Returns the bare item name as
		/// stored in the item DB, or empty string when the id isn't
		/// known. (For the styled, refine/element-prefixed name used
		/// in dialogs, see <see cref="ItemName(int, int, int, int)"/>.)
		/// </summary>
		public static string GetItemName(int itemId)
		{
			return ZoneServer.Instance.Data.Items.TryFind(itemId, out var data)
				? (data.Name ?? string.Empty)
				: string.Empty;
		}

		/// <summary>
		/// eAthena `getequipname(<slot>)`. Returns the name of the
		/// item currently equipped in the given slot, or empty when
		/// the slot is empty. The argument is the eAthena slot index
		/// cast through Sabine's <see cref="EquipSlots"/> bitmask —
		/// callers built by the converter already use that cast.
		/// </summary>
		public static string GetEquipName(PlayerCharacter player, EquipSlots slot)
		{
			var equipped = player?.Inventory.GetEquip(slot);
			return equipped?.Data?.Name ?? string.Empty;
		}

		/// <summary>
		/// eAthena `getequipweaponlv(<slot>)`. Returns the weapon
		/// level of the item in the given slot, or 0 when nothing is
		/// equipped or the equipped item has no weapon level.
		/// </summary>
		public static int GetEquipWeaponLevel(PlayerCharacter player, EquipSlots slot)
		{
			var equipped = player?.Inventory.GetEquip(slot);
			return equipped?.Data?.WeaponLevel ?? 0;
		}

		/// <summary>
		/// eAthena `getsavepoint(0)`. Returns the player's savepoint
		/// map's string id, or empty string if the player has no
		/// savepoint set.
		/// </summary>
		public static string GetSavepointMap(PlayerCharacter player)
		{
			if (player == null || player.SaveLocation.IsZero)
				return string.Empty;
			return ZoneServer.Instance.World.Maps.TryGet(player.SaveLocation.MapId, out var map)
				? map.StringId
				: string.Empty;
		}

		/// <summary>eAthena `getsavepoint(1)`. Savepoint x-coordinate.</summary>
		public static int GetSavepointX(PlayerCharacter player)
			=> player?.SaveLocation.X ?? 0;

		/// <summary>eAthena `getsavepoint(2)`. Savepoint y-coordinate.</summary>
		public static int GetSavepointY(PlayerCharacter player)
			=> player?.SaveLocation.Y ?? 0;

		/// <summary>
		/// eAthena `gettimetick(<mode>)`. Mode 0 returns server uptime
		/// in milliseconds (the "tick" since boot); mode 1 returns
		/// seconds since midnight; mode 2 returns Unix time. Other
		/// modes return 0 with a log warning so the surprise is
		/// visible during script porting.
		/// </summary>
		public static int GetTimeTick(int mode)
		{
			switch (mode)
			{
				case 0:
					return Environment.TickCount;
				case 1:
				{
					var now = DateTime.Now;
					return now.Hour * 3600 + now.Minute * 60 + now.Second;
				}
				case 2:
					return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				default:
					Log.Warning("GetTimeTick: unknown mode {0}, returning 0.", mode);
					return 0;
			}
		}

		/// <summary>
		/// eAthena `gettimestr(<format>, <maxlen>)`. The format string
		/// is a strftime-style template; we map the most common
		/// directives (%Y/%m/%d/%H/%M/%S/%a/%A/%b/%B) to their .NET
		/// equivalents. Anything we don't recognise is passed through
		/// as a literal so the human reviewer can spot it. Result is
		/// truncated to <paramref name="maxLen"/> characters.
		/// </summary>
		public static string GetTimeStr(string format, int maxLen)
		{
			if (string.IsNullOrEmpty(format) || maxLen <= 0)
				return string.Empty;

			// Map strftime → .NET DateTime format. Doubles each
			// .NET token through a sentinel so the percent-prefix
			// scheme of strftime doesn't collide mid-replace.
			var net = format
				.Replace("%Y", "yyyy")
				.Replace("%m", "MM")
				.Replace("%d", "dd")
				.Replace("%H", "HH")
				.Replace("%M", "mm")
				.Replace("%S", "ss")
				.Replace("%a", "ddd")
				.Replace("%A", "dddd")
				.Replace("%b", "MMM")
				.Replace("%B", "MMMM");

			string formatted;
			try
			{
				formatted = DateTime.Now.ToString(net);
			}
			catch (FormatException)
			{
				Log.Warning("GetTimeStr: bad format string '{0}', returning empty.", format);
				return string.Empty;
			}

			return formatted.Length <= maxLen ? formatted : formatted.Substring(0, maxLen);
		}

		/// <summary>
		/// eAthena `basicskillcheck()`. Returns 1 if the player has
		/// the basic-skill prerequisite for trade/party/storage, 0
		/// otherwise. Sabine treats Basic Skill level 9 as the
		/// "fully learned" mark, matching kRO's `bs_lv` ceiling.
		/// </summary>
		public static bool BasicSkillCheck(PlayerCharacter player)
		{
			if (player == null) return false;
			var basicSkillCheck = ZoneServer.Instance.Conf.World.CheckBasicSkills;
			if (!basicSkillCheck)
				return true;
			return player.Skills.GetLevel(SkillId.NV_BASIC) >= 9;
		}

		#endregion

		#region Instance system

		// Thin script-facing wrappers over `World.Instances`. The
		// engine model is in `World/Instances/Instance.cs` +
		// `InstanceManager.cs`. eAthena returns 0/"" sentinels on
		// failure, but Sabine prefers proper types: `Has*` returns
		// `bool`, `*Name` returns `string`, ids return `int`.

		/// <summary>
		/// eAthena `instance_create("name")`. Allocates a new instance
		/// scoped to the player's party (when one exists), otherwise
		/// to the player. When <paramref name="name"/> matches an
		/// entry in <c>system/data/instances.txt</c>, every source
		/// map listed in the template is auto-attached and the
		/// template's TTL replaces the default 1h. Returns the new
		/// instance's id, or 0 on failure (no player).
		/// </summary>
		public static int InstanceCreate(PlayerCharacter player, string name = null)
		{
			if (player == null)
				return 0;
			var partyId = player.Party?.Id ?? 0;
			var manager = ZoneServer.Instance.World.Instances;
			var inst = manager.Create(
				ownerCharId: player.Id,
				ownerPartyId: partyId);

			if (!string.IsNullOrEmpty(name)
				&& ZoneServer.Instance.Data.Instances.Entries.TryGetValue(name, out var tmpl))
			{
				inst.Ttl = TimeSpan.FromMinutes(tmpl.TtlMinutes);
				foreach (var sourceMap in tmpl.SourceMaps)
				{
					if (manager.AttachMap(inst, sourceMap) == null)
						Log.Warning("InstanceCreate: '{0}' template lists '{1}' but the map isn't loaded.",
							name, sourceMap);
				}
				Log.Info("InstanceCreate: '{0}' (id={1}, ttl={2}m) for char {3}.",
					name, inst.Id, tmpl.TtlMinutes, player.Id);
			}
			else if (!string.IsNullOrEmpty(name))
			{
				Log.Info("InstanceCreate: '{0}' (id={1}, no template) for char {2}.",
					name, inst.Id, player.Id);
			}

			return inst.Id;
		}

		/// <summary>
		/// eAthena `instance_attachmap("base_map", instanceId)`. Clones
		/// the source map into the instance and returns the cloned
		/// map's string id (e.g. `"orcs_lab@5"`), or empty on failure
		/// (instance not found, source map not loaded).
		/// </summary>
		public static string InstanceAttachMap(string sourceMapStringId, int instanceId)
		{
			var inst = ZoneServer.Instance.World.Instances.Get(instanceId);
			if (inst == null)
				return string.Empty;
			return ZoneServer.Instance.World.Instances.AttachMap(inst, sourceMapStringId)
				?? string.Empty;
		}

		/// <summary>
		/// eAthena `instance_id()`. Returns the active instance's id
		/// for the given player (resolved guild → party → char), or 0
		/// when the player isn't in any instance.
		/// </summary>
		public static int GetInstanceId(PlayerCharacter player)
		{
			return ZoneServer.Instance.World.Instances.TryFindByOwner(player, out var inst)
				? inst.Id
				: 0;
		}

		/// <summary>
		/// eAthena `instance_npcname("npcName" [, instanceId])`. Returns
		/// the per-instance NPC name (`"npcName#5"`) so events scoped
		/// to a single run don't bleed across instances. Falls back to
		/// the bare name when no instance is given and none can be
		/// resolved from context.
		/// </summary>
		public static string GetInstanceNpcName(string baseName, int instanceId = 0)
		{
			if (instanceId <= 0 || string.IsNullOrEmpty(baseName))
				return baseName ?? string.Empty;
			return $"{baseName}#{instanceId}";
		}

		/// <summary>
		/// eAthena `instance_mapname("base_map" [, instanceId])`.
		/// Returns the per-instance map name (`"base_map@5"`). When
		/// no explicit instance is given, the player's resolved
		/// instance is used; falls back to empty when nothing matches.
		/// </summary>
		public static string GetInstanceMapName(string baseMapStringId, int instanceId = 0, PlayerCharacter player = null)
		{
			if (string.IsNullOrEmpty(baseMapStringId))
				return string.Empty;
			if (instanceId <= 0 && player != null)
			{
				if (ZoneServer.Instance.World.Instances.TryFindByOwner(player, out var inst))
					instanceId = inst.Id;
			}
			if (instanceId <= 0)
				return baseMapStringId;
			return $"{baseMapStringId}@{instanceId}";
		}

		/// <summary>
		/// eAthena `has_instance("base_map")`. Returns `true` (and the
		/// resolved instance map name via <paramref name="resolved"/>)
		/// when the player has an active instance with the given
		/// source map attached. The bool form is the modern shape;
		/// callers that need rAthena's string-or-empty contract can
		/// use <see cref="HasInstanceMapName"/>.
		/// </summary>
		public static bool HasInstance(PlayerCharacter player, string baseMapStringId, out string resolved)
		{
			resolved = string.Empty;
			if (!ZoneServer.Instance.World.Instances.TryFindByOwner(player, out var inst))
				return false;
			var map = inst.TryGetMap(baseMapStringId);
			if (map == null)
				return false;
			resolved = map.StringId;
			return true;
		}

		/// <summary>
		/// String form of <see cref="HasInstance"/> mirroring eAthena's
		/// return contract: the resolved instance map name on success,
		/// or empty string when no match. Useful when the surrounding
		/// expression compares against `""` directly.
		/// </summary>
		public static string HasInstanceMapName(PlayerCharacter player, string baseMapStringId)
			=> HasInstance(player, baseMapStringId, out var resolved) ? resolved : string.Empty;

		/// <summary>
		/// eAthena `instance_destroy(instanceId)`. Tears down every
		/// cloned map attached to the instance and removes the
		/// instance from the registry. Returns true on success,
		/// false when the id is unknown — the bool form lets callers
		/// chain `if (InstanceDestroy(...))` cleanly.
		/// </summary>
		public static bool InstanceDestroy(int instanceId)
		{
			if (ZoneServer.Instance.World.Instances.Get(instanceId) == null)
				return false;
			ZoneServer.Instance.World.Instances.Destroy(instanceId);
			return true;
		}

		#endregion

		#region Marriage system

		// Script-facing helpers for the marriage subsystem (Phase 3).
		// State lives on `PlayerCharacter.MarriedToCharId` /
		// `MarriedAt`; persistence is in `ZoneDb.SaveCharacter` /
		// `GetCharacter` (migration `2026-05-03_001.sql`). The chat
		// commands `/marry` and `/divorce` route through these
		// helpers too so the rules stay in one place.

		/// <summary>
		/// eAthena `getpartnerid()`. Returns the partner's char id, or
		/// 0 when the player is unmarried.
		/// </summary>
		public static int GetPartnerId(PlayerCharacter player)
			=> player?.MarriedToCharId ?? 0;

		/// <summary>
		/// eAthena `ismarried()` (renewal). True when the player has
		/// a partner. Cheap pass-through; provided for symmetry with
		/// the rest of the bool-typed helpers.
		/// </summary>
		public static bool IsMarried(PlayerCharacter player)
			=> player?.IsMarried ?? false;

		/// <summary>
		/// Returns the partner's name. Tries the online roster first
		/// (cheap), falls back to a single-column DB lookup when the
		/// partner isn't connected. Returns empty string when the
		/// player is unmarried or the partner record has been
		/// deleted.
		/// </summary>
		public static string GetPartnerName(PlayerCharacter player)
		{
			if (player?.MarriedToCharId is not int partnerId)
				return string.Empty;
			if (ZoneServer.Instance.World.Maps.TryGetPlayerById(partnerId, out var partner))
				return partner.Name;
			return ZoneServer.Instance.Database.GetCharacterName(partnerId);
		}

		/// <summary>
		/// Marries the two characters. Sets both sides of
		/// <see cref="PlayerCharacter.MarriedToCharId"/> and stamps
		/// <see cref="PlayerCharacter.MarriedAt"/>. Returns false if
		/// either side is null, the two are the same character, or
		/// either is already married — bool result lets callers
		/// surface a refusal without inspecting state.
		/// </summary>
		public static bool Marry(PlayerCharacter a, PlayerCharacter b)
		{
			if (a == null || b == null || a.Id == b.Id)
				return false;
			if (a.IsMarried || b.IsMarried)
				return false;

			var now = DateTime.UtcNow;
			a.MarriedToCharId = b.Id;
			a.MarriedAt = now;
			b.MarriedToCharId = a.Id;
			b.MarriedAt = now;

			Log.Info("Marry: {0} (#{1}) and {2} (#{3}) at {4:u}.",
				a.Name, a.Id, b.Name, b.Id, now);
			return true;
		}

		/// <summary>
		/// Divorces the player from their partner. Clears both sides
		/// of <see cref="PlayerCharacter.MarriedToCharId"/>: the
		/// online partner is updated in-memory and will save on their
		/// next checkpoint; the offline partner is updated directly
		/// in the database. Returns true when the player was married.
		/// </summary>
		public static bool Divorce(PlayerCharacter player)
		{
			if (player?.MarriedToCharId is not int partnerId)
				return false;

			player.MarriedToCharId = null;
			player.MarriedAt = null;

			if (ZoneServer.Instance.World.Maps.TryGetPlayerById(partnerId, out var partner))
			{
				partner.MarriedToCharId = null;
				partner.MarriedAt = null;
			}
			else
			{
				// Offline partner: write the column directly so we
				// don't rely on the lazy reconcile-on-load path
				// catching it. Reconcile-on-load remains as a
				// belt-and-braces safety net for any state that
				// pre-dates this change.
				ZoneServer.Instance.Database.ClearMarriage(partnerId);
			}

			Log.Info("Divorce: {0} (#{1}) from char #{2}.",
				player.Name, player.Id, partnerId);
			return true;
		}

		#endregion

		#region Misc script-API helpers

		/// <summary>
		/// eAthena `isloggedin(charId)`. True when the given character
		/// is currently online, false otherwise (including for
		/// non-existent ids). Cheap pass-through over the world's
		/// player roster.
		/// </summary>
		public static bool IsLoggedIn(int characterId)
			=> ZoneServer.Instance.World.Maps.TryGetPlayerById(characterId, out _);

		/// <summary>
		/// eAthena `getnameditem(itemId, recipientName$)`. Adds the
		/// item to the player's inventory and stamps its
		/// <see cref="World.Actors.Item.EngravedName"/> with
		/// <paramref name="recipientName"/> so display logic can
		/// render `"<recipient>'s <itemName>"`. Returns true on
		/// success, false when the item id isn't known.
		/// </summary>
		public static bool GetNamedItem(PlayerCharacter player, int itemId, string recipientName)
		{
			if (player == null)
				return false;
			if (!ZoneServer.Instance.Data.Items.TryFind(itemId, out _))
				return false;

			var item = new World.Actors.Item(itemId);
			item.EngravedName = recipientName ?? string.Empty;
			player.Inventory.AddItem(item);
			return true;
		}

		/// <summary>
		/// eAthena `getguildname(guildId)`. Returns the guild's
		/// display name, or empty string when the id isn't known.
		/// </summary>
		public static string GetGuildName(int guildId)
		{
			var guild = ZoneServer.Instance.World.Guilds.Get(guildId);
			return guild?.Name ?? string.Empty;
		}

		/// <summary>
		/// eAthena `getpartyname(partyId)`. Returns the party's
		/// display name, or empty when the id isn't an active
		/// party. Parties are session-scoped today (no DB
		/// persistence) so an offline party returns empty.
		/// </summary>
		public static string GetPartyName(int partyId)
		{
			var party = World.Groups.PartyManager.Instance.GetParty(partyId);
			return party?.Name ?? string.Empty;
		}

		/// <summary>
		/// eAthena `getpartyleader(partyId)`. Default (no <c>type</c>
		/// arg) returns the leader's char id, matching rAthena's
		/// documented default. Returns 0 when the id is unknown or
		/// the party has no resolvable leader. The string-name
		/// variant (eAthena's <c>type=1</c>) goes through
		/// <see cref="GetPartyLeaderName"/>.
		/// </summary>
		public static int GetPartyLeader(int partyId)
		{
			var party = World.Groups.PartyManager.Instance.GetParty(partyId);
			var leader = party?.GetLeader();
			return leader?.CharacterId ?? 0;
		}

		/// <summary>
		/// eAthena `getpartyleader(partyId, 1)`. Returns the leader's
		/// character name; empty when the party id is unknown.
		/// </summary>
		public static string GetPartyLeaderName(int partyId)
		{
			var party = World.Groups.PartyManager.Instance.GetParty(partyId);
			var leader = party?.GetLeader();
			return leader?.Name ?? string.Empty;
		}

		/// <summary>
		/// eAthena `getguildmaster(guildId)`. Returns the master's
		/// character name. Falls back to a DB lookup for the offline
		/// case when the in-memory cache doesn't have it.
		/// </summary>
		public static string GetGuildMaster(int guildId)
		{
			var guild = ZoneServer.Instance.World.Guilds.Get(guildId);
			if (guild == null)
				return string.Empty;
			if (!string.IsNullOrEmpty(guild.MasterName))
				return guild.MasterName;
			// Master is offline and we never cached the name (or the
			// guild was loaded from disk before the master logged in).
			// One-shot DB lookup; result is intentionally not cached
			// here to keep the helper stateless.
			return ZoneServer.Instance.Database.GetCharacterName(guild.MasterCharId);
		}

		/// <summary>
		/// eAthena `getcastlename(<map>)`. Returns the castle's
		/// display name (e.g. <c>"Neuschwanstein"</c>) for the
		/// given map id. Falls back to the raw map id when the
		/// map isn't a known castle so dialogs still display
		/// something sensible. Phase-8 castle DB lookup.
		/// </summary>
		public static string GetCastleName(string mapStringId)
		{
			if (string.IsNullOrEmpty(mapStringId))
				return string.Empty;
			return ZoneServer.Instance.Data.Castles.Entries.TryGetValue(
					mapStringId, out var template)
				? template.Name
				: mapStringId;
		}

		/// <summary>
		/// eAthena `GetCastleData(<map>, <type>)`. Reads one of the
		/// numeric per-castle slots: 0 castle id, 1 owner guild,
		/// 2 economy, 3 defense, 4 triggerE, 5 triggerD, 10..17
		/// treasure-day flags. Returns 0 for unknown maps or types.
		/// </summary>
		public static int GetCastleData(string mapStringId, int dataType)
			=> ZoneServer.Instance.World.Castles.GetData(mapStringId, dataType);

		/// <summary>
		/// eAthena `SetCastleData(<map>, <type>, <value>)`. Persists
		/// the change to the <c>castle_state</c> table on success.
		/// Returns true when the type is recognised.
		/// </summary>
		public static bool SetCastleData(string mapStringId, int dataType, int value)
			=> ZoneServer.Instance.World.Castles.SetData(mapStringId, dataType, value);

		/// <summary>
		/// eAthena `GetCastleOwnerGuildId(<map>)` shorthand —
		/// <c>GetCastleData(<map>, 1)</c>. Provided for readability
		/// at the script-API layer.
		/// </summary>
		public static int GetCastleOwnerGuildId(string mapStringId)
			=> GetCastleData(mapStringId, 1);

		/// <summary>
		/// Atomic economy-investment helper. Mirrors the eAthena
		/// <c>agit_main</c> investment flow: validates ownership / daily
		/// trigger / cap / zeny, then deducts the cost and bumps the
		/// economy level by 1 in a single transaction. Returns the
		/// failure reason for the script's dialog branch (Ok = success).
		/// </summary>
		public static Sabine.Zone.World.Woe.CastleManager.InvestResult InvestCastleEconomy(string mapStringId, Sabine.Zone.World.Actors.PlayerCharacter character, int zenyCost)
			=> ZoneServer.Instance.World.Castles.TryInvestEconomy(mapStringId, character, zenyCost);

		/// <summary>
		/// Atomic defense-investment helper. See
		/// <see cref="InvestCastleEconomy"/>.
		/// </summary>
		public static Sabine.Zone.World.Woe.CastleManager.InvestResult InvestCastleDefense(string mapStringId, Sabine.Zone.World.Actors.PlayerCharacter character, int zenyCost)
			=> ZoneServer.Instance.World.Castles.TryInvestDefense(mapStringId, character, zenyCost);

		/// <summary>
		/// eAthena `agitstart`. Turns WoE on and fans out
		/// <c>Agit::OnAgitStart</c> to every subscriber.
		/// </summary>
		public static void Agitstart()
			=> ZoneServer.Instance.World.Woe.Start();

		/// <summary>
		/// eAthena `agitend`. Turns WoE off and fans out
		/// <c>Agit::OnAgitEnd</c>.
		/// </summary>
		public static void Agitend()
			=> ZoneServer.Instance.World.Woe.End();

		/// <summary>
		/// eAthena `agitcheck()`. True while WoE is currently active.
		/// </summary>
		public static bool IsAgitActive()
			=> ZoneServer.Instance.World.Woe.IsActive;

		/// <summary>
		/// eAthena `MapRespawnGuildID "<map>", <gid>, <flag>`. Kicks
		/// every player on the given map whose guild id doesn't
		/// match <paramref name="ownerGuildId"/> back to their
		/// savepoint. The <paramref name="flag"/> argument follows
		/// the eAthena bitfield: bit 0 = guild members of other
		/// guilds, bit 1 = unaffiliated players, bit 2 = monsters
		/// (we don't model the monster sweep yet).
		/// </summary>
		public static void MapRespawnGuildId(string mapStringId, int ownerGuildId, int flag)
		{
			if (string.IsNullOrEmpty(mapStringId))
				return;
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(mapStringId, out var map))
				return;

			var kickOtherGuilds = (flag & 1) != 0;
			var kickNonGuilded = (flag & 2) != 0;

			using var snapshot = Yggdrasil.Collections.PooledList<PlayerCharacter>.Rent();
			map.GetPlayers(snapshot, 0, static (_, _) => true);

			foreach (var player in snapshot)
			{
				var pgid = player.Guild?.Id ?? 0;
				if (pgid == ownerGuildId)
					continue; // Owner guild — keep them on the map.
				if (pgid == 0 && !kickNonGuilded)
					continue;
				if (pgid != 0 && !kickOtherGuilds)
					continue;
				if (player.SaveLocation.IsZero)
					continue;
				player.Warp(player.SaveLocation);
			}
		}

		/// <summary>
		/// eAthena `flagemblem <gid>`. Sets the calling NPC's
		/// guild-emblem to the given guild's emblem id (looked up
		/// against the in-memory guild registry). Pass 0 to clear.
		/// </summary>
		public static void FlagEmblem(World.Actors.Npc npc, int guildId)
		{
			if (npc == null)
				return;
			npc.SetGuildEmblem(guildId);
		}

		/// <summary>
		/// Registers a per-castle <c>OnRecvCastle</c> hook. eAthena
		/// scripts express this via the statement form
		/// <c>GetCastleData "&lt;map&gt;", 0, "&lt;NpcName::Event&gt;"</c> —
		/// at runtime, when the castle's owner guild changes,
		/// <see cref="World.Woe.CastleManager.SetData"/> raises the
		/// stored event name.
		/// </summary>
		public static void RegisterOnRecvCastle(string mapStringId, string eventName)
			=> ZoneServer.Instance.World.Castles.RegisterOnRecvCastle(mapStringId, eventName);

		/// <summary>
		/// Resolves a map by string id and applies <paramref name="mutator"/>.
		/// No-op when the map isn't loaded; the inner action sees
		/// the live <see cref="World.Maps.Map"/> instance. Helper
		/// for the per-map flag toggles below — not part of the
		/// eAthena script API itself.
		/// </summary>
		private static void WithMap(string mapStringId, System.Action<World.Maps.Map> mutator)
		{
			if (string.IsNullOrEmpty(mapStringId) || mutator == null)
				return;
			if (ZoneServer.Instance.World.Maps.TryGetByStringId(mapStringId, out var map))
				mutator(map);
		}

		/// <summary>
		/// eAthena `gvgon "<map>"`. Marks the map as a GvG zone —
		/// guild-vs-guild attack rules apply, used for WoE castle
		/// maps to enable PvP only for guild members during
		/// <c>agitstart</c>.
		/// </summary>
		public static void GvgOn(string mapStringId)
			=> WithMap(mapStringId, m => m.SetFlag(World.Maps.MapFlags.Gvg));

		/// <summary>eAthena `gvgoff "<map>"`. Clears the GvG flag.</summary>
		public static void GvgOff(string mapStringId)
			=> WithMap(mapStringId, m => m.ClearFlag(World.Maps.MapFlags.Gvg));

		/// <summary>eAthena `pvpon "<map>"`. Enables PvP on the map.</summary>
		public static void PvpOn(string mapStringId)
			=> WithMap(mapStringId, m => m.SetFlag(World.Maps.MapFlags.PvP));

		/// <summary>eAthena `pvpoff "<map>"`. Disables PvP on the map.</summary>
		public static void PvpOff(string mapStringId)
			=> WithMap(mapStringId, m => m.ClearFlag(World.Maps.MapFlags.PvP));

		// Map-flag name → MapFlags enum lookup. eAthena's constants
		// (`mf_pvp`, `mf_gvg`, `mf_nosave`, …) are case-insensitive
		// and may or may not include the `mf_` prefix depending on
		// the script. We accept either form and any casing.
		private static readonly System.Collections.Generic.Dictionary<string, World.Maps.MapFlags>
			_mapFlagAliases = new(System.StringComparer.OrdinalIgnoreCase)
		{
			["pvp"] = World.Maps.MapFlags.PvP,
			["gvg"] = World.Maps.MapFlags.Gvg,
			["noteleport"] = World.Maps.MapFlags.NoTeleport,
			["nomemo"] = World.Maps.MapFlags.NoMemo,
			["noreturn"] = World.Maps.MapFlags.NoReturn,
			["nosave"] = World.Maps.MapFlags.NoSave,
			["noexp"] = World.Maps.MapFlags.NoExp,
			["nodrop"] = World.Maps.MapFlags.NoDrop,
			["novending"] = World.Maps.MapFlags.NoVending,
			["nochat"] = World.Maps.MapFlags.NoChat,
			["noskill"] = World.Maps.MapFlags.NoSkill,
			["noicewall"] = World.Maps.MapFlags.NoIcewall,
			["noknockback"] = World.Maps.MapFlags.NoKnockback,
			["nocommand"] = World.Maps.MapFlags.NoCommand,
		};

		private static bool TryResolveMapFlag(string name, out World.Maps.MapFlags flag)
		{
			flag = World.Maps.MapFlags.None;
			if (string.IsNullOrEmpty(name))
				return false;
			var key = name.StartsWith("mf_", System.StringComparison.OrdinalIgnoreCase)
				? name.Substring(3) : name;
			return _mapFlagAliases.TryGetValue(key, out flag);
		}

		/// <summary>
		/// eAthena `setmapflag "<map>", <flag>`. Generic flag-set
		/// over <see cref="World.Maps.MapFlags"/>. Unknown flag
		/// names are logged and ignored.
		/// </summary>
		public static void SetMapFlag(string mapStringId, string flagName)
		{
			if (!TryResolveMapFlag(flagName, out var flag))
			{
				Log.Warning("SetMapFlag: unknown flag '{0}'.", flagName);
				return;
			}
			WithMap(mapStringId, m => m.SetFlag(flag));
		}

		/// <summary>
		/// eAthena `removemapflag "<map>", <flag>`. Generic flag-clear.
		/// </summary>
		public static void RemoveMapFlag(string mapStringId, string flagName)
		{
			if (!TryResolveMapFlag(flagName, out var flag))
			{
				Log.Warning("RemoveMapFlag: unknown flag '{0}'.", flagName);
				return;
			}
			WithMap(mapStringId, m => m.ClearFlag(flag));
		}

		/// <summary>
		/// eAthena `areawarp "<src>", x1, y1, x2, y2, "<dst>", dx, dy`.
		/// Warps every player whose position falls within the
		/// inclusive source rectangle to the destination map+coords.
		/// </summary>
		public static void AreaWarp(string srcMap, int x1, int y1, int x2, int y2,
									string dstMap, int dstX, int dstY)
		{
			if (string.IsNullOrEmpty(srcMap) || string.IsNullOrEmpty(dstMap))
				return;
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(srcMap, out var src))
				return;
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(dstMap, out var dst))
				return;

			// Normalise the rectangle so callers can pass corners in
			// any order — eAthena scripts aren't strict about it.
			var minX = System.Math.Min(x1, x2);
			var maxX = System.Math.Max(x1, x2);
			var minY = System.Math.Min(y1, y2);
			var maxY = System.Math.Max(y1, y2);

			using var snapshot = Yggdrasil.Collections.PooledList<PlayerCharacter>.Rent();
			src.GetPlayers(snapshot, 0, static (_, _) => true);
			foreach (var player in snapshot)
			{
				var px = player.Position.X;
				var py = player.Position.Y;
				if (px < minX || px > maxX || py < minY || py > maxY)
					continue;
				player.Warp(new Sabine.Shared.World.Location(dst.Id, dstX, dstY));
			}
		}

		/// <summary>
		/// eAthena `RequestGuildInfo <gid>`. In a multi-server
		/// architecture this asks the inter-server bus for an
		/// up-to-date snapshot of a remote guild's data. Sabine is
		/// single-server so guild data is already authoritative —
		/// the call is a no-op kept for script-API parity.
		/// </summary>
		public static void RequestGuildInfo(int guildId)
		{
			_ = guildId;
		}

		/// <summary>
		/// eAthena `deletearray <var>[<idx>], <count>` — clears
		/// (zeros) <paramref name="count"/> consecutive elements in
		/// the named non-local array starting at
		/// <paramref name="startIndex"/>. The local-scope `.@` /
		/// `@` form is handled at the converter layer because the
		/// LHS is a real C# array there.
		/// </summary>
		public static void DeleteArrayInt(Sabine.Shared.Util.Variables vars, string varName,
										   int startIndex, int count)
		{
			if (vars == null || string.IsNullOrEmpty(varName) || count <= 0)
				return;
			var arr = vars.GetIntArray(varName);
			var end = System.Math.Min(arr.Length, startIndex + count);
			for (var i = System.Math.Max(0, startIndex); i < end; i++)
				arr[i] = 0;
			vars.SetIntArray(varName, arr);
		}

		/// <summary>String-array companion to <see cref="DeleteArrayInt"/>.</summary>
		public static void DeleteArrayString(Sabine.Shared.Util.Variables vars, string varName,
											  int startIndex, int count)
		{
			if (vars == null || string.IsNullOrEmpty(varName) || count <= 0)
				return;
			var arr = vars.GetStringArray(varName);
			var end = System.Math.Min(arr.Length, startIndex + count);
			for (var i = System.Math.Max(0, startIndex); i < end; i++)
				arr[i] = string.Empty;
			vars.SetStringArray(varName, arr);
		}

		/// <summary>
		/// eAthena `setbattleflag "<name>", <value>`. Stores a
		/// server-config knob (drop rate, exp rate, etc.) under the
		/// world-scope variable container so reads via
		/// <see cref="GetBattleFlag"/> see the same value across
		/// scripts. Names are case-insensitive — eAthena script
		/// authors mix casing freely.
		/// </summary>
		public static void SetBattleFlag(string name, int value)
		{
			if (string.IsNullOrEmpty(name))
				return;
			ZoneServer.Instance.World.Vars.Perm.SetInt("BattleFlag." + name.ToLowerInvariant(), value);
		}

		/// <summary>
		/// eAthena `getbattleflag("<name>")`. Reads back a value
		/// previously set with <see cref="SetBattleFlag"/>; defaults
		/// to 0 when the flag has never been written.
		/// </summary>
		public static int GetBattleFlag(string name)
		{
			if (string.IsNullOrEmpty(name))
				return 0;
			return ZoneServer.Instance.World.Vars.Perm.GetInt(
				"BattleFlag." + name.ToLowerInvariant(), 0);
		}

		/// <summary>
		/// Tuple form of eAthena `getmapxy <map$>, <x>, <y>, <type>{, "<name>"}`.
		/// Returns the player's current map / x / y. Type values
		/// 1 (NPC) / 2 (pet) / 3 (homunculus) / 4 (mercenary) /
		/// 5 (elemental) aren't modelled yet; they fall back to the
		/// player's coords with a log warning so script flow
		/// survives. Caller (the converter) destructures into the
		/// three out-vars.
		/// </summary>
		public static (string Map, int X, int Y) GetMapXy(PlayerCharacter player, int type, string lookupName = null)
		{
			_ = lookupName;
			if (player == null)
				return (string.Empty, 0, 0);
			if (type != 0)
				Log.Warning("GetMapXy: type {0} not modelled, using player coords.", type);
			return (player.Map?.StringId ?? string.Empty,
				player.Position.X, player.Position.Y);
		}

		#endregion

		#region Phase 12 quick wins

		/// <summary>
		/// eAthena `setlook <type>, <value>`. The <paramref name="type"/>
		/// argument matches eAthena's LOOK_* enum (1 = hair, 2 = weapon,
		/// 3/4/5 = headgear slots, 6 = hair color, 7 = clothes color,
		/// 8 = shield, 10 = body, 12 = robe). Sabine's
		/// <see cref="PlayerCharacter.ChangeLook"/> already persists +
		/// broadcasts the field-backed types; for color / shield / robe
		/// we have no persistent field, so we just broadcast the sprite
		/// change so the rest of the map sees it.
		/// </summary>
		public static void SetLook(PlayerCharacter player, int type, int value)
		{
			if (player == null)
				return;
			var sprite = (SpriteType)type;
			switch (sprite)
			{
				case SpriteType.Hair:
				case SpriteType.Weapon:
				case SpriteType.HeadTop:
				case SpriteType.HeadMiddle:
				case SpriteType.HeadBottom:
					player.ChangeLook(sprite, value);
					return;
			}

			if (Sabine.Shared.Game.Version < Sabine.Shared.Versions.S500)
				Sabine.Zone.Network.Send.ZC_SPRITE_CHANGE(player, sprite, value);
			else
				Sabine.Zone.Network.Send.ZC_SPRITE_CHANGE2(player, sprite, value, 0);
		}

		/// <summary>
		/// eAthena `setd "<name>", <value>` — write a runtime-named
		/// variable. The name carries the scope sigil
		/// (<c>$</c>, <c>$@</c>, <c>#</c>, <c>@</c>, <c>.</c>); we route
		/// to the matching Sabine variable container based on the
		/// prefix. <paramref name="player"/> is required for character /
		/// account / npc-temp scope; world scope ignores it.
		/// </summary>
		public static void SetD(PlayerCharacter player, string name, int value)
			=> SetDImpl(player, name, value, asString: false);

		/// <summary>String overload of <see cref="SetD"/> — invoked by
		/// the converter when the rAthena variable name ends in `$`.</summary>
		public static void SetD(PlayerCharacter player, string name, string value)
			=> SetDImpl(player, name, value, asString: true);

		private static void SetDImpl(PlayerCharacter player, string name, object value, bool asString)
		{
			if (string.IsNullOrEmpty(name))
				return;

			var key = name;
			Sabine.Shared.Util.Variables vars = null;

			if (key.StartsWith("$@"))
			{
				vars = ZoneServer.Instance.World.Vars.Temp;
				key = key.Substring(2);
			}
			else if (key.StartsWith("$"))
			{
				vars = ZoneServer.Instance.World.Vars.Perm;
				key = key.Substring(1);
			}
			else if (key.StartsWith("#"))
			{
				vars = player?.Vars.Perm;
				key = key.Substring(1);
			}
			else if (key.StartsWith("@"))
			{
				vars = player?.Vars.Temp;
				key = key.Substring(1);
			}
			else if (key.StartsWith("."))
			{
				// .npc / .@local — we don't have a non-runtime way to
				// reach the script's locals, so this is a best-effort
				// no-op with a log entry.
				Log.Warning("SetD: scope '.{0}' not addressable at runtime.", key);
				return;
			}

			if (vars == null)
				return;

			if (key.EndsWith("$"))
				key = key.Substring(0, key.Length - 1);

			if (asString)
				vars.SetString(key, (string)value);
			else
				vars.SetInt(key, System.Convert.ToInt32(value));
		}

		/// <summary>String overload of <c>getd</c> — sigil-prefixed read.</summary>
		public static int GetDInt(PlayerCharacter player, string name)
		{
			var (vars, key) = ResolveVarScope(player, name);
			return vars?.GetInt(key, 0) ?? 0;
		}

		/// <summary>String-typed companion to <see cref="GetDInt"/>.</summary>
		public static string GetDStr(PlayerCharacter player, string name)
		{
			var (vars, key) = ResolveVarScope(player, name);
			return vars?.GetString(key, string.Empty) ?? string.Empty;
		}

		private static (Sabine.Shared.Util.Variables vars, string key) ResolveVarScope(PlayerCharacter player, string name)
		{
			if (string.IsNullOrEmpty(name))
				return (null, string.Empty);

			var key = name;
			Sabine.Shared.Util.Variables vars = null;

			if (key.StartsWith("$@"))
			{
				vars = ZoneServer.Instance.World.Vars.Temp;
				key = key.Substring(2);
			}
			else if (key.StartsWith("$"))
			{
				vars = ZoneServer.Instance.World.Vars.Perm;
				key = key.Substring(1);
			}
			else if (key.StartsWith("#"))
			{
				vars = player?.Vars.Perm;
				key = key.Substring(1);
			}
			else if (key.StartsWith("@"))
			{
				vars = player?.Vars.Temp;
				key = key.Substring(1);
			}

			if (key.EndsWith("$"))
				key = key.Substring(0, key.Length - 1);

			return (vars, key);
		}

		/// <summary>
		/// eAthena `npcwalkto <x>, <y>` — drive the script-context
		/// NPC's movement controller toward the target tile. Falls
		/// silently when the NPC is null (event firing on a script
		/// without an NPC handle).
		/// </summary>
		public static void NpcWalkTo(World.Actors.Npc npc, int x, int y)
		{
			if (npc == null)
				return;
			npc.Controller.MoveTo(new Sabine.Shared.World.Position(x, y));
		}

		/// <summary>
		/// eAthena `npcskilleffect <skillId>, <level>, <x>, <y>` —
		/// purely-cosmetic ground-skill broadcast originating from the
		/// scripted NPC. We log+drop instead of synthesising a packet:
		/// every script that uses this is for ambient flavour (the
		/// ToysFestival-style sparkles), and the gameplay loop is fine
		/// without it. Wire up `ZC_NOTIFY_GROUNDSKILL` here when we
		/// add a packet builder for it.
		/// </summary>
		public static void NpcSkillEffect(World.Actors.Npc npc, int skillId, int level, int x, int y)
		{
			_ = npc; _ = skillId; _ = level; _ = x; _ = y;
		}

		/// <summary>
		/// eAthena `areapercentheal "<map>", x1, y1, x2, y2, hp%, sp%`.
		/// Heals every player whose position is inside the inclusive
		/// rect by the given percentage of their max HP / SP.
		/// </summary>
		public static void AreaPercentHeal(string mapStringId, int x1, int y1, int x2, int y2, int hpPct, int spPct)
		{
			if (string.IsNullOrEmpty(mapStringId))
				return;
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(mapStringId, out var map))
				return;

			var minX = System.Math.Min(x1, x2);
			var maxX = System.Math.Max(x1, x2);
			var minY = System.Math.Min(y1, y2);
			var maxY = System.Math.Max(y1, y2);

			var matched = new System.Collections.Generic.List<PlayerCharacter>();
			map.GetPlayers(matched, (minX, maxX, minY, maxY),
				(state, p) =>
				{
					var pos = p.Position;
					return pos.X >= state.minX && pos.X <= state.maxX
						&& pos.Y >= state.minY && pos.Y <= state.maxY;
				});

			foreach (var p in matched)
			{
				var hp = (int)((long)p.Parameters.HpMax * hpPct / 100);
				var sp = (int)((long)p.Parameters.SpMax * spPct / 100);
				if (hp > 0) p.HealHp(hp);
				if (sp > 0) p.HealSp(sp);
			}
		}

		/// <summary>
		/// eAthena `makeitem <itemId>, <count>, "<map>", <x>, <y>` —
		/// drops a stack on the ground at the given tile. Stackable
		/// items collapse into a single floor entity; non-stackables
		/// drop one per call (we let the engine's floor-item dedup
		/// rules handle that downstream).
		/// </summary>
		public static void MakeItem(int itemId, int amount, string mapStringId, int x, int y)
		{
			if (amount <= 0 || string.IsNullOrEmpty(mapStringId))
				return;
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(mapStringId, out var map))
				return;

			var item = new World.Actors.Item(itemId, amount);
			item.Drop(map, new Sabine.Shared.World.Position(x, y));
		}

		/// <summary>
		/// eAthena `query_sql "<sql>", <out1>, <out2>, …` — exec the
		/// raw SQL against the zone DB connection. SELECTs scatter the
		/// first row's columns into <paramref name="outVarNames"/>;
		/// non-SELECTs (INSERT / UPDATE / DELETE) just return the
		/// affected-row count. Every call is logged. The string-concat
		/// nature of eAthena's API means we can't parameterise; the
		/// SQL is static script source authored by the server admin,
		/// so the trust model is "anyone who can edit scripts already
		/// owns the box."
		/// </summary>
		/// <param name="player">For routing `@` / `#` out-var sigils.</param>
		/// <param name="sql">The SQL text. Use single quotes around literals.</param>
		/// <param name="outVarNames">
		/// Optional list of sigil-prefixed var names that receive the
		/// columns of the first matching row. Skipped when empty.
		/// </param>
		/// <returns>Number of rows read (SELECT) or affected (DML).</returns>
		public static int QuerySql(PlayerCharacter player, string sql, params string[] outVarNames)
		{
			if (string.IsNullOrWhiteSpace(sql))
				return 0;
			Log.Debug("QuerySql: {0}", sql);

			try
			{
				using var conn = ZoneServer.Instance.Database.GetConnection();
				using var cmd = new MySqlConnector.MySqlCommand(sql, conn);

				if (outVarNames != null && outVarNames.Length > 0)
				{
					using var reader = cmd.ExecuteReader();
					if (!reader.Read())
						return 0;

					var cols = System.Math.Min(reader.FieldCount, outVarNames.Length);
					for (var i = 0; i < cols; i++)
					{
						var raw = reader.IsDBNull(i) ? null : reader.GetValue(i);
						AssignDynamicVar(player, outVarNames[i], raw);
					}
					return 1;
				}

				return cmd.ExecuteNonQuery();
			}
			catch (System.Exception ex)
			{
				Log.Error("QuerySql: {0}", ex.Message);
				return 0;
			}
		}

		/// <summary>
		/// Routes a value into a sigil-prefixed runtime variable name,
		/// auto-detecting int vs string by the trailing `$` (and
		/// falling back to the value's runtime type when the name has
		/// no `$`).
		/// </summary>
		private static void AssignDynamicVar(PlayerCharacter player, string name, object value)
		{
			if (string.IsNullOrEmpty(name))
				return;
			var asString = name.EndsWith("$");
			if (asString)
				SetDImpl(player, name, value?.ToString() ?? string.Empty, asString: true);
			else
				SetDImpl(player, name, System.Convert.ToInt32(value ?? 0), asString: false);
		}

		/// <summary>
		/// eAthena `escape_sql(<expr>)` — escape special chars for safe
		/// embedding in raw SQL. We just route to MySqlConnector's
		/// command-builder utility.
		/// </summary>
		public static string EscapeSql(string value)
			=> string.IsNullOrEmpty(value) ? string.Empty
				: MySqlConnector.MySqlHelper.EscapeString(value);

		/// <summary>
		/// eAthena `summon "<slave-name>", <mobId>{, <duration>{, "<event>"}}`.
		/// Spawns a temporary mob at the calling player's position,
		/// firing <paramref name="onDeathEvent"/> on death (if set).
		/// Mirrors eAthena's default 60-second auto-despawn via a
		/// scheduled <c>Killed</c> trigger so the slave can't outlive
		/// the encounter.
		/// </summary>
		public static void Summon(PlayerCharacter player, string slaveName, int monsterId,
			string onDeathEvent = null, int durationMs = 60_000)
		{
			_ = slaveName;
			if (player?.Map == null)
				return;

			var label = string.IsNullOrEmpty(onDeathEvent) ? null : onDeathEvent;
			var spawned = Sabine.Zone.World.SpawnService.SpawnMonster(player.Map.StringId,
				player.Position.X, player.Position.Y,
				label, monsterId, 1);

			if (spawned.Count == 0 || durationMs <= 0)
				return;

			var mob = spawned[0];
			ZoneServer.Instance.World.Scheduler.Schedule(System.TimeSpan.FromMilliseconds(durationMs),
				_ => { if (!mob.IsDead) mob.Kill(null); });
		}

		#endregion

		#region Battleground (Phase 12c)

		/// <summary>
		/// eAthena `bg_create_team(<map>, <x>, <y>, "<onQuit>", "<onDeath>")`.
		/// Creates a new BG team entry, returning its id. The two
		/// event labels are fired by future `Quit` / `OnDeath` plumbing
		/// once the rest of the BG framework lands; for now we just
		/// hold the strings on the team so reads round-trip correctly.
		/// </summary>
		public static int BgCreateTeam(string mapStringId, int x, int y,
			string onQuit = null, string onDeath = null)
			=> ZoneServer.Instance.World.Battlegrounds.Create(
				mapStringId, x, y, onQuit, onDeath).Id;

		/// <summary>eAthena `bg_join_team <bgId>{, <accountId>}`.</summary>
		public static int BgJoinTeam(int bgId, PlayerCharacter player)
		{
			if (player == null) return 0;
			var team = ZoneServer.Instance.World.Battlegrounds.Get(bgId);
			if (team == null) return 0;
			lock (team.CharacterIds)
			{
				if (!team.CharacterIds.Contains(player.Id))
					team.CharacterIds.Add(player.Id);
			}

			if (Sabine.Shared.Game.Version >= Sabine.Shared.Versions.S2500)
			{
				var camp = (short)team.Id;
				foreach (var charId in team.CharacterIds)
				{
					foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
					{
						if (map.TryGetPlayerById(charId, out var pc))
						{
							Sabine.Zone.Network.Send.ZC_BATTLEFIELD_NOTIFY_CAMPINFO(pc, player.Handle, player.Name, camp);
							break;
						}
					}
				}
			}

			return team.CharacterIds.Count;
		}

		/// <summary>eAthena `bg_leave`.</summary>
		public static void BgLeave(PlayerCharacter player)
		{
			if (player == null) return;
			foreach (var team in ZoneServer.Instance.World.Battlegrounds.All)
			{
				lock (team.CharacterIds)
					team.CharacterIds.Remove(player.Id);
			}
		}

		/// <summary>eAthena `bg_destroy <bgId>`.</summary>
		public static bool BgDestroy(int bgId)
			=> ZoneServer.Instance.World.Battlegrounds.Destroy(bgId);

		/// <summary>eAthena `bg_warp <bgId>, "<map>", <x>, <y>` —
		/// teleport every team member to the destination.</summary>
		public static void BgWarp(int bgId, string mapStringId, int x, int y)
		{
			var team = ZoneServer.Instance.World.Battlegrounds.Get(bgId);
			if (team == null) return;
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(mapStringId, out var map))
				return;
			List<int> members;
			lock (team.CharacterIds)
				members = new List<int>(team.CharacterIds);
			foreach (var charId in members)
			{
				var p = FindPlayerById(charId);
				p?.Warp(new Sabine.Shared.World.Location(map.Id, new Sabine.Shared.World.Position(x, y)));
			}
		}

		/// <summary>eAthena `bg_team_setxy <bgId>, <x>, <y>` —
		/// updates the team's respawn coords (used by the BG when
		/// members die in-match).</summary>
		public static void BgTeamSetXy(int bgId, int x, int y)
		{
			var team = ZoneServer.Instance.World.Battlegrounds.Get(bgId);
			if (team == null) return;
			team.RespawnX = x;
			team.RespawnY = y;
		}

		/// <summary>eAthena `bg_updatescore "<map>", <gScore>, <cScore>` —
		/// broadcast a scoreboard tick to everyone on the map.</summary>
		public static void BgUpdateScore(string mapStringId, int teamA, int teamB)
		{
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(mapStringId, out var map))
				return;
			map.Announce($"Score: {teamA} - {teamB}");
		}

		/// <summary>eAthena `bg_monster <bgId>, "<map>", <x>, <y>, "<name>", <mobId>, "<event>"`.</summary>
		public static void BgMonster(int bgId, string mapStringId, int x, int y,
			string name, int monsterId, string onDeathEvent = null)
		{
			_ = bgId; _ = name;
			Sabine.Zone.World.SpawnService.SpawnMonster(mapStringId, x, y,
				onDeathEvent, monsterId, 1);
		}

		/// <summary>eAthena `bg_monster_set_team <gid>, <bgId>` — bind
		/// a previously-spawned mob to a BG team. Stub for now; the
		/// gameplay piece (monster->team enmity) lands with the rest
		/// of the BG framework.</summary>
		public static void BgMonsterSetTeam(int gid, int bgId)
		{
			_ = gid; _ = bgId;
		}

		/// <summary>eAthena `waitingroom "<title>", <maxUsers>{, …}`.
		/// Stub: the chat-room plumbing for NPC-attached waiting rooms
		/// isn't built yet; we no-op so scripts that try to install
		/// one don't crash.</summary>
		public static void WaitingRoom(World.Actors.Npc npc, string title, int maxUsers,
			string triggerEvent = null, int triggerCount = 0)
		{
			_ = npc; _ = title; _ = maxUsers;
			_ = triggerEvent; _ = triggerCount;
		}

		/// <summary>eAthena `delwaitingroom{ "<npc>"}`.</summary>
		public static void DelWaitingRoom(World.Actors.Npc npc, string targetNpcName = null)
		{
			_ = npc; _ = targetNpcName;
		}

		/// <summary>eAthena `enablewaitingroomevent{ "<npc>"}`.</summary>
		public static void EnableWaitingRoomEvent(World.Actors.Npc npc, string targetNpcName = null)
		{
			_ = npc; _ = targetNpcName;
		}

		/// <summary>eAthena `disablewaitingroomevent{ "<npc>"}`.</summary>
		public static void DisableWaitingRoomEvent(World.Actors.Npc npc, string targetNpcName = null)
		{
			_ = npc; _ = targetNpcName;
		}

		/// <summary>eAthena `warpwaitingpc "<map>", <x>, <y>{, <count>}`.
		/// Stub — pairs with WaitingRoom; lands with BG match-start
		/// plumbing.</summary>
		public static void WarpWaitingPc(World.Actors.Npc npc, string mapStringId,
			int x, int y, int count = -1)
		{
			_ = npc; _ = mapStringId; _ = x; _ = y; _ = count;
		}

		private static PlayerCharacter FindPlayerById(int charId)
		{
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				var matched = new List<PlayerCharacter>();
				map.GetPlayers(matched, charId, (id, p) => p.Id == id);
				if (matched.Count > 0) return matched[0];
			}
			return null;
		}

		#endregion
	}
}

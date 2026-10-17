using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Database;
using Sabine.Shared.World;
using Sabine.Zone.Skills;
using Sabine.Zone.World.Actors;
using Yggdrasil.Db.MySql;
using Yggdrasil.Db.MySql.SimpleCommands;

namespace Sabine.Zone.Database
{
	/// <summary>
	/// Zone server's database interface.
	/// </summary>
	public class ZoneDb : Db
	{
		/// <summary>
		/// Reads the character with the given id, and belonging to the
		/// account, from the database and returns it.
		/// </summary>
		/// <param name="account"></param>
		/// <returns></returns>
		public PlayerCharacter GetCharacter(Account account, int characterId)
		{
			PlayerCharacter character;

			using (var conn = this.GetConnection())
			{
				using (var cmd = new MySqlCommand("SELECT * FROM `characters` WHERE `accountId` = @accountId AND `characterId` = @characterId", conn))
				{
					cmd.AddParameter("@accountId", account.Id);
					cmd.AddParameter("@characterId", characterId);

					using (var reader = cmd.ExecuteReader())
					{
						if (!reader.Read())
							return null;

						var jobId = (JobId)reader.GetInt32("job");

						character = new PlayerCharacter(jobId);

						character.Id = reader.GetInt32("characterId");
						character.Name = reader.GetStringSafe("name");
						character.MapId = reader.GetInt32("mapId");
						character.HairId = reader.GetInt32("hair");
						character.HeadTopLook = reader.GetInt32("headTop");
						character.HeadMiddleLook = reader.GetInt32("headMiddle");
						character.HeadBottomLook = reader.GetInt32("headBottom");
						character.WeaponLook = reader.GetInt32("weapon");
						character.AmmoClassId = reader.GetInt32("ammo");
						character.Parameters.Zeny = reader.GetInt32("zeny");
						character.Parameters.Speed = reader.GetInt32("speed");
						character.Parameters.BaseLevel = reader.GetInt32("baseLevel");
						character.Parameters.JobLevel = reader.GetInt32("jobLevel");
						character.Parameters.BaseExp = reader.GetInt32("baseExp");
						character.Parameters.JobExp = reader.GetInt32("jobExp");
						character.Parameters.HpMax = reader.GetInt32("hpMax");
						character.Parameters.Hp = reader.GetInt32("hp");
						character.Parameters.SpMax = reader.GetInt32("spMax");
						character.Parameters.Sp = reader.GetInt32("sp");
						character.Parameters.Str = reader.GetInt32("str");
						character.Parameters.Agi = reader.GetInt32("agi");
						character.Parameters.Vit = reader.GetInt32("vit");
						character.Parameters.Int = reader.GetInt32("int");
						character.Parameters.Dex = reader.GetInt32("dex");
						character.Parameters.Luk = reader.GetInt32("luk");
						character.Parameters.StatPoints = reader.GetInt32("statPoints");
						character.Parameters.SkillPoints = reader.GetInt32("skillPoints");
						character.Parameters.Weight = reader.GetInt32("weight");
						character.Parameters.WeightMax = reader.GetInt32("weightMax");

						// Marriage state — columns are nullable and
						// added by `sql/updates/2026-05-03_001.sql`.
						// Older databases that haven't run the
						// migration yet won't have either column;
						// swallow the exception so the character still
						// loads (mirrors the `location` pattern at
						// line 99 above).
						try
						{
							var partnerOrd = reader.GetOrdinal("marriedToCharId");
							if (!reader.IsDBNull(partnerOrd))
								character.MarriedToCharId = reader.GetInt32(partnerOrd);

							var marriedAtOrd = reader.GetOrdinal("marriedAt");
							if (!reader.IsDBNull(marriedAtOrd))
								character.MarriedAt = reader.GetDateTime(marriedAtOrd);
						}
						catch { }

						var x = reader.GetInt32("x");
						var y = reader.GetInt32("y");
						var dir = (Direction)reader.GetInt32("dir");

						character.Position = new Position(x, y);
						character.Direction = dir;
					}
				}

				using (var mc = new MySqlCommand("SELECT * FROM `items` WHERE `characterId` = @characterId", conn))
				{
					mc.AddParameter("@characterId", character.Id);

					using (var reader = mc.ExecuteReader())
					{
						while (reader.Read())
						{
							var item = new Item(reader.GetInt32("classId"));

							item.Amount = reader.GetInt32("amount");
							item.EquippedOn = (EquipSlots)reader.GetInt32("equipped");
							try { item.HireExpireDate = reader.GetInt32("hireExpireDate"); } catch { }

							if (item.HireExpireDate > 0
								&& item.HireExpireDate <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
								continue;

							// 1 = cart, 0 = body. New column may be missing
							// on older databases, so default to body.
							var location = 0;
							try { location = reader.GetInt32("location"); } catch { }

							if (location == 1)
								character.Inventory.AddCartItemInit(item);
							else
							{
								character.Inventory.AddItemInit(item);
								character.Inventory.ScheduleRentalExpiration(item);
							}
						}
					}
				}

				using (var mc = new MySqlCommand("SELECT * FROM `skills` WHERE `characterId` = @characterId", conn))
				{
					mc.AddParameter("@characterId", character.Id);

					using (var reader = mc.ExecuteReader())
					{
						while (reader.Read())
						{
							var skillId = (SkillId)reader.GetInt32("id");
							var level = reader.GetInt32("level");

							character.Skills.AddSilent(new Skill(character, skillId, level));
						}
					}
				}

				using (var mc = new MySqlCommand("SELECT * FROM `storage_items` WHERE `accountId` = @accountId", conn))
				{
					mc.AddParameter("@accountId", account.Id);

					using (var reader = mc.ExecuteReader())
					{
						while (reader.Read())
						{
							var item = new Item(reader.GetInt32("classId"));
							item.Amount = reader.GetInt32("amount");
							item.RefineLevel = (byte)reader.GetInt32("refine");
							item.IsIdentified = reader.GetInt32("identified") != 0;
							item.IsDamaged = reader.GetInt32("damaged") != 0;

							character.Storage.AddItemInit(item);
						}
					}
				}
			}

			character.Vars.Perm.Load(this.GetVars("vars_character", character.Id));

			// Homunculus: prefer the DB-backed row; fall back to the
			// legacy vars-keys path for characters who logged out before
			// the schema migration shipped. Once a player saves under
			// the new code their row exists, so this branch becomes the
			// fast path on subsequent logins.
			var homunSnapshot = this.LoadHomunculus(character.Id);
			if (homunSnapshot != null)
				character.Homunculus?.LoadFromSnapshot(homunSnapshot);
			else
				character.Homunculus?.LoadFrom(character.Vars.Perm);

			character.Pet?.LoadFrom(character.Vars.Perm);
			character.Mercenary?.LoadFrom(character.Vars.Perm);
			character.Parameters.RecalculateAll();

			// Set HP and SP to max on first load, after the characters'
			// parameters were calculated for the first time.
			if (character.Vars.Perm.ActivateOnce("Sabine.EverLoaded"))
			{
				character.Parameters.Hp = character.Parameters.HpMax;
				character.Parameters.Sp = character.Parameters.SpMax;
			}

			// Marriage reconcile: if we're marked as married but the
			// partner's record no longer points back at us (one-sided
			// divorce that happened while we were offline), clear the
			// local side so the next save persists the corrected
			// state. Cheap one-shot check at login time; no heartbeat
			// scan needed.
			if (character.MarriedToCharId is int partnerId
				&& this.GetMarriedPartnerId(partnerId) != character.Id)
			{
				character.MarriedToCharId = null;
				character.MarriedAt = null;
			}

			return character;
		}

		/// <summary>
		/// Returns the partner char id of the given char (whatever
		/// is currently in the `marriedToCharId` column), or 0 when
		/// the character isn't married or doesn't exist. Used by the
		/// reconcile-on-load path in <see cref="GetCharacter"/> and
		/// by `Shortcuts.GetPartnerName` for offline lookup.
		/// </summary>
		public int GetMarriedPartnerId(int characterId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand("SELECT `marriedToCharId` FROM `characters` WHERE `characterId` = @id", conn);
			cmd.AddParameter("@id", characterId);
			var result = cmd.ExecuteScalar();
			if (result == null || result == DBNull.Value)
				return 0;
			return Convert.ToInt32(result);
		}

		/// <summary>
		/// Returns the character name for the given char id, or empty
		/// string when the id isn't found. Cheap single-column lookup
		/// for use by `Shortcuts.GetPartnerName` when the partner is
		/// offline; heavier DB calls live in <see cref="GetCharacter"/>.
		/// </summary>
		public string GetCharacterName(int characterId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand("SELECT `name` FROM `characters` WHERE `characterId` = @id", conn);
			cmd.AddParameter("@id", characterId);
			var result = cmd.ExecuteScalar();
			return result as string ?? string.Empty;
		}

		/// <summary>
		/// Clears the marriage columns on the given character id
		/// directly in the database. Used by `Shortcuts.Divorce`
		/// when the partner is offline — keeps the two records in
		/// sync without waiting for the partner's next login.
		/// </summary>
		public void ClearMarriage(int characterId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"UPDATE `characters` SET `marriedToCharId` = NULL, `marriedAt` = NULL WHERE `characterId` = @id",
				conn);
			cmd.AddParameter("@id", characterId);
			cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Saves character to database.
		/// </summary>
		/// <param name="account"></param>
		/// <param name="character"></param>
		public void SaveCharacter(Account account, PlayerCharacter character)
		{
			using (var conn = this.GetConnection())
			using (var trans = conn.BeginTransaction())
			{
				using (var cmd = new UpdateCommand("UPDATE `characters` SET {parameters} WHERE `accountId` = @accountId AND `characterId` = @characterId", conn, trans))
				{
					cmd.AddParameter("@accountId", account.Id);
					cmd.AddParameter("@characterId", character.Id);

					cmd.Set("name", character.Name);
					cmd.Set("job", (int)character.JobId);
					cmd.Set("mapId", character.MapId);
					cmd.Set("x", character.Position.X);
					cmd.Set("y", character.Position.Y);
					cmd.Set("dir", (int)character.Direction);
					cmd.Set("hair", character.HairId);
					cmd.Set("headTop", character.HeadTopLook);
					cmd.Set("headMiddle", character.HeadMiddleLook);
					cmd.Set("headBottom", character.HeadBottomLook);
					cmd.Set("weapon", character.WeaponLook);
					cmd.Set("ammo", character.AmmoClassId);
					cmd.Set("zeny", character.Parameters.Zeny);
					cmd.Set("speed", character.Parameters.Speed);
					cmd.Set("baseLevel", character.Parameters.BaseLevel);
					cmd.Set("jobLevel", character.Parameters.JobLevel);
					cmd.Set("baseExp", character.Parameters.BaseExp);
					cmd.Set("jobExp", character.Parameters.JobExp);
					cmd.Set("hp", character.Parameters.Hp);
					cmd.Set("hpMax", character.Parameters.HpMax);
					cmd.Set("sp", character.Parameters.Sp);
					cmd.Set("spMax", character.Parameters.SpMax);
					cmd.Set("str", character.Parameters.Str);
					cmd.Set("agi", character.Parameters.Agi);
					cmd.Set("vit", character.Parameters.Vit);
					cmd.Set("int", character.Parameters.Int);
					cmd.Set("dex", character.Parameters.Dex);
					cmd.Set("luk", character.Parameters.Luk);
					cmd.Set("statPoints", character.Parameters.StatPoints);
					cmd.Set("skillPoints", character.Parameters.SkillPoints);
					cmd.Set("weight", character.Parameters.Weight);
					cmd.Set("weightMax", character.Parameters.WeightMax);
					cmd.Set("lastVersion", Game.Version);

					cmd.Execute();
				}

				using (var cmd = new MySqlCommand("DELETE FROM `items` WHERE `characterId` = @characterId", conn, trans))
				{
					cmd.AddParameter("@characterId", character.Id);
					cmd.ExecuteNonQuery();
				}

				using (var cmd = new MySqlCommand("DELETE FROM `skills` WHERE `characterId` = @characterId", conn, trans))
				{
					cmd.AddParameter("@characterId", character.Id);
					cmd.ExecuteNonQuery();
				}

				using (var cmd = new BatchedInsertCommand("items", conn, trans))
				{
					var items = character.Inventory.GetItems();

					foreach (var item in items)
					{
						cmd.Set("characterId", character.Id);
						cmd.Set("classId", item.ClassId);
						cmd.Set("amount", item.Amount);
						cmd.Set("equipped", (int)item.EquippedOn);
						cmd.Set("location", 0);
						cmd.Set("hireExpireDate", item.HireExpireDate);

						cmd.AddRow();
						cmd.ExecuteOn(200);
					}

					var cartItems = character.Inventory.GetCartItems();

					foreach (var item in cartItems)
					{
						cmd.Set("characterId", character.Id);
						cmd.Set("classId", item.ClassId);
						cmd.Set("amount", item.Amount);
						cmd.Set("equipped", 0);
						cmd.Set("location", 1);
						cmd.Set("hireExpireDate", item.HireExpireDate);

						cmd.AddRow();
						cmd.ExecuteOn(200);
					}

					cmd.Execute();
				}

				using (var cmd = new MySqlCommand("DELETE FROM `storage_items` WHERE `accountId` = @accountId", conn, trans))
				{
					cmd.AddParameter("@accountId", account.Id);
					cmd.ExecuteNonQuery();
				}

				using (var cmd = new BatchedInsertCommand("storage_items", conn, trans))
				{
					var storage = character.Storage.GetItems();

					foreach (var item in storage)
					{
						cmd.Set("accountId", account.Id);
						cmd.Set("classId", item.ClassId);
						cmd.Set("amount", item.Amount);
						cmd.Set("refine", (int)item.RefineLevel);
						cmd.Set("identified", item.IsIdentified ? 1 : 0);
						cmd.Set("damaged", item.IsDamaged ? 1 : 0);

						cmd.AddRow();
						cmd.ExecuteOn(200);
					}

					cmd.Execute();
				}

				using (var cmd = new BatchedInsertCommand("skills", conn, trans))
				{
					var skills = character.Skills.GetAll();

					foreach (var skill in skills)
					{
						if (skill.Perm != SkillPerm.Permanent)
							continue;
						
						cmd.Set("characterId", character.Id);
						cmd.Set("id", skill.Id);
						cmd.Set("level", skill.Level);

						cmd.AddRow();
						cmd.ExecuteOn(200);
					}

					cmd.Execute();
				}

				trans.Commit();
			}

			// Homunculus: write through to the dedicated row. The
			// vars-keys SaveTo still runs so a rolled-back binary can
			// read prior state during a backout window.
			this.SaveHomunculus(character.Id, character.Homunculus?.ToSnapshot());
			character.Homunculus?.SaveTo(character.Vars.Perm);

			character.Pet?.SaveTo(character.Vars.Perm);
			character.Mercenary?.SaveTo(character.Vars.Perm);
			this.SaveVars("vars_character", character.Id, character.Vars.Perm.GetList());
		}

		// ----- Guild persistence (Phase 6) ------------------------------
		// `LoadAllGuilds` is called once at startup; mutations
		// (`Insert/Update/Delete...`) are called by `GuildManager` as
		// state changes. Allies / antagonists / custom rank names are
		// kept in-memory for now — the schema covers the load-bearing
		// fields (id, name, master, level, exp, emblem, notice) and
		// the membership roster.

		/// <summary>
		/// Returns every guild row in the database, paired with its
		/// member rows. Used by <see cref="World.Groups.GuildManager"/>
		/// at startup.
		/// </summary>
		public List<World.Groups.GuildDbRow> LoadAllGuilds()
		{
			var guilds = new Dictionary<int, World.Groups.GuildDbRow>();
			using var conn = this.GetConnection();

			using (var cmd = new MySqlCommand("SELECT * FROM `guilds`", conn))
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					var row = new World.Groups.GuildDbRow
					{
						GuildId = reader.GetInt32("guildId"),
						Name = reader.GetStringSafe("name"),
						MasterCharId = reader.GetInt32("masterCharId"),
						Level = reader.GetInt32("level"),
						ExpCurrent = reader.GetInt32("expCurrent"),
						EmblemId = reader.GetInt32("emblemId"),
						Notice = reader.GetStringSafe("notice") ?? string.Empty,
						Zeny = reader.GetInt64("zeny"),
						EmblemData = reader["emblemData"] is byte[] bytes ? bytes : null,
					};
					guilds[row.GuildId] = row;
				}
			}

			using (var cmd = new MySqlCommand("SELECT * FROM `guild_members`", conn))
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					var gid = reader.GetInt32("guildId");
					if (!guilds.TryGetValue(gid, out var guild))
						continue;
					guild.Members.Add(new World.Groups.GuildMemberDbRow
					{
						CharacterId = reader.GetInt32("characterId"),
						IsMaster = reader.GetInt32("isMaster") != 0,
						Position = reader.GetStringSafe("position") ?? string.Empty,
					});
				}
			}

			using (var cmd = new MySqlCommand("SELECT * FROM `guild_skills`", conn))
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					var guildId = reader.GetInt32("guildId");
					if (!guilds.TryGetValue(guildId, out var guild))
						continue;
					guild.Skills.Add(new World.Groups.GuildSkillDbRow
					{
						SkillId = reader.GetInt32("skillId"),
						Level = reader.GetInt32("level"),
					});
				}
			}

			return guilds.Values.ToList();
		}

		/// <summary>
		/// Inserts a new guild row, returning the auto-generated
		/// <c>guildId</c>. The master member row is added in a
		/// separate <see cref="InsertGuildMember"/> call so the
		/// caller can sequence both inside one transaction if needed.
		/// </summary>
		public int InsertGuild(string name, int masterCharId, int level, int expCurrent, int emblemId, string notice)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"INSERT INTO `guilds` (`name`, `masterCharId`, `level`, `expCurrent`, `emblemId`, `notice`) "
				+ "VALUES (@name, @masterCharId, @level, @expCurrent, @emblemId, @notice); SELECT LAST_INSERT_ID();",
				conn);
			cmd.AddParameter("@name", name);
			cmd.AddParameter("@masterCharId", masterCharId);
			cmd.AddParameter("@level", level);
			cmd.AddParameter("@expCurrent", expCurrent);
			cmd.AddParameter("@emblemId", emblemId);
			cmd.AddParameter("@notice", notice ?? string.Empty);
			return Convert.ToInt32(cmd.ExecuteScalar());
		}

		/// <summary>
		/// Updates the load-bearing scalar fields on an existing guild
		/// row. Member roster mutations are handled separately via
		/// <see cref="InsertGuildMember"/> / <see cref="DeleteGuildMember"/>.
		/// </summary>
		public void UpdateGuild(int guildId, string name, int masterCharId, int level, int expCurrent, int emblemId, string notice, long zeny, byte[] emblemData)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"UPDATE `guilds` SET `name` = @name, `masterCharId` = @masterCharId, "
				+ "`level` = @level, `expCurrent` = @expCurrent, `emblemId` = @emblemId, "
				+ "`notice` = @notice, `zeny` = @zeny, `emblemData` = @emblemData "
				+ "WHERE `guildId` = @guildId",
				conn);
			cmd.AddParameter("@guildId", guildId);
			cmd.AddParameter("@name", name);
			cmd.AddParameter("@masterCharId", masterCharId);
			cmd.AddParameter("@level", level);
			cmd.AddParameter("@expCurrent", expCurrent);
			cmd.AddParameter("@emblemId", emblemId);
			cmd.AddParameter("@notice", notice ?? string.Empty);
			cmd.AddParameter("@zeny", zeny);
			cmd.AddParameter("@emblemData", (object)emblemData ?? System.DBNull.Value);
			cmd.ExecuteNonQuery();
		}

		/// <summary>Deletes a guild row; cascades to its member rows
		/// via the FK.</summary>
		public void DeleteGuild(int guildId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand("DELETE FROM `guilds` WHERE `guildId` = @id", conn);
			cmd.AddParameter("@id", guildId);
			cmd.ExecuteNonQuery();
		}

		/// <summary>Upserts one learned guild skill; level zero removes it.</summary>
		public void SaveGuildSkill(int guildId, int skillId, int level)
		{
			using var conn = this.GetConnection();
			if (level <= 0)
			{
				using var delete = new MySqlCommand("DELETE FROM `guild_skills` WHERE `guildId` = @guildId AND `skillId` = @skillId", conn);
				delete.AddParameter("@guildId", guildId);
				delete.AddParameter("@skillId", skillId);
				delete.ExecuteNonQuery();
				return;
			}
			using var cmd = new MySqlCommand("INSERT INTO `guild_skills` (`guildId`, `skillId`, `level`) VALUES (@guildId, @skillId, @level) ON DUPLICATE KEY UPDATE `level` = @level", conn);
			cmd.AddParameter("@guildId", guildId);
			cmd.AddParameter("@skillId", skillId);
			cmd.AddParameter("@level", level);
			cmd.ExecuteNonQuery();
		}

		/// <summary>Inserts a member into a guild's roster. PK on
		/// `characterId` enforces "one guild per character" at the DB
		/// layer.</summary>
		public void InsertGuildMember(int guildId, int characterId, bool isMaster, string position)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"INSERT INTO `guild_members` (`guildId`, `characterId`, `isMaster`, `position`) "
				+ "VALUES (@guildId, @characterId, @isMaster, @position) "
				+ "ON DUPLICATE KEY UPDATE `guildId` = @guildId, `isMaster` = @isMaster, `position` = @position",
				conn);
			cmd.AddParameter("@guildId", guildId);
			cmd.AddParameter("@characterId", characterId);
			cmd.AddParameter("@isMaster", isMaster ? 1 : 0);
			cmd.AddParameter("@position", position ?? string.Empty);
			cmd.ExecuteNonQuery();
		}

		/// <summary>Removes a member from any guild they belong to.
		/// The PK on `characterId` makes this implicitly idempotent.</summary>
		public void DeleteGuildMember(int characterId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand("DELETE FROM `guild_members` WHERE `characterId` = @id", conn);
			cmd.AddParameter("@id", characterId);
			cmd.ExecuteNonQuery();
		}

		// ----- Guild storage persistence -------------------------------
		// Guild storage rows live on `guild_storage_items` and are keyed
		// by `guildId`. Mirrors the personal `storage_items` shape; the
		// only difference is the owner column. Schema lives in
		// `sql/updates/2026-05-08_002.sql`.

		/// <summary>
		/// Returns every persisted item for the given guild's storage.
		/// Caller is responsible for seeding the in-memory
		/// <c>GuildStorage</c> with the result.
		/// </summary>
		public List<Item> LoadGuildStorage(int guildId)
		{
			var items = new List<Item>();

			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand("SELECT * FROM `guild_storage_items` WHERE `guildId` = @guildId", conn);
			cmd.AddParameter("@guildId", guildId);

			using var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				var item = new Item(reader.GetInt32("classId"));
				item.Amount = reader.GetInt32("amount");
				item.RefineLevel = (byte)reader.GetInt32("refine");
				item.IsIdentified = reader.GetInt32("identified") != 0;
				item.IsDamaged = reader.GetInt32("damaged") != 0;

				items.Add(item);
			}

			return items;
		}

		/// <summary>
		/// Replaces every persisted row for the given guild's storage
		/// with the supplied snapshot. Mirrors the personal-storage
		/// `DELETE` + batched-insert pattern from <see cref="SaveCharacter"/>.
		/// </summary>
		public void SaveGuildStorage(int guildId, IEnumerable<Item> items)
		{
			using var conn = this.GetConnection();
			using var trans = conn.BeginTransaction();

			using (var cmd = new MySqlCommand("DELETE FROM `guild_storage_items` WHERE `guildId` = @guildId", conn, trans))
			{
				cmd.AddParameter("@guildId", guildId);
				cmd.ExecuteNonQuery();
			}

			using (var cmd = new BatchedInsertCommand("guild_storage_items", conn, trans))
			{
				foreach (var item in items)
				{
					cmd.Set("guildId", guildId);
					cmd.Set("classId", item.ClassId);
					cmd.Set("amount", item.Amount);
					cmd.Set("refine", (int)item.RefineLevel);
					cmd.Set("identified", item.IsIdentified ? 1 : 0);
					cmd.Set("damaged", item.IsDamaged ? 1 : 0);

					cmd.AddRow();
					cmd.ExecuteOn(200);
				}

				cmd.Execute();
			}

			trans.Commit();
		}

		// ----- Party persistence ---------------------------------------
		// Parties survive across restarts. The schema is in
		// `sql/updates/2026-05-07_001.sql`. `InsertParty` mutates the
		// in-memory `Party.Id` to the auto-generated DB id; everything
		// else just writes through.

		/// <summary>
		/// Returns every party row joined with its members. Member
		/// names are joined in from `characters` so the roster has
		/// usable display names even before the players log in.
		/// </summary>
		public List<World.Groups.PartyDbRow> LoadAllParties()
		{
			var parties = new Dictionary<int, World.Groups.PartyDbRow>();
			using var conn = this.GetConnection();

			using (var cmd = new MySqlCommand("SELECT * FROM `parties`", conn))
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					var row = new World.Groups.PartyDbRow
					{
						PartyId = reader.GetInt32("partyId"),
						Name = reader.GetStringSafe("name"),
						ExpShare = reader.GetInt32("expShare") != 0,
						ItemShare = reader.GetInt32("itemShare") != 0,
						LeaderCharId = reader.GetInt32("leaderCharId"),
					};
					parties[row.PartyId] = row;
				}
			}

			using (var cmd = new MySqlCommand(
				"SELECT pm.`partyId`, pm.`characterId`, pm.`isLeader`, c.`name` "
				+ "FROM `party_members` pm "
				+ "LEFT JOIN `characters` c ON c.`characterId` = pm.`characterId`",
				conn))
			using (var reader = cmd.ExecuteReader())
			{
				while (reader.Read())
				{
					var pid = reader.GetInt32("partyId");
					if (!parties.TryGetValue(pid, out var party))
						continue;
					party.Members.Add(new World.Groups.PartyMemberDbRow
					{
						CharacterId = reader.GetInt32("characterId"),
						IsLeader = reader.GetInt32("isLeader") != 0,
						Name = reader.GetStringSafe("name") ?? string.Empty,
					});
				}
			}

			return parties.Values.ToList();
		}

		/// <summary>
		/// Inserts a new party row, assigns the auto-generated id back
		/// to <see cref="World.Groups.Party.Id"/>, then writes the
		/// initial member rows.
		/// </summary>
		public void InsertParty(World.Groups.Party party)
		{
			using var conn = this.GetConnection();

			int id;
			using (var cmd = new MySqlCommand(
				"INSERT INTO `parties` (`name`, `expShare`, `itemShare`, `leaderCharId`) "
				+ "VALUES (@name, @expShare, @itemShare, @leaderCharId); "
				+ "SELECT LAST_INSERT_ID();",
				conn))
			{
				cmd.AddParameter("@name", party.Name);
				cmd.AddParameter("@expShare", party.ExpShareOption == World.Groups.PartyExpShareOption.EvenShare ? 1 : 0);
				cmd.AddParameter("@itemShare", party.ItemShareOption == World.Groups.PartyItemShareOption.EvenShare ? 1 : 0);
				cmd.AddParameter("@leaderCharId", party.GetLeader()?.CharacterId ?? 0);
				id = Convert.ToInt32(cmd.ExecuteScalar());
			}

			party.Id = id;

			foreach (var m in party.Members)
			{
				using var cmd = new MySqlCommand(
					"INSERT INTO `party_members` (`partyId`, `characterId`, `isLeader`) "
					+ "VALUES (@partyId, @characterId, @isLeader) "
					+ "ON DUPLICATE KEY UPDATE `partyId` = @partyId, `isLeader` = @isLeader",
					conn);
				cmd.AddParameter("@partyId", id);
				cmd.AddParameter("@characterId", m.CharacterId);
				cmd.AddParameter("@isLeader", m.IsLeader ? 1 : 0);
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>Persists a party's options + leader.</summary>
		public void UpdatePartyOptions(int partyId, bool expShare, bool itemShare)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"UPDATE `parties` SET `expShare` = @expShare, `itemShare` = @itemShare WHERE `partyId` = @id",
				conn);
			cmd.AddParameter("@id", partyId);
			cmd.AddParameter("@expShare", expShare ? 1 : 0);
			cmd.AddParameter("@itemShare", itemShare ? 1 : 0);
			cmd.ExecuteNonQuery();
		}

		/// <summary>Updates the leader pointer + flips the
		/// <c>isLeader</c> flag on the member rows.</summary>
		public void UpdatePartyLeader(int partyId, int leaderCharId)
		{
			using var conn = this.GetConnection();
			using (var cmd = new MySqlCommand(
				"UPDATE `parties` SET `leaderCharId` = @leaderCharId WHERE `partyId` = @id",
				conn))
			{
				cmd.AddParameter("@id", partyId);
				cmd.AddParameter("@leaderCharId", leaderCharId);
				cmd.ExecuteNonQuery();
			}
			using (var cmd = new MySqlCommand(
				"UPDATE `party_members` SET `isLeader` = (`characterId` = @leaderCharId) WHERE `partyId` = @id",
				conn))
			{
				cmd.AddParameter("@id", partyId);
				cmd.AddParameter("@leaderCharId", leaderCharId);
				cmd.ExecuteNonQuery();
			}
		}

		/// <summary>Deletes a party row; cascades to its member rows
		/// via the FK.</summary>
		public void DeleteParty(int partyId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand("DELETE FROM `parties` WHERE `partyId` = @id", conn);
			cmd.AddParameter("@id", partyId);
			cmd.ExecuteNonQuery();
		}

		/// <summary>Inserts (or updates) a party-member row.</summary>
		public void InsertPartyMember(int partyId, int characterId, bool isLeader)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"INSERT INTO `party_members` (`partyId`, `characterId`, `isLeader`) "
				+ "VALUES (@partyId, @characterId, @isLeader) "
				+ "ON DUPLICATE KEY UPDATE `partyId` = @partyId, `isLeader` = @isLeader",
				conn);
			cmd.AddParameter("@partyId", partyId);
			cmd.AddParameter("@characterId", characterId);
			cmd.AddParameter("@isLeader", isLeader ? 1 : 0);
			cmd.ExecuteNonQuery();
		}

		/// <summary>Removes a character from any party they belong to.</summary>
		public void DeletePartyMember(int characterId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand("DELETE FROM `party_members` WHERE `characterId` = @id", conn);
			cmd.AddParameter("@id", characterId);
			cmd.ExecuteNonQuery();
		}

		// ----- Mail persistence ----------------------------------------
		// Inboxes are loaded lazily per receiver; mutations write
		// through immediately. Schema: `sql/updates/2026-05-08_001.sql`.

		/// <summary>
		/// Returns the inbox of <paramref name="receiverId"/>, oldest
		/// first. The receiver's <c>MailManager</c> caches and sorts.
		/// </summary>
		public List<World.Mail.MailMessage> LoadMailFor(int receiverId)
		{
			var list = new List<World.Mail.MailMessage>();
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"SELECT * FROM `mail_messages` WHERE `receiverId` = @id", conn);
			cmd.AddParameter("@id", receiverId);
			using var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				list.Add(new World.Mail.MailMessage
				{
					Id = reader.GetInt32("mailId"),
					SenderId = reader.GetInt32("senderId"),
					SenderName = reader.GetStringSafe("senderName") ?? string.Empty,
					ReceiverId = reader.GetInt32("receiverId"),
					Title = reader.GetStringSafe("title") ?? string.Empty,
					Body = reader.GetStringSafe("body") ?? string.Empty,
					Zeny = reader.GetInt32("zeny"),
					AttachedItemClassId = reader.GetInt32("itemClassId"),
					AttachedItemAmount = (short)reader.GetInt32("itemAmount"),
					AttachedItemRefine = (byte)reader.GetInt32("itemRefine"),
					AttachedItemIdentified = reader.GetInt32("itemIdentified") != 0,
					AttachedItemDamaged = reader.GetInt32("itemDamaged") != 0,
					Read = reader.GetInt32("read") != 0,
					AttachmentTaken = reader.GetInt32("attachmentTaken") != 0,
					IsReturned = reader.GetInt32("isReturned") != 0,
					SentAt = reader.GetDateTime("sentAt"),
				});
			}
			return list;
		}

		/// <summary>
		/// Inserts a new mail row, assigning the auto-generated
		/// <c>mailId</c> back onto <paramref name="msg"/>.
		/// </summary>
		public void InsertMail(World.Mail.MailMessage msg)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"INSERT INTO `mail_messages` "
				+ "(`senderId`, `senderName`, `receiverId`, `title`, `body`, "
				+ " `zeny`, `itemClassId`, `itemAmount`, `itemRefine`, "
				+ " `itemIdentified`, `itemDamaged`, `isReturned`, `sentAt`) "
				+ "VALUES (@senderId, @senderName, @receiverId, @title, @body, "
				+ " @zeny, @itemClassId, @itemAmount, @itemRefine, "
				+ " @itemIdentified, @itemDamaged, @isReturned, @sentAt); "
				+ "SELECT LAST_INSERT_ID();",
				conn);
			cmd.AddParameter("@senderId", msg.SenderId);
			cmd.AddParameter("@senderName", msg.SenderName ?? string.Empty);
			cmd.AddParameter("@receiverId", msg.ReceiverId);
			cmd.AddParameter("@title", msg.Title ?? string.Empty);
			cmd.AddParameter("@body", msg.Body ?? string.Empty);
			cmd.AddParameter("@zeny", msg.Zeny);
			cmd.AddParameter("@itemClassId", msg.AttachedItemClassId);
			cmd.AddParameter("@itemAmount", (int)msg.AttachedItemAmount);
			cmd.AddParameter("@itemRefine", (int)msg.AttachedItemRefine);
			cmd.AddParameter("@itemIdentified", msg.AttachedItemIdentified ? 1 : 0);
			cmd.AddParameter("@itemDamaged", msg.AttachedItemDamaged ? 1 : 0);
			cmd.AddParameter("@isReturned", msg.IsReturned ? 1 : 0);
			cmd.AddParameter("@sentAt", msg.SentAt);
			msg.Id = Convert.ToInt32(cmd.ExecuteScalar());
		}

		/// <summary>Marks a message as read.</summary>
		public void UpdateMailRead(int mailId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"UPDATE `mail_messages` SET `read` = 1 WHERE `mailId` = @id", conn);
			cmd.AddParameter("@id", mailId);
			cmd.ExecuteNonQuery();
		}

		/// <summary>Marks a message's attachment as picked up.</summary>
		public void UpdateMailAttachmentTaken(int mailId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"UPDATE `mail_messages` SET `attachmentTaken` = 1 WHERE `mailId` = @id", conn);
			cmd.AddParameter("@id", mailId);
			cmd.ExecuteNonQuery();
		}

		/// <summary>Removes a mail row.</summary>
		public void DeleteMail(int mailId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"DELETE FROM `mail_messages` WHERE `mailId` = @id", conn);
			cmd.AddParameter("@id", mailId);
			cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Deletes mail rows older than <paramref name="days"/> days
		/// (measured against <c>sentAt</c>) and returns the number of
		/// rows removed. Called from <c>MailManager.ExpirationTick</c>.
		/// Items attached to expired mail are forfeit — there is no
		/// refund-to-sender step in v1.
		/// </summary>
		public int PurgeExpiredMail(int days)
		{
			if (days <= 0)
				return 0;

			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"DELETE FROM `mail_messages` "
				+ "WHERE `sentAt` < UTC_TIMESTAMP() - INTERVAL @days DAY", conn);
			cmd.AddParameter("@days", days);
			return cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Returns mail rows older than <paramref name="days"/> days
		/// (measured against <c>sentAt</c>), in DB order. Used by the
		/// expiration sweep to refund unclaimed attachments back to
		/// the original sender before the rows are deleted.
		/// </summary>
		public List<World.Mail.MailMessage> LoadExpiringMail(int days)
		{
			var list = new List<World.Mail.MailMessage>();
			if (days <= 0)
				return list;

			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"SELECT * FROM `mail_messages` "
				+ "WHERE `sentAt` < UTC_TIMESTAMP() - INTERVAL @days DAY", conn);
			cmd.AddParameter("@days", days);
			using var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				list.Add(new World.Mail.MailMessage
				{
					Id = reader.GetInt32("mailId"),
					SenderId = reader.GetInt32("senderId"),
					SenderName = reader.GetStringSafe("senderName") ?? string.Empty,
					ReceiverId = reader.GetInt32("receiverId"),
					Title = reader.GetStringSafe("title") ?? string.Empty,
					Body = reader.GetStringSafe("body") ?? string.Empty,
					Zeny = reader.GetInt32("zeny"),
					AttachedItemClassId = reader.GetInt32("itemClassId"),
					AttachedItemAmount = (short)reader.GetInt32("itemAmount"),
					AttachedItemRefine = (byte)reader.GetInt32("itemRefine"),
					AttachedItemIdentified = reader.GetInt32("itemIdentified") != 0,
					AttachedItemDamaged = reader.GetInt32("itemDamaged") != 0,
					Read = reader.GetInt32("read") != 0,
					AttachmentTaken = reader.GetInt32("attachmentTaken") != 0,
					IsReturned = reader.GetInt32("isReturned") != 0,
					SentAt = reader.GetDateTime("sentAt"),
				});
			}
			return list;
		}

		/// <summary>
		/// Resolves a character name to its id, or 0 if no match.
		/// Used by mail send to validate the recipient.
		/// </summary>
		public int GetCharacterIdByName(string name)
		{
			if (string.IsNullOrEmpty(name))
				return 0;

			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"SELECT `characterId` FROM `characters` WHERE `name` = @name LIMIT 1", conn);
			cmd.AddParameter("@name", name);
			var result = cmd.ExecuteScalar();
			return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
		}

		// ----- Auction persistence (Slice G) ----------------------------
		// All open auctions are loaded once at startup; mutations
		// (insert / update bid / mark resolved) write through. The
		// resolution loop polls in-memory and calls
		// `UpdateAuctionStatus` once payout has fanned out via Mail.
		// Schema: `sql/updates/2026-05-09_001.sql`.

		/// <summary>
		/// Returns every auction whose status is <c>Open</c>. Resolved
		/// rows stay in the table for history but aren't re-loaded.
		/// </summary>
		public List<World.Auctions.AuctionEntry> LoadOpenAuctions()
		{
			var list = new List<World.Auctions.AuctionEntry>();
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"SELECT * FROM `auctions` WHERE `status` = 0", conn);
			using var reader = cmd.ExecuteReader();
			while (reader.Read())
				list.Add(ReadAuction(reader));
			return list;
		}

		private static World.Auctions.AuctionEntry ReadAuction(MySqlDataReader reader)
		{
			return new World.Auctions.AuctionEntry
			{
				Id = reader.GetInt32("auctionId"),
				SellerId = reader.GetInt32("sellerId"),
				SellerName = reader.GetStringSafe("sellerName") ?? string.Empty,
				ItemClassId = reader.GetInt32("itemClassId"),
				ItemAmount = (short)reader.GetInt32("itemAmount"),
				ItemRefine = (byte)reader.GetInt32("itemRefine"),
				ItemIdentified = reader.GetInt32("itemIdentified") != 0,
				ItemDamaged = reader.GetInt32("itemDamaged") != 0,
				StartingPrice = reader.GetInt32("startingPrice"),
				BuyoutPrice = reader.GetInt32("buyoutPrice"),
				CurrentBid = reader.GetInt32("currentBid"),
				BidderId = reader.GetInt32("bidderId"),
				BidderName = reader.GetStringSafe("bidderName") ?? string.Empty,
				Status = (World.Auctions.AuctionStatus)reader.GetInt32("status"),
				CreatedAt = reader.GetDateTime("createdAt"),
				Deadline = reader.GetDateTime("deadline"),
			};
		}

		/// <summary>
		/// Inserts a new auction row, assigning the auto-generated
		/// <c>auctionId</c> back onto <paramref name="entry"/>.
		/// </summary>
		public void InsertAuction(World.Auctions.AuctionEntry entry)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"INSERT INTO `auctions` "
				+ "(`sellerId`, `sellerName`, `itemClassId`, `itemAmount`, "
				+ " `itemRefine`, `itemIdentified`, `itemDamaged`, "
				+ " `startingPrice`, `buyoutPrice`, `currentBid`, "
				+ " `bidderId`, `bidderName`, `status`, `createdAt`, `deadline`) "
				+ "VALUES (@sellerId, @sellerName, @itemClassId, @itemAmount, "
				+ " @itemRefine, @itemIdentified, @itemDamaged, "
				+ " @startingPrice, @buyoutPrice, @currentBid, "
				+ " @bidderId, @bidderName, @status, @createdAt, @deadline); "
				+ "SELECT LAST_INSERT_ID();",
				conn);
			cmd.AddParameter("@sellerId", entry.SellerId);
			cmd.AddParameter("@sellerName", entry.SellerName ?? string.Empty);
			cmd.AddParameter("@itemClassId", entry.ItemClassId);
			cmd.AddParameter("@itemAmount", (int)entry.ItemAmount);
			cmd.AddParameter("@itemRefine", (int)entry.ItemRefine);
			cmd.AddParameter("@itemIdentified", entry.ItemIdentified ? 1 : 0);
			cmd.AddParameter("@itemDamaged", entry.ItemDamaged ? 1 : 0);
			cmd.AddParameter("@startingPrice", entry.StartingPrice);
			cmd.AddParameter("@buyoutPrice", entry.BuyoutPrice);
			cmd.AddParameter("@currentBid", entry.CurrentBid);
			cmd.AddParameter("@bidderId", entry.BidderId);
			cmd.AddParameter("@bidderName", entry.BidderName ?? string.Empty);
			cmd.AddParameter("@status", (int)entry.Status);
			cmd.AddParameter("@createdAt", entry.CreatedAt);
			cmd.AddParameter("@deadline", entry.Deadline);
			entry.Id = Convert.ToInt32(cmd.ExecuteScalar());
		}

		/// <summary>Updates an active auction's bid + bidder.</summary>
		public void UpdateAuctionBid(int auctionId, int currentBid, int bidderId, string bidderName)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"UPDATE `auctions` SET `currentBid` = @bid, "
				+ "`bidderId` = @bidderId, `bidderName` = @bidderName "
				+ "WHERE `auctionId` = @id", conn);
			cmd.AddParameter("@bid", currentBid);
			cmd.AddParameter("@bidderId", bidderId);
			cmd.AddParameter("@bidderName", bidderName ?? string.Empty);
			cmd.AddParameter("@id", auctionId);
			cmd.ExecuteNonQuery();
		}

		/// <summary>
		/// Marks an auction as resolved (Sold / Expired / Cancelled).
		/// </summary>
		public void UpdateAuctionStatus(int auctionId, World.Auctions.AuctionStatus status)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"UPDATE `auctions` SET `status` = @status WHERE `auctionId` = @id", conn);
			cmd.AddParameter("@status", (int)status);
			cmd.AddParameter("@id", auctionId);
			cmd.ExecuteNonQuery();
		}

		// ----- Homunculus persistence -----------------------------------
		// One row per character (UNIQUE on `characterId`). Replaces the
		// legacy `homun.*` keys on `vars_character`. Save is an UPSERT
		// so the same path covers both first-contract and ongoing
		// updates; delete drops the row when the contract is cleared.
		// Schema: `sql/updates/2026-05-15_001.sql`.

		/// <summary>
		/// Loads the persisted homunculus snapshot for the given
		/// character, or <c>null</c> when no row exists.
		/// </summary>
		public Sabine.Zone.World.Actors.Components.Characters.HomunculusSnapshot LoadHomunculus(int characterId)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"SELECT * FROM `homunculus` WHERE `characterId` = @cid LIMIT 1", conn);
			cmd.AddParameter("@cid", characterId);
			using var reader = cmd.ExecuteReader();
			if (!reader.Read()) return null;

			return new Sabine.Zone.World.Actors.Components.Characters.HomunculusSnapshot
			{
				Type = reader.GetInt32("type"),
				State = reader.GetInt32("state"),
				Name = reader.GetStringSafe("name") ?? string.Empty,
				Level = reader.GetInt32("level"),
				Hp = reader.GetInt32("hp"),
				HpMax = reader.GetInt32("hpMax"),
				Intimacy = reader.GetInt32("intimacy"),
				Hunger = reader.GetInt32("hunger"),
				Exp = reader.GetInt32("exp"),
				SkillPoints = reader.GetInt32("skillPoints"),
				Skills = reader.GetStringSafe("skills") ?? string.Empty,
			};
		}

		/// <summary>
		/// Upserts the homunculus snapshot for the given character.
		/// When <paramref name="snapshot"/> is null, deletes the row
		/// instead — the empty-contract case.
		/// </summary>
		public void SaveHomunculus(int characterId, Sabine.Zone.World.Actors.Components.Characters.HomunculusSnapshot snapshot)
		{
			using var conn = this.GetConnection();
			if (snapshot == null)
			{
				using var del = new MySqlCommand(
					"DELETE FROM `homunculus` WHERE `characterId` = @cid", conn);
				del.AddParameter("@cid", characterId);
				del.ExecuteNonQuery();
				return;
			}

			using var cmd = new MySqlCommand(
				"INSERT INTO `homunculus` "
				+ "(`characterId`, `type`, `state`, `name`, `level`, `hp`, `hpMax`, "
				+ " `intimacy`, `hunger`, `exp`, `skillPoints`, `skills`) "
				+ "VALUES (@cid, @type, @state, @name, @level, @hp, @hpMax, "
				+ " @intimacy, @hunger, @exp, @skillPoints, @skills) "
				+ "ON DUPLICATE KEY UPDATE "
				+ " `type` = @type, `state` = @state, `name` = @name, "
				+ " `level` = @level, `hp` = @hp, `hpMax` = @hpMax, "
				+ " `intimacy` = @intimacy, `hunger` = @hunger, "
				+ " `exp` = @exp, `skillPoints` = @skillPoints, `skills` = @skills",
				conn);
			cmd.AddParameter("@cid", characterId);
			cmd.AddParameter("@type", snapshot.Type);
			cmd.AddParameter("@state", snapshot.State);
			cmd.AddParameter("@name", snapshot.Name ?? string.Empty);
			cmd.AddParameter("@level", snapshot.Level);
			cmd.AddParameter("@hp", snapshot.Hp);
			cmd.AddParameter("@hpMax", snapshot.HpMax);
			cmd.AddParameter("@intimacy", snapshot.Intimacy);
			cmd.AddParameter("@hunger", snapshot.Hunger);
			cmd.AddParameter("@exp", snapshot.Exp);
			cmd.AddParameter("@skillPoints", snapshot.SkillPoints);
			cmd.AddParameter("@skills", snapshot.Skills ?? string.Empty);
			cmd.ExecuteNonQuery();
		}

		// ----- Castle persistence (Phase 8) -----------------------------
		// `LoadAllCastles` runs once at startup; `UpsertCastle` is
		// called by `CastleManager` whenever a script mutates state
		// via `SetCastleData`. The schema is in
		// `sql/updates/2026-05-03_003.sql`.

		/// <summary>
		/// Returns every persisted castle-state row. Castles without
		/// a row default to all-zero values when first touched.
		/// </summary>
		public List<World.Woe.CastleStateRow> LoadAllCastles()
		{
			var rows = new List<World.Woe.CastleStateRow>();
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand("SELECT * FROM `castle_state`", conn);
			using var reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				rows.Add(new World.Woe.CastleStateRow
				{
					MapStringId = reader.GetStringSafe("mapStringId"),
					OwnerGuildId = reader.GetInt32("ownerGuildId"),
					Economy = reader.GetInt32("economy"),
					Defense = reader.GetInt32("defense"),
					TriggerE = reader.GetInt32("triggerE"),
					TriggerD = reader.GetInt32("triggerD"),
					TreasureFlags = reader.GetInt32("treasureFlags"),
				});
			}
			return rows;
		}

		/// <summary>
		/// Inserts or updates a castle row. Single-row upsert keyed
		/// by <c>mapStringId</c>. Cheap enough to call on every
		/// `SetCastleData` mutation.
		/// </summary>
		public void UpsertCastle(World.Woe.CastleStateRow row)
		{
			using var conn = this.GetConnection();
			using var cmd = new MySqlCommand(
				"INSERT INTO `castle_state` (`mapStringId`, `ownerGuildId`, `economy`, `defense`, `triggerE`, `triggerD`, `treasureFlags`) "
				+ "VALUES (@mapStringId, @ownerGuildId, @economy, @defense, @triggerE, @triggerD, @treasureFlags) "
				+ "ON DUPLICATE KEY UPDATE `ownerGuildId` = @ownerGuildId, `economy` = @economy, "
				+ "`defense` = @defense, `triggerE` = @triggerE, `triggerD` = @triggerD, "
				+ "`treasureFlags` = @treasureFlags",
				conn);
			cmd.AddParameter("@mapStringId", row.MapStringId);
			cmd.AddParameter("@ownerGuildId", row.OwnerGuildId);
			cmd.AddParameter("@economy", row.Economy);
			cmd.AddParameter("@defense", row.Defense);
			cmd.AddParameter("@triggerE", row.TriggerE);
			cmd.AddParameter("@triggerD", row.TriggerD);
			cmd.AddParameter("@treasureFlags", row.TreasureFlags);
			cmd.ExecuteNonQuery();
		}

	}
}

using System;
using System.Threading.Tasks;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Shared.L10N;
using Sabine.Shared.Util;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.Scripting.Dialogues;
using Sabine.Zone.World.Actors.Components.Characters;
using Sabine.Zone.World.Groups;
using Yggdrasil.Collections;
using Yggdrasil.Logging;
using Yggdrasil.Util;
using static Sabine.Shared.Util.TaskHelper;

namespace Sabine.Zone.World.Actors
{
	/// <summary>
	/// Represents a player character.
	/// </summary>
	public partial class PlayerCharacter : Character
	{
		private readonly object _visibilitySyncLock = new();
		private readonly InOutTracker<IActor> _visibleActors = new();

		/// <summary>
		/// Gets or sets the connection that controls this player.
		/// </summary>
		public ZoneConnection Connection { get; internal set; } = new DummyConnection();

		/// <summary>
		/// Returns the character's variable container.
		/// </summary>
		public VariableContainer Vars { get; } = new VariableContainer();

		/// <summary>
		/// Returns a reference to the character's inventory.
		/// </summary>
		public Inventory Inventory { get; }

		/// <summary>
		/// Returns the player's account-bound storage. Loaded at character
		/// load and persisted by account id.
		/// </summary>
		public Storage Storage { get; }

		/// <summary>
		/// Returns a reference to the character's quest list.
		/// </summary>
		public QuestList Quests { get; }

		/// <summary>
		/// Equipment-flavored alias for <see cref="Inventory"/>. Lets converted
		/// scripts read <c>player.Equipment.IsEquipped(...)</c> the way rAthena
		/// expresses it without forcing a separate component.
		/// </summary>
		public Inventory Equipment => this.Inventory;

		/// <summary>
		/// Returns this character's username.
		/// </summary>
		public override string Username => this.Connection.Account.Username;

		/// <summary>
		/// Returns the character's handle.
		/// </summary>
		public override int Handle
		{
			get => this.Connection.Account.Id;
			protected set => throw new NotSupportedException();
		}

		/// <summary>
		/// Gets or sets this character's id.
		/// </summary>
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets this character's name.
		/// </summary>
		public override string Name { get; set; }

		/// <summary>
		/// Returns this character's/account's sex.
		/// </summary>
		public override Sex Sex => this.Connection.Account.Sex;

		/// <summary>
		/// Gets or sets this character's job.
		/// </summary>
		public JobId JobId { get; private set; }

		/// <summary>
		/// Returns a reference to the character's job's data.
		/// </summary>
		public JobData JobData { get; private set; }

		/// <summary>
		/// Returns the character's class id, which is equal to its
		/// current job id.
		/// </summary>
		public override IdentityId IdentityId
		{
			get => (IdentityId)this.JobId;
			protected set => throw new NotSupportedException();
		}

		/// <summary>
		/// Returns true if character is warping to a new location.
		/// </summary>
		public bool IsWarping { get; private set; }

		/// <summary>
		/// Returns the position the character is warping towards while
		/// IsWarping is true.
		/// </summary>
		public Location WarpLocation { get; private set; }

		/// <summary>
		/// Gets or sets the character's save location, where they respawn
		/// upon death.
		/// </summary>
		public Location SaveLocation { get; set; }

		/// <summary>
		/// Gets or sets whether the character is currently observing
		/// its surroundings, actively updating the visible entities.
		/// </summary>
		public bool IsObserving { get; protected set; }

		/// <summary>
		/// Gets or sets the player's selected language.
		/// </summary>
		public string SelectedLanguage
		{
			get => _selectedLanguage;
			set
			{
				_selectedLanguage = value;
				this.Localizer = ZoneServer.Instance.Localization.Get(value);
			}
		}
		private string _selectedLanguage;

		/// <summary>
		/// Returns the localizer for the player's selected language.
		/// </summary>
		public Localizer Localizer
		{
			get
			{
				if (_localizer == null)
					_localizer = ZoneServer.Instance.Localization.GetDefault();

				return _localizer;
			}
			private set => _localizer = value;
		}
		private Localizer _localizer;

		/// <summary>
		/// Returns a reference to the character's party, if any.
		/// </summary>
		public Party Party { get; set; }

		/// <summary>
		/// Returns a reference to the character's guild, if any.
		/// </summary>
		public Guild Guild { get; set; }

		/// <summary>
		/// Returns the character's guild ID, or 0 if not in a guild.
		/// </summary>
		public override int GuildId
		{
			get => Guild?.Id ?? 0;
			protected set => throw new NotSupportedException("GuildId is derived from Guild membership.");
		}

		/// <summary>
		/// UTC time until which the character is muted, if any. Public chat
		/// from this character is dropped while this is in the future.
		/// </summary>
		public DateTime? MutedUntil { get; set; }

		/// <summary>
		/// Character id this character is married to, if any.
		/// </summary>
		public int? MarriedToCharId { get; set; }

		/// <summary>
		/// Renewal-style cosmetic body style index. Stored verbatim; the
		/// alpha client ignores it, but the value persists.
		/// </summary>
		public int BodyStyle { get; set; }

		/// <summary>
		/// Returns true if the character is currently muted.
		/// </summary>
		public bool IsMuted => this.MutedUntil.HasValue && this.MutedUntil.Value > DateTime.UtcNow;

		/// <summary>
		/// Returns the character's party ID, or 0 if not in a party.
		/// </summary>
		public int PartyId => Party?.Id ?? 0;

		/// <summary>
		/// Gets or sets the id of the chat room the character is in.
		/// </summary>
		public int ChatRoomId { get; set; }

		/// <summary>
		/// Gets or sets the player's currently open vending shop, if any.
		/// </summary>
		public Sabine.Zone.World.Shops.VendingShop VendingShop { get; set; }

		/// <summary>
		/// Gets or sets the item class id currently designated as ammo
		/// for the character.
		/// </summary>
		public int AmmoClassId { get; set; }

		/// <summary>
		/// Homunculus-state bookkeeping for the player. Created with
		/// the player; Type=None means no contract yet.
		/// </summary>
		public HomunculusComponent Homunculus { get; private set; }

		/// <summary>
		/// Creates a new character.
		/// </summary>
		public PlayerCharacter(JobId jobId)
		{
			this.JobId = jobId;
			this.Inventory = new Inventory(this);
			this.Storage = new Storage(this);
			this.Quests = new QuestList(this);

			this.Parameters = new PlayerCharacterParameters(this);
			this.Components.Add(new RegenComponent(this));
			this.Homunculus = new HomunculusComponent(this);
			this.Components.Add(this.Homunculus);

			this.LoadJobData(jobId);
		}

		/// <summary>
		/// Loads the data for the given job.
		/// </summary>
		/// <param name="jobId"></param>
		/// <exception cref="ArgumentException"></exception>
		private void LoadJobData(JobId jobId)
		{
			if (!ZoneServer.Instance.Data.Jobs.TryFind(jobId, out var jobData))
				throw new ArgumentException($"No data found for job {jobId}.");

			this.JobData = jobData;
		}

		/// <summary>
		/// Sends a server message to the character's client that is
		/// displayed in the chat log.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public void ServerMessage(string format, params object[] args)
		{
			if (args.Length > 0)
				format = string.Format(format, args);

			var message = string.Format(Localization.Get("[Server] : {0}"), format);

			Send.ZC_NOTIFY_CHAT(this, 0, message);
		}

		/// <summary>
		/// Sends a debug message to the character's client that is
		/// displayed in the chat log.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public void DebugMessage(string format, params object[] args)
		{
			if (args.Length > 0)
				format = string.Format(format, args);

			var message = string.Format(Localization.Get("[Debug] : {0}"), format);

			Send.ZC_NOTIFY_CHAT(this, 0, message);
		}

		/// <summary>
		/// Warps character to given location.
		/// </summary>
		/// <param name="location"></param>
		public override void Warp(Location location)
		{
			if (this.IsWarping)
			{
				if (location != this.WarpLocation)
					throw new InvalidOperationException("A warp is already in progress.");

				// If we get two warp calls for the same location, we're going
				// to assume it was accidental and ignore the second one.
				Log.Debug("Encountered a double warp. Stacktrace: {0}", Environment.StackTrace);
				return;
			}

			if (!ZoneServer.Instance.World.Maps.TryGet(location.MapId, out var map))
			{
				Log.Warning("Warp: Map '{0}' not loaded; falling back to default start location for '{1}'.", location.MapId, this.Name);

				var charConf = ZoneServer.Instance.Conf.Char;
				var mapsDb = ZoneServer.Instance.Data.Maps;

				if (!StartLocation.TryGetDefault(mapsDb, charConf.StartMapStringId, charConf.StartPosition, out location)
					|| !ZoneServer.Instance.World.Maps.TryGet(location.MapId, out map))
				{
					throw new ArgumentException($"Map '{location.MapId}' not found and no fallback available.");
				}

				this.SaveLocation = location;
			}

			this.IsWarping = true;
			this.WarpLocation = location;

			this.CancelAction();
			this.StopObserving();

			// Detach the homunculus before the map switch — the
			// entity needs to come off the source map. The contract
			// persists via HomunculusComponent so the player can
			// recall after arriving.
			if (this.Homunculus?.IsActive == true)
				Sabine.Zone.Skills.Homunculi.HomunculusService.Detach(this);

			Send.ZC_NPCACK_MAPMOVE(this, map.StringId, location.Position);
		}

		/// <summary>
		/// Finalizes a warp, actually moving the character to the
		/// new location.
		/// </summary>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public void FinalizeWarp()
		{
			if (!this.IsWarping)
				throw new InvalidOperationException("No warp in process that could be finalized.");

			if (!ZoneServer.Instance.World.Maps.TryGet(this.WarpLocation.MapId, out var map))
				throw new ArgumentException($"Map '{this.WarpLocation.MapId}' not found.");

			this.Map.RemovePlayer(this);
			this.SetLocation(this.WarpLocation);
			map.AddPlayer(this);

			this.IsWarping = false;
			this.StartObserving();
		}

		/// <summary>
		/// Starts updating of visible entities. A visibility update is
		/// executed when this method is called.
		/// </summary>
		public void StartObserving()
		{
			lock (_visibilitySyncLock)
			{
				if (this.IsObserving)
					return;

				this.IsObserving = true;
				this.UpdateVisibility();
			}
		}

		/// <summary>
		/// Stops updating of visible entities. A visibility update is
		/// executed when this method is called.
		/// </summary>
		public void StopObserving()
		{
			lock (_visibilitySyncLock)
			{
				if (!this.IsObserving)
					return;

				this.IsObserving = false;
				this.RemoveVisibleActors();
			}
		}

		/// <summary>
		/// Updates visible entities around character.
		/// </summary>
		internal void UpdateVisibility()
		{
			lock (_visibilitySyncLock)
			{
				if (!this.IsObserving)
					return;

				_visibleActors.Begin();

				this.Map.GetVisibleActors(this, _visibleActors.UpdateList);

				_visibleActors.Update();

				foreach (var actor in _visibleActors.Added)
				{
					switch (actor)
					{
						case Character character:
						{
							Send.ZC_NOTIFY_STANDENTRY(this, character);

							// TODO: Cache chat ownership on player?
							if (character is PlayerCharacter player)
							{
								if (player.ChatRoomId != 0 && ZoneServer.Instance.World.ChatRooms.TryGet(player.ChatRoomId, out var room))
								{
									if (room.IsOwner(player))
										Send.ZC_ROOM_NEWENTRY(room);
								}
							}

							break;
						}

						case Item item:
						{
							Send.ZC_ITEM_ENTRY(this, item);
							break;
						}
					}
				}

				foreach (var actor in _visibleActors.Removed)
				{
					if (actor is Item)
						Send.ZC_ITEM_DISAPPEAR(this, actor.Handle);
					else
						Send.ZC_NOTIFY_VANISH(this, actor.Handle, DisappearType.Vanish);
				}

				_visibleActors.End();
			}
		}

		/// <summary>
		/// Adds actor to list of character's visible entities without
		/// updating the client.
		/// </summary>
		/// <remarks>
		/// AddVisibleEntity and RemoveVisibleEntity are to be used in
		/// cases where an outside source needs to control an entity's
		/// appear or disappear packets.
		/// </remarks>
		/// <param name="actor"></param>
		internal void AddVisibleActor(IActor actor)
		{
			if (!this.IsObserving)
				return;

			lock (_visibilitySyncLock)
				_visibleActors.InjectItem(actor);
		}

		/// <summary>
		/// Removes entity from list of character's visible entities
		/// without updating the client.
		/// </summary>
		/// <remarks>
		/// AddVisibleEntity and RemoveVisibleEntity are to be used in
		/// cases where an outside source needs to control an entity's
		/// appear or disappear packets.
		/// </remarks>
		/// <param name="actor"></param>
		internal void RemoveVisibleActor(IActor actor)
		{
			lock (_visibilitySyncLock)
			{
				if (!this.IsObserving)
					return;

				_visibleActors.EjectItem(actor);
			}
		}

		/// <summary>
		/// Clears the list of visible actors and updates the client.
		/// </summary>
		private void RemoveVisibleActors()
		{
			lock (_visibilitySyncLock)
			{
				foreach (var actor in _visibleActors.Current)
				{
					if (actor is Item)
						Send.ZC_ITEM_DISAPPEAR(this, actor.Handle);
					else
						Send.ZC_NOTIFY_VANISH(this, actor.Handle, DisappearType.Vanish);
				}

				_visibleActors.ClearItems();
			}
		}

		/// <summary>
		/// Returns true if the character is able to equip the given item,
		/// based on its level and job requirements.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool CanEquip(Item item)
		{
			if (item.Data.SexAllowed != Sex.Any && item.Data.SexAllowed != this.Sex)
				return false;

			// There's no mention of required levels in the GameFAQs
			// alpha guide. Did they not exist? Maybe finding enough
			// Zeny was challenge enough?
			if (this.Parameters.BaseLevel < item.Data.RequiredLevel)
				return false;

			if (!this.JobId.Matches(item.Data.JobsAllowed))
				return false;

			return true;
		}

		/// <summary>
		/// Changes the character's look and updates the client.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="lookId"></param>
		public void ChangeLook(SpriteType type, int lookId)
		{
			switch (type)
			{
				case SpriteType.Hair: this.HairId = lookId; break;
				case SpriteType.Weapon: this.WeaponLook = lookId; break;
				case SpriteType.HeadTop: this.HeadTopLook = lookId; break;
				case SpriteType.HeadMiddle: this.HeadMiddleLook = lookId; break;
				case SpriteType.HeadBottom: this.HeadBottomLook = lookId; break;
				default:
					throw new ArgumentException($"Unsupported type '{type}'.");
			}

			if (Game.Version < Versions.S500)
				Send.ZC_SPRITE_CHANGE(this, type, lookId);
			else
				Send.ZC_SPRITE_CHANGE2(this, type, lookId, 0);
		}

		/// <summary>
		/// Changes character's job and updates the client.
		/// </summary>
		/// <param name="jobId"></param>
		public void ChangeJob(JobId jobId)
		{
			this.JobId = jobId;
			this.LoadJobData(jobId);

			// Reset Job Level and Job Exp for the new job
			this.Parameters.JobLevel = 1;
			this.Parameters.JobExp = 0;

			// Get the new EXP requirements for Level 1 of this new job
			if (this.Parameters is PlayerCharacterParameters pcParams)
				pcParams.RecalculateExp();

			this.Inventory.CheckEquipRequirements();
			this.Inventory.RefreshClient();

			// This will now calculate bonuses based on the new Job ID and Job Level 1
			this.Parameters.RecalculateAll();

			this.Heal();

			// Visual change
			if (Game.Version < Versions.S500)
				Send.ZC_SPRITE_CHANGE(this, SpriteType.Class, (int)jobId);
			else
				Send.ZC_SPRITE_CHANGE2(this, SpriteType.Class, (int)jobId, 0);

			// Update client with new stats (Job Level 1, new Exp requirements, new HP/SP)
			Send.ZC_STATUS(this);

			// Specific hack for Alpha/Beta clients:
			// Send a BaseLevel change packet to force a "Level Up" animation to play 
			// as a visual indicator of the job change.
			Send.ZC_PAR_CHANGE(this, ParameterType.BaseLevel);

			this.Skills.UpdateClassSkills();
		}

		/// <summary>
		/// Returns the character's current attack range, based on its
		/// state and equipped items.
		/// </summary>
		/// <returns></returns>
		public override int GetAttackRange()
		{
			// Range is 3 for normal attacks and 16 for ranged
			// in the alpha client. This is hardcoded, based on
			// the type of the item that was equipped.
			if (Game.Version < Versions.Beta1)
			{
				if (this.Inventory.RightHand?.Type == ItemType.RangedWeapon)
					return 16;

				return 3;
			}

			// If you're coming here to check on the attack range of bows
			// in Beta1, I can tell you that the behavior you're
			// witnessing appears correct. While their range was hardcoded
			// to 16 in the alpha, all the data I saw suggests that all
			// bows had a range of 5 in the beta. That's what 2003 servers
			// used and the db websites of the time don't mention ranges
			// or range differences. But the range can be changed in the
			// item data, so all is good in the world.

			return this.Inventory.RightHand?.Data.AttackRange ?? 3;
		}

		/// <summary>
		/// Increases the character's exp by the given amount and levels
		/// them up if possible.
		/// </summary>
		/// <param name="amount"></param>
		public void GainBaseExp(int amount)
		{
			var exp = this.Parameters.BaseExp;
			var level = this.Parameters.BaseLevel;
			var expNeeded = this.Parameters.BaseExpNeeded;
			var maxLevel = ZoneServer.Instance.Data.ExpTables.GetMaxLevel(ExpTableType.Base, this.JobId);
			var levelsGained = 0;
			var statPointsGained = 0;

			exp = Math.Max(0, Math2.AddChecked(exp, amount));

			while (level < maxLevel && exp >= expNeeded)
			{
				exp -= expNeeded;

				level++;
				levelsGained++;
				statPointsGained += (level / 5) + 2;

				expNeeded = ZoneServer.Instance.Data.ExpTables.GetExpNeeded(ExpTableType.Base, this.JobId, level);
			}

			if (levelsGained != 0)
			{
				this.Parameters.Set(ParameterType.BaseLevel, level);
				this.Parameters.Set(ParameterType.BaseExpNeeded, expNeeded);
				this.Parameters.Modify(ParameterType.StatPoints, statPointsGained);
				this.Parameters.Modify(ParameterType.SkillPoints, levelsGained);

				// SkillPoints changed; resend skill list so the client
				// recomputes each skill's "upgradable" flag.
				this.Skills.RefreshClient();
			}

			this.Parameters.Set(ParameterType.BaseExp, exp);
		}

		/// <summary>
		/// Increases the character's exp by the given amount and levels
		/// them up if possible.
		/// </summary>
		/// <param name="amount"></param>
		public void GainJobExp(int amount)
		{
			// Don't give any job EXP if the feature is disabled
			if (!ZoneServer.Instance.Data.Features.IsEnabled(FeatureId.JobLevels))
				return;

			var exp = this.Parameters.JobExp;
			var level = this.Parameters.JobLevel;
			var expNeeded = this.Parameters.JobExpNeeded;
			var maxLevel = ZoneServer.Instance.Data.ExpTables.GetMaxLevel(ExpTableType.Job, this.JobId);

			// Prevent leveling past max level
			if (level >= maxLevel)
			{
				// Ensure exp doesn't accumulate indefinitely at max level
				if (exp > 0)
				{
					this.Parameters.Set(ParameterType.JobExp, 0);
					this.Parameters.Set(ParameterType.JobExpNeeded, 0);
				}
				return;
			}

			var levelsGained = 0;

			exp = Math2.AddChecked(exp, amount);
			if (exp < 0)
				exp = 0;

			// Level up loop
			while (level < maxLevel && exp >= expNeeded && expNeeded > 0)
			{
				exp -= expNeeded;

				level++;
				levelsGained++;

				expNeeded = ZoneServer.Instance.Data.ExpTables.GetExpNeeded(ExpTableType.Job, this.JobId, level);
			}

			if (levelsGained != 0)
			{
				// Update internal parameters
				this.Parameters.Set(ParameterType.JobLevel, level);
				this.Parameters.Set(ParameterType.JobExpNeeded, expNeeded);
				this.Parameters.Modify(ParameterType.SkillPoints, levelsGained);

				// NOTIFICATION:
				// 1. Send the Job Level update packet. 
				// On most clients, this triggers the "Job Level Up" angel effect and sound.
				// The original code noted Alpha clients might not update the UI, 
				// but sending the packet is still the correct protocol action.
				Send.ZC_PAR_CHANGE(this, ParameterType.JobLevel);

				// 2. Send Chat Message
				this.ServerMessage(Localization.Get("You have reached job level {0}."), level);

				// 3. Recalculate derived stats (Job Bonuses)
				this.Parameters.RecalculateAll();

				// SkillPoints changed; resend skill list so the client
				// recomputes each skill's "upgradable" flag.
				this.Skills.RefreshClient();
			}

			this.Parameters.Set(ParameterType.JobExp, exp);
		}

		/// <summary>
		/// Restores characters HP and SP and heals any negative status
		/// effects.
		/// </summary>
		public void Heal()
		{
			this.Heal(this.Parameters.HpMax, this.Parameters.SpMax);
		}

		/// <summary>
		/// Heals the given amount of HP and SP.
		/// </summary>
		/// <param name="amountHp"></param>
		/// <param name="amountSp"></param>
		public void Heal(int amountHp, int amountSp)
		{
			this.HealHp(amountHp);
			this.HealSp(amountSp);
		}

		/// <summary>
		/// Heals the given amount of HP.
		/// </summary>
		/// <param name="amount"></param>
		public void HealHp(int amount)
		{
			this.Parameters.Modify(ParameterType.Hp, amount);
		}

		/// <summary>
		/// Heals the given amount of SP.
		/// </summary>
		/// <param name="amount"></param>
		public void HealSp(int amount)
		{
			this.Parameters.Modify(ParameterType.Sp, amount);
		}

		/// <summary>
		/// Heals a percentage of the character's max HP and SP. Mirrors
		/// rAthena's <c>percentheal</c>. Negative values damage instead
		/// of heal. Values are clamped to [-100, 100].
		/// </summary>
		/// <param name="hpPercent">Percentage of max HP to restore (or remove, if negative).</param>
		/// <param name="spPercent">Percentage of max SP to restore (or remove, if negative).</param>
		public void HealPercent(int hpPercent, int spPercent)
		{
			hpPercent = Math.Clamp(hpPercent, -100, 100);
			spPercent = Math.Clamp(spPercent, -100, 100);

			if (hpPercent != 0)
			{
				var hpAmount = (int)((long)this.Parameters.HpMax * hpPercent / 100);
				this.HealHp(hpAmount);
			}

			if (spPercent != 0)
			{
				var spAmount = (int)((long)this.Parameters.SpMax * spPercent / 100);
				this.HealSp(spAmount);
			}
		}

		/// <summary>
		/// True if movement requests from this player should be ignored.
		/// Set via <see cref="SetMovementBlock"/> / rAthena's
		/// <c>setpcblock(PCBLOCK_MOVE, 1)</c>.
		/// </summary>
		public bool MovementBlocked { get; private set; }

		/// <summary>
		/// True if skill use requests from this player should be ignored.
		/// Set via <see cref="SetSkillBlock"/> / rAthena's
		/// <c>setpcblock(PCBLOCK_SKILL, 1)</c>.
		/// </summary>
		public bool SkillBlocked { get; private set; }

		/// <summary>
		/// Toggles the movement block flag for this player.
		/// </summary>
		public void SetMovementBlock(bool blocked)
		{
			this.MovementBlocked = blocked;
		}

		/// <summary>
		/// Toggles the skill block flag for this player.
		/// </summary>
		public void SetSkillBlock(bool blocked)
		{
			this.SkillBlocked = blocked;
		}

		/// <summary>
		/// Sets a marker on the player's minimap. Stub: alpha-era
		/// clients have no compass/viewpoint packet, so this is a
		/// no-op + debug log. Mirrors rAthena's <c>viewpoint</c>.
		/// </summary>
		/// <param name="id">Marker id.</param>
		/// <param name="pos">Map position.</param>
		/// <param name="color">Marker color (RGB int).</param>
		public void SetMinimapMark(int id, MapPos pos, int color)
		{
			Log.Debug("PlayerCharacter.SetMinimapMark: stubbed for alpha client (player='{0}', id={1}, pos={2}, color=0x{3:X6}).", this.Name, id, pos, color);
		}

		/// <summary>
		/// Kills the character.
		/// </summary>
		/// <param name="killer"></param>
		public override void Kill(Character killer)
		{
			base.Kill(killer);

			if (this.Homunculus?.IsActive == true)
				Sabine.Zone.Skills.Homunculi.HomunculusService.Detach(this);

			Send.ZC_NOTIFY_VANISH(this, DisappearType.StrikedDead);
		}

		/// <summary>
		/// Starts a dialog between the character and the given NPC.
		/// </summary>
		/// <param name="npc"></param>
		public void StartDialog(Npc npc)
			=> this.StartDialog(npc, npc.DialogFunc);

		/// <summary>
		/// Starts a dialog between the character and the given NPC, using
		/// the given talk function, instead of the NPC's default one.
		/// </summary>
		/// <param name="npc"></param>
		/// <param name="dialogFunc"></param>
		public void StartDialog(Npc npc, DialogFunc dialogFunc)
		{
			if (npc == null)
			{
				throw new InvalidOperationException("Starting a remote dialog with an null NPC");
			}
			if (dialogFunc == null)
			{
				throw new InvalidOperationException($"NPC '{npc.Name}' doesn't have a dialog function assigned to it.");
			}
			async Task RunNpcDialogAsync()
			{
				await using (var dialog = new Dialog(this, npc))
				{
					try
					{
						dialog.State = DialogState.Active;

						// Execute the provided script logic.
						await dialogFunc(dialog);
					}
					catch (OperationCanceledException)
					{
						// This is a normal exit path when a dialog is closed by the script or player.
					}
					catch (Exception ex)
					{
						// This is the error handling that was previously in Dialog.Start.
						Log.Error($"An exception occurred during an NPC dialog for '{this.Name}' with NPC '{npc?.Name}'. Error: {ex}");
					}
					finally
					{
						if (dialog.GetLastAction() == DialogActionType.Message /*&& isAlpha*/)
						{
							await dialog.Next();
							dialog.Close();
						}
						else if (dialog.GetLastAction() == DialogActionType.Input)
						{
							dialog.Close();
						}
						dialog.State = DialogState.Ended;
					}
				}
			}

			CallSafe(RunNpcDialogAsync());
		}
	}

	/// <summary>
	/// Map position struct for minimap markers and similar APIs that
	/// need a (map, x, y) tuple. Map is identified by string id (the
	/// rAthena-friendly form).
	/// </summary>
	public readonly struct MapPos
	{
		public string MapStringId { get; }
		public int X { get; }
		public int Y { get; }

		public MapPos(string mapStringId, int x, int y)
		{
			this.MapStringId = mapStringId;
			this.X = x;
			this.Y = y;
		}

		public override string ToString() => $"{this.MapStringId} ({this.X},{this.Y})";
	}
}

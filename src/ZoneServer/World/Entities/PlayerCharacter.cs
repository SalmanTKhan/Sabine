using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Shared.Data.Databases;
using Sabine.Shared.L10N;
using Sabine.Shared.Util;
using Sabine.Shared.World;
using Sabine.Zone.Network;
using Sabine.Zone.Scripting.Dialogues;
using Sabine.Zone.World.Entities.Components.Characters;
using Sabine.Zone.World.Groups;
using Yggdrasil.Logging;
using Yggdrasil.Util;
using static Sabine.Shared.Util.TaskHelper;

namespace Sabine.Zone.World.Entities
{
	/// <summary>
	/// Represents a player character.
	/// </summary>
	public partial class PlayerCharacter : Character
	{
		private readonly object _visibilityUpdateSyncLock = new();
		private readonly HashSet<int> _visibleEntities = new();

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
		/// Returns a reference to the character's skill component.
		/// </summary>
		public SkillComponent Skills { get; }

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
		public override int ClassId
		{
			get => (int)this.JobId;
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
		/// Returns the character's party ID, or 0 if not in a party.
		/// </summary>
		public int PartyId => Party?.Id ?? 0;

		/// <summary>
		/// Creates a new character.
		/// </summary>
		public PlayerCharacter(JobId jobId)
		{
			this.JobId = jobId;
			this.Inventory = new Inventory(this);

			this.Parameters = new PlayerCharacterParameters(this);
			this.Components.Add(new RegenComponent(this));

			this.LoadJobData(jobId);

			this.Components.Add(this.Skills = new SkillComponent(this));
			this.Components.Add(new RecoveryComponent(this));
		}

		/// <summary>
		/// Loads the data for the given job.
		/// </summary>
		/// <param name="jobId"></param>
		/// <exception cref="ArgumentException"></exception>
		private void LoadJobData(JobId jobId)
		{
			if (!SabineData.Jobs.TryFind(jobId, out var jobData))
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
				throw new ArgumentException($"Map '{location.MapId}' not found.");

			this.IsWarping = true;
			this.WarpLocation = location;

			this.CancelAction();
			this.StopObserving();

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

			this.Map.RemoveCharacter(this);
			this.SetLocation(this.WarpLocation);
			map.AddCharacter(this);

			this.IsWarping = false;
			this.StartObserving();
		}

		/// <summary>
		/// Updates character and its components.
		/// </summary>
		/// <param name="elapsed"></param>
		public override void Update(TimeSpan elapsed)
		{
			base.Update(elapsed);
			this.UpdateVisibility();
			this.UpdateAttackAction();
		}

		/// <summary>
		/// Starts updating of visible entities. A visibility update is
		/// executed when this method is called.
		/// </summary>
		public void StartObserving()
		{
			lock (_visibilityUpdateSyncLock)
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
			lock (_visibilityUpdateSyncLock)
			{
				if (!this.IsObserving)
					return;

				this.IsObserving = false;
				this.RemoveVisibleEntities();
			}
		}

		/// <summary>
		/// Updates visible entities around character.
		/// </summary>
		public void UpdateVisibility()
		{
			if (!this.IsObserving)
				return;

			var visibleEntities = this.Map.GetVisibleEntities(this);

			lock (_visibilityUpdateSyncLock)
			{
				var appeared = visibleEntities.Where(a => !_visibleEntities.Contains(a.Handle));
				var disappeared = _visibleEntities.Where(a => !visibleEntities.Exists(b => b.Handle == a));

				foreach (var entity in appeared)
				{
					if (entity == this)
						continue;

					switch (entity)
					{
						case Character character: Send.ZC_NOTIFY_STANDENTRY(this, character); break;
						case Item item: Send.ZC_ITEM_ENTRY(this, item); break;
					}
				}

				foreach (var handle in disappeared)
				{
					if (handle == this.Handle)
						continue;

					if (handle < 0x6000_0000)
						Send.ZC_NOTIFY_VANISH(this, handle, DisappearType.Vanish);
					else
						Send.ZC_ITEM_DISAPPEAR(this, handle);
				}

				// To remember the visible entities for the next run we store
				// their ids. There might be some cases where it would be
				// useful to have the actual references, but we can still
				// get those if we need to, and this way there's no chance
				// for any memory leaks because we're storing objects
				// that reference each other.

				_visibleEntities.Clear();
				_visibleEntities.UnionWith(visibleEntities.Select(a => a.Handle));
			}
		}

		/// <summary>
		/// Adds entity to list of character's visible entities without
		/// updating the client.
		/// </summary>
		/// <remarks>
		/// AddVisibleEntity and RemoveVisibleEntity are to be used in
		/// cases where an outside source needs to control an entity's
		/// appear or disappear packets.
		/// </remarks>
		/// <param name="handle"></param>
		internal void AddVisibleEntity(IEntity entity)
		{
			if (!this.IsObserving)
				return;

			lock (_visibilityUpdateSyncLock)
				_visibleEntities.Add(entity.Handle);
		}

		/// <summary>
		/// Removes entity from list of character's visible entities without
		/// updating the client.
		/// </summary>
		/// <param name="entity"></param>
		internal void RemoveVisibleEntity(IEntity entity)
		{
			if (!this.IsObserving)
				return;

			lock (_visibilityUpdateSyncLock)
				_visibleEntities.Remove(entity.Handle);
		}

		/// <summary>
		/// Clears the list of visible entities and updates the client.
		/// </summary>
		private void RemoveVisibleEntities()
		{
			lock (_visibilityUpdateSyncLock)
			{
				foreach (var handle in _visibleEntities)
					Send.ZC_NOTIFY_VANISH(this, handle, DisappearType.Vanish);

				_visibleEntities.Clear();
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
				case SpriteType.Weapon: this.WeaponId = lookId; break;
				default:
					throw new ArgumentException($"Unsupported type '{type}'.");
			}

			Send.ZC_SPRITE_CHANGE(this, type, lookId);
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
			Send.ZC_SPRITE_CHANGE(this, SpriteType.Class, (int)jobId);

			// Update client with new stats (Job Level 1, new Exp requirements, new HP/SP)
			Send.ZC_STATUS(this);

			// Specific hack for Alpha/Beta clients:
			// Send a BaseLevel change packet to force a "Level Up" animation to play 
			// as a visual indicator of the job change.
			Send.ZC_PAR_CHANGE(this, ParameterType.BaseLevel);
		}

		/// <summary>
		/// Returns the character's current attack range, based on its
		/// state and equipped items.
		/// </summary>
		/// <returns></returns>
		public int GetAttackRange()
		{
			// Range is 3 for normal attacks and 16 for ranged
			// in the alpha client. This is hardcoded, based on
			// the type of the item that was equipped.

			if (this.Inventory.RightHand?.Type == ItemType.RangedWeapon)
				return 16;

			return 3;
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
			var maxLevel = SabineData.ExpTables.GetMaxLevel(ExpTableType.Base, this.JobId);
			var levelsGained = 0;
			var statPointsGained = 0;

			exp = Math.Max(0, Math2.AddChecked(exp, amount));

			while (level < maxLevel && exp >= expNeeded)
			{
				exp -= expNeeded;

				level++;
				levelsGained++;
				statPointsGained += (level / 5) + 2;

				expNeeded = SabineData.ExpTables.GetExpNeeded(ExpTableType.Base, this.JobId, level);
			}

			if (levelsGained != 0)
			{
				this.Parameters.Set(ParameterType.BaseLevel, level);
				this.Parameters.Set(ParameterType.BaseExpNeeded, expNeeded);
				this.Parameters.Modify(ParameterType.StatPoints, statPointsGained);
				this.Parameters.Modify(ParameterType.SkillPoints, levelsGained);
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
			if (!SabineData.Features.IsEnabled(FeatureId.JobLevels))
				return;

			var exp = this.Parameters.JobExp;
			var level = this.Parameters.JobLevel;
			var expNeeded = this.Parameters.JobExpNeeded;
			var maxLevel = SabineData.ExpTables.GetMaxLevel(ExpTableType.Job, this.JobId);

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
			if (exp < 0) exp = 0;

			// Level up loop
			while (level < maxLevel && exp >= expNeeded && expNeeded > 0)
			{
				exp -= expNeeded;

				level++;
				levelsGained++;

				expNeeded = SabineData.ExpTables.GetExpNeeded(ExpTableType.Job, this.JobId, level);
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
		/// Kills the character.
		/// </summary>
		/// <param name="killer"></param>
		public override void Kill(Character killer)
		{
			base.Kill(killer);

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
}

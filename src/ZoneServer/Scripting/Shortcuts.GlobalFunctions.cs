using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Shared.Data.Databases;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Groups;
using Sabine.Zone.World.Instances;
using Sabine.Zone.Scripting.Dialogues;
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
		private static readonly ConcurrentDictionary<string, Func<Dialog, Task>> DialogTemplates
			= new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Registers a generated rAthena <c>Name::ref</c> dialog template.
		/// Duplicates may be declared in another source file, so callbacks are
		/// resolved when a player opens the clone rather than at script-load
		/// time.
		/// </summary>
		public static void RegisterDialogTemplate(string reference, Func<Dialog, Task> callback)
		{
			if (!string.IsNullOrWhiteSpace(reference) && callback != null)
				DialogTemplates[reference] = callback;
		}

		/// <summary>Invokes a generated cross-file <c>duplicate(ref)</c> template.</summary>
		public static Task RunDialogTemplate(string reference, Dialog dialog)
		{
			if (!string.IsNullOrWhiteSpace(reference)
				&& DialogTemplates.TryGetValue(reference, out var callback))
				return callback(dialog);

			Log.Warning("rAthena duplicate template '{0}' was not registered.", reference);
			return Task.CompletedTask;
		}

		/// <summary>
		/// rAthena's <c>F_Okolnir</c> shared quest helper. The source uses
		/// <c>strnpcinfo(4)</c> as the castle-script key; Sabine resolves the
		/// equivalent map identifier from the context actor.
		/// </summary>
		public static string OkolnirSubName(Npc npc)
		{
			var npcName = npc?.Name ?? string.Empty;
			if (npcName.Contains("main", StringComparison.OrdinalIgnoreCase))
				return string.Empty;

			// rAthena's strnpcinfo(4) is the NPC's map identifier here
			// (for example arug_cas01), not the visible display name.
			var mapName = npc?.Map?.StringId ?? string.Empty;
			return mapName.Contains("cas", StringComparison.OrdinalIgnoreCase)
				&& mapName.Length >= 10
				? mapName.Substring(0, 2) + mapName.Substring(8, 2)
				: mapName.Length >= 10 ? mapName.Substring(5, 5) : mapName;
		}

		/// <summary>
		/// rAthena's <c>F_tt</c> Turbo Track helper. It derives an arena key
		/// such as <c>n1</c> or <c>e16</c> from the invoking NPC's map ID.
		/// </summary>
		public static string TurboTrackKey(Npc npc)
		{
			var npcName = npc?.Name ?? string.Empty;
			if (npcName.Contains("main", StringComparison.OrdinalIgnoreCase))
				return string.Empty;

			var mapName = npc?.Map?.StringId ?? string.Empty;
			if (mapName.Length == 0)
				return string.Empty;

			// Equivalent to charat(map, 6) + (strlen(map) > 9
			// ? substr(map, 8, 9) : charat(map, strlen(map) - 1)).
			var type = mapName.Length > 6 ? mapName[6].ToString() : string.Empty;
			var size = mapName.Length > 9
				? mapName.Substring(8, Math.Min(2, mapName.Length - 8))
				: mapName[^1].ToString();
			return type + size;
		}

		/// <summary>Returns 0 when an equipped item's ID matches, 1 otherwise.</summary>
		public static int IsEquipIdMismatch(PlayerCharacter player, int position, int expectedItemId)
		{
			if (player == null)
				return 1;
			var item = GetEquippedItem(player, position);
			return item != null && item.ClassId == expectedItemId ? 0 : 1;
		}

		/// <summary>Returns 0 when an equipped item's refine level matches, 1 otherwise.</summary>
		public static int IsEquipRefineMismatch(PlayerCharacter player, int position, int expectedRefine)
		{
			if (player == null)
				return 1;
			var item = GetEquippedItem(player, position);
			return item != null && item.RefineLevel == expectedRefine ? 0 : 1;
		}

		/// <summary>Returns 0 when an equipped item's four cards match, 1 otherwise.</summary>
		public static int IsEquipCardMismatch(PlayerCharacter player, int position,
			int expectedCard0, int expectedCard1, int expectedCard2, int expectedCard3)
		{
			if (player == null)
				return 1;
			var item = GetEquippedItem(player, position);
			if (item == null)
				return 1;

			var expectedCards = new[] { expectedCard0, expectedCard1, expectedCard2, expectedCard3 };
			for (var i = 0; i < expectedCards.Length; ++i)
			{
				var actual = i < item.Cards.Length ? item.Cards[i] : 0;
				if (actual != expectedCards[i])
					return 1;
			}

			return 0;
		}

		/// <summary>Returns whether the player holds either official Eden Group mark.</summary>
		public static int HasEdenGroupMark(PlayerCharacter player)
			=> player?.Inventory.CountItem(6219) > 0 || player?.Inventory.CountItem(22508) > 0 ? 1 : 0;

		/// <summary>
		/// Connects an rAthena <c>OnTouch</c> label to Sabine's existing
		/// movement trigger-area system. The events receive the entering
		/// player followed by the owning NPC.
		/// </summary>
		public static void AttachScriptTouchTrigger(Npc npc, int rangeX, int rangeY,
			params string[] eventNames)
		{
			if (npc == null || eventNames == null || eventNames.Length == 0)
				return;

			npc.TriggerArea = new TriggerArea(npc, Math.Max(0, rangeX), Math.Max(0, rangeY))
			{
				Enter = (character, owner) =>
				{
					if (character is not PlayerCharacter player)
						return;

					foreach (var eventName in eventNames)
					{
						if (!string.IsNullOrWhiteSpace(eventName))
							_ = ScriptEvents.RaiseAsync(eventName, player, owner);
					}
				},
			};
		}

		/// <summary>
		/// Runs a generated touch-label callback in the entering player's
		/// dialog context. Entering a trigger while another dialog is open is
		/// ignored, matching the client interaction constraint.
		/// </summary>
		public static async Task RunTouchDialogAsync(object[] args, Func<Dialog, Task> callback)
		{
			if (callback == null || args == null)
				return;

			var player = args.OfType<PlayerCharacter>().FirstOrDefault();
			var npc = args.OfType<Npc>().FirstOrDefault();
			if (player?.Connection == null || npc == null || player.Connection.CurrentDialog != null)
				return;

			await using var dialog = new Dialog(player, npc);
			await callback(dialog);
		}

		/// <summary>rAthena's Func_Socket/Func_Socket2 slot-addition transaction.</summary>
		public static async Task SocketEnchant(Dialog dialog, params object[] args)
		{
			var player = dialog?.Player;
			if (player == null || args == null || args.Length < 7)
				return;

			var sourceId = Convert.ToInt32(args[0]);
			var resultId = Convert.ToInt32(args[1]);
			var successLow = Convert.ToInt32(args[2]);
			var successHigh = Convert.ToInt32(args[3]);
			var zenyThousands = Convert.ToInt32(args[4]);
			var material1 = Convert.ToInt32(args[5]);
			var amount1 = Convert.ToInt32(args[6]);
			var material2 = args.Length > 7 ? Convert.ToInt32(args[7]) : 0;
			var amount2 = args.Length > 8 ? Convert.ToInt32(args[8]) : 0;
			var zeny = zenyThousands * 1000;

			await dialog.Talk("Adding a slot may destroy the equipment and its cards if it fails.");
			if (await dialog.Select("Attempt Slot Addition", "Cancel") != 1)
				return;

			if (player.Parameters.Get(ParameterType.Zeny) < zeny
				|| player.Inventory.CountItem(sourceId) < 1
				|| player.Inventory.CountItem(material1) < amount1
				|| (material2 != 0 && amount2 != 0 && player.Inventory.CountItem(material2) < amount2))
			{
				await dialog.Talk("You are missing the equipment, required materials, or zeny.");
				return;
			}

			player.Inventory.Remove(sourceId, 1);
			player.Inventory.Remove(material1, amount1);
			if (material2 != 0 && amount2 != 0)
				player.Inventory.Remove(material2, amount2);
			player.Parameters.Set(ParameterType.Zeny, player.Parameters.Get(ParameterType.Zeny) - zeny);

			var roll = ItemScript.Random(1, 100);
			var success = roll > successLow && roll < successHigh;
			if (success)
			{
				player.Inventory.Add(resultId, 1);
				await dialog.Talk("Great, it seems to be successful.");
			}
			else
			{
				await dialog.Talk("I am sorry, it failed.");
			}
		}

		/// <summary>rAthena's novice-training reset helper, <c>F_NvErase</c>.</summary>
		public static void NoviceErase(PlayerCharacter player, int eraseQuests = 0)
		{
			if (player == null)
				return;

			if (eraseQuests != 0)
				foreach (var questId in new[] { 7123, 7124, 7126, 7127 })
					player.Quests.Erase(questId);

			foreach (var item in player.Inventory.GetEquippedItems())
				player.Inventory.UnequipItem(item);

			foreach (var manualId in new[] { 2819, 2820, 2821, 2822, 2823, 2824 })
			{
				var amount = player.Inventory.CountItem(manualId);
				if (amount > 0)
					player.Inventory.Remove(manualId, amount);
			}
		}

		/// <summary>
		/// Changes an NPC's visible body sprite, matching rAthena's
		/// <c>setnpcdisplay &quot;name&quot;, sprite</c>. The optional source color
		/// argument belongs to newer client behavior and has no PV50000 use.
		/// </summary>
		public static void SetNpcDisplay(string name, int sprite)
		{
			var npc = ZoneServer.Instance.World.GetNpc(name);
			npc?.SetDisplaySprite(sprite);
		}

		/// <summary>Unequips every item, matching rAthena's <c>nude</c> builtin.</summary>
		public static void UnequipAll(PlayerCharacter player)
		{
			if (player == null)
				return;

			foreach (var item in player.Inventory.GetEquippedItems())
				player.Inventory.UnequipItem(item);
		}

		/// <summary>
		/// rAthena's <c>F_GM_NPC</c> gate. It first enforces the source
		/// helper's authority-99 requirement, then returns the same password
		/// result contract: 1 correct, 0 incorrect, -1 zero/cancel, -2 out of
		/// range.
		/// </summary>
		public static async Task<int> GmNpcPassword(Dialog dialog, object password = null,
			object passwordType = null, object minimum = null, object maximum = null)
		{
			var player = dialog?.Player;
			if (player?.Connection?.Account?.Authority < 99)
				return 0;

			if (password == null)
				return 0;

			var stringPassword = Convert.ToInt32(passwordType ?? 0) != 0;
			if (stringPassword)
				return string.Equals(await dialog.InputString(), Convert.ToString(password), StringComparison.Ordinal) ? 1 : 0;

			var min = minimum == null ? int.MinValue : Convert.ToInt32(minimum);
			var max = maximum == null ? int.MaxValue : Convert.ToInt32(maximum);
			var value = await dialog.InputNumber(0, min, max);
			if (value == 0)
				return -1;
			return value == Convert.ToInt32(password) ? 1 : 0;
		}

		/// <summary>rAthena's <c>F_BG_Badge</c> shared Battleground reward helper.</summary>
		public static async Task BgBadgeReward(Dialog dialog, int won, string team, string bgType)
		{
			var player = dialog?.Player;
			if (player == null)
				return;

			var isTierra = string.Equals(bgType, "Tierra", StringComparison.OrdinalIgnoreCase);
			var isFlavius = string.Equals(bgType, "Flavius", StringComparison.OrdinalIgnoreCase);
			if (!isTierra && !isFlavius)
				return;

			var badgeId = isTierra ? 7828 : 7829;
			var amount = won != 0 ? (isTierra ? 3 : 9) : (isTierra ? 1 : 3);
			if (dialog != null)
			{
				if (string.Equals(team, "Guillaume", StringComparison.OrdinalIgnoreCase))
					dialog.Msg(won != 0 ? "[Axl Rose]\nBlessed Guillaume!\nLet's enjoy our glorious victory!" : "[Axl Rose]\nYou lost, but you're dedicated to this battle.");
				else if (string.Equals(team, "Croix", StringComparison.OrdinalIgnoreCase))
					dialog.Msg(won != 0 ? "[Swandery]\nBlessed Croix!\nLet's enjoy our glorious victory!" : "[Swandery]\nOh, " + player.Name + " Don't be sad.");
				dialog.Close2();
			}

			var room = Math.Max(0, 500 - player.Inventory.CountItem(badgeId));
			var grant = Math.Min(amount, room);
			if (grant > 0)
				player.Inventory.AddItem(new Item(badgeId, grant));
			await Task.CompletedTask;
		}

		/// <summary>
		/// rAthena guide helper <c>F_Navi</c>. PV50000 has no dialog
		/// navigation-link packet, so the destination is intentionally kept
		/// for a future client implementation while the text and its rAthena
		/// colour markup are preserved. Guide scripts pair this with
		/// <c>viewpoint</c>, which emits the minimap marker today.
		/// </summary>
		public static string NavigationLabel(PlayerCharacter player, string label, string destination = "", string color = "")
		{
			label ??= string.Empty;
			if (string.IsNullOrWhiteSpace(color))
				return label;

			// rAthena colours are already client markup (e.g. ^7F3300).
			// Add the reset only when callers supplied a colour, so the
			// following dialogue text cannot inherit it accidentally.
			return color + label + "^000000";
		}

		public static int SuccessRefineItem(PlayerCharacter player, int position, int count = 1)
		{
			var item = GetEquippedItem(player, position);
			if (item == null)
				return -1;

			var slots = item.EquippedOn;
			player.Inventory.UnequipItem(item);
			item.RefineLevel = (byte)Math.Clamp(item.RefineLevel + count, 0, 20);
			Send.ZC_ACK_ITEMREFINING(player, 0, item.InventoryId, item.RefineLevel);
			player.Inventory.EquipItem(item, slots);
			Send.ZC_NOTIFY_EFFECT(player, 3);
			return item.RefineLevel;
		}

		public static int DownRefineItem(PlayerCharacter player, int position, int count = 1)
		{
			var item = GetEquippedItem(player, position);
			if (item == null)
				return -1;

			var slots = item.EquippedOn;
			player.Inventory.UnequipItem(item);
			item.RefineLevel = (byte)Math.Max(0, item.RefineLevel - count);
			Send.ZC_ACK_ITEMREFINING(player, 2, item.InventoryId, item.RefineLevel);
			player.Inventory.EquipItem(item, slots);
			Send.ZC_NOTIFY_EFFECT(player, 2);
			return item.RefineLevel;
		}

		public static int FailRefineItem(PlayerCharacter player, int position)
		{
			var item = GetEquippedItem(player, position);
			if (item == null)
				return 0;

			player.Inventory.UnequipItem(item);
			item.RefineLevel = 0;
			Send.ZC_ACK_ITEMREFINING(player, 1, item.InventoryId, 0);
			player.Inventory.RemoveItem(item);
			Send.ZC_NOTIFY_EFFECT(player, 2);
			return 1;
		}

		public static bool NpcShopAddItems(string shopName, params int[] itemPricePairs)
		{
			if (!ZoneServer.Instance.World.NpcShops.TryGet(shopName, out var shop))
				return false;
			if (itemPricePairs.Length % 2 != 0)
				return false;

			for (var i = 0; i < itemPricePairs.Length; i += 2)
				shop.AddItem(itemPricePairs[i], itemPricePairs[i + 1]);
			return true;
		}

		public static bool NpcShopDeleteItems(string shopName, params int[] classIds)
		{
			if (!ZoneServer.Instance.World.NpcShops.TryGet(shopName, out var shop))
				return false;

			foreach (var classId in classIds)
				shop.RemoveItem(classId);
			return true;
		}

		private static Item GetEquippedItem(PlayerCharacter player, int position)
		{
			if (player == null)
				return null;
			var slot = position switch
			{
				// These are the stable EQI_* values emitted by the converter
				// (not the bit values of Sabine's EquipSlots enum).
				1 => EquipSlots.HeadTop,
				2 => EquipSlots.Body,
				3 => EquipSlots.LeftHand,
				4 => EquipSlots.RightHand,
				5 => EquipSlots.Garment,
				6 => EquipSlots.Shoes,
				7 => EquipSlots.AccessoryLeft,
				8 => EquipSlots.AccessoryRight,
				9 => EquipSlots.HeadMid,
				10 => EquipSlots.HeadLow,
				11 => EquipSlots.CostumeHeadTop,
				12 => EquipSlots.CostumeHeadMid,
				13 => EquipSlots.CostumeHeadLow,
				14 => EquipSlots.CostumeGarment,
				15 => EquipSlots.Ammo,
				16 => EquipSlots.ShadowArmor,
				17 => EquipSlots.ShadowWeapon,
				18 => EquipSlots.ShadowShield,
				19 => EquipSlots.ShadowShoes,
				20 => EquipSlots.ShadowAccRight,
				21 => EquipSlots.ShadowAccLeft,
				_ => EquipSlots.None,
			};
			return slot == EquipSlots.None ? null : player.Inventory.FindEquipped(slot);
		}

		/// <summary>rAthena <c>getequipid</c> using its positional slot index.</summary>
		public static int GetEquipId(PlayerCharacter player, int position)
			=> GetEquippedItem(player, position)?.ClassId ?? -1;

		/// <summary>rAthena <c>getequipisequiped</c> using its positional slot index.</summary>
		public static bool IsEquipEquipped(PlayerCharacter player, int position)
			=> GetEquippedItem(player, position) != null;

		/// <summary>rAthena <c>getequiprefinerycnt</c> using its positional slot index.</summary>
		public static int GetEquipRefineLevel(PlayerCharacter player, int position)
			=> GetEquippedItem(player, position)?.RefineLevel ?? 0;

		/// <summary>rAthena <c>getequipcardid</c> using its positional slot and card index.</summary>
		public static int GetEquipCardId(PlayerCharacter player, int position, int cardSlot)
		{
			var cards = GetEquippedItem(player, position)?.Cards;
			return cards != null && cardSlot >= 0 && cardSlot < cards.Length ? cards[cardSlot] : 0;
		}

		/// <summary>rAthena <c>replacestr</c>, replacing every literal occurrence.</summary>
		public static string ReplaceStr(string value, string oldValue, string newValue)
		{
			if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(oldValue))
				return value ?? string.Empty;
			return value.Replace(oldValue, newValue ?? string.Empty);
		}

		public static void AddEventTimer(PlayerCharacter player, int delayMs, string eventName)
		{
			if (player == null || string.IsNullOrEmpty(eventName))
				return;
			ZoneServer.Instance.World.Scheduler.Schedule(
				TimeSpan.FromMilliseconds(Math.Max(0, delayMs)),
				state => _ = ScriptEvents.RaiseAsync(eventName, player));
		}

		public static bool UnitKill(PlayerCharacter player, int handle)
		{
			var unit = ResolveUnit(player, handle);
			if (unit == null)
				return false;
			unit.Kill(null);
			return true;
		}

		public static bool UnitWarp(PlayerCharacter player, int handle, string mapName, int x, int y)
		{
			var unit = ResolveUnit(player, handle);
			if (unit == null)
				return false;
			if (string.Equals(mapName, "this", StringComparison.OrdinalIgnoreCase))
				unit.Warp(unit.MapId, new Sabine.Shared.World.Position(x, y));
			else
				unit.Warp(mapName, x, y);
			return true;
		}

		public static bool UnitWalk(PlayerCharacter player, int handle, int x, int y,
			string eventName = null)
		{
			var unit = ResolveUnit(player, handle);
			if (unit?.Controller == null)
				return false;

			var duration = unit.Controller.MoveTo(new Sabine.Shared.World.Position(x, y));
			if (!string.IsNullOrEmpty(eventName))
			{
				ZoneServer.Instance.World.Scheduler.Schedule(duration,
					state => _ = ScriptEvents.RaiseAsync(eventName, player));
			}
			return true;
		}

		public static bool UnitStopWalk(PlayerCharacter player, int handle)
		{
			var unit = ResolveUnit(player, handle);
			if (unit?.Controller == null)
				return false;
			unit.Controller.StopMove();
			return true;
		}

		public static bool MoveNpc(string name, int x, int y, int direction = -1)
		{
			var npc = ZoneServer.Instance.World.GetNpc(name);
			if (npc?.Map == null)
				return false;
			if (direction >= 0)
				npc.Direction = (Sabine.Shared.World.Direction)(direction % 8);
			npc.Warp(npc.MapId, new Sabine.Shared.World.Position(x, y));
			return true;
		}

		public static bool SetNpcSpeed(Npc npc, int speed)
		{
			if (npc == null || speed < 1)
				return false;
			npc.WalkSpeed = speed;
			return true;
		}

		public static bool SetNpcSpeed(string name, int speed)
			=> SetNpcSpeed(ZoneServer.Instance.World.GetNpc(name), speed);

		public static int GetNpcId(Npc context, int type, string name = null)
		{
			if (type != 0)
				return 0;
			var npc = string.IsNullOrEmpty(name)
				? context
				: ZoneServer.Instance.World.GetNpc(name);
			return npc?.Handle ?? 0;
		}

		/// <summary>Returns whether an actor with the rAthena game ID is still present.</summary>
		public static bool UnitExists(int handle)
		{
			if (handle == 0)
				return false;
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				if (map.GetActor(handle) != null)
					return true;
			}
			return false;
		}

		public static int IsBeginQuest(PlayerCharacter player, int questId)
		{
			if (player == null || !player.Quests.Contains(questId))
				return 0;
			return player.Quests.IsComplete(questId) ? 2 : 1;
		}

		public static void SetRiding(PlayerCharacter player, bool enabled)
		{
			player?.SetOptionFlag(OptionState.Riding, enabled);
		}

		public static void ReadBook(PlayerCharacter player, int bookId, int page)
		{
			if (player != null)
				Send.ZC_READ_BOOK(player.Connection, bookId, page);
		}

		public static PlayerCharacter AttachRid(PlayerCharacter current, int accountId)
		{
			return ZoneServer.Instance.World.Maps.TryGetPlayerByAccountId(accountId,
				out var attached) ? attached : current;
		}

		public static void NpcTalk(Npc context, string message, string npcName = null)
		{
			var npc = string.IsNullOrWhiteSpace(npcName)
				? context
				: ZoneServer.Instance.World.GetNpc(npcName);
			npc?.Talk(message);
		}

		public static string GetNpcStringElement(string npcName, string key, int index)
		{
			var npc = ZoneServer.Instance.World.GetNpc(npcName);
			return npc?.Vars.Temp.GetStringElement(key, index) ?? string.Empty;
		}

		public static string GetNpcString(string npcName, string key)
		{
			var npc = ZoneServer.Instance.World.GetNpc(npcName);
			return npc?.Vars.Temp.GetString(key, string.Empty) ?? string.Empty;
		}

		public static int GetNpcInt(string npcName, string key)
		{
			var npc = ZoneServer.Instance.World.GetNpc(npcName);
			return npc?.Vars.Temp.GetInt(key, 0) ?? 0;
		}

		public static int GetNpcIntElement(string npcName, string key, int index)
		{
			var npc = ZoneServer.Instance.World.GetNpc(npcName);
			return npc?.Vars.Temp.GetIntElement(key, index) ?? 0;
		}

		public static void SetNpcString(string npcName, string key, string value)
			=> ZoneServer.Instance.World.GetNpc(npcName)?.Vars.Temp.SetString(key, value ?? string.Empty);

		public static void SetNpcStringElement(string npcName, string key, int index, string value)
			=> ZoneServer.Instance.World.GetNpc(npcName)?.Vars.Temp.SetStringElement(key, index, value ?? string.Empty);

		public static void SetNpcInt(string npcName, string key, int value)
			=> ZoneServer.Instance.World.GetNpc(npcName)?.Vars.Temp.SetInt(key, value);

		public static void SetNpcIntElement(string npcName, string key, int index, int value)
			=> ZoneServer.Instance.World.GetNpc(npcName)?.Vars.Temp.SetIntElement(key, index, value);

		public static Npc AddFloatingNpc(string name)
			=> ZoneServer.Instance.World.RegisterScriptNpc(name);

		public static bool RentItem(PlayerCharacter player, int classId, int seconds)
		{
			if (player == null || seconds <= 0
				|| !ZoneServer.Instance.Data.Items.TryFind(classId, out _))
				return false;

			var expires = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + seconds;
			player.Inventory.AddRentalItem(new Item(classId),
				(int)Math.Min(int.MaxValue, expires));
			return true;
		}

		public static void GetExp2(PlayerCharacter player, int baseExp, int jobExp)
		{
			if (player == null)
				return;
			if (baseExp != 0)
				player.GainBaseExp(baseExp);
			if (jobExp != 0)
				player.GainJobExp(jobExp);
		}

		public static int GetBaseExpRatio(PlayerCharacter player, int percent, int baseLevel)
		{
			if (player == null || percent < 0 || percent > 100 || baseLevel < 1)
				return 0;
			var needed = ZoneServer.Instance.Data.ExpTables.GetExpNeeded(
				ExpTableType.Base, player.JobId, baseLevel);
			return (int)Math.Min(int.MaxValue, (long)needed * percent / 100);
		}

		public static bool ScriptAtCommand(PlayerCharacter player, string command)
		{
			if (player == null || string.IsNullOrWhiteSpace(command))
				return false;

			var atIndex = command.IndexOf('@');
			if (atIndex >= 0)
				command = command.Substring(atIndex);
			var parts = command.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var name = parts[0].TrimStart('@').ToLowerInvariant();
			if (name == "broadcast")
			{
				var message = command.Substring(command.IndexOf(parts[0], StringComparison.Ordinal)
					+ parts[0].Length).TrimStart();
				Send.ZC_BROADCAST($"{player.Name} : {message}");
				return true;
			}
			if (name == "changedress")
			{
				player.ClearDressOptions();
				return true;
			}
			if (name == "kill" && parts.Length >= 2
				&& ZoneServer.Instance.World.Maps.TryGetPlayerByName(parts[1], out var target))
			{
				target.Kill(player);
				return true;
			}
			if (name == "reloadmobdb")
			{
				ZoneServer.Instance.LoadData();
				return true;
			}
			if (name == "option" && parts.Length >= 4
				&& int.TryParse(parts[1], out var body)
				&& int.TryParse(parts[2], out var health)
				&& int.TryParse(parts[3], out var option))
			{
				player.SetScriptOptions(body, health, option);
				return true;
			}
			return false;
		}

		public static void SetScriptSkill(PlayerCharacter player, SkillId skillId,
			int level, SkillPerm perm = SkillPerm.Permanent)
		{
			if (player == null)
				return;
			if (level <= 0)
			{
				player.Skills.Remove(skillId);
				player.Skills.RefreshClient();
				return;
			}
			player.Skills.Add(skillId, level, perm);
		}

		private static Character ResolveUnit(PlayerCharacter player, int handle)
		{
			if (handle == 0)
				return player;
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				var unit = map.GetCharacter(handle);
				if (unit != null)
					return unit;
			}
			return null;
		}

		/// <summary>rAthena <c>message characterName, text</c>.</summary>
		public static void Message(string characterName, string message)
		{
			if (string.IsNullOrEmpty(characterName))
				return;
			if (ZoneServer.Instance.World.Maps.TryGetPlayerByName(characterName, out var player))
				player.ServerMessage(message ?? string.Empty);
		}

		/// <summary>rAthena <c>debugmes</c>.</summary>
		public static void DebugMessage(object message)
			=> Log.Debug("[Script] {0}", message);

		public static string Substr(string value, int start, int end)
		{
			if (string.IsNullOrEmpty(value) || start < 0 || end < start || end >= value.Length)
				return string.Empty;
			return value.Substring(start, end - start + 1);
		}

		public static string CharAt(string value, int index)
			=> string.IsNullOrEmpty(value) || index < 0 || index >= value.Length
				? string.Empty : value[index].ToString();

		public static string InsertChar(string value, string insert, int position)
		{
			value ??= string.Empty;
			insert ??= string.Empty;
			position = Math.Clamp(position, 0, value.Length);
			return value.Insert(position, insert);
		}

		/// <summary>rAthena <c>getpartymember</c>, backed by Sabine's Variables arrays.</summary>
		public static int GetPartyMembers(int partyId, int type,
			Sabine.Shared.Util.Variables destination, string resultKey,
			Sabine.Shared.Util.Variables countDestination, string countKey)
		{
			var party = ZoneServer.Instance.World.Parties.GetParty(partyId);
			if (party == null || destination == null)
			{
				countDestination?.SetInt(countKey, 0);
				return 0;
			}

			var members = party.Members;
			for (var i = 0; i < members.Count; i++)
			{
				var member = members[i];
				if (type == 0)
					destination.SetStringElement(resultKey, i, member.Name ?? string.Empty);
				else
					destination.SetIntElement(resultKey, i,
						type == 2 ? member.AccountId : member.CharacterId);
			}
			countDestination?.SetInt(countKey, members.Count);
			return members.Count;
		}

		/// <summary>Sets the rAthena unit-data fields currently modelled by Sabine.</summary>
		public static void SetUnitData(int handle, int field, int value)
		{
			Character unit = null;
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				unit = map.GetActor(handle) as Character;
				if (unit != null)
					break;
			}
			if (unit == null)
				return;

			if (unit is Monster)
			{
				switch (field)
				{
					case 2: unit.Parameters.Hp = value; break;       // UMOB_HP
					case 31: unit.DamageImmune = value != 0; break; // UMOB_DMGIMMUNE
				}
				return;
			}

			if (unit is Npc npc)
			{
				SpriteType? changed = null;
				switch (field)
				{
					case 33: npc.DisplayClassId = value; changed = SpriteType.Base; break;
					case 34: npc.HairId = value; changed = SpriteType.Hair; break;
					case 35: npc.HairColorLook = value; changed = SpriteType.HairColor; break;
					case 36: npc.HeadBottomLook = value; changed = SpriteType.HeadBottom; break;
					case 37: npc.HeadMiddleLook = value; changed = SpriteType.HeadMiddle; break;
					case 38: npc.HeadTopLook = value; changed = SpriteType.HeadTop; break;
					case 39: npc.ClothesColorLook = value; changed = SpriteType.ClothesColor; break;
					case 45: npc.ScriptGroupId = value; break;
				}
				if (changed.HasValue)
				{
					if (Sabine.Shared.Game.Version < Sabine.Shared.Versions.S500)
						Send.ZC_SPRITE_CHANGE(npc, changed.Value, value);
					else
						Send.ZC_SPRITE_CHANGE2(npc, changed.Value, value, 0);
				}
			}
		}

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
		/// rAthena <c>jobname(&lt;job id&gt;)</c>. Resolves runtime job ids
		/// through Sabine's loaded job database so generated scripts can pass
		/// locals and function arguments as well as <c>Job_*</c> constants.
		/// </summary>
		public static string GetJobName(int jobId)
		{
			return ZoneServer.Instance.Data.Jobs.TryFind((JobId)jobId, out var data)
				? (data.Name ?? string.Empty)
				: ((JobId)jobId).ToString();
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
		public static int InstanceCreate(PlayerCharacter player, string name = null,
			InstanceMode mode = InstanceMode.Party, int ownerId = 0, Npc npc = null)
		{
			if (string.IsNullOrWhiteSpace(name)
				|| !ZoneServer.Instance.Data.Instances.Entries.TryGetValue(name, out var template))
				return -1;

			if (ownerId == 0)
				ownerId = mode == InstanceMode.None ? npc?.Handle ?? 0 : InstanceManager.GetOwnerId(player, mode);
			if (!OwnerExists(player, mode, ownerId))
				return -2;

			var manager = ZoneServer.Instance.World.Instances;
			if (manager.GetAll().Any(i => i.Mode == mode && i.OwnerId == ownerId))
				return -3;

			var sourceMaps = template.SourceMaps.Count > 0
				? template.SourceMaps
				: new List<string> { template.EntryMap };
			if (sourceMaps.Any(map => string.IsNullOrWhiteSpace(map)
				|| !ZoneServer.Instance.World.Maps.TryGetByStringId(map, out _)))
			{
				Log.Warning("InstanceCreate: template '{0}' has an unavailable source map.", name);
				return -1;
			}

			var ownerCharId = mode == InstanceMode.Character ? ownerId : player?.Id ?? 0;
			var ownerPartyId = mode == InstanceMode.Party ? ownerId : 0;
			var ownerGuildId = mode == InstanceMode.Guild ? ownerId : 0;
			var ownerClanId = mode == InstanceMode.Clan ? ownerId : 0;
			var inst = manager.Create(
				name, mode, ownerId, ownerCharId, ownerPartyId, ownerGuildId,
				ownerClanId, template.EntryMap, template.EntryX, template.EntryY);
			inst.Ttl = TimeSpan.FromMinutes(template.TtlMinutes);
			inst.IdleTimeout = TimeSpan.FromSeconds(template.IdleTimeoutSeconds);

			foreach (var sourceMap in sourceMaps)
			{
				if (manager.AttachMap(inst, sourceMap) != null)
					continue;

				manager.Destroy(inst.Id);
				return -1;
			}

			Log.Info("InstanceCreate: '{0}' (id={1}, mode={2}, owner={3}).",
				name, inst.Id, mode, ownerId);

			return inst.Id;
		}

		private static bool OwnerExists(PlayerCharacter player, InstanceMode mode, int ownerId)
		{
			return mode switch
			{
				InstanceMode.None => ownerId != 0,
				InstanceMode.Character => ownerId != 0 && FindPlayer(ownerId) != null,
				InstanceMode.Party => PartyManager.Instance.GetParty(ownerId) != null,
				InstanceMode.Guild => GuildManager.Instance.Get(ownerId) != null,
				InstanceMode.Clan => ClanManager.Instance.GetById(ownerId) != null,
				_ => false,
			};
		}

		private static PlayerCharacter FindPlayer(int characterId)
		{
			foreach (var map in ZoneServer.Instance.World.Maps.GetAll())
			{
				if (map.TryGetPlayerById(characterId, out var player))
					return player;
			}
			return null;
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
		public static int GetInstanceId(PlayerCharacter player, InstanceMode mode = InstanceMode.Party)
		{
			return ZoneServer.Instance.World.Instances.TryFindByOwner(player, mode, out var inst)
				? inst.Id
				: 0;
		}

		public static int GetInstanceId(Npc npc) => npc?.Map?.InstanceId ?? 0;

		public static string GetInstanceLiveName(int instanceId)
			=> ZoneServer.Instance.World.Instances.Get(instanceId)?.TemplateName ?? string.Empty;

		public static int GetInstanceLiveMode(int instanceId)
			=> ZoneServer.Instance.World.Instances.Get(instanceId) is { } instance
				? (int)instance.Mode : -1;

		public static int GetInstanceLiveOwner(int instanceId)
			=> ZoneServer.Instance.World.Instances.Get(instanceId)?.OwnerId ?? -1;

		public static int InstanceEnter(PlayerCharacter player, string name, int x = -1,
			int y = -1, int characterId = 0, int instanceId = 0)
		{
			var target = characterId == 0 ? player : FindPlayer(characterId);
			if (target == null || string.IsNullOrWhiteSpace(name))
				return (int)InstanceEnterResult.Other;

			if (instanceId <= 0)
				instanceId = GetInstanceId(target, InstanceMode.Party);
			var instance = ZoneServer.Instance.World.Instances.Get(instanceId);
			if (instance == null)
			{
				return target.Party == null
					? (int)InstanceEnterResult.NoMember
					: (int)InstanceEnterResult.NoInstance;
			}

			if (!string.Equals(instance.TemplateName, name, StringComparison.Ordinal))
				return (int)InstanceEnterResult.Other;
			if (!IsMemberOfInstance(target, instance))
				return (int)InstanceEnterResult.NoMember;
			if (!ZoneServer.Instance.World.Maps.TryGetByStringId(instance.EntryMap, out var entryMap))
				return (int)InstanceEnterResult.Other;

			if (x < 0 || y < 0)
			{
				x = instance.EntryX;
				y = instance.EntryY;
			}

			target.Warp(entryMap.Id, new Sabine.Shared.World.Position(x, y));
			instance.MarkOccupied();
			return (int)InstanceEnterResult.Ok;
		}

		private static bool IsMemberOfInstance(PlayerCharacter player, Instance instance)
		{
			return instance.Mode switch
			{
				InstanceMode.None => true,
				InstanceMode.Character => player.Id == instance.OwnerId,
				InstanceMode.Party => player.Party?.Id == instance.OwnerId,
				InstanceMode.Guild => player.Guild?.Id == instance.OwnerId,
				InstanceMode.Clan => ClanManager.Instance.GetByMember(player.Id)?.Id == instance.OwnerId,
				_ => false,
			};
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

		/// <summary>rAthena <c>getgdskilllv(guildId, skillId)</c>.</summary>
		public static int GetGuildSkillLevel(int guildId, int skillId)
			=> ZoneServer.Instance.World.Guilds.Get(guildId)?.GetSkillLevel(skillId) ?? 0;

		/// <summary>String-constant variant used by newer rAthena scripts.</summary>
		public static int GetGuildSkillLevel(int guildId, string skillName)
		{
			if (string.IsNullOrWhiteSpace(skillName))
				return 0;
			if (int.TryParse(skillName, out var numeric))
				return GetGuildSkillLevel(guildId, numeric);
			if (skillName.StartsWith("GD_", StringComparison.OrdinalIgnoreCase)
				&& int.TryParse(skillName.AsSpan(3), out numeric))
				return GetGuildSkillLevel(guildId, 10000 + numeric);
			return skillName.ToUpperInvariant() switch
			{
				"GD_GUILD_STORAGE" => GetGuildSkillLevel(guildId, 10001),
				_ => 0,
			};
		}

		/// <summary>
		/// rAthena <c>strcharinfo(type)</c>, including selectors computed at
		/// runtime. Unknown selectors follow the script command and return an
		/// empty string.
		/// </summary>
		public static string StrCharInfo(PlayerCharacter player, int type)
		{
			if (player == null)
				return string.Empty;

			return type switch
			{
				0 => player.Name ?? string.Empty,
				1 => GetPartyName(player.PartyId),
				2 => GetGuildName(player.GuildId),
				3 => player.Map?.StringId ?? string.Empty,
				_ => string.Empty,
			};
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
		/// rAthena's SE-castle <c>LinkFlag</c> function. Only members of
		/// the current castle owner guild may use these intra-castle links.
		/// The remaining arguments are either a named destination plus x/y,
		/// or repeated label/x/y triples.
		/// </summary>
		public static async Task LinkFlag(Dialog dialog, params object[] args)
		{
			var player = dialog?.Player;
			var map = dialog?.Npc?.Map?.StringId;
			if (player == null || string.IsNullOrWhiteSpace(map)
				|| player.Guild?.Id != GetCastleOwnerGuildId(map))
				return;

			var values = args.Select(x => Convert.ToString(x) ?? string.Empty).ToArray();
			if (values.Length < 3)
				return;

			if (values[0] == "Convenience Facility" || values[0] == "Emperium Center")
			{
				var destination = values[0] == "Convenience Facility"
					? "Go to Convenience Facility"
					: "Teleport";
				await dialog.Talk(values[0] == "Convenience Facility"
					? "This is the Stronghold Teleport Service. Would you like to teleport to the Convenience Facility for guild members?"
					: "This is the Stronghold Teleport Service. Would you like to teleport to the Emperium Center?");
				if (await dialog.Select(destination, "Cancel") == 1
					&& int.TryParse(values[1], out var x)
					&& int.TryParse(values[2], out var y))
					player.Warp(map, x, y);
				dialog.Close();
				return;
			}

			var choices = new List<string>();
			for (var i = 0; i + 2 < values.Length; i += 3)
				choices.Add(values[i]);
			choices.Add("Cancel");
			await dialog.Talk("This is the Stronghold Teleport Service. Please choose a destination within the stronghold.");
			var choice = await dialog.Select(choices.ToArray()) - 1;
			if (choice >= 0 && choice * 3 + 2 < values.Length
				&& int.TryParse(values[choice * 3 + 1], out var destinationX)
				&& int.TryParse(values[choice * 3 + 2], out var destinationY))
				player.Warp(map, destinationX, destinationY);
			dialog.Close();
		}

		/// <summary>
		/// Renewal 15.2 bunker cooldown gate (<c>F_time_limit_recorder</c>).
		/// Returns <c>false</c> when it closes the dialog and the caller must
		/// terminate; otherwise populates the five daily-progress flags.
		/// The source quest DB defines each gate as a 22-hour play-time quest.
		/// </summary>
		public static async Task<bool> TimeLimitRecorder(Dialog dialog, int[] status,
			bool checkWeight = true, bool warpOnExpiry = true)
		{
			var player = dialog?.Player;
			if (player == null || status == null || status.Length < 5)
				return false;

			if (checkWeight && !player.Inventory.CanCarry(1301, 1))
			{
				await dialog.Talk("- You are carrying too many items to proceed with the quest -");
				dialog.Close();
				return false;
			}

			var questIds = new[] { 5341, 5351, 5358, 5363, 5366 };
			for (var i = 0; i < questIds.Length; ++i)
			{
				var questId = questIds[i];
				if (!player.Quests.IsActive(questId) && !player.Quests.IsComplete(questId))
				{
					status[i] = 0;
					continue;
				}

				var acceptedAt = player.Quests.GetAcceptedAt(questId);
				if (acceptedAt != default && DateTime.UtcNow - acceptedAt >= TimeSpan.FromHours(22))
				{
					await dialog.Talk("Your whole body suddenly becomes heavy and time seems to be passing faster around you.");
					player.Vars.Perm.Set("ep15_2_memory", 0);
					for (var expiredQuest = 5341; expiredQuest <= 5369; ++expiredQuest)
						player.Quests.Erase(expiredQuest);
					if (warpOnExpiry)
						player.Warp("un_bunker", 98, 91);
					else if (!player.SaveLocation.IsZero)
						player.Warp(player.SaveLocation);
					dialog.Close();
					return false;
				}

				status[i] = 1;
			}

			return true;
		}

		/// <summary>
		/// One-time migration used by rAthena's legacy The Sign quest. Modern
		/// progress lives in <c>sign_q</c>; this translates the retired
		/// tracking variables once, then clears them just like the source.
		/// </summary>
		public static void UpdateSignVars(PlayerCharacter player)
		{
			if (player == null || player.Vars.Perm.GetInt("sign_q", 0) != 0)
				return;
			var vars = player.Vars.Perm;
			int Get(string name) => vars.GetInt(name, 0);
			void Set(string name, int value) => vars.Set(name, value);
			var signQuest = Get("signquest");
			var signQ = 0;

			if (signQuest <= 4) signQ = signQuest;
			else if (signQuest == 5) signQ = 14;
			else if (signQuest == 6) signQ = 15;
			else if (signQuest == 7) signQ = 17;
			else if (signQuest == 8) signQ = 19;
			else if (signQuest == 9) signQ = 20;
			else if (signQuest == 10) signQ = 25;
			else if (signQuest == 11) signQ = 27;
			else if (signQuest == 12) signQ = 35;
			else if (signQuest == 13) signQ = 53;
			else if (signQuest is >= 14 and <= 15) signQ = 54;
			else if (signQuest is >= 16 and <= 18) signQ = signQuest + 39;
			else if (signQuest == 19 && player.Inventory.Contains(7278)) signQ = 61;
			else if (signQuest == 20) signQ = 65;
			else if (signQuest == 21) signQ = 69;
			else if (signQuest is >= 22 and <= 28) signQ = signQuest + 48;
			else if (signQuest is >= 29 and <= 34) signQ = signQuest + 49;
			else if (signQuest is >= 35 and <= 41) { signQ = 83; Set("sign_sq", signQuest - 34); }
			else if (signQuest is >= 43 and <= 48) signQ = signQuest + 42;
			else if (signQuest is >= 49 and <= 52) signQ = signQuest + 44;
			else if (signQuest == 53) signQ = 100;
			else if (signQuest == 54) signQ = 117;
			else if (signQuest == 55) signQ = 118;
			else if (signQuest == 56) signQ = 127;
			else if (signQuest == 57) signQ = 129;
			else if (signQuest == 58) signQ = 130;
			else if (signQuest is >= 59 and <= 66) signQ = signQuest + 78;

			if (Get("sign_fail") == 1) signQ = 200;
			else if (Get("sign_fail") >= 2) signQ = 201;
			if (Get("ariantest") >= 2) signQ = Get("ariantest") + 3;
			if (Get("gaanantest") == 1) signQ = 12;
			if (Get("SignJore") == 1) signQ = 16;
			if (Get("ScareAlchSign") == 1) signQ = 18;
			if (Get("dearles_test") is >= 1 and <= 3) signQ = Get("dearles_test") + 27;
			if (Get("SignDance") is >= 1 and <= 2) signQ = Get("SignDance") + 31;
			var bakerlan = Get("bakerlan_test");
			if (bakerlan is >= 1 and <= 3) signQ = bakerlan + 35;
			else if (bakerlan is >= 4 and <= 6) signQ = bakerlan + 36;
			else if (bakerlan is >= 7 and <= 13) signQ = bakerlan + 39;
			if (Get("signanvil") is >= 1 and <= 4) Set("sign_sq", Get("signanvil") + 57);
			if (Get("signlaichin") >= 1) { player.Inventory.Remove(7306, player.Inventory.CountItem(7306)); player.Inventory.Remove(7307, player.Inventory.CountItem(7307)); signQ = ItemScript.Random(97, 100); }
			if (Get("signengelhour") != 0) { player.Inventory.Add(7314, 1); signQ = 139; }
			var branch8A = Get("Sign_Branch8A");
			if (branch8A == 1) signQ = 119; else if (branch8A == 2) signQ = 120; else if (branch8A == 3) signQ = 122; else if (branch8A == 4) signQ = 124; else if (branch8A == 5) signQ = 134; else if (branch8A is 7 or 8) signQ = 126;
			if (Get("Sign_Branch8B") > 0) signQ = 132;
			if (Get("Sign_Branch2B") != 0) signQ = 91;
			if (Get("signmetzhour") != 0) { player.Inventory.Add(7314, 1); signQ = 138; }
			Set("sign_q", signQ);

			foreach (var name in new[] { "signquest", "sign_fail", "gaananpoint", "gaanantest", "ariantest", "SignJore", "ScareAlchSign", "dearles_test", "SignDance", "bakerlan_test", "signanvil", "signengelhour", "signlaichin", "Sign_Seal1", "Sign_Seal2", "Sign_Seal3", "Sign_Seal4", "brokenseal", "Sign_Branch8A", "Sign_Branch8B", "Sign_Branch2B", "serinring", "signmetzhour" })
				Set(name, 0);
			foreach (var name in new[] { "Sign_Seal1", "Sign_Seal2", "Sign_Seal3", "Sign_Seal4" })
				vars.Set(name, string.Empty);
		}

		/// <summary>rAthena adventure-board monster lookup menu.</summary>
		public static async Task<int> AvenBoard(Dialog dialog, params object[] args)
		{
			var player = dialog?.Player;
			if (player == null)
				return 4;
			var options = new List<string> { "Look up monsters by floor", "Monster Information Retrieval System" };
			if (player.Inventory.CountItem(909) == 30000)
				options.Add("Request");
			options.Add("Cancel");
			var choice = await dialog.Select(options.ToArray());
			if (choice == 1 || choice == options.Count)
				return choice == 1 ? 1 : 4;
			if (options[choice - 1] == "Request")
			{
				await dialog.Talk("Requested information will appear - There's no problem if it appears");
				await dialog.Next();
				return 3;
			}

			while (true)
			{
				await dialog.Talk("[Monster Information Retrieval System]<br/>Enter a monster name, including spaces, to search this dungeon's information.");
				await dialog.Next();
				var name = await dialog.InputString();
				var found = -1;
				for (var i = 0; i + 2 < args.Length; i += 3)
					if (string.Equals(Convert.ToString(args[i + 1]), name, StringComparison.OrdinalIgnoreCase)) { found = i; break; }
				if (found < 0)
					await dialog.Talk("[Monster Information Retrieval System]<br/>There is no information available for that monster. Please check your monster name.");
				else
				{
					var id = Convert.ToInt32(args[found]);
					ZoneServer.Instance.Data.Monsters.TryFind((IdentityId)id, out var monster);
					await dialog.Talk($"[Monster Information Retrieval System]<br/>- Name : {Convert.ToString(args[found + 1])}<br/>- Element : {monster?.Element}<br/>- Size : {monster?.Size}<br/>- Race : {monster?.Race}<br/>--------------------------------^FD8D02<br/>{Convert.ToString(args[found + 2])}");
				}
				await dialog.Next();
				await dialog.Talk("[Monster Information Retrieval System]<br/>Would you like to search for other monster information?");
				await dialog.Next();
				if (await dialog.Select("Search", "Cancel") != 1)
				{
					await dialog.Talk("[Monster Information Retrieval System]<br/>Monster search system will now shut down.");
					await dialog.Next();
					return 2;
				}
			}
		}

		/// <summary>Companion text used by adventure-board floor listings.</summary>
		public static async Task InfoBoard(Dialog dialog)
		{
			await dialog.Next();
			await dialog.Talk("[Adventurer Group]<br/>For more information on the monsters, use the Monster Information Retrieval System.");
			await dialog.Next();
		}

		/// <summary>rAthena's shared Kafra post-service dialog (<c>F_KafEnd</c>).</summary>
		public static async Task KafraEnd(Dialog dialog, int type, bool saved, string area)
		{
			var player = dialog?.Player;
			await dialog.Talk("[Kafra Employee]");
			if (saved)
				await dialog.Talk($"Your Respawn Point has been saved here {area}.<br/>Thank you for using the Kafra Services.");
			else if (type is 0 or 5)
				await dialog.Talk("We, here at Kafra Corporation, are always endeavoring to provide you with the best services. We hope that we meet your adventuring needs and standards of excellence.");
			else if (type == 1)
			{
				player?.HealPercent(0, -25);
				await dialog.Talk("^666666Kaffffra n-never<br/>diiiiiiiiiiiiiies. On...<br/>On y-yooour siiiiide~^000000");
			}
			else if (type == 2)
				await dialog.Talk("Saved.<br/>Thank you for your patronage.");
			dialog.Close();
		}

		/// <summary>
		/// Shared rAthena Kafra service menu. Returns true only for the Save
		/// selection, letting the generated caller execute its exact following
		/// <c>savepoint</c> statement; every other completed path owns and
		/// terminates the interaction.
		/// </summary>
		public static async Task<bool> KafraService(Dialog dialog, int welcome, int menuType,
			int infoMode, int storageFee, int cartFee)
		{
			var player = dialog?.Player;
			if (player == null)
				return false;
			if (welcome != 5)
			{
				var greeting = welcome switch
				{
					1 => "^666666W-weeeelc-c-come<br/>to th-the K-kaaafrrrra<br/>C-coorpoor-r-ratioooonn...^000000",
					2 => $"Welcome. ^ff0000{player.Guild?.Name ?? string.Empty}^000000 Member.<br/>The Kafra Corporation will stay with you wherever you go.",
					_ => "Welcome to the Kafra Corporation.<br/>The Kafra services are always on your side.<br/>How may I assist you?",
				};
				await dialog.Talk("[Kafra Employee]<br/>" + greeting);
				await dialog.Next();
			}

			var menu = KafraMenu(menuType, welcome == 2);
			while (true)
			{
				var choice = await dialog.Select(menu.ToArray());
				var action = menu[choice - 1];
				switch (action)
				{
					case "Save":
						return true;
					case "Use Storage":
						if (!CanOpenStorage(player))
						{
							await dialog.Talk("[Kafra Employee]<br/>You need Novice Basic Skill Level 6 to use the Storage Service.");
							return false;
						}
						if (welcome != 2 && !TryPayKafraFee(player, storageFee, 7059))
						{
							await dialog.Talk($"[Kafra Employee]<br/>You don't have enough zeny to use the Storage Service. The access fee is {storageFee} zeny.");
							return false;
						}
						OpenStorage(player);
						dialog.Close();
						return false;
					case "Use Guild Storage":
						OpenGuildStorage(player);
						dialog.Close();
						return false;
					case "Use Teleport Service":
						await KafraTeleport(dialog, player, welcome == 2);
						return false;
					case "Rent a Pushcart":
						if (player.Parameters.Get(ParameterType.Cart) != 0)
						{
							await dialog.Talk("[Kafra Employee]<br/>You already have a Pushcart equipped.");
							break;
						}
						await dialog.Talk($"[Kafra Employee]<br/>The Pushcart rental fee is {cartFee} zeny. Would you like to rent one?");
						await dialog.Next();
						if (await dialog.Select("Rent a Pushcart", "Cancel") == 1 && (welcome == 2 || TryPayKafraFee(player, cartFee, 7061)))
							player.Parameters.Set(ParameterType.Cart, 1);
						break;
					case "Check Other Information":
						await dialog.Talk($"[Kafra Employee]<br/>{player.Name}, you have {player.Vars.Perm.GetInt("RESRVPTS", 0)} Special Reserve Points.");
						if (infoMode == 1) return false;
						break;
					default:
						await KafraEnd(dialog, welcome, false, string.Empty);
						return false;
				}
			}
		}

		private static List<string> KafraMenu(int menuType, bool guildCastle)
		{
			if (guildCastle) return new() { "Use Storage", "Use Guild Storage", "Rent a Pushcart", "Use Teleport Service", "Cancel" };
			return menuType switch
			{
				1 => new() { "Save", "Use Storage", "Cancel" },
				2 => new() { "Use Storage", "Cancel" },
				3 => new() { "Save", "Use Storage", "Rent a Pushcart", "Check Other Information", "Cancel" },
				5 => new() { "Use Storage", "Rent a Pushcart", "Check Other Information", "Cancel" },
				6 => new() { "Use Storage", "Check Other Information", "Cancel" },
				7 => new() { "Save", "Use Storage", "Rent a Pushcart", "Cancel" },
				8 => new() { "Save", "Use Storage", "Check Other Information", "Cancel" },
				9 => new() { "Use Storage", "Rent a Pushcart", "Use Teleport Service", "Check Other Information", "Cancel" },
				10 => new() { "Use Storage", "Save", "Rent a Pushcart", "Cancel" },
				_ => new() { "Save", "Use Storage", "Use Teleport Service", "Rent a Pushcart", "Check Other Information", "Cancel" },
			};
		}

		private static bool TryPayKafraFee(PlayerCharacter player, int fee, int ticketItemId)
		{
			if (player.Inventory.Contains(ticketItemId, 1)) { player.Inventory.Remove(ticketItemId, 1); return true; }
			var zeny = player.Parameters.Get(ParameterType.Zeny);
			if (zeny < fee) return false;
			player.Parameters.Set(ParameterType.Zeny, zeny - fee);
			player.Vars.Perm.Set("RESRVPTS", player.Vars.Perm.GetInt("RESRVPTS", 0) + fee / 5);
			return true;
		}

		private static async Task KafraTeleport(Dialog dialog, PlayerCharacter player, bool guildCastle)
		{
			var destinations = GetKafraDestinations(dialog.Npc?.Map?.StringId);
			await dialog.Talk("[Kafra Employee]<br/>Please choose your destination.");
			await dialog.Next();
			var labels = destinations.Select(x => $"{x.Name} -> {x.Fee} z").Append("Cancel").ToArray();
			var choice = await dialog.Select(labels);
			if (choice > destinations.Length) return;
			var destination = destinations[choice - 1];
			if (!guildCastle && !TryPayKafraFee(player, destination.Fee, 7060))
			{
				await dialog.Talk($"[Kafra Employee]<br/>You don't have enough zeny for travel to {destination.Name}.");
				return;
			}
			player.Warp(destination.Map, destination.X, destination.Y);
		}

		// F_KafSet in rAthena populated temporary player arrays based on the
		// invoking Kafra's map. Keeping the same data here prevents a Prontera
		// employee, for example, from offering routes that only exist elsewhere.
		private static (string Name, string Map, int X, int Y, int Fee)[] GetKafraDestinations(string sourceMap)
		{
			var all = new (string Name, string Map, int X, int Y, int Fee)[] {
				("Prontera", "prontera", 116, 72, 1200), ("Geffen", "geffen", 120, 39, 1200),
				("Payon", "payon", 161, 58, 1200), ("Morocc", "morocc", 156, 46, 1200),
				("Izlude", "izlude", 128, 98, 1200), ("Alberta", "alberta", 117, 56, 1800),
				("Al De Baran", "aldebaran", 168, 112, 1200), ("Juno", "yuno", 158, 125, 1200),
				("Umbala", "umbala", 100, 154, 1800), ("Comodo", "comodo", 209, 143, 1800),
				("Comodo Pharos Beacon", "cmd_fild07", 127, 134, 1200),
				("Mjolnir Dead Pit", "mjolnir_02", 99, 351, 1700), ("Orc Dungeon", "gef_fild10", 52, 326, 1700),
			};

			string[] routes = sourceMap switch
			{
				"prontera" => new[] { "Izlude", "Geffen", "Payon", "Morocc", "Orc Dungeon", "Alberta" },
				"alberta" => new[] { "Payon", "Morocc", "Prontera" },
				"aldebaran" => new[] { "Geffen", "Juno", "Izlude", "Mjolnir Dead Pit" },
				"comodo" => new[] { "Morocc", "Comodo Pharos Beacon", "Umbala" },
				"cmd_fild07" => new[] { "Comodo", "Morocc" },
				"geffen" => new[] { "Prontera", "Al De Baran", "Orc Dungeon", "Mjolnir Dead Pit" },
				"izlude" => new[] { "Geffen", "Payon", "Morocc", "Al De Baran" },
				"morocc" => new[] { "Prontera", "Payon", "Alberta", "Comodo", "Comodo Pharos Beacon" },
				"umbala" => new[] { "Comodo" },
				"payon" => new[] { "Prontera", "Alberta", "Morocc" },
				"yuno" => new[] { "Al De Baran" },
				"job3_rune01" => new[] { "Izlude", "Geffen", "Payon", "Morocc", "Alberta" },
				_ => null,
			};

			if (routes == null)
				return all;

			return routes.Select(route => all.First(destination => destination.Name == route)).ToArray();
		}

		/// <summary>MVP arena Protector menu (<c>Protector</c>).</summary>
		public static async Task Protector(Dialog dialog, string harderMap)
		{
			var player = dialog?.Player;
			if (player == null) return;
			var options = string.IsNullOrEmpty(harderMap)
				? new[] { "Heal", "Exit" }
				: new[] { "Harder Monsters", "Heal", "Exit" };
			var choice = await dialog.Select(options);
			if (!string.IsNullOrEmpty(harderMap) && choice == 1)
				player.Warp(harderMap, 102, 102);
			else if (choice == (string.IsNullOrEmpty(harderMap) ? 1 : 2))
				player.HealPercent(100, 100);
			else
				player.Warp("prontera", 156, 179);
			dialog.Close();
		}

		/// <summary>elRO airship porter gate (<c>F_Porter</c>).</summary>
		public static async Task AirshipPorter(Dialog dialog, int destinationId)
		{
			var player = dialog?.Player;
			if (player == null) return;
			var current = ZoneServer.Instance.World.Vars.Perm.GetInt("destination", -1);
			if (current == destinationId)
			{
				await dialog.Talk("^FF0000[Porter]^000000<br/>The plane is currently ported. Would you like to board?");
				await dialog.Next();
				if (await dialog.Select("Yes", "No", "View Hourly Itinerary") == 1)
				{
					player.Warp("airplane", 105, 72);
					dialog.Close();
					return;
				}
			}
			else
			{
				await dialog.Talk("^FF0000[Porter]^000000<br/>Sorry, but the plane currently isn't ported. You can view the airplane's hourly itinerary.");
				await dialog.Next();
				if (await dialog.Select("View Hourly Itinerary", "No thanks") != 1)
				{
					dialog.Close();
					return;
				}
			}
			await dialog.Talk($"^FF0000[Porter]^000000<br/>The airship is currently scheduled at stop {current}. Please check back when it is ported here.");
			await dialog.Next();
			dialog.Close();
		}

		/// <summary>
		/// Random exit used by the Lighthalzen cube maze. Mirrors the three
		/// destinations in <c>npc/warps/dungeons/lhz_dun.txt::randomw</c>.
		/// </summary>
		public static void RandomLhzCubeWarp(PlayerCharacter player)
		{
			if (player == null)
				return;

			switch (ItemScript.Random(0, 2))
			{
				case 1: player.Warp("lhz_cube", 66, 136); break;
				case 2: player.Warp("lhz_cube", 66, 74); break;
				default: player.Warp("lhz_cube", 67, 193); break;
			}
		}

		/// <summary>
		/// Gives the level-scaled Party Relay reward from
		/// <c>npc/quests/partyrelay.txt::F_PartyRelay_Exp</c>. The source
		/// awards the smaller table under Renewal and multiplies the legacy
		/// table by ten otherwise.
		/// </summary>
		public static void PartyRelayExp(PlayerCharacter player)
		{
			if (player == null)
				return;

			var level = player.Parameters.Get(ParameterType.BaseLevel);
			var exp = level switch
			{
				> 94 => 104760,
				> 89 => 85920,
				> 79 => 38520,
				> 69 => 19560,
				> 59 => 6720,
				> 49 => 2880,
				_ => 1080,
			};
			player.GainBaseExp(Sabine.Shared.Game.IsRenewal ? exp : exp * 10);
		}

		/// <summary>Message shown by Kagerou/Oboro's Test of Survival markers.</summary>
		public static async Task SurvivalMessage(Dialog dialog, int block)
		{
			if (dialog == null)
				return;
			await dialog.Talk($"[No. {block}]<br/>^1A95E6Other letters are too small to read. Let's look up close.^000000");
			dialog.Close();
		}

		/// <summary>Moves a Test of Survival player to its exact rAthena board square.</summary>
		public static void SurvivalWarp(PlayerCharacter player, int block)
		{
			if (player == null)
				return;
			var position = block switch
			{
				0 => (63, 16), 1 => (54, 16), 2 => (46, 16), 3 => (38, 16), 4 => (30, 16), 5 => (22, 16),
				6 => (17, 19), 7 => (17, 27), 8 => (17, 35), 9 => (17, 43), 10 => (17, 51), 11 => (17, 60),
				12 => (25, 60), 13 => (33, 60), 14 => (41, 60), 15 => (49, 60), 16 => (57, 60), 17 => (62, 56),
				18 => (62, 48), 19 => (62, 40), 20 => (62, 32), 21 => (62, 23), 22 => (54, 23), 23 => (46, 23),
				24 => (38, 23), 25 => (30, 23), 26 => (25, 27), 27 => (25, 35), 28 => (25, 43), 29 => (25, 52),
				30 => (33, 52), 31 => (41, 52), 32 => (49, 52), 33 => (54, 48), 34 => (54, 40), 35 => (54, 31),
				36 => (46, 31), 37 => (38, 31), 38 => (33, 35), 39 => (33, 44), 40 => (41, 44),
				_ => (63, 16),
			};
			player.Warp("job_ko", position.Item1, position.Item2);
		}

		/// <summary>Rolls a Survival die and returns the result for the source's explicit return-value form.</summary>
		public static async Task<int> SurvivalDice(Dialog dialog, int location, int cost = 1)
		{
			var player = dialog?.Player;
			if (player == null)
				return 0;
			cost = Math.Max(1, cost);
			await dialog.Talk($"Location: {location}<br/>Total LP: {player.Vars.Perm.GetInt("job_kagero_lp", 0)}<br/>To roll the dice, you need to consume {cost} ^FD0260LP^000000.");
			await dialog.Next();
			await dialog.Select("Roll the dice.");
			var lp = player.Vars.Perm.GetInt("job_kagero_lp", 0);
			if (lp < cost)
			{
				await dialog.Talk("Due to insufficient ^FD0260LP^000000, you will be returned to starting position.");
				dialog.Close();
				SurvivalWarp(player, 0);
				return 0;
			}
			player.Vars.Perm.Set("job_kagero_lp", lp - cost);
			var dice = ItemScript.Random(1, 6);
			await dialog.Talk($"The dice came out as {dice}.");
			return dice;
		}

		/// <summary>Default Survival dice form: roll, then close, move, and end.</summary>
		public static async Task SurvivalDiceAuto(Dialog dialog, int location, int cost = 1)
		{
			var dice = await SurvivalDice(dialog, location, cost);
			if (dice == 0 || dialog?.Player == null)
				return;
			await dialog.Talk($"Press Close to move forward {dice} blocks.");
			dialog.Close();
			SurvivalWarp(dialog.Player, location + dice);
		}

		/// <summary>Result of the two-dice Survival branch, including its early-warp outcome.</summary>
		public readonly record struct SurvivalDicePairResult(bool Completed, int First, int Second);

		/// <summary>Implements the Test of Survival's two-roll LP branch.</summary>
		public static async Task<SurvivalDicePairResult> SurvivalDicePair(Dialog dialog, int location, int cost = 2)
		{
			var player = dialog?.Player;
			if (player == null)
				return new(false, 0, 0);
			cost = Math.Max(1, cost);
			var lp = player.Vars.Perm.GetInt("job_kagero_lp", 0);
			await dialog.Talk($"Location: {location}<br/>Total LP: {lp}<br/>To roll the dice, you need to consume {cost} ^FD0260LP^000000.");
			await dialog.Next();
			await dialog.Select("Roll the first dice.");
			if (lp < cost)
			{
				await dialog.Talk("Due to insufficient ^FD0260LP^000000, you will be returned to starting position.");
				dialog.Close();
				SurvivalWarp(player, 0);
				return new(false, 0, 0);
			}
			player.Vars.Perm.Set("job_kagero_lp", lp - cost);
			await dialog.Talk($"< Used {cost} LP >");
			await dialog.Next();
			var first = ItemScript.Random(1, 6);
			await dialog.Talk($"First dice result is {first}. Do you want to roll the second dice?");
			await dialog.Next();
			await dialog.Select("Roll the second dice.");
			return new(true, first, ItemScript.Random(1, 6));
		}

		/// <summary>Runs the Survival rock-paper-scissors machine and returns 1, 0, or -1.</summary>
		public static async Task<int> SurvivalRockPaperScissors(Dialog dialog)
		{
			if (dialog == null)
				return -1;
			await dialog.Talk("Rock! Paper! Scissors!");
			await dialog.Next();
			var playerChoice = await dialog.Select("Give scissors.", "Give rock.", "Give paper.");
			var machineChoice = ItemScript.Random(1, 3);
			var names = new[] { string.Empty, "Scissors", "Rock", "Paper" };
			await dialog.Talk($"=Game Results=<br/>Machine: {names[machineChoice]}.<br/>You: {names[playerChoice]}.");
			await dialog.Next();
			if (playerChoice == machineChoice)
			{
				await dialog.Talk("Draw");
				return 0;
			}
			var won = (machineChoice == 1 && playerChoice == 2)
				|| (machineChoice == 2 && playerChoice == 3)
				|| (machineChoice == 3 && playerChoice == 1);
			await dialog.Talk(won ? "You Win" : "You Lose");
			return won ? 1 : -1;
		}

		/// <summary>Gets rAthena's display name field from the monster database.</summary>
		public static string GetMonsterName(object monster)
			=> FindMonster(monster)?.Name ?? monster?.ToString() ?? string.Empty;

		/// <summary>
		/// Gets the numeric <c>getmonsterinfo</c> fields used by converted
		/// scripts. Unsupported fields deliberately return zero, matching the
		/// script engine's failed lookup behavior without making generated code
		/// untyped.
		/// </summary>
		public static int GetMonsterInfoInt(object monster, string field)
		{
			var data = FindMonster(monster);
			if (data == null)
				return 0;
			return field?.ToUpperInvariant() switch
			{
				"MOB_LV" or "MOB_LEVEL" => data.Level,
				"MOB_HP" => data.Hp,
				"MOB_SP" => data.Sp,
				"MOB_BASEEXP" => data.BaseExp,
				"MOB_JOBEXP" => data.JobExp,
				"MOB_MVPEXP" => data.MvpExp,
				"MOB_SPEED" => data.Speed,
				"MOB_RACE" => (int)data.Race,
				"MOB_ELEMENT" => (int)data.Element,
				"MOB_SIZE" => (int)data.Size,
				"MOB_MODE" => data.IsBoss ? 1 : 0,
				_ => 0,
			};
		}

		private static MonsterData FindMonster(object monster)
		{
			if (monster == null)
				return null;
			if (monster is string name)
			{
				if (ZoneServer.Instance.Data.Monsters.TryFind(name, out var named))
					return named;
				if (!int.TryParse(name, out var parsed))
					return null;
				monster = parsed;
			}
			try
			{
				return ZoneServer.Instance.Data.Monsters.TryFind(
					(IdentityId)System.Convert.ToInt32(monster), out var data)
					? data : null;
			}
			catch (System.Exception)
			{
				return null;
			}
		}

		/// <summary>Gets numeric rAthena <c>getiteminfo</c> fields from Sabine item data.</summary>
		public static int GetItemInfoInt(object item, string field)
		{
			var data = FindItem(item);
			if (data == null)
				return -1;

			return field?.ToUpperInvariant() switch
			{
				"ITEMINFO_BUY" => data.Price,
				"ITEMINFO_SELL" => data.SellPrice,
				"ITEMINFO_TYPE" => (int)data.Type,
				"ITEMINFO_GENDER" => (int)data.SexAllowed,
				"ITEMINFO_LOCATIONS" => (int)data.WearSlots,
				"ITEMINFO_WEIGHT" => data.Weight,
				"ITEMINFO_ATTACK" => data.Attack,
				"ITEMINFO_DEFENSE" => data.Defense,
				"ITEMINFO_RANGE" => data.AttackRange,
				"ITEMINFO_SLOT" => data.Slots,
				"ITEMINFO_VIEW" => data.LookId,
				"ITEMINFO_EQUIPLEVELMIN" => data.RequiredLevel,
				"ITEMINFO_WEAPONLEVEL" => data.WeaponLevel,
				"ITEMINFO_MAGICATTACK" => data.MagicAttack,
				"ITEMINFO_ID" => data.ClassId,
				"ITEMINFO_ARMORLEVEL" => data.ArmorLevel,
				"ITEMINFO_SUBTYPE" => data.SubType,
				_ => -1,
			};
		}

		/// <summary>Gets the rAthena/Aegis identifier used by <c>getiteminfo</c>.</summary>
		public static string GetItemAegisName(object item)
			=> FindItem(item)?.AegisName ?? string.Empty;

		private static ItemData FindItem(object item)
		{
			if (item == null)
				return null;
			if (item is string name)
			{
				if (ZoneServer.Instance.Data.Items.TryFind(name, out var named))
					return named;
				if (!int.TryParse(name, out var parsed))
					return null;
				item = parsed;
			}
			try
			{
				return ZoneServer.Instance.Data.Items.TryFind(System.Convert.ToInt32(item), out var data)
					? data : null;
			}
			catch (System.Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Shared Mineral Detector prompt for the Episode 13.2 treasure hunt.
		/// A <see langword="true"/> result means the caller should continue with
		/// its location-specific marker logic; all other source paths call
		/// <c>end</c> or <c>close</c> and therefore stop the generated dialog.
		/// </summary>
		public static async Task<bool> Find132Mineral(Dialog dialog, int area, bool expectsResult)
		{
			var player = dialog?.Player;
			if (player == null)
				return false;

			var (requiredStage, requiredMinerals, areaName) = area switch
			{
				0 => (2, 10, "Mt. Mjolnir"),
				1 => (3, 5, "Abyss Lake"),
				2 => (4, 5, "Thor Volcano"),
				_ => (0, 0, string.Empty),
			};
			if (requiredStage == 0
				|| player.Vars.Perm.GetInt("ep13_2_tre", 0) != requiredStage
				|| player.Inventory.CountItem(6076) != 1
				|| player.Inventory.CountItem(6077) >= requiredMinerals)
				return false;

			await dialog.Talk("- Just arrived at " + areaName + ". I think I can find the mineral by using the Mineral Detector in the Portable Toolbox. -");
			await dialog.Next();
			if (await dialog.Select("Check current location.", "Do nothing.") != 1)
			{
				await dialog.Talk("- You decide to do nothing. -");
				dialog.Close();
				return false;
			}

			await dialog.Talk("- You pressed the button of the detector. -");
			await dialog.Next();
			await dialog.Talk("[Mineral Detector]");
			await dialog.Talk("Bee beep -");
			if (expectsResult)
				return true;

			await dialog.Next();
			await dialog.Talk("- Nothing's found. -");
			dialog.Close();
			return false;
		}

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
			var list = new List<int>(vars.GetIntArray(varName));
			var start = System.Math.Max(0, startIndex);
			if (start >= list.Count) return;
			list.RemoveRange(start, System.Math.Min(count, list.Count - start));
			vars.SetIntArray(varName, list);
		}

		/// <summary>String-array companion to <see cref="DeleteArrayInt"/>.</summary>
		public static void DeleteArrayString(Sabine.Shared.Util.Variables vars, string varName,
											  int startIndex, int count)
		{
			if (vars == null || string.IsNullOrEmpty(varName) || count <= 0)
				return;
			var list = new List<string>(vars.GetStringArray(varName));
			var start = System.Math.Max(0, startIndex);
			if (start >= list.Count) return;
			list.RemoveRange(start, System.Math.Min(count, list.Count - start));
			vars.SetStringArray(varName, list);
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
			var normalized = name.ToLowerInvariant();
			var defaultValue = normalized switch
			{
				// Defaults from rAthena's current conf/battle/client.conf.
				"max_hair_style" => 42,
				"max_hair_color" => 8,
				"max_cloth_color" => 7,
				_ => 0,
			};
			return ZoneServer.Instance.World.Vars.Perm.GetInt(
				"BattleFlag." + normalized, defaultValue);
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
		/// rAthena <c>getlook(type)</c>. Unknown/unmodelled look slots use
		/// rAthena's -1 sentinel.
		/// </summary>
		public static int GetLook(PlayerCharacter player, int type)
		{
			if (player == null)
				return -1;

			return (SpriteType)type switch
			{
				SpriteType.Hair => player.HairId,
				SpriteType.Weapon => player.WeaponLook,
				SpriteType.HeadBottom => player.HeadBottomLook,
				SpriteType.HeadTop => player.HeadTopLook,
				SpriteType.HeadMiddle => player.HeadMiddleLook,
				SpriteType.HairColor => player.HairColorLook,
				SpriteType.ClothesColor => player.ClothesColorLook,
				SpriteType.Shield => player.ShieldLook,
				SpriteType.Body => player.BodyLook,
				SpriteType.Robe => player.RobeLook,
				SpriteType.Body2 => player.Body2Look,
				_ => -1,
			};
		}

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

			switch (sprite)
			{
				case SpriteType.HairColor: player.HairColorLook = value; break;
				case SpriteType.ClothesColor: player.ClothesColorLook = value; break;
				case SpriteType.Shield: player.ShieldLook = value; break;
				case SpriteType.Body: player.BodyLook = value; break;
				case SpriteType.Robe: player.RobeLook = value; break;
				case SpriteType.Body2: player.Body2Look = value; break;
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

		public static void SetD(PlayerCharacter player, int name, int value)
			=> SetD(player, name.ToString(), value);

		/// <summary>String overload of <see cref="SetD"/> — invoked by
		/// the converter when the rAthena variable name ends in `$`.</summary>
		public static void SetD(PlayerCharacter player, string name, string value)
			=> SetDImpl(player, name, value, asString: true);

		private static void SetDImpl(PlayerCharacter player, string name, object value, bool asString)
		{
			var (vars, key) = ResolveVarScope(player, name);
			if (vars == null)
				return;

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
				key = "gtmp_" + key.Substring(2);
			}
			else if (key.StartsWith("$"))
			{
				vars = ZoneServer.Instance.World.Vars.Perm;
				key = "g_" + key.Substring(1);
			}
			else if (key.StartsWith("#"))
			{
				vars = player?.Connection?.Account?.Vars.Perm;
				key = "acc_" + key.Substring(1);
			}
			else if (key.StartsWith("@"))
			{
				vars = player?.Vars.Temp;
				key = "p_" + key.Substring(1);
			}

			if (key.EndsWith("$"))
				key = key.Substring(0, key.Length - 1);

			return (vars, key);
		}

		public static int GetDInt(PlayerCharacter player, int name)
			=> GetDInt(player, name.ToString());

		public static void SetDynamicIntArray(PlayerCharacter player, string name, int[] values)
		{
			var (vars, key, start) = ResolveDynamicArray(player, name);
			if (vars == null) return;
			for (var i = 0; i < (values?.Length ?? 0); i++)
				vars.SetIntElement(key, start + i, values[i]);
		}

		public static void SetDynamicStringArray(PlayerCharacter player, string name, string[] values)
		{
			var (vars, key, start) = ResolveDynamicArray(player, name);
			if (vars == null) return;
			for (var i = 0; i < (values?.Length ?? 0); i++)
				vars.SetStringElement(key, start + i, values[i] ?? string.Empty);
		}

		public static void DeleteDynamicArray(PlayerCharacter player, string name, int count = 1)
		{
			var (vars, key, start) = ResolveDynamicArray(player, name);
			if (vars == null)
				return;
			var baseName = Regex.Replace(name ?? string.Empty, @"\[[^\]]*\]$", string.Empty);
			if (baseName.EndsWith("$", StringComparison.Ordinal))
				DeleteArrayString(vars, key, start, count);
			else
				DeleteArrayInt(vars, key, start, count);
		}

		public static void DeleteLocalArray<T>(ref T[] values, int start, int count = 1)
		{
			if (values == null || start < 0 || count <= 0 || start >= values.Length)
				return;
			var removeCount = Math.Min(count, values.Length - start);
			Array.Copy(values, start + removeCount, values, start,
				values.Length - start - removeCount);
			Array.Resize(ref values, values.Length - removeCount);
		}

		private static (Sabine.Shared.Util.Variables vars, string key, int start) ResolveDynamicArray(PlayerCharacter player, string name)
		{
			var start = 0;
			var baseName = name ?? string.Empty;
			var match = Regex.Match(baseName, @"\[(-?\d+)\]$");
			if (match.Success)
			{
				int.TryParse(match.Groups[1].Value, out start);
				baseName = baseName.Substring(0, match.Index);
			}
			var (vars, key) = ResolveVarScope(player, baseName);
			return (vars, key, System.Math.Max(0, start));
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
			if (npc == null)
				return;
			Send.ZC_NOTIFY_GROUNDSKILL(npc, (SkillId)skillId, npc.Handle, level, x, y, 0);
		}

		public static void NpcSkillEffect(World.Actors.Npc npc, string skillName, int level, int x, int y)
		{
			if (Enum.TryParse<SkillId>(skillName, true, out var skillId))
				NpcSkillEffect(npc, (int)skillId, level, x, y);
		}

		/// <summary>
		/// Resolves the Aegis item names accepted by rAthena script commands
		/// while keeping Sabine's inventory API strongly typed by class ID.
		/// </summary>
		public static int ResolveItemId(int itemId) => itemId;

		public static int ResolveItemId(string itemName)
		{
			if (int.TryParse(itemName, out var itemId))
				return itemId;
			if (ZoneServer.Instance.Data.Items.TryFind(itemName, out var item))
				return item.ClassId;

			return itemName switch
			{
				"Apple" => 512,
				"Spawn" => 908,
				"Jellopy" => 909,
				"Knife" => 1201,
				"Premium_Reset_Stone" => 6320,
				"Monster_Ticket" => 7514,
				"Marvelous_Medal" => 7515,
				"Main_Lucky_Box" => 23914,
				"K_Secret_Key" => 23919,
				"Juice_Package" => 25246,
				_ => 0,
			};
		}

		public static void ScriptSkillEffect(PlayerCharacter player, SkillId skillId, int level)
		{
			if (player != null)
				Send.ZC_USESKILL_ACK(player, player.Handle, player.Handle, skillId, level, 0);
		}

		public static void ScriptSkillEffect(PlayerCharacter player, string skillName, int level)
		{
			if (Enum.TryParse<SkillId>(skillName, true, out var skillId))
				ScriptSkillEffect(player, skillId, level);
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

		public static bool BgReserve(string mapStringId, bool reserve = true)
			=> reserve
				? ZoneServer.Instance.World.Battlegrounds.Reserve(mapStringId)
				: ZoneServer.Instance.World.Battlegrounds.Unbook(mapStringId);

		public static bool BgUnbook(string mapStringId)
			=> ZoneServer.Instance.World.Battlegrounds.Unbook(mapStringId);

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

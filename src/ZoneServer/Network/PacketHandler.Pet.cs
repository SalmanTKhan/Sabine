using Sabine.Shared;
using Sabine.Shared.Network;
using Sabine.Shared.Util;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;
using Yggdrasil.Logging;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Handlers for the rAthena pet incubator + capture-roulette flow.
	/// The plain double-click "consume egg" shortcut is already wired in
	/// <c>PacketHandler.CZ_USE_ITEM</c>; these handlers cover the
	/// incubator UI path (<c>CZ_SELECT_PETEGG</c>) and the taming-lure
	/// path (<c>CZ_TRYCAPTURE_MONSTER</c>).
	/// </summary>
	public partial class PacketHandler
	{
		/// <summary>
		/// CZ_SELECT_PETEGG — client picked an egg from the incubator
		/// list (sent earlier via <c>ZC_PETEGG_LIST</c>). Wire: short op,
		/// short eggInvId. Consumes the egg + adopts + summons.
		/// </summary>
		[PacketHandler(Op.CZ_SELECT_PETEGG)]
		public void CZ_SELECT_PETEGG(ZoneConnection conn, Packet packet)
		{
			var eggInvId = packet.GetShort();
			var character = conn.GetCurrentCharacter();

			if (character.Pet.HasContract)
			{
				character.ServerMessage(Localization.Get("A pet contract already exists."));
				return;
			}

			var egg = character.Inventory.GetItem(eggInvId);
			if (egg == null || egg.Type != Sabine.Shared.Const.ItemType.PetEgg)
			{
				Log.Debug("CZ_SELECT_PETEGG: Char '{0}' picked InvId {1} that isn't an egg.", character.Name, eggInvId);
				return;
			}

			var entry = PetTypeData.GetByEggItemId(egg.ClassId);
			if (entry == null)
			{
				Log.Debug("CZ_SELECT_PETEGG: No pet definition for egg item {0}.", egg.ClassId);
				return;
			}

			character.Inventory.DecrementItem(egg, 1);
			character.Pet.Adopt(entry, entry.Name);
			Sabine.Zone.Skills.Pets.PetService.Spawn(character);
		}

		/// <summary>
		/// CZ_PETEGG_INFO — legacy egg-details probe; rAthena ignores it.
		/// Registered as a logged no-op so it doesn't appear in the
		/// unknown-op log path.
		/// </summary>
		[PacketHandler(Op.CZ_PETEGG_INFO)]
		public void CZ_PETEGG_INFO(ZoneConnection conn, Packet packet)
		{
			// rAthena drops this packet entirely; client gets pet info via
			// the periodic ZC_PROPERTY_PET refresh instead.
		}

		/// <summary>
		/// CZ_TRYCAPTURE_MONSTER — client invoked a taming lure on a
		/// target monster (issued from the Taming Monster skill, which
		/// arms the lure use). Wire: short op, long targetHandle.
		/// </summary>
		/// <remarks>
		/// rAthena <c>clif_parse_CatchPet</c> → <c>pet_catch_process2</c>:
		/// resolves the target, validates the lure item, rolls a capture
		/// chance from <c>pet_db.capture</c>, and emits
		/// <c>ZC_TRYCAPTURE_MONSTER</c> with the result. On success the
		/// monster is removed and an egg item is added.
		/// </remarks>
		[PacketHandler(Op.CZ_TRYCAPTURE_MONSTER)]
		public void CZ_TRYCAPTURE_MONSTER(ZoneConnection conn, Packet packet)
		{
			var targetHandle = packet.GetInt();
			var character = conn.GetCurrentCharacter();

			if (character.Pet.HasContract)
			{
				Send.ZC_TRYCAPTURE_MONSTER(character, success: false);
				return;
			}

			if (!character.Map.TryGetCharacter(targetHandle, out var entity) || entity is not Monster monster)
			{
				Send.ZC_TRYCAPTURE_MONSTER(character, success: false);
				return;
			}

			var petData = PetTypeData.GetByMobId((int)monster.Data.Id);
			if (petData == null)
			{
				// Targeted mob has no pet definition — capture cannot work.
				Send.ZC_TRYCAPTURE_MONSTER(character, success: false);
				return;
			}

			Send.ZC_START_CAPTURE(character);

			// Capture chance: rAthena uses pet_db.capture out of 10000.
			// Sabine's PetData doesn't carry a capture-rate column today,
			// so we use 50% as a placeholder until the column is plumbed.
			// TODO: thread an actual capture rate through PetData.
			var roll = Yggdrasil.Util.RandomProvider.Get().Next(0, 100);
			var success = roll < 50;

			if (!success)
			{
				Send.ZC_TRYCAPTURE_MONSTER(character, success: false);
				return;
			}

			monster.Map?.RemoveNpc(monster);
			character.Inventory.AddItem(new Item(petData.EggItemId, 1));
			Send.ZC_TRYCAPTURE_MONSTER(character, success: true);
		}
	}
}

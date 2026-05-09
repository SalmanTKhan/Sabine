using System.Linq;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Zone.Skills.Handlers.Blacksmith;
using Sabine.Zone.World.Actors;
using Yggdrasil.Logging;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// CZ_REQ_ITEMREPAIR (op 0x01FD, EP4+) handler — reply step of the
	/// BS_REPAIRWEAPON exchange. Validates job, ingredient, and that
	/// the picker target still has the chosen broken item, then clears
	/// the broken flag, re-equips if applicable, and sends
	/// ZC_ACK_ITEMREPAIR to both caster and target.
	/// </summary>
	public partial class PacketHandler : PacketHandler<ZoneConnection>
	{
		[PacketHandler(Op.CZ_REQ_ITEMREPAIR)]
		public void CZ_REQ_ITEMREPAIR(ZoneConnection conn, Packet packet)
		{
			// EP4+: clients < 0x0600 don't carry this opcode and the
			// PacketTable wouldn't even register it for them, but guard
			// defensively against version-mixed dispatch.
			if (Game.Version < Versions.S600)
				return;

			var inventoryIndex = packet.GetShort();

			var caster = conn.GetCurrentCharacter();
			if (caster == null)
				return;

			// Caster job gate: only Blacksmith / Whitesmith / Mech can
			// resolve a repair attempt. Mirrors eA skill.c:7170.
			if (!IsRepairJob(caster.JobId))
			{
				caster.ServerMessage("Only Blacksmiths can repair items.");
				return;
			}

			// Resolve the picker target stashed by the cast handler.
			var targetAid = caster.Vars.Temp.GetInt(RepairWeaponHandler.PendingTargetVar, 0);
			if (targetAid == 0
				|| !ZoneServer.Instance.World.Maps.TryGetPlayerByAccountId(targetAid, out var target))
			{
				caster.ServerMessage("Repair target is no longer available.");
				return;
			}

			// One-shot: clear immediately so a duplicate reply can't
			// double-consume materials.
			caster.Vars.Temp.SetInt(RepairWeaponHandler.PendingTargetVar, 0);

			var item = target.Inventory.GetItem(inventoryIndex);
			if (item == null || !item.IsBroken)
			{
				Send.ZC_ACK_ITEMREPAIR(caster, inventoryIndex, 1); // failure
				return;
			}

			// Ingredient: one ore matching the item's tier. Mirrors
			// eA skill.c:skill_repairweapon material[] lookup. We
			// accept any of the tier ores below; this collapses
			// armor-vs-weapon into one path until the full data lookup
			// lands.
			// TODO(repair-ore): pull material id from a per-item tier
			// table once the data is wired; for now we accept any of
			// the four classic ores so the flow is testable.
			var oreId = ResolveTierOre(item);
			if (oreId == 0 || caster.Inventory.CountItem(oreId) <= 0)
			{
				caster.ServerMessage("You need a {0} to repair this item.", DescribeOre(oreId));
				Send.ZC_ACK_ITEMREPAIR(caster, inventoryIndex, 1); // failure
				return;
			}

			ConsumeOne(caster, oreId);

			// Restore: clear the broken flag, refresh client view, and
			// re-equip if the slot is free. EquippedOn is wiped on
			// break so we use WearSlots' first valid slot.
			item.IsBroken = false;

			target.Inventory.RefreshClient();
			target.Parameters.RecalculateAll();

			Send.ZC_ACK_ITEMREPAIR(caster, inventoryIndex, 0); // success
			if (target != caster)
				Send.ZC_ACK_ITEMREPAIR(target, inventoryIndex, 0);

			Log.Debug("CZ_REQ_ITEMREPAIR: '{0}' repaired '{1}' for '{2}' (item {3})",
				caster.Name, item.Data.Name, target.Name, item.ClassId);
		}

		private static bool IsRepairJob(JobId jobId)
		{
			return jobId == JobId.Blacksmith
				|| jobId == JobId.Whitesmith
				|| jobId == JobId.BabyBlacksmith
				|| jobId == JobId.Mechanic
				|| jobId == JobId.MechanicT
				|| jobId == JobId.BabyMechanic;
		}

		// eA skill.c:skill_repairweapon material[] : wlv1=Phracon (1010),
		// wlv2=Emveretarcon (1011), wlv3/4=Oridecon (984), armor=Steel
		// (999). Returns 0 when the item isn't repairable.
		private const int Phracon = 1010;
		private const int Emveretarcon = 1011;
		private const int Oridecon = 984;
		private const int Steel = 999;

		private static int ResolveTierOre(Item item)
		{
			var type = item.Data.Type;
			if (type == ItemType.Armor)
				return Steel;

			if (type == ItemType.Weapon || type == ItemType.RangedWeapon || type == ItemType.Weapon3)
			{
				return item.Data.WeaponLevel switch
				{
					1 => Phracon,
					2 => Emveretarcon,
					3 => Oridecon,
					4 => Oridecon,
					_ => 0,
				};
			}

			return 0;
		}

		private static string DescribeOre(int id) => id switch
		{
			Phracon => "Phracon",
			Emveretarcon => "Emveretarcon",
			Oridecon => "Oridecon",
			Steel => "Steel",
			_ => "matching ore",
		};

		private static void ConsumeOne(PlayerCharacter pc, int itemId)
		{
			foreach (var stack in pc.Inventory.GetItems(i => i.ClassId == itemId))
			{
				pc.Inventory.DecrementItem(stack, 1);
				return;
			}
		}
	}
}

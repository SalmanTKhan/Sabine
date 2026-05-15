using Sabine.Shared.Network;
using Sabine.Shared.Util;
using Sabine.Zone.Skills.Homunculi;
using Sabine.Zone.Skills.Mercenaries;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Player-driven homunculus opcodes. Despite the <c>_MER</c> suffix
	/// rA/eA give them, opcodes 0x022D (<c>CZ_COMMAND_MER</c>) and 0x0231
	/// (<c>CZ_RENAME_MER</c>) are actually the homunculus menu and rename
	/// in the EP9 wire — the standalone mercenary system uses 0x029F
	/// (<c>CZ_MER_COMMAND</c>) instead. To preserve the previously-wired
	/// merc behavior, these handlers dual-dispatch: a live homunculus
	/// takes priority, otherwise the handler falls through to the
	/// mercenary path.
	/// </summary>
	public partial class PacketHandler
	{
		/// <summary>
		/// CZ_COMMAND_MER (0x022D). Wire: short op, short type, byte command.
		/// Sabine's table sizes this at 5 bytes total (op + 3-byte body).
		/// command: 0 = info refresh, 1 = feed, 2 = delete/dismiss.
		/// Routes to homunculus if one is active; else falls through to
		/// the legacy mercenary dismiss path.
		/// </summary>
		[PacketHandler(Op.CZ_COMMAND_MER)]
		public void CZ_COMMAND_MER(ZoneConnection conn, Packet packet)
		{
			// EP9 layout: short type + byte command (3-byte body).
			var type = packet.GetShort();
			var command = packet.GetByte();
			var character = conn.GetCurrentCharacter();

			// Homunculus takes priority — this is the wire's real owner
			// per rA clif_parse_HomMenu.
			if (character.Homunculus != null && character.Homunculus.IsActive)
			{
				switch (command)
				{
					case 0: // info refresh
					{
						var entity = character.Homunculus.Entity;
						if (entity != null)
							Send.ZC_PROPERTY_HOMUN(character, entity);
						Send.ZC_HOSKILLINFO_LIST(character);
						break;
					}
					case 1: // feed
						HomunculusService.Feed(character);
						break;
					case 2: // delete / return-to-egg
						HomunculusService.Rest(character);
						break;
				}
				return;
			}

			// Legacy fallback: pre-homunc-aware code wired this opcode
			// to mercenary dismiss. Keep that behavior for sessions
			// where only a merc is present.
			if (character.Mercenary != null && character.Mercenary.IsActive)
			{
				if (command == 2)
					MercenaryService.Dismiss(character);
			}
		}

		/// <summary>
		/// CZ_RENAME_MER (0x0231). Wire: short op, 24-byte name. In rA
		/// this is <c>clif_parse_ChangeHomunculusName</c> — the homunculus
		/// rename, despite the misleading opcode name. Routes to
		/// homunculus first; if none is active, falls through to the
		/// previously-wired mercenary rename for legacy compatibility.
		/// </summary>
		[PacketHandler(Op.CZ_RENAME_MER)]
		public void CZ_RENAME_MER(ZoneConnection conn, Packet packet)
		{
			var name = packet.GetString(24);
			var character = conn.GetCurrentCharacter();

			if (string.IsNullOrWhiteSpace(name))
				return;

			if (character.Homunculus != null && character.Homunculus.HasContract)
			{
				HomunculusService.Rename(character, name);
				return;
			}

			if (character.Mercenary != null && character.Mercenary.IsActive)
			{
				character.Mercenary.Rename(name);
				var entity = character.Mercenary.Entity;
				if (entity != null)
				{
					entity.Name = name;
					Send.ZC_MER_INIT(character, entity);
				}
				character.ServerMessage(Localization.Get("Mercenary renamed to {0}."), name);
			}
		}
	}
}

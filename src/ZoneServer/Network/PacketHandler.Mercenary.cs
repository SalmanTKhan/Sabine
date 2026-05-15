using Sabine.Shared.Network;
using Sabine.Shared.Util;
using Sabine.Zone.Skills.Mercenaries;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Handlers for the rAthena mercenary panel: command (dismiss / info
	/// request) and rename. EP9 ships <c>CZ_COMMAND_MER</c>; EP10 renames
	/// it to <c>CZ_MER_COMMAND</c> with the same shape. Both opcodes
	/// route through the same logic so old clients still work.
	/// </summary>
	public partial class PacketHandler
	{
		/// <summary>
		/// CZ_MER_COMMAND (EP10+). Wire: short op, byte command.
		/// command: 1 = request mercenary info refresh, 2 = dismiss.
		/// </summary>
		[PacketHandler(Op.CZ_MER_COMMAND)]
		public void CZ_MER_COMMAND(ZoneConnection conn, Packet packet)
		{
			var command = packet.GetByte();
			var character = conn.GetCurrentCharacter();

			if (character.Mercenary == null || !character.Mercenary.IsActive)
				return;

			switch (command)
			{
				case 1: // info refresh
				{
					var entity = character.Mercenary.Entity;
					if (entity != null)
					{
						Send.ZC_MER_INIT(character, entity);
						Send.ZC_MER_SKILLINFO_LIST(character, entity);
					}
					break;
				}
				case 2: // dismiss
					MercenaryService.Dismiss(character);
					break;
			}
		}

		/// <summary>
		/// CZ_COMMAND_MER (EP9 legacy). Wire: short op, 3-byte body.
		/// Same intent as <c>CZ_MER_COMMAND</c>; rAthena's old clients
		/// send a wider body that includes a 16-bit pad before the
		/// command byte.
		/// </summary>
		[PacketHandler(Op.CZ_COMMAND_MER)]
		public void CZ_COMMAND_MER(ZoneConnection conn, Packet packet)
		{
			// EP9 layout: byte command + short pad.
			var command = packet.GetByte();
			var character = conn.GetCurrentCharacter();

			if (character.Mercenary == null || !character.Mercenary.IsActive)
				return;

			if (command == 2)
				MercenaryService.Dismiss(character);
		}

		/// <summary>
		/// CZ_RENAME_MER. Wire: short op, 24-byte name. Sabine's merc
		/// state doesn't currently carry a custom name (the mob template
		/// name is used), so this is wired but only updates the live
		/// entity's display name for the session. Promote to persisted
		/// rename when MercenaryComponent grows a Name field.
		/// </summary>
		[PacketHandler(Op.CZ_RENAME_MER)]
		public void CZ_RENAME_MER(ZoneConnection conn, Packet packet)
		{
			var name = packet.GetString(24);
			var character = conn.GetCurrentCharacter();

			if (character.Mercenary == null || !character.Mercenary.IsActive)
				return;

			if (string.IsNullOrWhiteSpace(name))
				return;

			var entity = character.Mercenary.Entity;
			if (entity != null)
				entity.Name = name;

			character.ServerMessage(Localization.Get("Mercenary renamed to {0}."), name);
		}
	}
}

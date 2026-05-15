using Sabine.Shared.Network;
using Sabine.Zone.Skills.Mercenaries;

namespace Sabine.Zone.Network
{
	/// <summary>
	/// Handlers for the standalone mercenary panel. EP10+ ships
	/// <c>CZ_MER_COMMAND</c> (0x029F) for the mercenary menu —
	/// distinct from the homunculus menu (0x022D) which kRO chose to
	/// label <c>CZ_COMMAND_MER</c>. The homunc-aware handler for that
	/// older opcode lives in <see cref="PacketHandler.Homunculus.cs"/>
	/// and includes a merc-dismiss fallback for sessions without a
	/// homunculus. Rename also uses 0x0231, routed via the homun
	/// handler with the same fallback.
	/// </summary>
	public partial class PacketHandler
	{
		/// <summary>
		/// CZ_MER_COMMAND (EP10+, 0x029F). Wire: short op, byte command.
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
	}
}

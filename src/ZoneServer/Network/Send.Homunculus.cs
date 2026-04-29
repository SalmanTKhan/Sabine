using Sabine.Shared.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Network
{
	public static partial class Send
	{
		/// <summary>
		/// Pushes the homunculus's full property block to the owner.
		/// Only sent when the current packet table includes
		/// <c>ZC_PROPERTY_HOMUN</c> (euRO EP10 / S2000 onward); on
		/// older clients the call is a no-op so the homun pipeline
		/// works without crashing on missing packets.
		/// </summary>
		public static void ZC_PROPERTY_HOMUN(PlayerCharacter owner, Homunculus homun)
		{
			if (!PacketTable.IsRegistered(Op.ZC_PROPERTY_HOMUN))
				return;

			var state = homun.State;
			var p = homun.Parameters;

			using var packet = Packet.Rent(Op.ZC_PROPERTY_HOMUN);

			// Layout follows rAthena ZC_PROPERTY_HOMUN (0x022E).
			// Fixed size 71 incl. 2-byte op header. Field count and
			// types match the homun-side surface; rendering hinges on
			// the client's UI implementation.
			packet.PutString(homun.Name, 24);
			packet.PutByte(0);                           // ucModified (rename flag)
			packet.PutShort((short)state.Level);
			packet.PutShort((short)state.Hunger);
			packet.PutShort((short)state.Intimacy);
			packet.PutShort(0);                          // weapon look
			packet.PutShort((short)p.AttackMax);
			packet.PutShort((short)p.MagicAttack);
			packet.PutShort((short)p.Hit);
			packet.PutShort((short)p.Critical);
			packet.PutShort((short)p.MeleeDefense);
			packet.PutShort((short)p.MagicDefense);
			packet.PutShort((short)p.Flee);
			packet.PutShort((short)p.AttackDelay);
			packet.PutShort((short)p.Hp);
			packet.PutShort((short)p.HpMax);
			packet.PutShort((short)p.Sp);
			packet.PutShort((short)p.SpMax);
			packet.PutInt(state.Exp);
			packet.PutInt(state.ExpToNext(state.Level));
			packet.PutShort((short)state.SkillPoints);
			packet.PutShort(0);                          // attack range / spare

			owner.Connection.Send(packet);
		}
	}
}

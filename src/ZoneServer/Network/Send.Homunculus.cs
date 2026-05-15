using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;

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

		/// <summary>
		/// Dumps the homunculus skill tree (rA <c>clif_homskillinfoblock</c>,
		/// 0x0235, variable). Each entry is 37 bytes: short skillId,
		/// int type, short level, short spCost, short atkRange, 24-byte
		/// name, byte upgradable. Header is the 4-byte standard variable
		/// packet prelude (op + size). No-op when the opcode isn't
		/// registered for the current client version.
		/// </summary>
		public static void ZC_HOSKILLINFO_LIST(PlayerCharacter owner)
		{
			if (!PacketTable.IsRegistered(Op.ZC_HOSKILLINFO_LIST))
				return;

			var state = owner.Homunculus;
			if (state == null || state.Type == HomunculusType.None)
				return;

			using var packet = Packet.Rent(Op.ZC_HOSKILLINFO_LIST);

			foreach (var entry in HomunculusTypeData.GetSkills(state.Type))
			{
				var learned = state.GetSkillLevel(entry.SkillId);
				var upgradable = state.SkillPoints > 0
					&& learned < entry.MaxLevel
					&& state.Level >= entry.RequiredHomunLevel
					&& (entry.Prerequisite == SkillId.None
						|| state.GetSkillLevel(entry.Prerequisite) >= entry.PrerequisiteLevel);

				packet.PutShort((short)entry.SkillId);
				packet.PutInt(1);                            // type — 1 = active
				packet.PutShort((short)learned);
				packet.PutShort(0);                          // spCost (unknown without skill-db lookup)
				packet.PutShort(0);                          // atkRange
				packet.PutString(entry.SkillId.ToString(), 24);
				packet.PutByte(upgradable ? (byte)1 : (byte)0);
			}

			owner.Connection.Send(packet);
		}

		/// <summary>
		/// Single-skill rank-up notification (rA <c>clif_homskillup</c>,
		/// 0x0239, 11 bytes total). Sent right after <see cref="HomunculusComponent.TryLearn"/>
		/// succeeds so the panel reflects the new rank without a full
		/// re-list. No-op on older eras that don't register the opcode.
		/// </summary>
		public static void ZC_HOSKILLINFO_UPDATE(PlayerCharacter owner, SkillId skillId, int level)
		{
			if (!PacketTable.IsRegistered(Op.ZC_HOSKILLINFO_UPDATE))
				return;

			using var packet = Packet.Rent(Op.ZC_HOSKILLINFO_UPDATE);
			packet.PutShort((short)skillId);
			packet.PutShort((short)level);
			packet.PutShort(0);              // spCost
			packet.PutShort(0);              // atkRange
			packet.PutByte(0);               // upgradable (post-update: usually 0 if at max)

			owner.Connection.Send(packet);
		}
	}
}

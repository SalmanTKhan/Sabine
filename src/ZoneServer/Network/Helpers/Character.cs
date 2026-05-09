using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Shared.Network.Helpers;
using Sabine.Shared.World;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Network.Helpers
{
	/// <summary>
	/// Character-related extension methods for Packet.
	/// </summary>
	public static class PacketCharacterHelpers
	{
		/// <summary>
		/// Returns the network id the entry should appear as on the
		/// client. For <see cref="Character"/> actors this honors the
		/// <see cref="Character.DisplayClassId"/> override (set by Hocus
		/// Pocus transforms and similar effects); other entries fall
		/// back to their <see cref="IStandEntry.IdentityId"/>.
		/// </summary>
		private static int GetDisplayNetworkId(IStandEntry entry)
		{
			if (entry is Character ch)
				return ((IdentityId)ch.DisplayClassId).ToNetwork();

			return entry.IdentityId.ToNetwork();
		}

		/// <summary>
		/// Adds stand entry information about character to packet.
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="character"></param>
		public static void AddStandEntry(this Packet packet, IStandEntry character)
		{
			packet.PutInt(character.Handle);
			packet.PutShort((short)character.Speed);

			if (Game.Version < Versions.Beta2)
			{
				if (Game.Version >= Versions.Beta1)
				{
					packet.PutShort(0);
					packet.PutShort(0);
					packet.PutShort((short)character.OptionState); // opt0 — falcon / peco / hide / cloak
					packet.PutByte(0);
				}

				packet.PutByte((byte)GetDisplayNetworkId(character));
				packet.PutByte((byte)character.Sex);
				packet.AddPackedPosition(character.Position, character.Direction);
				packet.PutByte(0);
				packet.PutByte(0);
				packet.PutByte((byte)character.HairId);
				packet.PutByte((byte)character.WeaponLook);
				packet.PutByte((byte)character.HeadBottomLook);
				packet.PutByte((byte)character.State);
			}
			else
			{
				packet.PutShort(0);     // Status Effects?
				packet.PutShort(0);     // Status Effects?
				packet.PutShort(0);     // Status Effects?
				packet.PutShort((short)GetDisplayNetworkId(character));
				packet.PutShort((short)character.HairId);
				packet.PutShort((short)character.WeaponLook);
				packet.PutShort((short)character.HeadBottomLook);
				packet.PutShort(0);     // Shield
				packet.PutShort((short)character.HeadTopLook);
				packet.PutShort((short)character.HeadMiddleLook);
				packet.PutShort(0);     // HairColor
				packet.PutShort(0);     // ClothesColor
				packet.PutShort((short)character.HeadTurn);
				packet.PutInt(0);       // GuildId?
				packet.PutShort(0);     // GuildEmblemId?
				packet.PutShort(0);     // Manner?
				packet.PutShort(0);     // Karma?
				packet.PutByte(0);      // BattleStance?
				packet.PutByte((byte)character.Sex);
				packet.AddPackedPosition(character.Position, character.Direction);
				packet.PutByte(0);      // ?
				packet.PutByte(0);      // ?
				packet.PutByte((byte)character.State);

				if (Game.Version >= Versions.S600)
					packet.PutShort(0); // ?
			}
		}

		/// <summary>
		/// Adds stand entry information about character to packet.
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="character"></param>
		public static void AddStandEntryNpc(this Packet packet, IStandEntry character)
		{
			packet.PutInt(character.Handle);
			packet.PutShort((short)character.Speed);

			if (Game.Version < Versions.S600)
			{
				if (Game.Version >= Versions.Beta1)
				{
					packet.PutShort(0);
					packet.PutShort(0);
					packet.PutShort((short)character.OptionState); // opt0 — falcon / peco / hide / cloak
					packet.PutByte(0);
				}

				packet.PutByte((byte)GetDisplayNetworkId(character));
				packet.PutByte((byte)character.Sex);
				packet.AddPackedPosition(character.Position, character.Direction);
				packet.PutByte(0);
				packet.PutByte(0);
				packet.PutByte((byte)character.HairId);
				packet.PutByte((byte)character.WeaponLook);
				packet.PutByte((byte)character.HeadBottomLook);
			}
			else
			{
				packet.PutShort(0);     // Status Effects?
				packet.PutShort(0);     // Status Effects?
				packet.PutShort(0);     // Status Effects?
				packet.PutShort((short)GetDisplayNetworkId(character));
				packet.PutShort((short)character.HairId);
				packet.PutShort((short)character.WeaponLook);
				packet.PutShort((short)character.HeadBottomLook);
				packet.PutShort(0);     // Shield?
				packet.PutShort((short)character.HeadTopLook);
				packet.PutShort((short)character.HeadMiddleLook);
				packet.PutShort(0);     // HairColor?
				packet.PutShort(0);     // ClothesColor?
				packet.PutShort(0);     // HeadDir?
				packet.PutByte(0);      // BattleStance?
				packet.PutByte((byte)character.Sex);
				packet.AddPackedPosition(character.Position, character.Direction);
				packet.PutByte(0);      // ?
				packet.PutByte(0);      // ?
			}
		}

		/// <summary>
		/// Adds new entry information about character to packet.
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="character"></param>
		public static void AddNewEntry(this Packet packet, IStandEntry character)
		{
			packet.PutInt(character.Handle);
			packet.PutShort((short)character.Speed);

			if (Game.Version < Versions.S500)
			{
				if (Game.Version >= Versions.Beta1)
				{
					packet.PutShort(0);
					packet.PutShort(0);
					packet.PutShort((short)character.OptionState); // opt0 — falcon / peco / hide / cloak
					packet.PutByte(0);
				}

				packet.PutByte((byte)GetDisplayNetworkId(character));
				packet.PutByte((byte)character.Sex);
				packet.AddPackedPosition(character.Position, character.Direction);
				packet.PutByte(0);
				packet.PutByte(0);
				packet.PutByte((byte)character.HairId);
				packet.PutByte((byte)character.WeaponLook);
				packet.PutByte((byte)character.HeadBottomLook);
			}
			else
			{
				packet.PutShort(0);     // Status Effects?
				packet.PutShort(0);     // Status Effects?
				packet.PutShort(0);     // Status Effects?
				packet.PutShort((short)GetDisplayNetworkId(character));
				packet.PutShort((short)character.HairId);
				packet.PutShort((short)character.WeaponLook);
				packet.PutShort((short)character.HeadBottomLook);
				packet.PutShort(0);     // Shield?
				packet.PutShort((short)character.HeadTopLook);
				packet.PutShort((short)character.HeadMiddleLook);
				packet.PutShort(0);     // HairColor
				packet.PutShort(0);     // ClothesColor
				packet.PutShort(0);     // HeadDir
				packet.PutInt(0);       // GuildId?
				packet.PutShort(0);     // GuildEmblemId?
				packet.PutShort(0);     // Manner?
				packet.PutShort(0);     // Karma?
				packet.PutByte(0);      // BattleStance?
				packet.PutByte((byte)character.Sex);
				packet.AddPackedPosition(character.Position, character.Direction);
				packet.PutByte(0);      // ?
				packet.PutByte(0);      // ?

				if (Game.Version >= Versions.S600)
					packet.PutShort(0); // ?
			}
		}

		/// <summary>
		/// Adds stand entry information about character to packet.
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="character"></param>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static void AddMoveEntry(this Packet packet, IStandEntry character, Position from, Position to)
		{
			packet.PutInt(character.Handle);
			packet.PutShort((short)character.Speed);

			if (Game.Version < Versions.S500)
			{
				if (Game.Version >= Versions.Beta1)
				{
					packet.PutShort(0);
					packet.PutShort(0);
					packet.PutShort((short)character.OptionState); // opt0 — falcon / peco / hide / cloak
					packet.PutByte(0);
				}

				packet.PutByte((byte)GetDisplayNetworkId(character));
				packet.PutByte((byte)character.Sex);
				packet.AddPackedMove(from, to, 8, 8);
				packet.PutShort(0);
				packet.PutByte((byte)character.HairId);
				packet.PutByte((byte)character.WeaponLook);
				packet.PutByte((byte)character.HeadBottomLook);
				packet.PutInt(Game.GetTick());
			}
			else
			{
				packet.PutShort(0);     // Status Effects?
				packet.PutShort(0);     // Status Effects?
				packet.PutShort(0);     // Status Effects?
				packet.PutShort((short)GetDisplayNetworkId(character));
				packet.PutShort((short)character.HairId);
				packet.PutShort((short)character.WeaponLook);
				packet.PutShort((short)character.HeadBottomLook);
				packet.PutInt(Game.GetTick());
				packet.PutShort(0);     // Shield
				packet.PutShort((short)character.HeadTopLook);
				packet.PutShort((short)character.HeadMiddleLook);
				packet.PutShort(0);     // HairColor
				packet.PutShort(0);     // ClothesColor
				packet.PutShort(0);     // HeadDir
				packet.PutInt(0);       // GuildId?
				packet.PutShort(0);     // GuildEmblemId?
				packet.PutShort(0);     // Manner?
				packet.PutShort(0);     // Karma?
				packet.PutByte(0);      // BattleStance?
				packet.PutByte((byte)character.Sex);
				packet.AddPackedMove(from, to, 8, 8);
				packet.PutByte(0);      // ?
				packet.PutByte(0);      // ?

				if (Game.Version >= Versions.S600)
					packet.PutShort(0); // ?
			}
		}
	}
}

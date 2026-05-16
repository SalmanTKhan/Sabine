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
			if (Game.Version >= Versions.Sabine)
			{
				AddStandEntrySabine(packet, character);
				return;
			}

			if (Game.Version >= Versions.S2500)
			{
				AddStandEntryV4(packet, character);
				return;
			}

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
			if (Game.Version >= Versions.Sabine)
			{
				// Sabine collapses the PC/NPC entry split — rA serves
				// both from the same packet_idle_unit shape with the
				// objecttype byte distinguishing roles.
				AddStandEntrySabine(packet, character);
				return;
			}

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
			if (Game.Version >= Versions.Sabine)
			{
				AddNewEntrySabine(packet, character);
				return;
			}

			if (Game.Version >= Versions.S2500)
			{
				AddNewEntryV4(packet, character);
				return;
			}

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
			if (Game.Version >= Versions.Sabine)
			{
				AddMoveEntrySabine(packet, character, from, to);
				return;
			}

			if (Game.Version >= Versions.S2500)
			{
				AddMoveEntryV4(packet, character, from, to);
				return;
			}

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

		// ---- S2500 / PV23 (kRO Sak EP12) actor entry v4 helpers ----------
		//
		// Layouts mirror rAthena's packet_idle_unit / packet_spawn_unit /
		// packet_unit_walking at PACKETVER 20080910 (Ref/rathena/src/map/
		// packets_struct.hpp:758,832,687 and clif.cpp:1041 clif_set_unit_idle).
		// Field naming below tracks rA's struct member names so the source
		// stays diff-able against future PV bumps.

		private static void AddStandEntryV4(Packet packet, IStandEntry character)
		{
			// 0x02EE, 60 bytes (58 body). Idle unit at PV>=20080102 &&
			// PV<20091103: no PacketLength, no objecttype, no AID; the
			// trailing `state` byte distinguishes idle from spawn.
			packet.PutInt(character.Handle);                                    // GID
			packet.PutShort((short)character.Speed);                            // speed
			packet.PutShort(0);                                                 // bodyState  (sc->opt1)
			packet.PutShort(0);                                                 // healthState(sc->opt2)
			packet.PutInt((int)character.OptionState);                          // effectState(sc->option) — int32 at PV>=20080102
			packet.PutShort((short)GetDisplayNetworkId(character));             // job (LOOK_BASE)
			packet.PutShort((short)character.HairId);                           // head (LOOK_HAIR)
			packet.PutInt(character.WeaponLook);                                // weapon (LOOK_WEAPON) — uint32 at PV>7
			packet.PutShort((short)character.HeadBottomLook);                   // accessory (LOOK_HEAD_BOTTOM)
			packet.PutShort((short)character.HeadTopLook);                      // accessory2 (LOOK_HEAD_TOP)
			packet.PutShort((short)character.HeadMiddleLook);                   // accessory3 (LOOK_HEAD_MID)
			packet.PutShort(0);                                                 // headpalette (LOOK_HAIR_COLOR)
			packet.PutShort(0);                                                 // bodypalette (LOOK_CLOTHES_COLOR)
			packet.PutShort((short)character.HeadTurn);                         // headDir
			packet.PutInt(0);                                                   // GUID
			packet.PutShort(0);                                                 // GEmblemVer
			packet.PutShort(0);                                                 // honor (manner)
			packet.PutInt(0);                                                   // virtue (sc->opt3) — int32 at PV>7
			packet.PutByte(0);                                                  // isPKModeON
			packet.PutByte((byte)character.Sex);                                // sex
			packet.AddPackedPosition(character.Position, character.Direction);  // PosDir[3]
			packet.PutByte(5);                                                  // xSize (PC default)
			packet.PutByte(5);                                                  // ySize
			packet.PutByte((byte)character.State);                              // state (vd->dead_sit)
			packet.PutShort(0);                                                 // clevel
			packet.PutShort(0);                                                 // font (PV>=20080102)
		}

		private static void AddNewEntryV4(Packet packet, IStandEntry character)
		{
			// 0x02ED, 59 bytes (57 body). Spawn unit — identical to idle
			// but without the trailing `state` byte.
			packet.PutInt(character.Handle);
			packet.PutShort((short)character.Speed);
			packet.PutShort(0);
			packet.PutShort(0);
			packet.PutInt((int)character.OptionState);
			packet.PutShort((short)GetDisplayNetworkId(character));
			packet.PutShort((short)character.HairId);
			packet.PutInt(character.WeaponLook);
			packet.PutShort((short)character.HeadBottomLook);
			packet.PutShort((short)character.HeadTopLook);
			packet.PutShort((short)character.HeadMiddleLook);
			packet.PutShort(0);
			packet.PutShort(0);
			packet.PutShort((short)character.HeadTurn);
			packet.PutInt(0);
			packet.PutShort(0);
			packet.PutShort(0);
			packet.PutInt(0);
			packet.PutByte(0);
			packet.PutByte((byte)character.Sex);
			packet.AddPackedPosition(character.Position, character.Direction);
			packet.PutByte(5);
			packet.PutByte(5);
			packet.PutShort(0);                                                 // clevel
			packet.PutShort(0);                                                 // font
		}

		private static void AddMoveEntryV4(Packet packet, IStandEntry character, Position from, Position to)
		{
			// 0x02EC, 67 bytes (65 body). Walking unit — has objecttype
			// (>= 20071106), moveStartTime, and MoveData[6] in place of PosDir[3].
			packet.PutByte((byte)ClifBlType(character));                        // objecttype
			packet.PutInt(character.Handle);                                    // GID
			packet.PutShort((short)character.Speed);                            // speed
			packet.PutShort(0);                                                 // bodyState
			packet.PutShort(0);                                                 // healthState
			packet.PutInt((int)character.OptionState);                          // effectState
			packet.PutShort((short)GetDisplayNetworkId(character));             // job
			packet.PutShort((short)character.HairId);                           // head
			packet.PutInt(character.WeaponLook);                                // weapon
			packet.PutShort((short)character.HeadBottomLook);                   // accessory
			packet.PutInt(Game.GetTick());                                      // moveStartTime
			packet.PutShort((short)character.HeadTopLook);                      // accessory2
			packet.PutShort((short)character.HeadMiddleLook);                   // accessory3
			packet.PutShort(0);                                                 // headpalette
			packet.PutShort(0);                                                 // bodypalette
			packet.PutShort((short)character.HeadTurn);                         // headDir
			packet.PutInt(0);                                                   // GUID
			packet.PutShort(0);                                                 // GEmblemVer
			packet.PutShort(0);                                                 // honor
			packet.PutInt(0);                                                   // virtue
			packet.PutByte(0);                                                  // isPKModeON
			packet.PutByte((byte)character.Sex);                                // sex
			packet.AddPackedMove(from, to, 8, 8);                               // MoveData[6]
			packet.PutByte(5);                                                  // xSize
			packet.PutByte(5);                                                  // ySize
			packet.PutShort(0);                                                 // clevel
			packet.PutShort(0);                                                 // font
		}

		/// <summary>
		/// Maps an <see cref="IStandEntry"/> to rAthena's <c>clif_bl_type</c>
		/// object-type byte (PC=0, NPC=1, ITEM=2, SKILL=3, UNKNOWN=4,
		/// MOB=5, EVENT=6, PET=7, HOMUN=8, MER=9). Used by the v4
		/// walking entry; idle/spawn at PV20080910 omit this field.
		/// </summary>
		private static int ClifBlType(IStandEntry character)
		{
			return character switch
			{
				PlayerCharacter => 0,
				Monster => 5,
				_ => 1, // NPC catch-all; pets/mercs (when reachable
				        // through this path) render via their owner's
				        // viewdata anyway.
			};
		}

		// ---- Sabine PV 50000 (rA-latest) actor entry helpers ------------
		//
		// Layouts mirror rAthena's packet_idle_unit / packet_spawn_unit /
		// packet_unit_walking at PACKETVER_MAIN_NUM >= 20181121 (shield
		// field added) with all post-S2500 growth applied:
		//   - 20091103: packetLength + objecttype
		//   - 20101124: robe
		//   - 20120221: maxHP + HP + isBoss
		//   - 20131223: AID, name[24]
		//   - 20150513: body
		//   - 20181121: shield
		// Wire sizes (header inclusive): idle=108, spawn=107, walking=114.
		// Refs: rathena packets_struct.hpp:687,758,832 and clif.cpp
		// clif_set_unit_idle / clif_spawn_unit / clif_set_unit_walking.

		private static (int hp, int maxHp, int level, byte isBoss) GetUnitVitals(IStandEntry character)
		{
			if (character is Monster mob)
				return (mob.Parameters.Hp, mob.Parameters.HpMax, mob.Parameters.BaseLevel, (byte)0);

			if (character is Character ch)
				return (ch.Parameters.Hp, ch.Parameters.HpMax, ch.Parameters.BaseLevel, (byte)0);

			return (0, 0, 0, 0);
		}

		private static int GetGuildId(IStandEntry character)
			=> character is Character ch ? ch.GuildId : 0;

		/// <summary>
		/// Returns rA's <c>AID</c> for a stand-entry actor. For
		/// <see cref="PlayerCharacter"/> this is the account id (so
		/// whisper / party / friend UIs can route by AID, matching
		/// official kRO semantics). All non-PC actors collapse AID to
		/// the handle since they have no account distinction.
		/// </summary>
		private static int GetAid(IStandEntry character)
		{
			if (character is PlayerCharacter pc && pc.Connection?.Account != null)
				return pc.Connection.Account.Id;

			return character.Handle;
		}

		private static void AddStandEntrySabine(Packet packet, IStandEntry character)
		{
			// packet_idle_unit at PV 20181121+. Total wire = 108.
			// Body = 106; PacketLength field carries the full wire size.
			var (hp, maxHp, level, isBoss) = GetUnitVitals(character);

			packet.PutShort(108);                                              // packetLength
			packet.PutByte((byte)ClifBlType(character));                       // objecttype
			packet.PutInt(GetAid(character));                                  // AID (account id for PCs)
			packet.PutInt(character.Handle);                                   // GID
			packet.PutShort((short)character.Speed);                           // speed
			packet.PutShort(0);                                                // bodyState
			packet.PutShort(0);                                                // healthState
			packet.PutInt((int)character.OptionState);                         // effectState
			packet.PutShort((short)GetDisplayNetworkId(character));            // job
			packet.PutShort((short)character.HairId);                          // head
			packet.PutInt(character.WeaponLook);                               // weapon
			packet.PutInt(0);                                                  // shield (PV>=20181121)
			packet.PutShort((short)character.HeadBottomLook);                  // accessory
			packet.PutShort((short)character.HeadTopLook);                     // accessory2
			packet.PutShort((short)character.HeadMiddleLook);                  // accessory3
			packet.PutShort(0);                                                // headpalette
			packet.PutShort(0);                                                // bodypalette
			packet.PutShort((short)character.HeadTurn);                        // headDir
			packet.PutShort(0);                                                // robe
			packet.PutInt(GetGuildId(character));                              // GUID
			packet.PutShort(0);                                                // GEmblemVer
			packet.PutShort(0);                                                // honor
			packet.PutInt(0);                                                  // virtue
			packet.PutByte(0);                                                 // isPKModeON
			packet.PutByte((byte)character.Sex);                               // sex
			packet.AddPackedPosition(character.Position, character.Direction); // PosDir[3]
			packet.PutByte(5);                                                 // xSize
			packet.PutByte(5);                                                 // ySize
			packet.PutByte((byte)character.State);                             // state
			packet.PutShort((short)level);                                     // clevel
			packet.PutShort(0);                                                // font
			packet.PutInt(maxHp);                                              // maxHP
			packet.PutInt(hp);                                                 // HP
			packet.PutByte(isBoss);                                            // isBoss
			packet.PutShort(0);                                                // body
			packet.PutString(character.Name, 24);                              // name[24]
		}

		private static void AddNewEntrySabine(Packet packet, IStandEntry character)
		{
			// packet_spawn_unit at PV 20181121+. Total wire = 107.
			// Same as idle but without the trailing `state` byte.
			var (hp, maxHp, level, isBoss) = GetUnitVitals(character);

			packet.PutShort(107);
			packet.PutByte((byte)ClifBlType(character));
			packet.PutInt(GetAid(character));                                  // AID
			packet.PutInt(character.Handle);                                   // GID
			packet.PutShort((short)character.Speed);
			packet.PutShort(0);
			packet.PutShort(0);
			packet.PutInt((int)character.OptionState);
			packet.PutShort((short)GetDisplayNetworkId(character));
			packet.PutShort((short)character.HairId);
			packet.PutInt(character.WeaponLook);
			packet.PutInt(0);
			packet.PutShort((short)character.HeadBottomLook);
			packet.PutShort((short)character.HeadTopLook);
			packet.PutShort((short)character.HeadMiddleLook);
			packet.PutShort(0);
			packet.PutShort(0);
			packet.PutShort((short)character.HeadTurn);
			packet.PutShort(0);
			packet.PutInt(GetGuildId(character));
			packet.PutShort(0);
			packet.PutShort(0);
			packet.PutInt(0);
			packet.PutByte(0);
			packet.PutByte((byte)character.Sex);
			packet.AddPackedPosition(character.Position, character.Direction);
			packet.PutByte(5);
			packet.PutByte(5);
			packet.PutShort((short)level);
			packet.PutShort(0);
			packet.PutInt(maxHp);
			packet.PutInt(hp);
			packet.PutByte(isBoss);
			packet.PutShort(0);
			packet.PutString(character.Name, 24);
		}

		private static void AddMoveEntrySabine(Packet packet, IStandEntry character, Position from, Position to)
		{
			// packet_unit_walking at PV 20181121+. Total wire = 114.
			// Inserts `moveStartTime` after accessory and replaces
			// PosDir[3] with MoveData[6]; no `state` byte.
			var (hp, maxHp, level, isBoss) = GetUnitVitals(character);

			packet.PutShort(114);
			packet.PutByte((byte)ClifBlType(character));
			packet.PutInt(GetAid(character));                                  // AID
			packet.PutInt(character.Handle);                                   // GID
			packet.PutShort((short)character.Speed);
			packet.PutShort(0);
			packet.PutShort(0);
			packet.PutInt((int)character.OptionState);
			packet.PutShort((short)GetDisplayNetworkId(character));
			packet.PutShort((short)character.HairId);
			packet.PutInt(character.WeaponLook);
			packet.PutInt(0);                                                  // shield
			packet.PutShort((short)character.HeadBottomLook);                  // accessory
			packet.PutInt(Game.GetTick());                                     // moveStartTime
			packet.PutShort((short)character.HeadTopLook);                     // accessory2
			packet.PutShort((short)character.HeadMiddleLook);                  // accessory3
			packet.PutShort(0);
			packet.PutShort(0);
			packet.PutShort((short)character.HeadTurn);
			packet.PutShort(0);
			packet.PutInt(GetGuildId(character));
			packet.PutShort(0);
			packet.PutShort(0);
			packet.PutInt(0);
			packet.PutByte(0);
			packet.PutByte((byte)character.Sex);
			packet.AddPackedMove(from, to, 8, 8);                              // MoveData[6]
			packet.PutByte(5);
			packet.PutByte(5);
			packet.PutShort((short)level);
			packet.PutShort(0);
			packet.PutInt(maxHp);
			packet.PutInt(hp);
			packet.PutByte(isBoss);
			packet.PutShort(0);
			packet.PutString(character.Name, 24);
		}
	}
}

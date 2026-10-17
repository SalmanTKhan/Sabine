using Sabine.Char.Database;
using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Shared.Data.Databases;
using Sabine.Shared.Network;

namespace Sabine.Char.Network.Helpers
{
	/// <summary>
	/// Character related extensions for the Packet class.
	/// </summary>
	public static class PacketCharacterExtensions
	{
		/// <summary>
		/// Writes character's information to packet.
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="character"></param>
		public static void AddCharacter(this Packet packet, Character character)
		{
			var jobLevel = character.JobLevel;
			var jobExp = character.JobExp;

			// Always display job level and EXP as 0 if the feature
			// isn't enabled
			if (!CharServer.Instance.Data.Features.IsEnabled(FeatureId.JobLevels))
			{
				jobLevel = 0;
				jobExp = 0;
			}

			if (Game.IsGoro)
			{
				packet.AddCharacterGoro(character, jobLevel, jobExp);
				return;
			}

			if (Game.IsNostalRo)
			{
				packet.AddCharacterNostalRo(character, jobLevel, jobExp);
				return;
			}

			packet.PutInt(character.Id);
			packet.PutInt(character.BaseExp);
			packet.PutInt(character.Zeny);
			packet.PutInt(jobExp);
			packet.PutInt(jobLevel); // Job Level in Beta1
			packet.PutInt(0); // ?
			packet.PutInt(0); // status effects bitmask?

			if (Game.Version >= Versions.Beta1)
			{
				packet.PutInt(0); // ?
				packet.PutInt(0); // Karma?
				packet.PutInt(0); // Manner?
			}

			packet.PutShort((short)character.StatPoints);
			packet.PutShort((short)character.Hp);
			packet.PutShort((short)character.HpMax);
			packet.PutShort((short)character.Sp);
			packet.PutShort((short)character.SpMax);
			packet.PutShort((short)character.Speed);

			if (Game.Version < Versions.Beta1)
			{
				packet.PutShort(0); // Karma?
				packet.PutShort(0); // Manner?
			}

			if (Game.Version < Versions.Beta2)
			{
				packet.PutString(character.Name, Sizes.CharacterNames);
				packet.PutByte((byte)character.JobId);
				packet.PutByte((byte)character.BaseLevel);
				packet.PutByte((byte)jobLevel); // Job Level in Alpha
				packet.PutByte((byte)character.Str);
				packet.PutByte((byte)character.Agi);
				packet.PutByte((byte)character.Vit);
				packet.PutByte((byte)character.Int);
				packet.PutByte((byte)character.Dex);
				packet.PutByte((byte)character.Luk);
				packet.PutByte((byte)character.Slot);
				packet.PutByte(0); // Gap
				packet.PutByte((byte)character.HairId);
				packet.PutByte((byte)character.WeaponLook);
				packet.PutByte((byte)character.HeadBottomLook);
			}
			else
			{
				packet.PutShort((short)character.JobId);
				packet.PutShort((short)character.HairId);
				packet.PutShort((short)character.WeaponLook);
				packet.PutShort((short)character.BaseLevel);
				packet.PutShort(0); // SkillPoints
				packet.PutShort((short)character.HeadBottomLook);
				packet.PutShort(0); // ShieldId
				packet.PutShort((short)character.HeadTopLook);
				packet.PutShort((short)character.HeadMiddleLook);
				packet.PutShort((short)character.HairColorId);
				packet.PutShort(0); // ClothesColorId
				packet.PutString(character.Name, Sizes.CharacterNames);
				packet.PutByte((byte)character.Str);
				packet.PutByte((byte)character.Agi);
				packet.PutByte((byte)character.Vit);
				packet.PutByte((byte)character.Int);
				packet.PutByte((byte)character.Dex);
				packet.PutByte((byte)character.Luk);

				if (Game.Version < Versions.S2500)
				{
					packet.PutByte((byte)character.Slot);
					packet.PutByte(0);
				}
				else
				{
					packet.PutShort((short)character.Slot);
					packet.PutShort(0);
				}
			}
		}

		private static void AddCharacterGoro(this Packet packet, Character character, int jobLevel, int jobExp)
		{
			// rAthena CHARACTER_INFO at PACKETVER 2008-09-10 is a fixed
			// 108-byte record (the HC_ACCEPT_MAKECHAR packet is 110 bytes
			// including its header). Keep every field at its legacy width;
			// the later 64-bit HP/EXP and slot extensions are not present.
			packet.PutInt(character.Id);
			packet.PutInt(character.BaseExp);
			packet.PutInt(character.Zeny);
			packet.PutInt(jobExp);
			packet.PutInt(jobLevel);
			packet.PutInt(0); // body state
			packet.PutInt(0); // health state
			packet.PutInt(0); // effect state
			packet.PutInt(0); // virtue
			packet.PutInt(0); // honor
			packet.PutShort((short)character.StatPoints);
			packet.PutShort((short)character.Hp);
			packet.PutShort((short)character.HpMax);
			packet.PutShort((short)character.Sp);
			packet.PutShort((short)character.SpMax);
			packet.PutShort((short)character.Speed);
			packet.PutShort((short)character.JobId);
			packet.PutShort((short)character.HairId);
			packet.PutShort((short)character.WeaponLook);
			packet.PutShort((short)character.BaseLevel);
			packet.PutShort(0); // skill points
			packet.PutShort((short)character.HeadBottomLook);
			packet.PutShort(0); // shield
			packet.PutShort((short)character.HeadTopLook);
			packet.PutShort((short)character.HeadMiddleLook);
			packet.PutShort((short)character.HairColorId);
			packet.PutShort(0); // body palette
			packet.PutString(character.Name, Sizes.CharacterNames);
			packet.PutByte((byte)character.Str);
			packet.PutByte((byte)character.Agi);
			packet.PutByte((byte)character.Vit);
			packet.PutByte((byte)character.Int);
			packet.PutByte((byte)character.Dex);
			packet.PutByte((byte)character.Luk);
			packet.PutShort((short)character.Slot);
			packet.PutShort(0); // changed-name / reserved tail
		}

		private static void AddCharacterNostalRo(this Packet packet, Character character, int jobLevel, int jobExp)
		{
			// CharacterInfoNeoUnion at PACKETVER 20111102 is 144 bytes.
			packet.PutInt(character.Id);
			packet.PutInt(character.BaseExp);
			packet.PutInt(character.Zeny);
			packet.PutInt(jobExp);
			packet.PutInt(jobLevel);
			packet.PutInt(0); // body state
			packet.PutInt(0); // health state
			packet.PutInt(0); // effect state
			packet.PutInt(0); // virtue
			packet.PutInt(0); // honor
			packet.PutShort((short)character.StatPoints);
			packet.PutInt(character.Hp);
			packet.PutInt(character.HpMax);
			packet.PutShort((short)character.Sp);
			packet.PutShort((short)character.SpMax);
			packet.PutShort((short)character.Speed);
			packet.PutShort((short)character.JobId);
			packet.PutShort((short)character.HairId);
			packet.PutShort((short)character.WeaponLook);
			packet.PutShort((short)character.BaseLevel);
			packet.PutShort(0); // skill points
			packet.PutShort((short)character.HeadBottomLook);
			packet.PutShort(0); // shield
			packet.PutShort((short)character.HeadTopLook);
			packet.PutShort((short)character.HeadMiddleLook);
			packet.PutShort((short)character.HairColorId);
			packet.PutShort(0); // body color
			packet.PutString(character.Name, 24);
			packet.PutByte((byte)character.Str);
			packet.PutByte((byte)character.Agi);
			packet.PutByte((byte)character.Vit);
			packet.PutByte((byte)character.Int);
			packet.PutByte((byte)character.Dex);
			packet.PutByte((byte)character.Luk);
			packet.PutByte((byte)character.Slot);
			packet.PutByte((byte)character.HairColorId);
			packet.PutShort(0); // changed character name
			packet.PutString("", 16); // last map
			packet.PutInt(0); // delete date
			packet.PutInt(0); // robe
			packet.PutInt(0); // slot addon
			packet.PutInt(0); // rename addon
		}
	}
}

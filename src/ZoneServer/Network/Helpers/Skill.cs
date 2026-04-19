using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Network;
using Sabine.Zone.Skills;
using Sabine.Zone.World.Entities;
using Sabine.Zone.World.Entities.Components.Characters;

namespace Sabine.Zone.Network.Helpers
{
	/// <summary>
	/// Provides extension methods for writing skill-related data to packets.
	/// </summary>
	public static class PacketSkillHelpers
	{
		/// <summary>
		/// Writes skill data to the packet.
		/// </summary>
		public static void AddSkillData(this Packet packet, PlayerCharacter player, Skill skill)
		{
			if (!ZoneServer.Instance.Data.Skills.TryFind(skill.Id, out var skillData))
				return;

			packet.PutShort((short)skill.Id);

			// 'inf' is a bitmask of skill properties (e.g., passive, targeted).
			packet.PutInt((int)skill.Data.TypeFlags);
			packet.PutShort((short)skill.Level);
			packet.PutShort((short)skillData.GetSpCost(skill.Level));
			packet.PutShort((short)skillData.GetRange(skill.Level));

			var nameLength = (Game.Version < Versions.Beta1) ? 16 : 24;
			packet.PutString(skillData.StringId, nameLength);

			var canUpgrade = player.Skills.CanUpgrade(skill.Id);
			packet.PutByte(canUpgrade);

			// Alpha client's ZC_ADD_SKILL packet has a total size of 33 bytes.
			// Opcode (2) + Data (29) + Padding (2).
			if (Game.Version < Versions.Beta1)
			{
				packet.PutEmpty(2);
			}
		}

		/// <summary>
		/// Writes skill data to the packet.
		/// </summary>
		public static void AddSkill(this Packet packet, PlayerCharacter player, Skill skill)
			=> packet.AddSkillData(player, skill);

		/// <summary>
		/// Writes skill data to the packet.
		/// </summary>
		public static void AddSkill(this Packet packet, Skill skill)
		{
			if (skill.Character is PlayerCharacter player)
			{
				packet.AddSkillData(player, skill);
				return;
			}

			packet.PutShort((short)skill.Id);
			packet.PutInt((int)skill.Data.TypeFlags);
			packet.PutShort((short)skill.Level);
			packet.PutShort((short)skill.SpCost);
			packet.PutShort((short)skill.Range);
			packet.PutString(skill.Data.StringId, Game.Version < Versions.Beta1 ? 16 : 24);
			packet.PutByte(skill.CanBeLeveled);

			if (Game.Version < Versions.Beta1)
				packet.PutEmpty(2);
		}
	}
}

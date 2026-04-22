using Sabine.Shared;
using Sabine.Shared.Network;
using Sabine.Zone.Skills;
using Sabine.Zone.World.Actors;

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
		/// Adds the skill's information to the packet.
		/// </summary>
		/// <param name="packet"></param>
		/// <param name="skill"></param>
		public static void AddSkill(this Packet packet, Skill skill)
		{
			// The alpha client has some handlers for skill packets, but
			// they're limited to information about the skills. There are
			// no usage packets and the client doesn't even display the
			// skills sent to it. Still, the version checks here allow
			// us to send the skill info without issues.

			packet.PutShort((short)skill.Id);

			if (Game.Version >= Versions.Beta1)
			{
				packet.PutShort((short)skill.Data.TargetType);
				packet.PutShort(0);
			}

			packet.PutShort((short)skill.Level);
			packet.PutShort((short)skill.SpCost);

			if (Game.Version >= Versions.Beta1)
			{
				packet.PutShort((short)skill.Range);
			}

			packet.PutString(skill.Data.StringId, Sizes.SkillNames);
			packet.PutByte(skill.CanBeLeveled);

			if (Game.Version < Versions.Beta1)
				packet.PutEmpty(2);
		}
	}
}

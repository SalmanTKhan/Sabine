using Sabine.Shared;
using Sabine.Shared.Const;
using Sabine.Shared.Data;
using Sabine.Shared.Data.Databases;
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
			// This assumes a data structure for skills exists in SabineData.
			if (!SabineData.Skills.TryFind(skill.Id, out var skillData))
				return;

			packet.PutShort((short)skill.Id);

			// 'inf' is a bitmask of skill properties (e.g., passive, targeted).
			packet.PutInt((int)skill.Data.TypeFlags);
			packet.PutShort((short)skill.Level);
			packet.PutShort((short)skillData.GetSpCost(skill.Level));
			packet.PutShort((short)skillData.GetRange(skill.Level));

			var nameLength = (Game.Version < Versions.Beta1) ? 16 : 24;
			packet.PutString(skillData.Id.ToString(), nameLength);

			var canUpgrade = skill.Level < skillData.MaxLevel && player.Parameters.SkillPoints > 0;
			// A more complete check would include job level and prerequisite skills.
			// canUpgrade &= player.Parameters.JobLevel >= skillData.GetJobLevelRequirement(skill.Level + 1);
			packet.PutByte(canUpgrade);

			// Alpha client's ZC_ADD_SKILL packet has a total size of 33 bytes.
			// Opcode (2) + Data (29) + Padding (2).
			if (Game.Version < Versions.Beta1)
			{
				packet.PutEmpty(2);
			}
		}
	}
}

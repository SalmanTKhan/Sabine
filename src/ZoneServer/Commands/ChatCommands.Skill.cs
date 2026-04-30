using Sabine.Shared.Const;
using Sabine.Shared.Util;
using Sabine.Zone.Network;
using Sabine.Zone.Skills;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util.Commands;

namespace Sabine.Zone.Commands
{
	public partial class ChatCommands
	{
		private void LoadSkill()
		{
			this.Add("skill", "<id> [level]", Localization.Get("Adds the skill to the character."), this.Skill);
			this.Add("allskill", "", Localization.Get("Grants every skill in the target's tree."), this.AllSkill);
			this.Add("allskills", "", Localization.Get("Grants every skill in the target's tree."), this.AllSkill);
			this.Add("skillall", "", Localization.Get("Grants every skill in the target's tree."), this.AllSkill);
			this.Add("skillsall", "", Localization.Get("Grants every skill in the target's tree."), this.AllSkill);
			this.Add("skillon", "", Localization.Get("Enables skill use on the target's map."), this.SkillOn);
			this.Add("skilloff", "", Localization.Get("Disables skill use on the target's map."), this.SkillOff);
			this.Add("useskill", "<id> [level]", Localization.Get("Triggers a skill cast on the target."), this.UseSkill);
			this.Add("displaystatus", "<id>", Localization.Get("Plays a status effect display on the target."), this.DisplayStatus);
			this.Add("displayskill", "<id> [level]", Localization.Get("Plays a skill cast effect."), this.DisplaySkill);
			this.Add("displayskillcast", "<id> [level]", Localization.Get("Plays a skill cast animation."), this.DisplaySkill);
			this.Add("displayskillunit", "<id>", Localization.Get("Spawns a skill unit at the target."), this.DisplaySkill);
			this.Add("questskill", "<id>", Localization.Get("Grants a quest skill to the target."), this.QuestSkill);
			this.Add("lostskill", "<id>", Localization.Get("Removes a skill from the target."), this.LostSkill);
			this.Add("resetcooltime", "", Localization.Get("Resets the target's skill cooldowns."), this.ResetCoolTime);
			this.Add("healap", "", Localization.Get("Restores the target's AP."), this.HealAp);
		}

		private CommandResult AllSkill(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			var n = 0;
			foreach (var entry in ZoneServer.Instance.Data.Skills.Entries.Values)
			{
				try { target.Skills.Add(new Skill(target, entry.Id, entry.MaxLevel)); n++; } catch { }
			}
			sender.ServerMessage(Localization.Get("Granted {0} skill(s)."), n);
			return CommandResult.Okay;
		}

		private CommandResult SkillOn(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Map.ClearFlag(Sabine.Zone.World.Maps.MapFlags.NoSkill);
			sender.ServerMessage(Localization.Get("Skills enabled on {0}."), target.Map.StringId);
			return CommandResult.Okay;
		}

		private CommandResult SkillOff(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Map.SetFlag(Sabine.Zone.World.Maps.MapFlags.NoSkill);
			sender.ServerMessage(Localization.Get("Skills disabled on {0}."), target.Map.StringId);
			return CommandResult.Okay;
		}

		private CommandResult UseSkill(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("useskill: not implemented (needs targeting context)."));
			return CommandResult.Okay;
		}

		private CommandResult DisplayStatus(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!int.TryParse(args.Get(0), out var id)) return CommandResult.InvalidArgument;
			Send.ZC_NOTIFY_EFFECT(target, id);
			return CommandResult.Okay;
		}

		private CommandResult DisplaySkill(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("displayskill: not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult QuestSkill(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			SkillId id;
			if (System.Enum.TryParse<SkillId>(args.Get(0), out id)) { /* ok */ }
			else if (int.TryParse(args.Get(0), out var n)) id = (SkillId)n;
			else return CommandResult.InvalidArgument;
			target.Skills.Add(new Skill(target, id, 1));
			sender.ServerMessage(Localization.Get("Granted skill {0}."), id);
			return CommandResult.Okay;
		}

		private CommandResult LostSkill(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;
			if (!System.Enum.TryParse<SkillId>(args.Get(0), out var id))
			{
				if (!int.TryParse(args.Get(0), out var n)) return CommandResult.InvalidArgument;
				id = (SkillId)n;
			}
			try { target.Skills.Remove(id); sender.ServerMessage(Localization.Get("Removed skill {0}."), id); } catch { }
			return CommandResult.Okay;
		}

		private CommandResult ResetCoolTime(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			sender.ServerMessage(Localization.Get("Cooldown reset is not implemented."));
			return CommandResult.Okay;
		}

		private CommandResult HealAp(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			target.Heal();
			sender.ServerMessage(Localization.Get("Healed AP."));
			return CommandResult.Okay;
		}

		private CommandResult Skill(PlayerCharacter sender, PlayerCharacter target, string message, string commandName, Arguments args)
		{
			if (args.Count < 1) return CommandResult.InvalidArgument;

			SkillId skillId;
			var level = 0;
			var arg0 = args.Get(0);

			if (System.Enum.TryParse<SkillId>(arg0, out var skillIdEnum))
				skillId = skillIdEnum;
			else if (int.TryParse(arg0, out var skillIdInt))
				skillId = (SkillId)skillIdInt;
			else
			{
				sender.ServerMessage(Localization.Get("Invalid skill id '{0}'."), arg0);
				return CommandResult.Okay;
			}

			if (!ZoneServer.Instance.Data.Skills.TryFind(skillId, out var skillData))
			{
				sender.ServerMessage(Localization.Get("Skill with id '{0}' not found."), skillId);
				return CommandResult.Okay;
			}

			if (args.Count > 1)
			{
				if (!int.TryParse(args.Get(1), out level)) return CommandResult.InvalidArgument;
				level = Yggdrasil.Util.Math2.Clamp(0, skillData.MaxLevel, level);
			}

			target.Skills.Add(new Sabine.Zone.Skills.Skill(target, skillId, level));

			sender.ServerMessage(Localization.Get("Added skill '{0}' at level {1}."), skillData.StringId, level);
			if (sender != target)
				target.ServerMessage(Localization.Get("Skill '{0}' was added at level {1} by {2}."), skillData.StringId, level, sender.Name);
			return CommandResult.Okay;
		}
	}
}

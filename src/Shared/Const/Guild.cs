using System;

namespace Sabine.Shared.Const
{
	/// <summary>
	/// Defines the War of Emperium edition for a castle.
	/// </summary>
	public enum WoeType
	{
		FirstEdition = 1,
		SecondEdition,
		ThirdEdition,
	}

	/// <summary>
	/// Defines script-accessible data points for a guild castle.
	/// </summary>
	public enum CastleData : byte
	{
		None = 0,
		GuildId,
		CurrentEconomy,
		CurrentDefense,
		InvestedEconomy,
		InvestedDefense,
		NextTime,
		PayTime,
		CreateTime,
		EnabledKafra,
		EnabledGuardian00,
	}

	/// <summary>
	/// Defines permissions for guild members.
	/// </summary>
	[Flags]
	public enum GuildPermission
	{
		Invite = 0x001,
		Expel = 0x010,
		Storage = 0x100,
	}

	/// <summary>
	/// Defines which guild information to change.
	/// </summary>
	public enum GuildInfo
	{
		Exp = 1,
		GuildLevel,
		SkillPoint,
		SkillLevel,
	}

	/// <summary>
	/// Defines which guild member information to change.
	/// </summary>
	public enum GuildMemberInfo
	{
		Position = 0,
		Exp,
		Hair,
		HairColor,
		Gender,
		Class,
		Level,
	}

	/// <summary>
	/// Defines IDs for guild skills.
	/// </summary>
	public enum GuildSkill
	{
		Approval = 10000,
		KafraContract,
		GuardResearch,
		GuardUp,
		Extension,
		GloryGuild,
		Leadership,
		GloryWounds,
		SoulCold,
		Hawkeyes,
		BattleOrder,
		Regeneration,
		Restore,
		EmergencyCall,
		Development,
		ItemEmergencyCall,
		GuildStorage,
		ChargeShoutFlag,
		ChargeShoutBeating,
		EmergencyMove,
	}
}

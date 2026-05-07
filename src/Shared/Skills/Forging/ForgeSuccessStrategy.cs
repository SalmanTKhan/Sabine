namespace Sabine.Shared.Skills.Forging
{
	/// <summary>
	/// Inputs handed to a forge success strategy. Pure data so the
	/// strategy is trivially unit-testable without standing up a
	/// player or world.
	/// </summary>
	public readonly struct ForgeContext
	{
		public ForgeContext(int dex, int luk, int jobLevel, int weaponLevel, int forgeSkillLevel, int weaponResearchLevel, int oridecoResearchLevel, int starCrumbs, bool elementStone, int anvilBonus, int randomBoost = 0)
		{
			this.Dex = dex;
			this.Luk = luk;
			this.JobLevel = jobLevel;
			this.WeaponLevel = weaponLevel;
			this.ForgeSkillLevel = forgeSkillLevel;
			this.WeaponResearchLevel = weaponResearchLevel;
			this.OridecoResearchLevel = oridecoResearchLevel;
			this.StarCrumbs = starCrumbs;
			this.ElementStone = elementStone;
			this.AnvilBonus = anvilBonus;
			this.RandomBoost = randomBoost;
		}

		public int Dex { get; }
		public int Luk { get; }
		public int JobLevel { get; }
		/// <summary>Weapon level (1/2/3); pulled from the recipe's item level.</summary>
		public int WeaponLevel { get; }
		public int ForgeSkillLevel { get; }
		public int WeaponResearchLevel { get; }
		public int OridecoResearchLevel { get; }
		public int StarCrumbs { get; }
		public bool ElementStone { get; }
		public int AnvilBonus { get; }
		/// <summary>
		/// rAthena renewal mixes a per-cast random component into the
		/// forge formula (`rnd_value(1,100)*10`). The caller rolls it
		/// once and hands the result in here so the strategy stays a
		/// pure function and tests can pin the value. Classic ignores
		/// this field.
		/// </summary>
		public int RandomBoost { get; }
	}

	/// <summary>
	/// Era-specific forge success-rate calculator.
	/// </summary>
	/// <remarks>
	/// One implementation per <see cref="Sabine.Shared.Const.Era"/>; the
	/// active strategy is resolved once at handler construction time
	/// rather than per-cast. This is the pattern future era-divergent
	/// skills should follow: an interface plus per-era impls, picked
	/// from <see cref="Sabine.Shared.Game.Era"/> at startup.
	/// </remarks>
	public interface IForgeSuccessStrategy
	{
		/// <summary>
		/// Returns the success rate as parts-per-10000 (so 5000 == 50%).
		/// </summary>
		int CalcSuccessPer10000(ForgeContext ctx);
	}
}

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Identifies which damage formula a calculation should run.
	/// </summary>
	public enum AttackKind
	{
		/// <summary>
		/// Weapon-based physical attack (auto-attack, most warrior skills).
		/// Defense is reduced by the target's <c>DEF</c>/<c>VIT</c>-based
		/// stats. Rolled against <c>HIT</c>/<c>FLEE</c>.
		/// </summary>
		Physical,

		/// <summary>
		/// Spell-based magic attack. Defense is reduced by the target's
		/// <c>MDEF</c>/<c>INT</c>-based stats. Cannot miss in pre-renewal
		/// (only blocked by Pneuma / Safety Wall etc.).
		/// </summary>
		Magic,
	}
}

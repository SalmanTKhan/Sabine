using Sabine.Shared.Const;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Outcome of a damage calculation.
	/// </summary>
	public class AttackResult
	{
		/// <summary>Total damage dealt across all hits (0 on a miss).</summary>
		public int Damage { get; set; }

		/// <summary>Damage of each individual hit; <c>HitCount</c> entries.</summary>
		public int DamagePerHit { get; set; }

		public int HitCount { get; set; } = 1;

		public bool IsMiss { get; set; }
		public bool IsCritical { get; set; }
		public bool IsLuckyDodge { get; set; }

		public ElementType AttackElement { get; set; } = ElementType.Neutral;
		public ElementType TargetElement { get; set; } = ElementType.Neutral;

		/// <summary>
		/// The action type to broadcast in <c>ZC_NOTIFY_ACT</c> /
		/// <c>ZC_NOTIFY_SKILL</c>.
		/// </summary>
		public ActionType ActionType { get; set; } = ActionType.Attack;

		public static AttackResult Miss() => new AttackResult
		{
			IsMiss = true,
			ActionType = ActionType.Attack,
		};
	}
}

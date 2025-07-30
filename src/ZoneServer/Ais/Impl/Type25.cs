using System.Collections;
using Sabine.Zone.World.Entities;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// AI Type 25: Pet AI
	/// </summary>
	/// <remarks>
	/// Aegis: 25
	/// Behavior: A non-combat pet. It simply follows its owner around
	/// and does not engage in combat.
	/// </remarks>
	[Ai("Type25")]
	public class Type25 : MonsterAi
	{
		protected int OwnerHandle => (Character as Monster)?.OwnerHandle ?? 0;
		private const int FollowDistance = 4;

		protected override void Init()
		{
			During("Idle", CheckDistanceToOwner);
		}

		protected override void Start()
		{
			StartRoutine("Idle", Idle());
		}

		private IEnumerable Idle()
		{
			// Wait a bit before checking distance again to avoid spamming checks.
			yield return Wait(500);
		}

		private IEnumerable Follow()
		{
			if (OwnerHandle == 0 || !TryGetEntity(OwnerHandle, out var owner))
			{
				StartRoutine("Idle", Idle());
				yield break;
			}

			// Move towards the owner. This routine ends when movement is complete.
			yield return MoveTo(owner.Position);
			StartRoutine("Idle", Idle());
		}

		private void CheckDistanceToOwner(CallbackState state)
		{
			if (OwnerHandle == 0 || !TryGetEntity(OwnerHandle, out var owner))
				return;

			if (!owner.Position.InRange(Character.Position, FollowDistance))
			{
				StartRoutine("Follow", Follow());
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009

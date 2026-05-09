#pragma warning disable IDE0009

using Sabine.Zone.Ais.Base;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// Pet AI: stays close to its owner, no combat. Mirrors only the
	/// FollowOwner arm of <see cref="HomunculusAi"/>. eAthena's pet AI
	/// optionally retaliates on the owner's attacker when intimacy ≥
	/// pet_support_min_friendly; that arm lands with the pet attack /
	/// defense_attack rate fields (pet_db columns 18/19).
	/// </summary>
	[Ai("Pet")]
	public class PetAi : ReactiveAi
	{
		private const int FollowDistance = 3;

		protected override void Init()
		{
			base.Init();
			During("Idle", FollowOwner);
		}

		private PlayerCharacter Owner => (this.Character as Pet)?.Owner;

		private void FollowOwner(CallbackState state)
		{
			if (state.Handled) return;
			var owner = this.Owner;
			if (owner == null) return;
			if (owner.Map != this.Character.Map) return;
			if (this.Character.Position.InRange(owner.Position, FollowDistance)) return;

			this.Character.Controller.MoveTo(owner.Position);
			state.Handled = true;
		}
	}
}

#pragma warning restore IDE0009

using Sabine.Shared.Const;
using Sabine.Zone.World.Actors.Components.Characters;

namespace Sabine.Zone.World.Actors
{
	/// <summary>
	/// A mercenary companion. Lives on a map as a Monster-derived
	/// entity owned by a player. Wild monsters won't aggro on it
	/// (AggressiveAi scans for PlayerCharacter only); enemies of the
	/// owner are treated as hostile by the merc AI, matching Homunculus
	/// behaviour. Stat lines flow from the mob template; the merc-side
	/// metadata (faction, faith, contract clock) lives on
	/// <see cref="MercenaryComponent"/>.
	/// </summary>
	public class Mercenary : Monster
	{
		public PlayerCharacter Owner { get; }
		public MercenaryComponent State { get; }

		public Mercenary(PlayerCharacter owner, MercenaryComponent state, IdentityId identityId)
			: base(identityId)
		{
			this.Owner = owner;
			this.State = state;
			this.Master = owner;
			this.Name = this.Data.Name;
		}

		internal override bool IsHostileTo(Character target)
		{
			if (target == null || target == this) return false;
			if (target == this.Owner) return false;
			if (target is PlayerCharacter) return false;
			if (target is Mercenary) return false;
			if (target is Pet) return false;
			if (target is Homunculus) return false;

			return target is Monster;
		}
	}
}

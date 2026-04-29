#pragma warning disable IDE0009

using System;
using System.Collections;
using System.Linq;
using Sabine.Shared.Const;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.World.Actors;
using Sabine.Zone.World.Actors.Components.Characters;
using Yggdrasil.Collections;

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// Homunculus AI: stays close to its owner, engages whatever is
	/// hostile to the owner (or whatever the owner is currently
	/// attacking). Disengages and returns to the owner if the target
	/// dies, leaves the map, or moves outside the homun's chase range.
	/// </summary>
	[Ai("Homunculus")]
	public class HomunculusAi : ReactiveAi
	{
		private const int FollowDistance = 3;
		private const int LeashRange = 14;
		private const int AssistScanRange = 9;

		// Per-skill cooldown so the homun doesn't spam.
		private DateTime _nextHealAt;
		private DateTime _nextOffenseAt;
		private DateTime _nextCastleAt;

		protected override void Init()
		{
			base.Init();
			During("Idle", PickTarget);
			During("Idle", FollowOwner);
			During("Idle", AutoCast);
			During("Combat", LeashCheck);
			During("Combat", AutoCast);
		}

		private PlayerCharacter Owner => (this.Character as Homunculus)?.Owner;

		private void PickTarget(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var owner = this.Owner;
			if (owner == null || owner.IsDead) return;

			// Priority 1: whatever the owner is currently targeting.
			if (owner.TargetHandle != 0
				&& this.Character.Map.TryGetCharacter(owner.TargetHandle, out var ownerTarget)
				&& !ownerTarget.IsDead
				&& this.Character.IsHostileTo(ownerTarget))
			{
				StartRoutine("Combat", Combat(ownerTarget.Handle));
				state.Handled = true;
				return;
			}

			// Priority 2: any nearby hostile within assist scan range.
			using var nearby = PooledList<Character>.Rent();
			foreach (var c in this.Character.Map.GetCharactersInRange(owner.Position, AssistScanRange))
			{
				if (this.Character.IsHostileTo(c) && !c.IsDead)
					nearby.Add(c);
			}
			if (nearby.Any())
			{
				var pick = nearby.OrderBy(c => c.Position.GetDistance(this.Character.Position)).First();
				StartRoutine("Combat", Combat(pick.Handle));
				state.Handled = true;
			}
		}

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

		private void LeashCheck(CallbackState state)
		{
			var owner = this.Owner;
			if (owner == null) return;
			if (this.Character.Position.InRange(owner.Position, LeashRange)) return;

			// Owner moved too far — drop the target and the routine
			// machinery returns to Idle, which will re-follow.
			_targetCharacterHandle = 0;
			StartRoutine("Idle", Idle());
		}

		private void AutoCast(CallbackState state)
		{
			var homun = this.Character as Homunculus;
			if (homun == null) return;
			var owner = this.Owner;
			if (owner == null || owner.IsDead) return;

			var now = DateTime.Now;

			// Lif: heal owner when below 50% HP.
			if (homun.Type == HomunculusType.Lif && now >= _nextHealAt)
			{
				if (owner.Parameters.Hp < owner.Parameters.HpMax / 2
					&& this.Character.Position.InRange(owner.Position, 9))
				{
					homun.CastSkill(SkillId.HLIF_HEAL, owner, 5);
					_nextHealAt = now.AddSeconds(8);
				}
			}

			// Amistr: Castling rescues the owner when they're at
			// critical HP and an enemy is adjacent to them.
			if (homun.Type == HomunculusType.Amistr && now >= _nextCastleAt)
			{
				if (owner.Parameters.Hp < owner.Parameters.HpMax / 4
					&& OwnerHasAdjacentHostile(owner))
				{
					homun.CastSkill(SkillId.HAMI_CASTLE, owner, 5);
					_nextCastleAt = now.AddSeconds(15);
				}
			}

			// Vanilmirth: caprice the engaged target.
			if (homun.Type == HomunculusType.Vanilmirth && now >= _nextOffenseAt && _targetCharacterHandle != 0)
			{
				if (homun.Map.TryGetCharacter(_targetCharacterHandle, out var t)
					&& !t.IsDead
					&& homun.Position.InRange(t.Position, 9))
				{
					homun.CastSkill(SkillId.HVAN_CAPRICE, t, 3);
					_nextOffenseAt = now.AddSeconds(5);
				}
			}

			// Filir: Moonlight on engaged target when close.
			if (homun.Type == HomunculusType.Filir && now >= _nextOffenseAt && _targetCharacterHandle != 0)
			{
				if (homun.Map.TryGetCharacter(_targetCharacterHandle, out var t)
					&& !t.IsDead
					&& homun.Position.InRange(t.Position, 2))
				{
					homun.CastSkill(SkillId.HFLI_MOON, t, 3);
					_nextOffenseAt = now.AddSeconds(6);
				}
			}
		}

		private bool OwnerHasAdjacentHostile(PlayerCharacter owner)
		{
			foreach (var c in owner.Map.GetCharactersInRange(owner.Position, 1))
			{
				if (c == owner || c == this.Character) continue;
				if (this.Character.IsHostileTo(c) && !c.IsDead) return true;
			}
			return false;
		}

		// Override the base wandering Idle: a homun shouldn't drift
		// off on its own — it idles in place and the FollowOwner
		// during-callback brings it back to the owner.
		protected override IEnumerable Idle()
		{
			while (true)
			{
				foreach (var _ in Wait(500)) yield return true;
			}
		}

		protected override IEnumerable Combat(int handle)
		{
			_targetCharacterHandle = handle;
			foreach (var _ in HuntDown(handle))
				yield return true;

			_targetCharacterHandle = 0;
			StartRoutine("Idle", Idle());
		}
	}
}

#pragma warning restore IDE0009

using System.Collections;

#pragma warning disable IDE0009

namespace Sabine.Zone.Ais.Base
{
	/// <summary>
	/// Base class for AIs that are passive but fight back when attacked.
	/// </summary>
	public abstract class ReactiveAi : MonsterAi
	{
		// Skip the cosmetic walk-back if the mob never strayed far from
		// where it spawned. Matches the spirit of rAthena's spawn anchor
		// where small kites don't trigger a visible return.
		private const int LeashSkipDistance = 3;

		protected override void Init()
		{
			base.Init();
			During("Idle", CheckAttacks);
			During("Leash", CheckAttacks);
			During("Combat", TryMobSkill);
		}

		/// <summary>
		/// Per-tick mob-skill cast attempt during combat. Rolls each
		/// skill in <c>Game.CurrentData.MobSkills</c> against its rate;
		/// the per-skill cooldown lives on the Monster so AI re-attach
		/// doesn't desync timing. Skills inflict their effects through
		/// the standard handler registry — same path PlayerCharacters
		/// take — so anything that works for a player works for a mob.
		/// </summary>
		protected virtual void TryMobSkill(CallbackState state)
		{
			if (state.Handled) return;
			if (_targetCharacterHandle == 0) return;
			if (!Character.Map.TryGetCharacter(_targetCharacterHandle, out var target)) return;

			var monster = Character as Sabine.Zone.World.Actors.Monster;
			if (monster == null) return;

			if (monster.TryCastMobSkill(target))
				state.Handled = true;
		}

		protected override void Start()
		{
			StartRoutine("Idle", Idle());
		}

		/// <summary>
		/// The main combat routine. The monster will hunt down its target,
		/// then leash back toward its spawn before resuming idle behavior.
		/// </summary>
		/// <param name="handle">The handle of the target character.</param>
		protected virtual IEnumerable Combat(int handle)
		{
			_targetCharacterHandle = handle;
			yield return HuntDown(handle);
			_targetCharacterHandle = 0;
			Character.AttackerHandleTest = 0;

			StartRoutine("Leash", Leash());
		}

		/// <summary>
		/// Walks the monster back toward its spawn anchor after losing a
		/// target. Mirrors rAthena's mob_unlocktarget return-to-spawn pass.
		/// CheckAttacks/CheckForTargets remain hooked while leashing, so
		/// re-aggro mid-leash drops the routine and resumes Combat.
		/// </summary>
		protected virtual IEnumerable Leash()
		{
			var spawn = SpawnPosition;
			var here = Character.Position;

			if (here.GetDistance(spawn) <= LeashSkipDistance)
			{
				StartRoutine("Idle", Idle());
				yield break;
			}

			// MoveTo already bails if spawn is impassable or unpathable;
			// in that rare case we'll just resume Idle from here.
			yield return MoveTo(spawn);

			StartRoutine("Idle", Idle());
		}

		/// <summary>
		/// The main idle routine. The monster will wait and then wander.
		/// </summary>
		protected virtual IEnumerable Idle()
		{
			while (true)
			{
				yield return Wait(3000, 10000);
				yield return Wander(5);
			}
		}

		/// <summary>
		/// Checks if the monster has been attacked and starts combat if so.
		/// </summary>
		protected virtual void CheckAttacks(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			if (Character.AttackerHandleTest != 0)
			{
				StartRoutine("Combat", Combat(Character.AttackerHandleTest));
				state.Handled = true;
			}
		}
	}
}

#pragma warning restore IDE0009

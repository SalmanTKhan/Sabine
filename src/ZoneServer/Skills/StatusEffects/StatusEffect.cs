using System;
using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Skills.StatusEffects
{
	/// <summary>
	/// An active status effect (buff or debuff) on a character.
	/// </summary>
	/// <remarks>
	/// Val1..Val4 mirror the rAthena/eAthena status_change_entry convention:
	/// handlers store any per-instance numbers they need (level, applied
	/// deltas, etc.) in these fields.
	/// </remarks>
	public class StatusEffect
	{
		public StatusId Id { get; }
		public int Level { get; }

		public int Val1 { get; set; }
		public int Val2 { get; set; }
		public int Val3 { get; set; }
		public int Val4 { get; set; }

		/// <summary>
		/// Free-form string slot for handlers that need to stash a
		/// non-numeric scratch value across OnStart / OnEnd. Used by
		/// SA_REVERSEORCISH to remember the target's original name
		/// while the override is active.
		/// </summary>
		public string OriginalName { get; set; }

		public TimeSpan Duration { get; }
		public TimeSpan RemainingDuration { get; private set; }

		public Character Caster { get; }
		public Character Target { get; }

		public bool IsExpired => this.RemainingDuration <= TimeSpan.Zero;

		public StatusEffect(StatusId id, int level, TimeSpan duration, Character target, Character caster, int val1 = 0, int val2 = 0, int val3 = 0, int val4 = 0)
		{
			this.Id = id;
			this.Level = level;
			this.Duration = duration;
			this.RemainingDuration = duration;
			this.Target = target;
			this.Caster = caster;
			this.Val1 = val1;
			this.Val2 = val2;
			this.Val3 = val3;
			this.Val4 = val4;
		}

		/// <summary>
		/// Decrements the remaining duration.
		/// </summary>
		public void Tick(TimeSpan elapsed)
		{
			this.RemainingDuration -= elapsed;
		}

		/// <summary>
		/// Resets the remaining duration back to the full duration. Used
		/// when a buff is recast on a target that already has it.
		/// </summary>
		public void Refresh()
		{
			this.RemainingDuration = this.Duration;
		}

		/// <summary>
		/// Sets the remaining duration to the larger of the current value
		/// and the supplied span. Used by the song/dance aura pipeline so
		/// a per-tick refresh can't accidentally shorten the buff.
		/// </summary>
		public void ExtendDuration(TimeSpan duration)
		{
			if (duration > this.RemainingDuration)
				this.RemainingDuration = duration;
		}
	}
}

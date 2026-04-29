using System;
using System.Threading;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// A single ground-effect cell placed on a map by a skill (Pneuma,
	/// Safety Wall, Fire Wall, Sanctuary, etc.). Mirrors eAthena's
	/// <c>struct skill_unit</c> and rAthena's <c>skill_unit</c>.
	/// </summary>
	public abstract class SkillUnit : IUpdateable
	{
		private static int _handlePool = 700_000_000;
		private static int GetNewHandle() => Interlocked.Increment(ref _handlePool);

		public int Handle { get; }
		public Character Owner { get; }
		public Map Map { get; internal set; }
		public Position Position { get; }
		public SkillId SkillId { get; }
		public int SkillLevel { get; }

		/// <summary>
		/// Wall-clock time at which this unit expires and should be
		/// removed from the map.
		/// </summary>
		public DateTime ExpireTime { get; protected set; }

		/// <summary>
		/// Remaining hits the unit can absorb / deal before expiring.
		/// Negative means uncapped (time-based only).
		/// </summary>
		public int HitsRemaining { get; protected set; } = -1;

		public bool IsExpired => DateTime.Now >= this.ExpireTime || this.HitsRemaining == 0;

		protected SkillUnit(Character owner, Position position, SkillId skillId, int skillLevel, TimeSpan duration)
		{
			this.Handle = GetNewHandle();
			this.Owner = owner;
			this.Position = position;
			this.SkillId = skillId;
			this.SkillLevel = skillLevel;
			this.ExpireTime = DateTime.Now + duration;
		}

		/// <summary>
		/// Returns true if the incoming attack should be blocked by this
		/// unit. Default: not a blocking unit.
		/// </summary>
		public virtual bool BlocksAttack(AttackContext ctx, bool isRanged) => false;

		/// <summary>
		/// Called when <see cref="BlocksAttack"/> returned true and the
		/// guard fired. Default behavior decrements hit counter.
		/// </summary>
		public virtual void OnAttackBlocked(AttackContext ctx)
		{
			if (this.HitsRemaining > 0)
				this.HitsRemaining--;
		}

		/// <summary>
		/// Called when a character steps onto this unit's cell. Default
		/// is a no-op; damage-on-touch units (Fire Wall, Sanctuary) override.
		/// </summary>
		public virtual void OnTouch(Character entrant) { }

		/// <summary>
		/// Per-tick update. Default is a no-op; recurring effects override.
		/// </summary>
		public virtual void Update(TimeSpan elapsed) { }

		/// <summary>
		/// Predicate: is the given character an enemy of the unit's owner?
		/// </summary>
		protected bool IsEnemy(Character other)
		{
			if (other == null || other == this.Owner)
				return false;
			return other.IsHostileTo(this.Owner);
		}
	}
}

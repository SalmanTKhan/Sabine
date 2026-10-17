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
		public SkillUnitId UnitId { get; }
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
			this.UnitId = ResolveUnitId(skillId);
			this.SkillLevel = skillLevel;
			this.ExpireTime = DateTime.Now + duration;
		}

		internal static SkillUnitId ResolveUnitId(SkillId skillId)
		{
			return skillId switch
			{
				SkillId.MG_SAFETYWALL => SkillUnitId.SafetyWall,
				SkillId.MG_FIREWALL => SkillUnitId.FireWall,
				SkillId.AL_WARP => SkillUnitId.WarpPortal,
				SkillId.PR_SANCTUARY => SkillUnitId.Sanctuary,
				SkillId.PR_MAGNUS => SkillUnitId.Magnus,
				SkillId.AL_PNEUMA => SkillUnitId.Pneuma,
				SkillId.WZ_FIREPILLAR => SkillUnitId.FirePillarWaiting,
				SkillId.WZ_ICEWALL => SkillUnitId.IceWall,
				SkillId.WZ_QUAGMIRE => SkillUnitId.Quagmire,
				SkillId.HT_BLASTMINE => SkillUnitId.BlastMine,
				SkillId.HT_SKIDTRAP => SkillUnitId.SkidTrap,
				SkillId.HT_ANKLESNARE => SkillUnitId.AnkleSnare,
				SkillId.AS_VENOMDUST => SkillUnitId.VenomDust,
				SkillId.HT_LANDMINE => SkillUnitId.LandMine,
				SkillId.HT_SHOCKWAVE => SkillUnitId.ShockwaveTrap,
				SkillId.HT_SANDMAN => SkillUnitId.Sandman,
				SkillId.HT_FLASHER => SkillUnitId.Flasher,
				SkillId.HT_FREEZINGTRAP => SkillUnitId.FreezingTrap,
				SkillId.HT_CLAYMORETRAP => SkillUnitId.ClaymoreTrap,
				SkillId.HT_TALKIEBOX => SkillUnitId.TalkieBox,
				SkillId.SA_VOLCANO => SkillUnitId.Volcano,
				SkillId.SA_DELUGE => SkillUnitId.Deluge,
				SkillId.SA_VIOLENTGALE => SkillUnitId.ViolentGale,
				SkillId.SA_LANDPROTECTOR => SkillUnitId.LandProtector,
				SkillId.BD_LULLABY => SkillUnitId.Lullaby,
				SkillId.BD_RICHMANKIM => SkillUnitId.RichManKim,
				SkillId.BD_ETERNALCHAOS => SkillUnitId.EternalChaos,
				SkillId.BD_DRUMBATTLEFIELD => SkillUnitId.DrumBattlefield,
				SkillId.BD_RINGNIBELUNGEN => SkillUnitId.RingNibelungen,
				SkillId.BD_ROKISWEIL => SkillUnitId.RokisWeil,
				SkillId.BD_INTOABYSS => SkillUnitId.IntoAbyss,
				SkillId.BD_SIEGFRIED => SkillUnitId.Siegfried,
				SkillId.BA_DISSONANCE => SkillUnitId.Dissonance,
				SkillId.BA_WHISTLE => SkillUnitId.Whistle,
				SkillId.BA_ASSASSINCROSS => SkillUnitId.AssassinCross,
				SkillId.BA_POEMBRAGI => SkillUnitId.PoemBragi,
				SkillId.BA_APPLEIDUN => SkillUnitId.AppleIdun,
				SkillId.DC_UGLYDANCE => SkillUnitId.UglyDance,
				SkillId.DC_HUMMING => SkillUnitId.Humming,
				SkillId.DC_DONTFORGETME => SkillUnitId.DontForgetMe,
				SkillId.DC_FORTUNEKISS => SkillUnitId.FortuneKiss,
				SkillId.DC_SERVICEFORYOU => SkillUnitId.ServiceForYou,
				SkillId.HP_BASILICA => SkillUnitId.Basilica,
				SkillId.HW_GRAVITATION => SkillUnitId.Gravitation,
				_ => SkillUnitId.None,
			};
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

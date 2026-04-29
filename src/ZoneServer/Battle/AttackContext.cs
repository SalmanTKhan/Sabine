using Sabine.Shared.Const;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.Battle
{
	/// <summary>
	/// Inputs to a single damage calculation. Built by the caller (basic
	/// attack path or skill handler) and passed to <c>BattleCalculator</c>.
	/// </summary>
	public class AttackContext
	{
		public Character Attacker { get; }

		/// <summary>
		/// Target of the attack. Settable so PreAttackResolve listeners
		/// can redirect (Devotion) or null it out.
		/// </summary>
		public Character Target { get; set; }

		/// <summary>
		/// If a PreAttackResolve listener sets this, BattleCalculator
		/// short-circuits to a Miss without rolling damage. Used by
		/// Auto-Guard.
		/// </summary>
		public bool Cancelled { get; set; }

		/// <summary>
		/// The skill driving this attack, or <c>None</c> for basic attacks.
		/// </summary>
		public SkillId SkillId { get; set; } = SkillId.None;

		/// <summary>
		/// Skill level. Ignored for basic attacks.
		/// </summary>
		public int SkillLevel { get; set; }

		public AttackKind Kind { get; set; } = AttackKind.Physical;

		/// <summary>
		/// Multiplier applied to the base ATK/MATK before defense. 1.0 for
		/// a basic attack; e.g. 1.5 for Bash 1, 4.0 for Fire Ball 5. The
		/// per-skill ratio is the small piece of formula every handler
		/// supplies; everything else (def, ele, crit, hit) is shared.
		/// </summary>
		public float SkillRatio { get; set; } = 1.0f;

		/// <summary>
		/// Number of hits this attack lands. Damage is divided across the
		/// hits. Defaults to 1.
		/// </summary>
		public int HitCount { get; set; } = 1;

		/// <summary>
		/// Element of the attack. <c>Weapon</c> means "use the attacker's
		/// weapon element"; any concrete element overrides it.
		/// </summary>
		public ElementType AttackElement { get; set; } = ElementType.Weapon;

		/// <summary>
		/// If set, treats the attack as ignoring DEF/MDEF entirely (used
		/// by some skills that bypass armor).
		/// </summary>
		public bool IgnoreDefense { get; set; }

		/// <summary>
		/// If set, hit/flee is skipped — the attack always connects.
		/// </summary>
		public bool AlwaysHits { get; set; }

		/// <summary>
		/// If set, target FLEE is bypassed but perfect-dodge / ground-effect
		/// guards still apply. Distinct from <see cref="AlwaysHits"/> which
		/// also bypasses lucky dodge.
		/// </summary>
		public bool IgnoreFlee { get; set; }

		/// <summary>
		/// Treat this attack as long-ranged for card / element interactions
		/// even if the attacker is wielding a melee weapon. Skills like
		/// Spear Boomerang and Shield Boomerang set this.
		/// </summary>
		public bool IsLongRange { get; set; }

		/// <summary>
		/// If set, the handler requires a weapon to be equipped. Bash and
		/// most physical 2nd-job skills set this; the handler is expected
		/// to gate on it before entering combat.
		/// </summary>
		public bool WeaponRequired { get; set; }

		/// <summary>
		/// Minimum damage floor applied after defense and elements. 0 means
		/// no floor. Used by Sonic Blow, Soul Breaker, etc.
		/// </summary>
		public int MinDamage { get; set; }

		/// <summary>
		/// Multiplicative DEF reduction in percent (0..100). 100 ignores
		/// DEF entirely, 50 halves it. Pierce and Mammonite use this; for
		/// full bypass prefer <see cref="IgnoreDefense"/>.
		/// </summary>
		public int DefRatio { get; set; } = 100;

		/// <summary>
		/// Splash radius in cells around the primary target. 0 = single
		/// target. Used by ground-AoE skills that don't spawn a SkillUnit
		/// (Magnum Break, Bowling Bash, Grand Cross).
		/// </summary>
		public int SplashRange { get; set; }

		public AttackContext(Character attacker, Character target)
		{
			this.Attacker = attacker;
			this.Target = target;
		}
	}
}

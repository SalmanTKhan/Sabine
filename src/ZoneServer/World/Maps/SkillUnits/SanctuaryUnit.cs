using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// PR_SANCTUARY cell. Heals living allies who stand on it; Holy
	/// magic-damages Undead/Demon-race enemies. Tick interval ≈ 0.4s.
	/// Hit count = 3 + 2*level (eAthena classic).
	/// </summary>
	public class SanctuaryUnit : SkillUnit
	{
		private static readonly TimeSpan HitInterval = TimeSpan.FromMilliseconds(400);
		private DateTime _nextHitAt = DateTime.MinValue;

		public SanctuaryUnit(Character owner, Position position, int level)
			: base(owner, position, SkillId.PR_SANCTUARY, level, TimeSpan.FromSeconds(4 + level / 2))
		{
			this.HitsRemaining = 3 + 2 * level;
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == null) return;

			var now = DateTime.Now;
			if (now < _nextHitAt) return;
			_nextHitAt = now + HitInterval;

			var heal = 100 * this.SkillLevel + (this.Owner.Parameters.BaseLevel + this.Owner.Parameters.Int);

			if (IsUndeadOrDemon(entrant))
			{
				var ctx = new AttackContext(this.Owner, entrant)
				{
					SkillId = SkillId.PR_SANCTUARY,
					SkillLevel = this.SkillLevel,
					Kind = AttackKind.Magic,
					SkillRatio = 1.0f,
					AttackElement = ElementType.Holy,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
				{
					var damage = Math.Max(1, heal);
					entrant.TakeDamage(damage, this.Owner);
					Send.ZC_NOTIFY_SKILL(this.Owner, entrant.Handle, SkillId.PR_SANCTUARY, this.SkillLevel, damage, 0, 0, ActionType.Skill);
				}
			}
			else if (!entrant.IsHostileTo(this.Owner))
			{
				entrant.HealHp(heal);
				if (entrant is PlayerCharacter pc)
					Send.ZC_RECOVERY(pc, ParameterType.Hp, heal);
			}

			if (this.HitsRemaining > 0)
				this.HitsRemaining--;
		}

		private static bool IsUndeadOrDemon(Character target)
		{
			if (target is not Monster monster) return false;
			return monster.Data.Element == ElementType.Undead
				|| monster.Data.Race == RaceType.Undead
				|| monster.Data.Race == RaceType.Demon;
		}
	}
}

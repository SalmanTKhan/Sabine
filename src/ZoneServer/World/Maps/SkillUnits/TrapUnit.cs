using System;
using Sabine.Shared.Const;
using Sabine.Shared.World;
using Sabine.Zone.Battle;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.World.Maps.SkillUnits
{
	/// <summary>
	/// Base for Hunter traps. Persistent ground unit that triggers
	/// once when an enemy steps on it. Subclasses override
	/// <see cref="OnTrigger"/> with the trap's effect.
	/// </summary>
	public abstract class TrapUnit : SkillUnit
	{
		protected TrapUnit(Character owner, Position position, SkillId skillId, int level, TimeSpan duration)
			: base(owner, position, skillId, level, duration)
		{
			this.HitsRemaining = 1;
		}

		public override void OnTouch(Character entrant)
		{
			if (entrant == null || entrant == this.Owner) return;
			if (!this.IsEnemy(entrant)) return;
			if (this.HitsRemaining == 0) return;

			this.HitsRemaining = 0;
			this.OnTrigger(entrant);
		}

		/// <summary>
		/// Force-triggers the trap as if an enemy had stepped on it.
		/// Used by HT_SPRINGTRAP. Picks the nearest hostile (to the
		/// trap's owner) within the trap cell or one cell out as the
		/// victim. If no hostile is in range, the trap is consumed
		/// without an effect (still single-use).
		/// </summary>
		public void Spring()
		{
			if (this.HitsRemaining == 0) return;
			this.HitsRemaining = 0;

			Character victim = null;
			if (this.Map != null)
			{
				foreach (var c in this.Map.GetCharactersInRange(this.Position, 1))
				{
					if (c == this.Owner) continue;
					if (!this.IsEnemy(c)) continue;
					victim = c;
					break;
				}
			}

			// AOE traps ignore the victim arg; single-target traps will
			// no-op gracefully on null since most checks guard for it,
			// but to keep semantics aligned with OnTouch we only fire
			// when a real victim exists.
			if (victim != null)
				this.OnTrigger(victim);
		}

		protected abstract void OnTrigger(Character victim);
	}

	public class SkidTrapUnit : TrapUnit
	{
		public SkidTrapUnit(Character owner, Position pos, int level)
			: base(owner, pos, SkillId.HT_SKIDTRAP, level, TimeSpan.FromSeconds(300)) { }

		protected override void OnTrigger(Character victim)
		{
			victim.Controller?.Knockback(this.Position, 5 + this.SkillLevel);
			Send.ZC_NOTIFY_SKILL(this.Owner, victim.Handle, this.SkillId, this.SkillLevel, 0, 0, 0, ActionType.Skill);
		}
	}

	public class LandMineUnit : TrapUnit
	{
		public LandMineUnit(Character owner, Position pos, int level)
			: base(owner, pos, SkillId.HT_LANDMINE, level, TimeSpan.FromSeconds(150)) { }

		protected override void OnTrigger(Character victim)
		{
			var ctx = new AttackContext(this.Owner, victim)
			{
				SkillId = this.SkillId,
				SkillLevel = this.SkillLevel,
				Kind = AttackKind.Magic,
				SkillRatio = 1.0f + 0.40f * this.SkillLevel,
				AttackElement = ElementType.Earth,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				victim.TakeDamage(result.Damage, this.Owner);
				if (RandomProvider.Get().Next(100) < 50)
					victim.StatusEffects.Start(StatusId.Stun, 1, TimeSpan.FromSeconds(3), this.Owner);
			}
			Send.ZC_NOTIFY_SKILL(this.Owner, victim.Handle, this.SkillId, this.SkillLevel, result.Damage, 0, 0, ActionType.Skill);
		}
	}

	public class AnkleSnareUnit : TrapUnit
	{
		public AnkleSnareUnit(Character owner, Position pos, int level)
			: base(owner, pos, SkillId.HT_ANKLESNARE, level, TimeSpan.FromSeconds(120)) { }

		protected override void OnTrigger(Character victim)
		{
			victim.StatusEffects.Start(StatusId.AnkleSnare, this.SkillLevel, TimeSpan.FromSeconds(4 + this.SkillLevel), this.Owner);
			Send.ZC_NOTIFY_SKILL(this.Owner, victim.Handle, this.SkillId, this.SkillLevel, 0, 0, 0, ActionType.Skill);
		}
	}

	public class ShockwaveTrapUnit : TrapUnit
	{
		public ShockwaveTrapUnit(Character owner, Position pos, int level)
			: base(owner, pos, SkillId.HT_SHOCKWAVE, level, TimeSpan.FromSeconds(150)) { }

		protected override void OnTrigger(Character victim)
		{
			// Drains 15% of victim's max SP per level. Approximated as
			// flat 50*level SP burn through TrySpendSp.
			var sp = 50 * this.SkillLevel;
			victim.TrySpendSp(sp);
			Send.ZC_NOTIFY_SKILL(this.Owner, victim.Handle, this.SkillId, this.SkillLevel, sp, 0, 0, ActionType.Skill);
		}
	}

	public class SandmanUnit : TrapUnit
	{
		public SandmanUnit(Character owner, Position pos, int level)
			: base(owner, pos, SkillId.HT_SANDMAN, level, TimeSpan.FromSeconds(150)) { }

		protected override void OnTrigger(Character victim)
		{
			if (RandomProvider.Get().Next(100) < (50 + 5 * this.SkillLevel))
				victim.StatusEffects.Start(StatusId.Sleep, this.SkillLevel, TimeSpan.FromSeconds(20 + 5 * this.SkillLevel), this.Owner);
			Send.ZC_NOTIFY_SKILL(this.Owner, victim.Handle, this.SkillId, this.SkillLevel, 0, 0, 0, ActionType.Skill);
		}
	}

	public class FlasherUnit : TrapUnit
	{
		public FlasherUnit(Character owner, Position pos, int level)
			: base(owner, pos, SkillId.HT_FLASHER, level, TimeSpan.FromSeconds(150)) { }

		protected override void OnTrigger(Character victim)
		{
			victim.StatusEffects.Start(StatusId.Blind, this.SkillLevel, TimeSpan.FromSeconds(30), this.Owner);
			Send.ZC_NOTIFY_SKILL(this.Owner, victim.Handle, this.SkillId, this.SkillLevel, 0, 0, 0, ActionType.Skill);
		}
	}

	public class FreezingTrapUnit : TrapUnit
	{
		public FreezingTrapUnit(Character owner, Position pos, int level)
			: base(owner, pos, SkillId.HT_FREEZINGTRAP, level, TimeSpan.FromSeconds(150)) { }

		protected override void OnTrigger(Character victim)
		{
			var ctx = new AttackContext(this.Owner, victim)
			{
				SkillId = this.SkillId,
				SkillLevel = this.SkillLevel,
				Kind = AttackKind.Magic,
				SkillRatio = 0.30f * this.SkillLevel,
				AttackElement = ElementType.Water,
				AlwaysHits = true,
			};
			var result = BattleCalculator.Calc(ctx);
			if (!result.IsMiss)
			{
				victim.TakeDamage(result.Damage, this.Owner);
				if (RandomProvider.Get().Next(100) < (40 + 10 * this.SkillLevel))
					victim.StatusEffects.Start(StatusId.Freeze, this.SkillLevel, TimeSpan.FromSeconds(20), this.Owner);
			}
			Send.ZC_NOTIFY_SKILL(this.Owner, victim.Handle, this.SkillId, this.SkillLevel, result.Damage, 0, 0, ActionType.Skill);
		}
	}

	public class BlastMineUnit : TrapUnit
	{
		public BlastMineUnit(Character owner, Position pos, int level)
			: base(owner, pos, SkillId.HT_BLASTMINE, level, TimeSpan.FromSeconds(25)) { }

		protected override void OnTrigger(Character victim)
		{
			var ratio = 0.50f + 0.50f * this.SkillLevel;
			foreach (var enemy in this.Map.GetCharactersInRange(this.Position, 2))
			{
				if (!enemy.IsHostileTo(this.Owner)) continue;
				var ctx = new AttackContext(this.Owner, enemy)
				{
					SkillId = this.SkillId,
					SkillLevel = this.SkillLevel,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Wind,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					enemy.TakeDamage(result.Damage, this.Owner);
			}
		}
	}

	public class ClaymoreTrapUnit : TrapUnit
	{
		public ClaymoreTrapUnit(Character owner, Position pos, int level)
			: base(owner, pos, SkillId.HT_CLAYMORETRAP, level, TimeSpan.FromSeconds(150)) { }

		protected override void OnTrigger(Character victim)
		{
			var ratio = 0.75f + 0.75f * this.SkillLevel;
			foreach (var enemy in this.Map.GetCharactersInRange(this.Position, 3))
			{
				if (!enemy.IsHostileTo(this.Owner)) continue;
				var ctx = new AttackContext(this.Owner, enemy)
				{
					SkillId = this.SkillId,
					SkillLevel = this.SkillLevel,
					Kind = AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Fire,
					AlwaysHits = true,
				};
				var result = BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					enemy.TakeDamage(result.Damage, this.Owner);
			}
		}
	}

	public class TalkieBoxUnit : TrapUnit
	{
		public string Message { get; }
		public TalkieBoxUnit(Character owner, Position pos, int level, string message)
			: base(owner, pos, SkillId.HT_TALKIEBOX, level, TimeSpan.FromSeconds(60))
		{
			this.Message = message ?? string.Empty;
		}

		protected override void OnTrigger(Character victim)
		{
			if (victim is PlayerCharacter pc)
				pc.ServerMessage(this.Message);
		}
	}
}

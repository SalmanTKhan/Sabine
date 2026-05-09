#pragma warning disable IDE0009

using System;
using System.Linq;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Zone.Ais.Base;
using Sabine.Zone.World.Actors;
using Yggdrasil.Util;

namespace Sabine.Zone.Ais.Impl
{
	/// <summary>
	/// Mercenary AI: stays close to its owner, engages whatever the
	/// owner is currently fighting (or whoever just hit the owner),
	/// and casts a faction-appropriate offensive skill on cooldown.
	/// Mirrors <see cref="HomunculusAi"/>'s shape with eA's MS_* /
	/// MA_* / ML_* substituted for the existing player skills (eA's
	/// merc-specific skill ids are functionally identical rebrandings
	/// of SM_BASH / AC_DOUBLE / KN_PIERCE etc., so reusing the player
	/// SkillId is simpler than adding the MS_* range to the enum).
	/// </summary>
	[Ai("Mercenary")]
	public class MercenaryAi : ReactiveAi
	{
		private const int FollowDistance = 3;
		private const int LeashRange = 14;
		private const int AssistScanRange = 9;

		private DateTime _nextSkillAt;

		protected override void Init()
		{
			base.Init();
			During("Idle", PickTarget);
			During("Idle", FollowOwner);
			During("Combat", LeashCheck);
			During("Combat", AutoCast);
		}

		private PlayerCharacter Owner => (this.Character as Mercenary)?.Owner;

		private void PickTarget(CallbackState state)
		{
			if (state.Handled || _targetCharacterHandle != 0) return;

			var owner = this.Owner;
			if (owner == null || owner.IsDead) return;

			// Priority 1: owner's current target.
			if (owner.TargetHandle != 0
				&& this.Character.Map.TryGetCharacter(owner.TargetHandle, out var ownerTarget)
				&& !ownerTarget.IsDead
				&& this.Character.IsHostileTo(ownerTarget))
			{
				StartRoutine("Combat", Combat(ownerTarget.Handle));
				state.Handled = true;
				return;
			}

			// Priority 2: whoever just hit the owner.
			if (owner.AttackerHandleTest != 0
				&& this.Character.Map.TryGetCharacter(owner.AttackerHandleTest, out var hitter)
				&& !hitter.IsDead
				&& this.Character.IsHostileTo(hitter))
			{
				StartRoutine("Combat", Combat(hitter.Handle));
				state.Handled = true;
				return;
			}

			// Priority 3: scan radius for nearby aggressors.
			var nearby = this.Character.Map.GetCharactersInRange(this.Character.Position, AssistScanRange)
				.Where(c => this.Character.IsHostileTo(c) && !c.IsDead);
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
			if (state.Handled) return;
			if (_targetCharacterHandle == 0) return;

			var owner = this.Owner;
			if (owner == null) return;

			if (!this.Character.Map.TryGetCharacter(_targetCharacterHandle, out var target)
				|| target.IsDead
				|| !owner.Position.InRange(target.Position, LeashRange))
			{
				_targetCharacterHandle = 0;
				StartRoutine("Idle", Idle());
				state.Handled = true;
			}
		}

		/// <summary>
		/// Per-faction offensive cast roll. Archers double-strafe,
		/// swordsmen bash, spearmen pierce. Higher-rank mercs get
		/// a higher skill level (1-5 scaling with the merc's rank).
		/// Cooldown is 5s so the cast doesn't spam every tick.
		/// </summary>
		private void AutoCast(CallbackState state)
		{
			if (state.Handled) return;
			if (DateTime.Now < _nextSkillAt) return;
			if (_targetCharacterHandle == 0) return;
			if (!this.Character.Map.TryGetCharacter(_targetCharacterHandle, out var target)) return;
			if (target.IsDead) return;

			var merc = this.Character as Mercenary;
			if (merc?.State == null) return;

			var (skillId, level) = ResolveSkill(merc);
			if (skillId == SkillId.None) return;

			if (!ZoneServer.Instance.SkillHandlers.TryGetHandler<Sabine.Zone.Skills.Handlers.ITargetedSkillHandler>(skillId, out var handler))
				return;

			var skill = new Sabine.Zone.Skills.Skill(this.Character, skillId, level);
			handler.Handle(new Sabine.Zone.Skills.Handlers.UseSkillParams(this.Character, target, skill, level));

			_nextSkillAt = DateTime.Now.AddSeconds(5);
		}

		private static (SkillId skillId, int level) ResolveSkill(Mercenary merc)
		{
			var data = Sabine.Shared.Game.CurrentData?.Mercenaries?.Entries;
			if (data == null) return (SkillId.None, 0);
			if (!data.TryGetValue(merc.State.MobId, out var entry)) return (SkillId.None, 0);

			// Per-row override: when the data entry names an explicit
			// skill, parse and use it instead of the faction default.
			// Lets per-rank mercs in mercenaries.txt carry signature
			// skills (e.g. rank-10 archer using DoubleStrafing while
			// lower ranks stay on Double).
			var rankLevel = Math.Max(1, Math.Min(5, (entry.Rank + 1) / 2));

			if (!string.IsNullOrEmpty(entry.OffensiveSkill)
				&& Enum.TryParse<SkillId>(entry.OffensiveSkill, true, out var overrideSkill)
				&& overrideSkill != SkillId.None)
			{
				var lvl = entry.OffensiveSkillLevel > 0 ? entry.OffensiveSkillLevel : rankLevel;
				return (overrideSkill, lvl);
			}

			// Faction defaults — same mapping as before, applied when
			// no per-row override is set.
			return entry.Faction switch
			{
				MercenaryFaction.Archer => (SkillId.AC_DOUBLE, rankLevel),
				MercenaryFaction.Sword => (SkillId.SM_BASH, rankLevel),
				MercenaryFaction.Spearman => (SkillId.KN_PIERCE, rankLevel),
				MercenaryFaction.Lancer => (SkillId.KN_BRANDISHSPEAR, rankLevel),
				_ => (SkillId.None, 0),
			};
		}
	}
}

#pragma warning restore IDE0009

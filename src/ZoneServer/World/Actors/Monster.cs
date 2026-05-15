using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sabine.Shared.Const;
using Sabine.Shared.Data.Databases;
using Sabine.Zone.Network;
using Sabine.Zone.World.Actors.Components.Characters;
using Yggdrasil.Logging;
using Yggdrasil.Util;

namespace Sabine.Zone.World.Actors
{
	/// <summary>
	/// Represents a monster NPC.
	/// </summary>
	public class Monster : Npc
	{
		private readonly List<Item> _dropItems = new();
		private TimeSpan _aliveTime = TimeSpan.Zero;

		/// <summary>
		/// Character that summoned this monster, if any. Friendly to the
		/// master and the master's party — see <see cref="IsHostileTo"/>.
		/// Mirrors eAthena's <c>md->master_id</c> on summoned mobs (e.g.
		/// AM_CANNIBALIZE plants, AM_SPHEREMINE Marine Spheres).
		/// </summary>
		public Character Master { get; set; }

		/// <summary>
		/// Optional auto-despawn duration. When set, the monster removes
		/// itself from the map after this much wall time, like eAthena's
		/// <c>mob_setdelayspawn</c> on summon mobs.
		/// </summary>
		public TimeSpan? Lifespan { get; set; }

		/// <summary>
		/// When &gt; 0, this monster behaves as a contact mine: it scans
		/// for the first hostile character within this cell radius and
		/// detonates, dealing AoE damage and removing itself.
		/// </summary>
		public int SelfDestructRadius { get; set; }

		/// <summary>
		/// Skill level cached on the summon — drives the detonation
		/// damage formula. eAthena's AM_SPHEREMINE damage scales with
		/// the original cast level.
		/// </summary>
		public int SelfDestructLevel { get; set; }

		/// <summary>
		/// Skill the mine reports when it explodes (cosmetic + battle
		/// log). For Sphere Mines this is <see cref="SkillId.AM_SPHEREMINE"/>.
		/// </summary>
		public SkillId SelfDestructSkill { get; set; }

		private bool _detonated;

		/// <summary>
		/// Drop-table indices already taken by Steal on this instance.
		/// Prevents stealing the same drop slot twice from the same mob.
		/// In-memory only; cleared implicitly on respawn.
		/// </summary>
		public HashSet<int> StolenDropSlots { get; } = new();

		/// <summary>
		/// Set once Steal Coin succeeds against this mob; eAthena's
		/// <c>state.steal_coin_flag</c>. Prevents repeated zeny drains
		/// from the same mob instance.
		/// </summary>
		public bool StolenCoin { get; set; }

		/// <summary>
		/// Return a reference to the monster's data.
		/// </summary>
		public MonsterData Data { get; }

		/// <summary>
		/// Raised when the monster was killed.
		/// </summary>
		public event Action<Monster> Killed;

		/// <summary>
		/// Creates new monster.
		/// </summary>
		/// <param name="identityId"></param>
		/// <exception cref="ArgumentException"></exception>
		public Monster(IdentityId identityId)
			: base(identityId)
		{
			if (!ZoneServer.Instance.Data.Monsters.TryFind(identityId, out var data))
				throw new ArgumentException($"Data for monster '{identityId}' not found.");

			this.Name = data.Name;

			this.Data = data;
			this.ApplyData();

			this.Components.Add(new RecoveryComponent(this));
		}

		/// <summary>
		/// Modifies monster and its stats based on the loaded data.
		/// </summary>
		private void ApplyData()
		{
			this.Parameters.BaseLevel = this.Data.Level;

			this.Parameters.HpMax = this.Data.Hp;
			this.Parameters.Hp = this.Data.Hp;
			this.Parameters.SpMax = this.Data.Sp;
			this.Parameters.Sp = this.Data.Sp;

			this.Parameters.Str = this.Data.Str;
			this.Parameters.Agi = this.Data.Agi;
			this.Parameters.Vit = this.Data.Vit;
			this.Parameters.Int = this.Data.Int;
			this.Parameters.Dex = this.Data.Dex;
			this.Parameters.Luk = this.Data.Luk;

			this.Parameters.AttackMin = this.Data.AttackMin;
			this.Parameters.AttackMax = this.Data.AttackMax;
			this.Parameters.MagicAttackMin = this.Data.AttackMin;
			this.Parameters.MagicAttackMax = this.Data.AttackMax;
			this.Parameters.Defense = this.Data.Defense;
			this.Parameters.Hit = this.Parameters.BaseLevel + this.Parameters.Dex;
			this.Parameters.Flee = this.Parameters.BaseLevel + this.Parameters.Agi;

			this.Parameters.Speed = this.Data.Speed;
			this.Parameters.AttackDelay = this.Data.AttackDelay;
			this.Parameters.AttackMotionDelay = this.Data.AttackMotion;
			this.Parameters.DamageMotionDelay = this.Data.DamageMotion;

			this.Parameters.RecalculateAll();
		}

		/// <summary>
		/// Emperium-specific damage gate: incoming damage from a
		/// player only counts during active WoE, when the player is in
		/// a guild, and when that guild is *not* the current owner of
		/// the castle the Emperium sits on. All other paths (NPC
		/// damage, GM kills, scripts) are unaffected.
		/// </summary>
		/// <remarks>
		/// Mirrors eAthena's <c>battle_check_target</c> handling for
		/// emperium: outside WoE the mob is invulnerable; inside, the
		/// owning guild can't break their own crystal.
		/// </remarks>
		public override int TakeDamage(int amount, Character attacker)
		{
			if ((int)this.Data.Id == Sabine.Shared.Const.MonsterId.EMPERIUM)
			{
				if (!this.IsEmperiumDamageAllowed(attacker))
					return this.Parameters.Hp;
			}

			return base.TakeDamage(amount, attacker);
		}

		private bool IsEmperiumDamageAllowed(Character attacker)
		{
			// Engine-driven damage paths (status DoT, scripts) pass
			// null attackers — let those through so GM cleanup still
			// works.
			if (attacker is not PlayerCharacter playerCharacter)
				return true;

			var woe = ZoneServer.Instance.World.Woe;
			if (!woe.IsActive)
				return false;

			if (playerCharacter.Guild == null)
				return false;

			if (this.Map == null)
				return true;

			var castle = ZoneServer.Instance.World.Castles.Get(this.Map.StringId);
			if (castle == null)
				return true;

			return castle.OwnerGuildId != playerCharacter.Guild.Id;
		}

		/// <summary>
		/// Kills the monster and removes it from the map with a death
		/// animation.
		/// </summary>
		/// <param name="killer"></param>
		public async override void Kill(Character killer)
		{
			try
			{
				base.Kill(killer);

				this.GiveExp(killer);
				this.GiveMvpExp(killer);
				this.AnnounceMvpKill(killer);

				// Removing the monster with a delay seems wonky, but if we
				// don't, the client will not display the damage for the last
				// hit and the monster will disappear before the hit animation
				// is even done. It feels good with a 1s delay though.
				// Especially with Porings, which pop at the height of their
				// hit animation. Alternative implementation: DisappearTime,
				// which will despawn the monster after X amount of time.
				await Task.Delay(1000);

				this.DropItems(killer);
				this.DropMvpItems(killer);
				this.DropFixedItems(killer);

				this.Killed?.Invoke(this);
				Sabine.Zone.Battle.CombatEvents.RaiseMonsterKilled(this, killer);

				// WoE Emperium-broken hook — transfers castle ownership to
				// the killer's guild and fans out the per-castle
				// `OnGuildBreak` event before the mob is removed from the
				// map. Guarded on identity id so the path is a no-op for
				// every other mob.
				if ((int)this.Data.Id == Sabine.Shared.Const.MonsterId.EMPERIUM
					&& killer is PlayerCharacter killerPc
					&& killerPc.Guild != null
					&& this.Map != null)
				{
					var castle = ZoneServer.Instance.World.Castles.Get(this.Map.StringId);
					if (castle != null)
						ZoneServer.Instance.World.Woe.OnEmperiumBroken(castle, killerPc.Guild.Id);
				}

				this.Map.RemoveNpc(this);
			}
			catch (Exception ex)
			{
				Log.Error("Monster.Kill: " + ex);
			}
		}

		/// <summary>
		/// Gives EXP to the character(s) that killed the monster.
		/// </summary>
		/// <param name="killer"></param>
		private void GiveExp(Character killer)
		{
			if (killer is not PlayerCharacter playerCharacter)
				return;

			// Apply the cross-cutting reward multiplier on the pool
			// before any party share. BD_RICHMANKIM and future event
			// boosts amplify the whole party's gain when the killer is
			// the aura bearer; per-member multipliers run later via the
			// individual GainBaseExp paths.
			var expMult = RewardModifiers.GetExpMultiplier(playerCharacter);
			var baseExp = (int)(this.Data.BaseExp * expMult);
			var jobExp = (int)(this.Data.BaseExp * expMult);

			// Even-share among same-map party members when the
			// killer's party has the option enabled. Falls back to
			// individual when the party isn't eligible.
			if (!Sabine.Zone.World.Groups.PartyDistribution.TryShareExp(playerCharacter, baseExp, jobExp))
			{
				playerCharacter.GainBaseExp(baseExp);
				playerCharacter.GainJobExp(jobExp);
			}

			// Homunculus EXP share — 10% of the owner's base EXP.
			// Mirrors eAthena: pet/homun share with no kill-credit
			// requirement on the homun itself.
			if (playerCharacter.Homunculus?.IsActive == true)
				playerCharacter.Homunculus.GainExp(baseExp / 10);
		}

		/// <summary>
		/// Gives EXP for killing an MVP if applicable.
		/// </summary>
		/// <param name="killer"></param>
		private void GiveMvpExp(Character killer)
		{
			if (killer is not PlayerCharacter playerCharacter)
				return;

			var exp = this.Data.MvpExp;
			if (exp <= 0)
				return;

			playerCharacter.GainBaseExp(exp);

			Send.ZC_MVP(playerCharacter);
			Send.ZC_MVP_GETTING_SPECIAL_EXP(playerCharacter, exp);
		}

		/// <summary>
		/// Broadcasts a server-wide announcement when an MVP or boss-mode
		/// mob is killed by a player.
		/// </summary>
		/// <param name="killer"></param>
		private void AnnounceMvpKill(Character killer)
		{
			if (killer is not PlayerCharacter playerCharacter)
				return;

			if (this.Data.MvpExp <= 0 && !this.Data.IsBoss)
				return;

			var message = string.Format("[{0}] has defeated MVP [{1}]", playerCharacter.Name, this.Data.Name);
			Send.ZC_BROADCAST(message);
		}

		/// <summary>
		/// Drops the monster's items.
		/// </summary>
		/// <param name="killer"></param>
		private void DropItems(Character killer)
			=> this.DropRandomItems(killer, this.Data.Drops);

		/// <summary>
		/// Drops the monster's MVP items.
		/// </summary>
		/// <param name="killer"></param>
		private void DropMvpItems(Character killer)
			=> this.DropRandomItems(killer, this.Data.MvpDrops, isMvp: true);

		/// <summary>
		/// Drops the monster's items.
		/// </summary>
		/// <param name="killer"></param>
		private async void DropRandomItems(Character killer, List<DropData> dropsData, bool isMvp = false)
		{
			try
			{
				if (dropsData.Count == 0)
					return;

				var rnd = RandomProvider.Get();
				var map = this.Map;
				var pos = this.Position;
				var killerPlayer = killer as PlayerCharacter;
				var rewardDropMult = RewardModifiers.GetDropMultiplier(killer);

				for (var i = 0; i < dropsData.Count; ++i)
				{
					var dropData = dropsData[i];
					var dropRate = (ZoneServer.Instance.Conf.World.ItemDropRate / 100f) * rewardDropMult;
					var dropChance = dropData.Chance * dropRate;

					if (dropChance < rnd.Next(100))
						continue;

					if (!ZoneServer.Instance.Data.Items.Contains(dropData.ItemId))
					{
						Log.Warning("Monster.DropRandomItems: Drop item '{0}' not found.", dropData.ItemId);
						continue;
					}

					await Task.Delay(100);

					var item = new Item(dropData.ItemId);
					var dropPos = pos.GetRandomInSquareRange(1);

					item.Drop(map, dropPos);

					// For MVP drops, send the special "MVP getting item"
					// message to the killer so the client displays the
					// signature pop-up for the loot they received.
					if (isMvp && killerPlayer != null)
						Send.ZC_MVP_GETTING_ITEM(killerPlayer, dropData.ItemId);
				}
			}
			catch (Exception ex)
			{
				Log.Error("Monster.DropRandomItems: " + ex);
			}
		}

		/// <summary>
		/// Drops items that were added to the monster to be dropped.
		/// </summary>
		/// <param name="killer"></param>
		private void DropFixedItems(Character killer)
		{
			// TODO: Create a shared method for dropping a list of items?

			var rnd = RandomProvider.Get();
			var map = this.Map;
			var pos = this.Position;

			lock (_dropItems)
			{
				foreach (var item in _dropItems)
				{
					var dropPos = pos.GetRandomInSquareRange(1);
					item.Drop(map, dropPos);
				}
			}
		}

		// Per-skill cooldown clocks read by the AI's mob-skill tick.
		// Keyed by skill id; value is the next-eligible UTC tick. The
		// monster keeps its own dictionary instead of stuffing cooldowns
		// onto AI state so re-attaching the AI (or attaching multiple
		// reactive AIs in stacking ladders) doesn't desync the timing.
		private readonly Dictionary<SkillId, DateTime> _mobSkillNextAt = new();

		/// <summary>
		/// Tries to cast one of the mob's skills at the given target.
		/// Reuses the standard skill-handler registry — no separate mob
		/// pipeline. Reads <c>Game.CurrentData.MobSkills</c> for the
		/// skill list, rolls each entry independently against its rate,
		/// gates on per-skill cooldown, and routes through
		/// <see cref="ITargetedSkillHandler"/>. Self-target entries pass
		/// the monster as both caster and target.
		/// </summary>
		public bool TryCastMobSkill(Character target)
		{
			if (this.IsImmobilized || this.IsSilenced) return false;
			if (target == null || target.IsDead) return false;

			var skills = Sabine.Shared.Game.CurrentData?.MobSkills?.GetForMob((int)this.Data.Id);
			if (skills == null || skills.Count == 0) return false;

			var rnd = Yggdrasil.Util.RandomProvider.Get();
			var now = DateTime.UtcNow;

			foreach (var entry in skills)
			{
				if (_mobSkillNextAt.TryGetValue(entry.SkillId, out var nextAt) && now < nextAt)
					continue;

				if (rnd.Next(100) >= entry.Rate)
					continue;

				if (!ZoneServer.Instance.SkillHandlers.TryGetHandler<Sabine.Zone.Skills.Handlers.ITargetedSkillHandler>(entry.SkillId, out var handler))
					continue;

				var castTarget = entry.Target == "self" ? this : target;
				var skill = new Sabine.Zone.Skills.Skill(this, entry.SkillId, entry.Level);
				handler.Handle(new Sabine.Zone.Skills.Handlers.UseSkillParams(this, castTarget, skill, entry.Level));

				_mobSkillNextAt[entry.SkillId] = now.AddMilliseconds(entry.CooldownMs);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Sets up the AI and attaches it to the monster.
		/// </summary>
		/// <param name="aiName"></param>
		public void AttachAi(string aiName)
		{
			if (!ZoneServer.Instance.AiManager.TryCreateAi(aiName, out var ai))
			{
				// Let's fall back silently for now, since most AIs don't
				// exist yet.
				Log.Warning("Monster.AttachAi: AI '{0}' not found, using fallback.", aiName);

				var fallback = "Type01";
				if (!ZoneServer.Instance.AiManager.TryCreateAi(fallback, out ai))
				{
					Log.Warning("Monster.AttachAi: AI '{0}' not found, using fallback.", aiName);
					Log.Error("Monster.AttachAi: Fallback AI '{0}' not found.", fallback);
				}
			}

			ai.Character = this;
			this.Components.Add(ai);
		}

		/// <summary>
		/// Adds the given to the list of items that are dropped when
		/// the monster is killed.
		/// </summary>
		/// <param name="item"></param>
		public void AddDropItem(Item item)
		{
			lock (_dropItems)
				_dropItems.Add(item);
		}

		/// <summary>
		/// Drives lifespan expiry on summoned mobs. Owned summons silently
		/// vanish without giving EXP/loot, matching eAthena's behaviour
		/// for AM_CANNIBALIZE plants and AM_SPHEREMINE mines that time
		/// out without exploding.
		/// </summary>
		public override void Update(TimeSpan elapsed)
		{
			base.Update(elapsed);

			if (this.IsDead)
				return;

			_aliveTime += elapsed;

			if (this.SelfDestructRadius > 0 && !_detonated)
				this.TryDetonate();

			if (this.Lifespan == null || _detonated)
				return;

			if (_aliveTime < this.Lifespan.Value)
				return;

			this.Lifespan = null;
			this.Map?.RemoveNpc(this);
		}

		/// <summary>
		/// Scans the local area for any hostile and, if one is in range,
		/// detonates the mine: deals magic Fire damage to every hostile
		/// inside the blast radius and removes the mine. Mirrors eAthena
		/// <c>skill_castend_damage_id</c> for AM_SPHEREMINE — the sphere
		/// itself is the hit center, not the original cast position.
		/// </summary>
		private void TryDetonate()
		{
			if (this.Master == null || this.Map == null)
				return;

			Character trigger = null;
			foreach (var c in this.Map.GetCharactersInRange(this.Position, this.SelfDestructRadius))
			{
				if (c == this || c == this.Master || c.IsDead) continue;
				if (!this.IsHostileTo(c)) continue;
				trigger = c;
				break;
			}

			if (trigger == null)
				return;

			_detonated = true;

			var attacker = this.Master;
			var level = Math.Max(1, this.SelfDestructLevel);
			var ratio = 2.0f + 1.0f * level;

			Send.ZC_NOTIFY_GROUNDSKILL(attacker, this.SelfDestructSkill, attacker.Handle, level, this.Position.X, this.Position.Y, 0);

			foreach (var enemy in this.Map.GetCharactersInRange(this.Position, this.SelfDestructRadius))
			{
				if (enemy == this || enemy.IsDead) continue;
				if (!this.IsHostileTo(enemy)) continue;

				var ctx = new Sabine.Zone.Battle.AttackContext(attacker, enemy)
				{
					SkillId = this.SelfDestructSkill,
					SkillLevel = level,
					Kind = Sabine.Zone.Battle.AttackKind.Magic,
					SkillRatio = ratio,
					AttackElement = ElementType.Fire,
					AlwaysHits = true,
				};
				var result = Sabine.Zone.Battle.BattleCalculator.Calc(ctx);
				if (!result.IsMiss)
					enemy.TakeDamage(result.Damage, attacker);
			}

			this.Map.RemoveNpc(this);
		}

		internal override bool IsHostileTo(Character target)
		{
			if (this.Master != null)
			{
				if (target == this.Master)
					return false;

				if (this.Master is PlayerCharacter masterPc && target is PlayerCharacter targetPc)
				{
					if (masterPc.Party != null && targetPc.Party == masterPc.Party)
						return false;
				}
			}

			return base.IsHostileTo(target);
		}
	}
}

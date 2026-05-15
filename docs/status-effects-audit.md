# Status Effect Coverage Audit (Sabine vs eAthena)

Snapshot of where each `StatusId` stands relative to eAthena/rAthena's
`SC_*` table. Used as a punch list for future iterations; verdicts can
go stale as work lands, so re-check the relevant handler / listener
before relying on an entry.

Verdict key:

- **WORKS** — handler applies the effect and a consumer (BattleCalculator,
  movement controller, on-hit listener, AI) reads it.
- **PARTIAL** — handler exists but the consumer is incomplete, or the
  consumer exists but the handler doesn't supply the expected values.
- **STUB** — defined in the enum (or has a hollow handler) with no
  gameplay impact yet.
- **MISSING** — eAthena has a `SC_*` that probably belongs in Sabine but
  has no `StatusId` yet.

## Common ailments (Universal / Novice)

- **Stone** — WORKS. Immobilization + half-DEF + ×1.5 damage taken.
- **Freeze** — WORKS. Action block + half-DEF + damage multiplier.
- **Stun** — WORKS. Blocks input via `IsStunned` + movement controller.
- **Sleep** — WORKS. Action block + ×1.5 damage; breaks on first non-zero hit.
- **Poison** — WORKS. DoT tick. Slow Poison antidote missing.
- **Curse** — WORKS. Stat reductions; battle consumption indirect through stat cache.
- **Silence** — WORKS. Spell-cast gate.
- **Confusion** — WORKS. Randomizes attack target.
- **Blind** — WORKS. −25% HIT/FLEE.
- **Bleeding** — WORKS. DoT + ~0.3% proc on weapon hits.
- **TrickDead** — WORKS. AI drops aggro; breaks on damage.

## Swordsman / Knight / Lord Knight

- **Provoke** — WORKS. ATK%+/DEF%− via `BonusModifiers`; force-aggro through `AttackerHandleTest`.
- **Endure** — WORKS. Suppresses dmotion stagger + StopMove on hit; consumes one of 7 charges per hit; blocks knockback.
- **Two Hand Quicken** — WORKS. ASPD; cancels IncreaseAgi.
- **Spear Quicken** — WORKS. Speed; mirrors quicken pipeline.
- **Auto Counter** — WORKS. Guaranteed-crit return swing on melee hit, consumes window.
- **Riding** — WORKS. OPTION_RIDING + 25% move-speed bonus.
- **Parrying (LK)** — WORKS. Block roll for 2H sword in BattleCalculator.
- **LK Concentration** — WORKS. HIT/FLEE/Crit bonus.
- **Aura Blade** — WORKS. +level×10 ATK in BattleCalculator.
- **Berserk** — WORKS. +20% ATK; aggro side-effects.
- **Tension Relax** — WORKS. `RegenComponent.RegenHp` triples the natural tick while active.
- **Moving Recovery** — WORKS. `RegenComponent` skips its walk-pause gate while the status is held.
- **AutoBerserk** — WORKS. `Character.TakeDamage` auto-starts Provoke Lv10 when HP drops to 25% MaxHP while the skill is learned.

## Crusader / Paladin

- **Defender** — WORKS. Long-range damage reduction in BattleCalculator.
- **Auto Guard** — WORKS. 5%×level full-block roll on physical hits.
- **Reflect Shield** — WORKS. 10+3×level% physical return; pierces filter.
- **Devotion** — WORKS. Redirect listener + 5-link cap enforced via `Character.DevotionLinkedTargets`; bonds drop on caster death.
- **Providence** — WORKS. SubElement[Holy] + SubRace[Demon] gating is correct as-is (BattleCalculator only reads SubRace for the attacker's race).
- **Trust (Faith)** — WORKS. Holy resist + 200×level MaxHP bonus per eAthena status.c:2376.
- **Sacrifice** — WORKS. Per-hit `SacrificeListener` charges 9% MaxHP, deals fixed damage, decrements 5-charge buffer, ends at zero.
- **Gospel** — WORKS. 10s OnTick rolls representative subset of eAthena's gospel table on party (heal / Blessing / IncreaseAgi / Angelus / Concentration) and enemies (Curse / Blind / DecreaseAgi / smite damage).

## Priest / High Priest

- **Blessing** — WORKS. +STR/INT/DEX.
- **Increase Agi / Decrease Agi** — WORKS. AGI + Speed deltas; mutually cancelling.
- **Angelus** — WORKS. +max HP (Val-snapshotted bonus).
- **Imposition Manus** — WORKS. +MATK.
- **Magnificat** — WORKS. SP regen flag.
- **Gloria** — WORKS. Tiered stat bonus.
- **Aspersio** — WORKS. Endow Holy.
- **Lex Aeterna** — WORKS. ×2 damage on next hit, consumed.
- **Kyrie Eleison** — WORKS. Barrier HP + hit counter, expires in calc.
- **Assumptio** — WORKS. ~50% physical reduction.
- **Suffragium** — WORKS. `Skill.GetEffectiveCastTime` reduces cast time by 15/30/45% per buff level; consumed by the cast scheduler after one cast.
- **Sanctuary** — WORKS. `SanctuaryUnit` heals living allies / damages Undead per tick.
- **B.S. Sacramenti** — WORKS. PR_BENEDICTIO applies `StatusId.BSSacramenti` to the caster and nearby party members for 60s; grants +Holy resist and +MaxHP per level.

## Wizard / High Wizard

- **Quagmire** — WORKS. Dispels IncreaseAgi / TwoHandQuicken / Concentration.
- **Magic Power (HW)** — WORKS. +MATK.
- **Frost Nova** — PARTIAL. Freeze applied; central AoE damage missing.
- **Storm Gust Counter** — WORKS. Pre-renewal Storm Gust now stacks a 3-hit counter status (4s window); the third hit freezes and clears the counter, matching eAthena classic.
- **MISSING — Pneuma** — eAthena SC ground-effect blocking ranged.
- **MISSING — Safety Wall** — Cell-based magical-attack block.

## Hunter / Sniper

- **Falcon Fly** — PARTIAL. OPTION_FALCON flag; no Blitz / Beast Strafe damage tie-in beyond skill gate.
- **True Sight (Sniper)** — WORKS. Snapshot primary-stat bonus.
- **Wind Walk** — WORKS. +FLEE.
- **Ankle Snare** — WORKS. Trap unit applies status; `IsRooted` reads it so movement controller blocks pathing. BattleCalculator now zeroes hard DEF while the snare holds (eAthena status.c:5552).

## Blacksmith / Whitesmith

- **Adrenaline Rush** — WORKS. +20% weapon ATK (Val2 snapshot).
- **Weapon Perfection** — WORKS. Bypasses size penalty.
- **Power Maximize** — WORKS. Max-roll attack.
- **Over Thrust / Max Over Thrust** — WORKS. Damage % bonus; MaxOT cancels OT.
- **Cart Boost** — WORKS. Speed bonus (cart-equipped not enforced).
- **Melt Down** — WORKS. EquipBreakListener rolls weapon (100*level/1000) and armor (70*level/1000) break per hit, honoring CR_FULLPROTECTION.

## Assassin

- **Cloaking** — WORKS for attack-cancel; basic-attack now breaks both Hiding and Cloaking via `Character.Attack`. Wall-adjacent reveal still deferred.
- **Enchant Poison** — WORKS. Poison endow.
- **Poison React** — PARTIAL. Counter listener present; poison-elem requirement check thin.
- **Venom Dust / Venom Splasher** — WORKS. VenomDustUnit poisons on-step; VenomSplasher tags target and detonates on expiry.
- **MISSING — EDP** — Per-hit ATK% escalator.

## Rogue / Stalker

- **Strip Weapon / Shield / Armor / Helm** — WORKS. Status handlers unequip the matching slot on apply via `Inventory.FindEquipped` + `UnequipItem`. Re-equip block during status duration deferred.
- **Plagiarism** — PARTIAL. Listener exists; copy-state expiration not durable across logout.
- **Preserve** — STUB. Plagiarism state lock missing.
- **Tunnel Drive** — WORKS. Status handler applies a per-level Speed delta (30 ms/level, capped at 120) mirroring eAthena's classic walk-while-hidden bonus.
- **Chase Walk** — WORKS. OPTION_HIDE flipped; speed slowdown (eAthena 35-5*level%) applied via Speed delta. SP drain tick deferred.
- **Reject Sword** — WORKS. `RejectSwordListener` rolls 15%*level reflect on melee non-skill hits, consumes one of 3 charges per fire, ends at zero.

## Merchant / Alchemist / Creator

- **CP Weapon / Shield / Armor / Helm** — WORKS. Strip skills query before applying.
- **Full Protection** — STUB. Equip-break exemption listener missing.
- **Fortune (Hocus Pocus)** — WORKS. +20 LUK.
- **Gravity (Hocus Pocus)** — WORKS. Immobilization.
- **Monocell / ClassChange / ReverseOrcish / Question** — PARTIAL. Sprite / name swap; no gameplay effect.

## Monk / Champion

- **Spirits** — WORKS. MO_CALLSPIRITS gathers, Val1 stores count, MO_FINGEROFFENSIVE / MO_EXTREMITYFIST / MO_ABSORBSPIRITS consume.
- **Explosion Spirits** — WORKS. +20 ATK / +50 CRIT for 60s via status handler.
- **Mental Strength (Steel Body)** — WORKS. 90% damage cap in BattleCalculator.ApplyStatusMultipliers.
- **Chain Combo / Combo Finish** — WORKS. 2s window markers, consumed by chained skill handlers.
- **Blade Stop** — WORKS. Catch + mutual stun.

## Sage

- **Auto Spell** — WORKS. AutoSpellListener rolls 5+2*level% on-hit proc of stored bolt skill (defaults MG_FIREBOLT) at AttackResolved.
- **Dragonology** — WORKS. +4 ATK / level vs Dragon-race targets via `GetRaceMasteryBonus`.
- **Free Cast** — WORKS. The MovementController's cast-cancel branch checks `Skill.HasFreeCast` so the caster can move while a spell is in flight.
- **Mind Breaker** — WORKS. PF_MINDBREAKER applies `StatusId.MindBreaker` to the target: -10+(level-1)*4 MDEF and +20+(level-1)*2 outgoing MATK% via `BattleCalculator.ApplyMatkRate`.
- **Endow Fire / Water / Wind / Earth** — WORKS. Element coating.
- **Magic Rod** — WORKS. Magic absorb to SP.
- **Volcano / Deluge / Whirlwind / Magnetic Earth** — WORKS. Field bonuses.

## Bard / Dancer / Clown / Gypsy

- **Aura pipeline** — WORKS. `SongUnit.OnTouch` now extends an active status's duration via `StatusEffectComponent.Refresh` instead of restarting it, preventing stat-bonus thrash and icon spam while a character stands inside the song footprint.
- **Whistle** — WORKS. +4*level Flee.
- **Assassin Cross of Sunset** — WORKS. +5*level% ASPD bonus.
- **Poem of Bragi** — WORKS. Val2 = 3*level% feeds the cast-time reduction in `Skill.GetEffectiveCastTime`.
- **Apple of Idun** — WORKS. +5*level% MaxHP.
- **Humming** — WORKS. +4*level Hit.
- **Don't Forget Me** — WORKS. Enemy debuff: −3*level% ASPD + slower walk.
- **Fortune Kiss** — WORKS. +5+2*level Critical.
- **Service For You** — PARTIAL. Grants +INT bonus as stand-in; eAthena reduces SP cost on cast (needs cast pipeline).
- **Dissonance** — WORKS. Per-tick 24+8*level DoT on enemies in the cell.
- **Ugly Dance** — WORKS. Per-tick 5+3*level SP drain on enemies.
- **Lullaby** — WORKS. Per-tick Sleep proc roll on enemies.
- **Mental Sensing (Loki's Veil)** — WORKS. Dispels Hide / Cloak on enemies entering the aura.
- **Mr. Kim a Rich Man** — WORKS. New `StatusId.RichmanKim` aura; `RewardModifiers.GetExpMultiplier` amplifies the pre-share EXP pool by 25+25*level%.
- **Ragnarok (BD_RAGNAROK)** — WORKS. Strips all dispellable buffs from characters in the aura via the shared `DispelCategory` API.
- **Drum on the Battlefield** — WORKS. +25*level ATK + 5*level Hit for party.
- **Eternal Chaos** — WORKS. Enemy debuff: -15*level soft DEF.
- **Ring of the Nibelungen** — WORKS. +50*level ATK for party (weapon-type gate deferred).
- **Into the Abyss** — WORKS. -50% SP cost on casts via `PacketHandler.TryDeductSkillCost`.
- **Invulnerable Siegfried** — WORKS. +20+5*level% resist on every non-Neutral element.
- **Marionette / Marionette2** — WORKS. Donor / receiver pair tracked via `MarionettePair` helper; stat-transfer packed in vals.
- **Longing Freedom** — STUB. Ensemble lock missing.
- **Hermode** — WORKS. `StatusEffectComponent.DispelCategory(Buff)` short-circuits when the bearer has Hermode — matches eAthena's status_change_clear_buffs gate.

## Underused flags (no consumer)

- `Sight` — skill itself owns the reveal; the status is just a duration marker.
- (removed — MovingRecovery now exempts HP regen from the walk-pause, see Swordsman section)
- `Basilica` — cell list flagged; damage / movement integration missing.

## Remaining gaps (priority order)

1. Bard/Dancer per-tick aura passive broadcast (Power Chord / Lullaby / Mental Sensing / Service For You / Fortune Kiss).
2. Sage SA_DRAGONOLOGY (+ATK vs Dragon race), Mind Breaker (-INT/VIT on hit), Free Cast (requires cast bars first).
3. Wizard Pneuma / Safety Wall ground-effect framework.
4. Assassin EDP (Assassin Cross — Reborn tier).
5. Crusader/Paladin Sacramenti, Creator Full Protection equip-break exemption.
6. Rogue Tunnel Drive speed burst, Preserve plagiarism lock.
7. Hocus Pocus Monocell / ClassChange / ReverseOrcish / Question gameplay impact.
8. Tension Relax stagger / regen tie-in.
9. Storm Gust freeze counter (PARTIAL).
10. Frost Nova AoE damage leg.

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
- **Tension Relax** — PARTIAL. Flag set; no faster HP/SP regen tie-in.
- **AutoBerserk** — STUB. Skill watches HP threshold; status flag has no consumer.

## Crusader / Paladin

- **Defender** — WORKS. Long-range damage reduction in BattleCalculator.
- **Auto Guard** — WORKS. 5%×level full-block roll on physical hits.
- **Reflect Shield** — WORKS. 10+3×level% physical return; pierces filter.
- **Devotion** — PARTIAL. Damage redirect listener exists; charge cap / link-loss disconnect logic incomplete.
- **Providence** — PARTIAL. Element-class reduction not gated to Demon/Undead.
- **Trust (Faith)** — PARTIAL. Flat reduction applied; no pre-/re-renewal split.
- **Sacrifice** — PARTIAL. Flag only; finite-charge HP redirect not wired.
- **Gospel** — STUB. Per-tick random buff roll missing.

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
- **Suffragium** — STUB. Cast-time reduction missing.
- **Sanctuary** — STUB. Ground-effect heal + undead damage missing.
- **Sacramenti** — STUB. Defined but never applied.

## Wizard / High Wizard

- **Quagmire** — WORKS. Dispels IncreaseAgi / TwoHandQuicken / Concentration.
- **Magic Power (HW)** — WORKS. +MATK.
- **Frost Nova** — PARTIAL. Freeze applied; central AoE damage missing.
- **Storm Gust Counter** — STUB. Hit-counter for freeze-proc missing.
- **MISSING — Pneuma** — eAthena SC ground-effect blocking ranged.
- **MISSING — Safety Wall** — Cell-based magical-attack block.

## Hunter / Sniper

- **Falcon Fly** — PARTIAL. OPTION_FALCON flag; no Blitz / Beast Strafe damage tie-in beyond skill gate.
- **True Sight (Sniper)** — WORKS. Snapshot primary-stat bonus.
- **Wind Walk** — WORKS. +FLEE.
- **Ankle Snare** — STUB. Trap unit applies; root + 0-DEF not wired.

## Blacksmith / Whitesmith

- **Adrenaline Rush** — WORKS. +20% weapon ATK (Val2 snapshot).
- **Weapon Perfection** — WORKS. Bypasses size penalty.
- **Power Maximize** — WORKS. Max-roll attack.
- **Over Thrust / Max Over Thrust** — WORKS. Damage % bonus; MaxOT cancels OT.
- **Cart Boost** — WORKS. Speed bonus (cart-equipped not enforced).
- **Melt Down** — STUB. Equip-break on hit not wired.

## Assassin

- **Cloaking** — PARTIAL. OPTION_CLOAK + speed; wall-adjacent reveal / attack-cancel not enforced.
- **Enchant Poison** — WORKS. Poison endow.
- **Poison React** — PARTIAL. Counter listener present; poison-elem requirement check thin.
- **Venom Dust / Venom Splasher** — STUB. No handlers.
- **MISSING — EDP** — Per-hit ATK% escalator.

## Rogue / Stalker

- **Strip Weapon / Shield / Armor / Helm** — STUB. Equip removal listener missing.
- **Plagiarism** — PARTIAL. Listener exists; copy-state expiration not durable across logout.
- **Preserve** — STUB. Plagiarism state lock missing.
- **Tunnel Drive** — STUB. Speed burst + cell-skip movement.
- **Chase Walk** — PARTIAL. OPTION_HIDE flipped; speed math + attack-toggle off missing.
- **Reject Sword** — STUB. Reflect-charge listener missing.

## Merchant / Alchemist / Creator

- **CP Weapon / Shield / Armor / Helm** — WORKS. Strip skills query before applying.
- **Full Protection** — STUB. Equip-break exemption listener missing.
- **Fortune (Hocus Pocus)** — WORKS. +20 LUK.
- **Gravity (Hocus Pocus)** — WORKS. Immobilization.
- **Monocell / ClassChange / ReverseOrcish / Question** — PARTIAL. Sprite / name swap; no gameplay effect.

## Monk / Champion

- **Spirits / Explosion Spirits / Mental Strength / Chain Combo / Combo Finish** — STUB. Spirit sphere system entirely missing.
- **Blade Stop** — WORKS. Catch + mutual stun.
- **MISSING — Asura strike state hooks** — Sphere consumption / cooldown lockouts.

## Sage

- **Free Cast / Auto Spell / Mind Breaker / Dragonology** — STUB. No handlers / listeners.
- **Endow Fire / Water / Wind / Earth** — WORKS. Element coating.
- **Magic Rod** — WORKS. Magic absorb to SP.
- **Volcano / Deluge / Whirlwind / Magnetic Earth** — WORKS. Field bonuses.

## Bard / Dancer / Clown / Gypsy

- **Bard Song / Dancer Dance** — PARTIAL. Aura membership flagged; per-tick passive broadcast missing.
- **Power Chord / Lullaby / Mental Sensing / Service For You / Fortune Kiss** — STUB. No specific song handlers.
- **Marionette / Marionette2** — PARTIAL. Pair flag; stat-cap transfer not implemented.
- **Longing Freedom** — STUB. Ensemble lock missing.
- **Hermode** — STUB. Dispel immunity not enforced.

## Underused flags (no consumer)

- `Sight` — skill itself owns the reveal; the status is just a duration marker.
- `LoudExclamation` — only STR delta applied; eAthena grants +4 to all primary stats.
- `MovingRecovery` — no recovery-on-move tick.
- `Basilica` — cell list flagged; damage / movement integration missing.

## Critical gaps to add (priority order)

1. Monk spirit sphere system (unblocks 5+ skills).
2. Rogue Strip equip-removal on-hit listener.
3. Priest Sanctuary ground effect + Pneuma / Safety Wall ground-effect framework.
4. Hunter Ankle Snare root.
5. Bard/Dancer per-tick aura passive broadcast.
6. Stalker Chase Walk speed math + Reject Sword reflect charges.
7. Crusader Sacrifice charges + Devotion link-loss handling.
8. EDP (Assassin) per-hit ATK escalator.

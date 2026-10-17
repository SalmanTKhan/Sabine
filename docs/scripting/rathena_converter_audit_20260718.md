# rAthena Converter Audit — 2026-07-18

Scope: the 990 scripts listed by `user/tmp/active-rathena-script-files-20260718.txt`.

## Regeneration reassessment (2026-07-18)

The active rAthena source families were regenerated with the current
converter and routed back into their existing `rathena/{re,pre-re,npcs,warps}`
script roots. Of the 990 active files, 424 were emitted from a recognised
NPC definition; the other 566 were retained because their source has no
definition the converter can currently host (typically function-only or
floating-script files).

| Finding | Before regeneration | Current active corpus |
| --- | ---: | ---: |
| `TODO:` | 15,240 | 6,476 |
| `TODO: duplicate(...)` | 9,591 | 3,151 |
| `TODO: callfunc(...)` | 1,877 | 944 |
| `Untranslated.Int/Str` | 2,852 | 2,233 |
| floating script TODO | 645 | 645 |
| `System.Convert.ToString` | 10,215 | 3,161 |

The generated subset now contains 2,250 TODOs (145 duplicate and 372
callfunc); retained skipped files contain the other 4,226 TODOs (3,006
duplicate and 572 callfunc). This makes floating-script hosting and
cross-file duplicate resolution the largest remaining blockers.

OnTouch remains a runtime-system gap, rather than a resolved conversion:
328 generated comments still request trigger wiring and 257 scripts only
subscribe `OnTouch` labels. No NPC trigger dispatcher raises those label
events today.

After the subsequent OnTouch runtime bridge and regeneration, 82 active
scripts emit `AttachScriptTouchTrigger` and the explicit trigger-wiring
diagnostic fell to 262. The remaining entries are in source files that still
cannot be hosted by the converter. Current active totals are 6,410 TODOs,
3,151 duplicate TODOs, 944 callfunc TODOs, 2,233 untranslated expressions,
645 floating-script TODOs, and 3,161 `System.Convert.ToString` calls.

## Current diagnostics

| Finding | Occurrences | Files | Meaning |
| --- | ---: | ---: | --- |
| `TODO:` | 15,240 | 561 | Generated conversion/system gaps |
| `TODO: duplicate(...)` | 9,591 | — | Cross-file NPC template reference was not resolved |
| `TODO: callfunc(...)` | 1,877 | 217 | Shared rAthena function was not lowered |
| `Untranslated.Int/Str` | 2,852 | 286 | Expression parser lost semantic content (the active corpus still contains empty legacy payloads) |
| OnTouch wiring TODO | 733 | — | Npc trigger-area runtime registration is missing |
| floating script TODO | 645 | — | Global/floating script lifecycle has no generated runtime host |
| `System.Convert.ToString` | 10,215 | 392 | Type-inference quality debt; usually not a runtime-system gap |

The largest content categories by combined TODO/untranslated count are quests (4,734), other (3,864), jobs (1,890), custom (1,844), instances (1,724), and merchants (1,288).

## Root causes and work order

1. **Corpus symbol index (highest leverage)**
   - First pass: index all `script Name::ref` declarations, `duplicate(ref)` declarations, and top-level `function script Name` declarations.
   - Second pass: resolve duplicate templates across files and lower known shared functions through a typed runtime registry.
   - This removes the 9,591 duplicate TODOs and establishes the correct foundation for the 1,877 `callfunc` sites.

   Status: implemented in `SabineTools/convert_rathena.py` with a runtime
   dialog-template registry. A full staged conversion reduced unresolved
   duplicate diagnostics from 9,523 to 1,700; it generated 1,250 cross-file
   duplicate dispatches. The remaining entries have no matching template in
   the rAthena NPC corpus and remain named diagnostics for source/feature
   review.

2. **NPC trigger and floating-script runtime**
   - Add an async OnTouch trigger-area registration API to `Npc` / `Shortcuts` and have generated scripts register their OnTouch handlers.
   - Add a generated host/dispatcher for floating scripts and their OnInit/OnTimer/event labels.
   - This enables 733 trigger areas and 645 currently inert global scripts.

3. **Expression diagnostics before broad regeneration**
   - Preserve compact operation names and argument shapes instead of the active corpus's empty `Untranslated.Int(\"\")` payloads.
   - Classify each unresolved expression by value, condition, assignment, or dialogue context; do not replace it with a guessed numeric default.

4. **Shared-function cohorts after the registry exists**
   - Already implemented as runtime lowerings: `F_BG_Badge`, `F_GM_NPC`, `F_Okolnir`, `F_tt`, the formatting helpers (`F_InsertComma`, `F_GetNumSuffix`, `F_GetArticle`, `F_GetPlural`, `F_InsertArticle`, `F_InsertPlural`), the equipment anti-swap guards (`F_IsEquipIDHack`, `F_IsEquipRefineHack`, `F_IsEquipCardHack`), `F_HasEdenGroupMark`, and `F_CanOpenStorage`.
   - The Turbo Track lowering reads the invoking NPC's map ID to preserve keys such as `n1` and `e16`. The anti-swap lowerings compare the currently equipped item ID, refine level, and four card slots after the player has made a refine selection.
   - Next cohorts: Kafra (`F_Kafra`, `F_KafSet`, `F_KafEnd`), socket helpers (`Func_Socket`, `Func_Socket2`), and Eden/board helpers (`F_AvenBoard`).
   - Leave custom quest-only helpers such as `LinkFlag` and `F_time_limit_recorder` as named diagnostics until their defining source and state model are indexed.

5. **System-specific conversion gaps**
   - Battleground scripts have both missing primitives (`waitingroom2bg`, `bg_get_data`, team/queue lifecycle) and trigger/duplicate failures. Treat Battleground as a bounded server-system project after the shared runtime foundations above.
   - Commands such as `Catwarp`, `F_InfoBoard`, `Pick`, `Disp`, `Restrict`, `Go`, and `setnpcdisplay` require source classification before mapping; several are custom script helpers rather than rAthena built-ins.

6. **Typing cleanup last**
   - Reduce redundant `System.Convert.ToString` only when an inferred helper return contract is known. This should be a compile/test-backed cleanup pass, not a global textual rewrite.

## Promotion gate

Do not replace the active generated corpus until the staged output has:

1. no empty unresolved diagnostic payloads;
2. no unresolved `duplicate()` references where the source ref is present in the corpus;
3. a script-load/compile validation pass; and
4. a diff review limited to the intended converter changes.

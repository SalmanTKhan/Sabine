# Sabine wire-format spec

Canonical, machine-readable description of every packet exchanged between
Sabine servers and clients on the wire. Hand-edited YAML; treated as
documentation only (no codegen, no lint at this stage). The C# in
`src/Shared/Network/PacketTable.*.cs` and the emit/consume code in
`src/{Auth,Char,Zone}Server/Network/Send.cs` + `PacketHandler.cs` are the
authoritative implementations — this directory exists so both Sabine and
[open-midgard](https://github.com/.../open-midgard) can agree on what
"PV23 / S2500" means without re-reading the C# every time.

## Why this exists

eAthena's `db/packet_db.txt` (the de-facto industry reference) only
records `(op_id, size, parser, field_offsets)`. It carries no field
*types*, no sub-record reuse, and no decomposition of packed fields like
RO's 3-byte position. That's enough for eA to wire up dispatching, but
not enough to act as an interop contract between two independent
implementations. This spec fills that gap.

## Layout

```
wire/
  spec-version.txt   "1"; bump when the schema changes (not the data)
  types.yaml         scalar vocabulary; packed_pos; packed_move; arrays
  ops.yaml           catalog of every NAMED op in Sabine.Shared.Network.Op
                     (655 entries) — name + direction + one-line summary.
                     No wire IDs, no sizes, no bodies; those vary by phase.
  records.yaml       shared sub-records (CHARACTER_INFO, ...)
  phases/            one file per Sabine PacketTable phase. Carries the
                     per-phase wire IDs, sizes, and full body layouts for
                     the SUBSET of ops actually emitted/consumed at that
                     phase.
    s2500.yaml       kRO Sak EP12 / 2008-09-10aSakexe / PV23 (~47 active ops)
  examples/          hex captures + decoded JSON, one pair per worked op
```

`ops.yaml` and `phases/*.yaml` answer different questions:

- **"Does this op name exist in Sabine?"** -> `ops.yaml`. The catalog covers
  every named member of the `Op` enum, including ops that no current phase
  emits (e.g. memorial-dungeon, IR-mail, the friend-list family at S2500
  even though Sabine has no handlers yet).
- **"What's on the wire when phase X sends op Y?"** -> `phases/X.yaml`.
  Only populated for ops the phase actually exchanges, with full body
  layouts derived from Sabine's emit/consume code at HEAD.

Earlier eras (alpha, beta1, beta2, EP3/4/8/9/10) are not populated yet —
back-fill lazily when someone needs them.

## Reading an op

Each op in `phases/s2500.yaml` looks like:

```yaml
- op: ZC_NOTIFY_PLAYERMOVE
  summary: "Self-move ACK; client uses move_start_time to interpolate."
  direction: S2C
  versions:
    s2500:
      wire_id: 0x0087
      size: 12
      body:
        - { name: move_start_time, type: tick }
        - { name: move,            type: packed_move }
  refs:
    - "src/ZoneServer/Network/Send.cs:328"
    - "eA src/map/clif.c clif_walkok"
```

- **`direction`**: `S2C` (server → client) or `C2S` (client → server).
- **`versions.<phase>.wire_id`**: the 16-bit op code emitted on the wire,
  little-endian.
- **`versions.<phase>.size`**: total packet bytes including the 2-byte
  op-code header. Use the literal `dynamic` for variable-size packets;
  those carry a 2-byte total-size field after the op code (see
  *Framing*).
- **`body`**: ordered list of fields *after* the framing bytes. The op
  code header is implicit; the total-size short (dynamic packets only)
  is implicit.
- **`refs`**: pointers to the authoritative implementation. Sabine paths
  are relative to repo root.

## Framing

Every packet on the wire is:

```
fixed-size:    [ wire_id : u16 LE ] [ body... ]
dynamic:       [ wire_id : u16 LE ] [ total_size : u16 LE ] [ body... ]
```

Both the `wire_id` and the dynamic-only `total_size` are managed by the
PacketTable runtime; they don't appear in `body:`.

## Endianness

Little-endian, globally. `types.yaml` declares this once. No op overrides
it.

## Type vocabulary (summary)

Full definitions live in `types.yaml`. Quick reference:

| Type | Bytes | Purpose |
|---|---|---|
| `u8`/`i8` | 1 | |
| `u16`/`i16` | 2 | |
| `u32`/`i32` | 4 | |
| `tick` | 4 | u32 server tick ms |
| `string_fixed length=N` | N | CP949, NUL-padded |
| `blob length=N` | N | opaque bytes |
| `pad length=N` | N | reserved-zero; reader ignores, writer MUST zero-fill |
| `packed_pos` | 3 | (x:10, y:10, dir:4) |
| `packed_move` | 6 | (fx:10, fy:10, tx:10, ty:10, sx:4, sy:4) |
| `array<T>` | var | length controlled by `count_kind` + `count_from` |
| `enum<T>` | sizeof(T) | typed reference into an enum table |

Strings are CP949 by default (matches kRO-era clients). New Sabine-native
ops may opt into UTF-8 per-field by writing
`type: string_fixed, encoding: utf8, length: N`.

## Versioning — base + delta

When phase files for earlier eras get back-filled, each op's `versions:`
map is ordered by phase. The first phase that defines an op gives a
full `body:`. Later phases either:

1. **Inherit and tweak** — `extends: <prior_phase>` plus one or more of:
   - `insert_after: <field_name>` + `add: [...]`
   - `remove: [<field_name>, ...]`
   - `replace: { <field_name>: <new_def> }`
2. **Redefine wholesale** — give a fresh `body:` (no `extends`) when the
   layout was substantially redesigned. This corresponds to a
   `Redefine(...)` call in `src/Shared/Network/PacketTable.*.cs`; a
   `ChangeSize(...)` corresponds to inherit-and-tweak.

Since this commit only populates `s2500.yaml`, every op has a single
`versions.s2500` entry with a full `body:`. The pattern is documented
here so a future EP3-author knows what to do.

## Adding a new op

1. Add it to `ops.yaml` (registry: name, direction, one-line summary).
2. Add a body block to `phases/<phase>.yaml`.
3. Cite the Sabine source file:line in `refs:`.
4. If the op shares a sub-record with another op, factor the sub-record
   into `records.yaml` (don't inline it twice).
5. Optional but encouraged: add a hex/json fixture pair under
   `examples/` so a future reader can verify the layout by hand.

## Adding a new phase

1. Create `phases/<new-phase>.yaml`.
2. For each op the phase touches, add a `versions.<new-phase>` entry,
   preferring `extends:` for incremental changes.
3. Update `phases/README.md` (if exists) listing phases in chronological
   order with their Sabine `Versions.*` constant.

## What this spec does NOT cover

- `UNK_03XX` placeholder ops in `src/Shared/Network/PacketTable.2500_sakEP12.cs`
  — those are slot reservations, not part of the active wire surface.
- The full ~200-op kRO catalogue — only ops Sabine actually emits or
  consumes.
- Connection-level framing below the packet layer (TLS, compression,
  obfuscation). None of those exist in the Sabine ↔ open-midgard path
  today.
- Encryption/obfuscation of opcode IDs — the table is plain.

## Verifying the spec against the implementation

Spot-check via grep on op names:

```sh
# Every op in phases/s2500.yaml must have a matching Register / Redefine
# in PacketTable.2500_sakEP12.cs (or be inherited from PacketTable.cs).
```

A future commit may add a `tools/wire-lint` CLI that does this
automatically. Until then it's manual.

## Prior art

- eAthena `db/packet_db.txt` — the format we deliberately don't use. See
  the rationale at the top of this file.
- rAthena `src/map/packets.hpp` + `packets_struct.hpp` — closer to what
  this spec wants (named struct fields), but tied to C++ struct layout
  and PACKETVER `#ifdef`s. We borrow the body-layout discipline without
  the C++ language coupling.

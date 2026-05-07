# MapCacheTool

`src/MapCacheTool` is a small CLI that turns Gravity client `.gat` /
`.rsw` files into the binary `system/data/map_cache_*.dat` files Sabine
loads at startup. Use it whenever you need to support a map that isn't
already in the shipped caches.

## Quick start

1. Extract the maps you need from a client GRF (e.g. `data.grf` /
   `rdata.grf`) using **GRFTool** or **GRFEditor**. You want the
   `.gat` and matching `.rsw` for each map, in any folder.
2. Make a map list — one map name per line (with or without `.gat`),
   `//` lines are comments. Example `tmp/maps.txt`:
   ```
   prontera
   prt_fild08
   // izlude
   ```
3. Run the tool from the repo root:
   ```
   dotnet run --project src/MapCacheTool -- \
     --input-dir tmp/extracted \
     --map-list  tmp/maps.txt \
     --output    system/data/map_cache_200.dat
   ```

The era is inferred from the output filename (`_100` → alpha, `_200` →
beta, otherwise modern). Override with `--era alpha|beta|modern`.

## Why eras matter

Alpha and beta clients distinguish two walkable cell types:

| Raw GAT | Sabine `TileType` (alpha/beta) | Sabine `TileType` (modern) |
|---------|--------------------------------|----------------------------|
| 0       | `Unk0` (field ground)          | `Unk0`                     |
| 1       | `Unpassable`                   | `Unpassable`               |
| 2       | `Unk2` (snipeable)             | `Unk2`                     |
| 3       | `Unk2` (walkable water)        | `Unk2`                     |
| 5       | `Passable` (town floor)        | `Unk0`                     |
| 6       | `Passable`                     | `Unk0`                     |

Modern clients collapse type 5 onto type 0, which is why a modern-era
build remaps both onto `Unk0`.

Walkable-ground cells whose corner height exceeds the `.rsw` water
level are remapped to walkable water (`Unk2`), matching rAthena's
`mapcache.cpp` behavior.

## Flags

| Flag                | Required | Notes |
|---------------------|----------|-------|
| `--input-dir <dir>` | one of   | Folder of loose `.gat` / `.rsw` files. |
| `--grf <file>`      | one of   | **Reserved**, throws today. |
| `--grf-list <file>` | one of   | **Reserved**, throws today. |
| `--map-list <file>` | yes      | Map names, one per line. |
| `--output <file>`   | yes      | Output `.dat` path. |
| `--era <name>`      | no       | `alpha`, `beta`, `modern`. Inferred from output filename if omitted. |
| `--no-compress`     | no       | Write blobs uncompressed, useful for diffing. |
| `--rebuild`         | no       | Accepted for parity with rAthena; the tool always rebuilds. |

## Output format

The writer mirrors `MapCacheDb.Read` in
`src/Shared/Data/Databases/MapCache.cs`:

```
[4 bytes] magic ("MCAC")
[int32  ] version (1)
[bool   ] compressed flag
[int32  ] map count
per map:
  [int32] blob length
  [bytes] blob (deflate when compressed=true)
    payload after decompress:
      [string] stringId    (BinaryWriter 7-bit length-prefixed)
      [int32 ] width
      [int32 ] height
      [byte  ] type * width * height (row-major: outer y, inner x)
```

Compression uses `DeflateStream` with no zlib wrapper — same as the
reader expects.

## Limits / future work

- GRF reading is not implemented. The flag surface is reserved so a
  later PR can add it without breaking callers.
- The tool emits a single `TileType` byte per cell; runtime
  `CellFlags` overlays (basilica, ice wall, etc.) are still set by
  scripts at runtime.

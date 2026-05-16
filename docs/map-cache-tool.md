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

Gravity's `.gat` cell type byte has a well-documented meaning per the
[openkore field file format wiki][okwiki] and the
[rdw-archive RagnarokFileFormats GAT notes][rdwgat]. The canonical
names are:

| Raw GAT | Canonical name (openkore / rdw)        | Walkable |
|---------|----------------------------------------|----------|
| 0       | Walkable ground                        | yes      |
| 1       | Non-walkable block (wall / cliff)      | no       |
| 2       | Non-walkable water                     | no       |
| 3       | Walkable water                         | yes      |
| 4       | Walkable ground (snipable, rarely used)| yes      |
| 5       | Walkable cliff / town floor (alpha/beta); collapsed onto 0 in modern | varies |
| 6       | Walkable ground (snipable cliff edge)  | yes      |

Sabine's own `TileType` enum is narrower — it only encodes four
states (`Walkable`, `Unpassable`, `Water`, `Passable`) — so the tool
collapses the GAT byte onto a `TileType` per era:

| Raw GAT | Sabine `TileType` (alpha/beta) | Sabine `TileType` (modern) |
|---------|--------------------------------|----------------------------|
| 0       | `Walkable` (field ground)      | `Walkable`                 |
| 1       | `Unpassable`                   | `Unpassable`               |
| 2       | `Water` (non-walkable water)   | `Water`                    |
| 3       | `Water` (walkable water)       | `Water`                    |
| 5       | `Passable` (town floor)        | `Walkable`                 |
| 6       | `Passable` (snipable cliff)    | `Walkable`                 |

Modern clients collapse type 5 onto type 0 (everything walkable is
just "ground"), which is why a modern-era build remaps both onto
`Walkable`. Alpha and beta clients preserved the `{0, 5}` split so
towns look distinct from fields, and the hand-authored shipped caches
keep that split.

Walkable-ground cells whose bottom-left corner sits below the `.rsw`
water level are remapped to `Water`, matching rAthena's
`mapcache.cpp` behavior.

[okwiki]: https://openkore.com/wiki/Field_file_format
[rdwgat]: https://github.com/rdw-archive/RagnarokFileFormats

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

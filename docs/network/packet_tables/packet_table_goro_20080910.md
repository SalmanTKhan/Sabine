# Goro 2008-09-10 compatibility matrix

This is the wire-compatibility contract for Sabine's `goro-20080910`
profile. The server-side authority is the local rAthena `goro` branch at
commit `57c5b4d229291e7737c79f80b7b5fd326e00d4b7`. The client-side evidence is
the local `D:\Projects\goro` builders/parsers and their byte-fixture tests.

Sabine loads the S2500 effective data stack, clears the static packet maps,
and applies this overlay last. This is a wire profile, not a claim of full
rAthena database, formula, or script parity.

## Evidence labels

- `rAthena reference`: present in the pinned rAthena packet database or
  serializer.
- `goro-observed`: represented by a goro builder/parser or exact fixture.
- `inferred`: body semantics inferred from the reference layout or existing
  Sabine helper; it still needs runtime confirmation.
- `implemented`: Sabine has the mapping and sender/receiver path.
- `not performed`: the required live or loopback check has not been run.

## Playable-core matrix

| Direction | Packet | Wire ID / size | Evidence and Sabine state |
|---|---|---:|---|
| char -> server | `CZ_ENTER` | `0x0436 / 19` | rAthena reference; goro-observed; implemented |
| server -> char | `HC_ACCEPT_MAKECHAR` | `0x006D / 110` | rAthena reference; implemented with 108-byte character records |
| char -> server | `CH_DELETE_CHAR` | `0x01FB / 56` | rAthena reference; goro-observed; implemented |
| server -> zone | `HC_NOTIFY_ZONESVR` | `0x0071 / 28` | rAthena reference; implemented |
| char -> server | `CZ_REQUEST_TIME` | `0x0089 / 8` | rAthena reference; goro-observed; implemented; tick at offset 4 |
| server -> zone | `ZC_ACCEPT_ENTER` | `0x02EB / 13` | rAthena reference; implemented; start tick at offset 2 |
| server -> zone | `ZC_AID` | not sent | rAthena reference; implemented as an explicit Goro omission |
| server -> zone | `ZC_NOTIFY_STANDENTRY` | `0x0078 / 55` | rAthena reference; goro-observed; implemented |
| server -> zone | `ZC_NOTIFY_NEWENTRY` | `0x0079 / 53` | rAthena reference; goro-observed; implemented |
| server -> zone | `ZC_NOTIFY_ACTENTRY` | `0x007A / 58` | rAthena reference; goro-observed; inferred serializer path; implemented |
| server -> zone | `ZC_NOTIFY_MOVEENTRY` | `0x007B / 60` | rAthena reference; goro-observed; implemented |
| server -> zone | `ZC_NOTIFY_STANDENTRY_NPC` | `0x007C / 42` | rAthena reference; goro-observed; implemented |
| char -> server | `CZ_REQUEST_MOVE` | `0x00A7 / 8` | rAthena reference; goro-observed; implemented; packed position at offset 5 |
| char -> server | `CZ_CHANGE_DIRECTION` | `0x0085 / 11` | rAthena reference; goro-observed; implemented; head/body fields at 7/10 |
| char -> server | `CZ_REQUEST_ACT` | `0x0437 / 7` | rAthena reference; goro-observed; implemented |
| char -> server | legacy action alias | `0x0190 / 19` | rAthena reference; implemented receive-only alias |
| server -> zone | `ZC_NOTIFY_ACT` | `0x008A / 29` | rAthena reference; inferred legacy body; implemented |
| server -> zone | `ZC_NOTIFY_ACT_POSITION` | `0x008B / 23` | rAthena reference; inferred legacy body; implemented |
| char -> server | `CZ_REQNAME` | `0x008C / 11` | rAthena reference; goro-observed; implemented; GID at offset 7 |
| server -> zone | `ZC_ACK_REQNAME` | `0x0095 / 30` | rAthena reference; implemented through shared classic sender |
| char -> server | `CZ_REQUEST_CHAT` | `0x00F3 / dynamic` | rAthena reference; inferred from cumulative table; implemented |
| server -> zone | `ZC_NOTIFY_CHAT` / `ZC_NOTIFY_PLAYERCHAT` | dynamic | rAthena reference; inferred from shared classic chat path; implemented |
| char -> server | `CZ_ITEM_PICKUP` | `0x00F5 / 8` | rAthena reference; goro-observed; implemented; GID at offset 4 |
| char -> server | `CZ_ITEM_THROW` | `0x0116 / 10` | rAthena reference; goro-observed; implemented; index/amount at 5/8 |
| char -> server | `CZ_USE_ITEM` | `0x0439 / 8` | rAthena reference; goro-observed; implemented |
| server -> zone | `ZC_USE_ITEM_ACK` | `0x00A8 / 7` | rAthena reference; inferred legacy body; implemented |
| char -> server | `CZ_REQ_WEAR_EQUIP` | `0x00A9 / 6` | rAthena reference; inferred from cumulative table; implemented |
| server -> zone | `ZC_REQ_WEAR_EQUIP_ACK` | `0x00AA / 7` | rAthena reference; inferred legacy body; implemented |
| char -> server | `CZ_REQ_TAKEOFF_EQUIP` | `0x00AB / 4` | rAthena reference; implemented |
| server -> zone | `ZC_REQ_TAKEOFF_EQUIP_ACK` | `0x00AC / 7` | rAthena reference; inferred legacy body; implemented |
| server -> zone | `ZC_ITEM_ENTRY` | `0x009E / 17` | rAthena reference; implemented through classic floor-item helper |
| server -> zone | `ZC_ITEM_DISAPPEAR` | `0x00A1 / 6` | rAthena reference; implemented |
| server -> zone | inventory list | `0x02E8 / dynamic` | rAthena reference; goro-observed V3 entries; implemented |
| server -> zone | equipment list | `0x02D0 / dynamic` | rAthena reference; goro-observed V3 entries; implemented |
| server -> zone | `ZC_PAR_CHANGE` / `ZC_LONGPAR_CHANGE` | `0x00B0 / 8`, `0x00B1 / 8` | rAthena reference; implemented |
| server -> zone | `ZC_STATUS_CHANGE_ACK` | `0x00BC / 6` | rAthena reference; inferred legacy body; implemented |
| server -> zone | `ZC_STATUS` | `0x00BD / 44` | rAthena reference; inferred legacy body; implemented |
| server -> zone | `ZC_STATUS_CHANGE` | `0x00BE / 5` | rAthena reference; implemented |
| char -> server | `CZ_USE_SKILL` | `0x0438 / 10` | rAthena reference; goro-observed; implemented |
| char -> server | `CZ_USE_SKILL_TOGROUND` | `0x0113 / 22` | rAthena reference; goro-observed; implemented; fields at 5/9/12/20 |
| char -> server | `CZ_USE_SKILL_TOGROUND_WITHTALKBOX` | `0x007E / 102` | rAthena reference; goro-observed; implemented; text at offset 22 |
| server -> zone | `ZC_SKILLINFO_UPDATE` | `0x010E / 11` | rAthena reference; goro-observed; implemented |
| server -> zone | `ZC_SKILLINFO_LIST` | `0x010F / dynamic` | rAthena reference; goro-observed; implemented |
| server -> zone | `ZC_ADD_SKILL` | `0x0111 / 39` | rAthena reference; goro-observed; implemented |
| server -> zone | `ZC_NOTIFY_SKILL` | `0x0114 / 31` | rAthena reference; goro-observed; implemented; legacy damage width |
| server -> zone | `ZC_NOTIFY_SKILL_POSITION` | `0x0115 / 35` | rAthena reference; implemented |
| server -> zone | `ZC_NOTIFY_GROUNDSKILL` | `0x0117 / 18` | rAthena reference; implemented |
| server -> zone | NPC dialog | `0x0090`, `0x00B8`, `0x00B9` | rAthena reference; inferred shared classic handlers; implemented |
| server -> zone | classic trade/chat/item flows | cumulative classic IDs | inferred; implemented where the shared Sabine path has a sender and handler |

## Deferred or unverified surface

The following remain outside the playable-core milestone or require a live
client before they can be called compatible:

| Surface | Status |
|---|---|
| Login -> character list/create/delete -> map -> actor -> movement/action -> NPC/chat/item/equipment/skill reconnect smoke | not performed |
| Comparison against the pinned rAthena `goro` server | not performed |
| Guild, pets/homunculus, mail, auction, GM-only, and other optional flows | not performed; deferred |
| Full rAthena formula/database/script parity | not performed; out of scope |

The required acceptance boundary is therefore still `NOT PERFORMED` for live
compatibility. Passing unit tests and a Release build establish the static
wire contract only; they do not establish end-to-end client acceptance.

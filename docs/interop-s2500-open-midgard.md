# Interop testing: Sabine S2500 ↔ open-midgard

This runbook describes how to launch Sabine in `S2500` mode (kRO Sak EP12,
2008-09-10aSakexe) and connect the open-source open-midgard client to it
for live wire-format validation. Both projects target the same client
build — Sabine labels it `Versions.S2500 = 2500`, open-midgard labels the
same protocol family `PacketVer23`. They are expected to interop without
further protocol work, but bugs surface fastest by booting both and
walking through login → char select → map enter.

## 1. Configure Sabine for S2500

Sabine's default version is Beta1. Override per-developer via the user
conf so you don't dirty the tracked default.

```text
# E:\Projects\GitHub\Sabine\user\conf\version.conf
packet_version: 2500
```

Confirm port bindings in the tracked confs are usable on your box:

| Server | Default port(s) | Conf file |
|---|---|---|
| AuthServer | `7000, 6900` | `system\conf\auth.conf` |
| CharServer | `6121` | `system\conf\char.conf` |
| ZoneServer | `5121` | `system\conf\zone.conf` |

`char_server_ip` and `zone_server_ip` (the addresses Sabine sends back to
the client after login / char-select) default to `127.0.0.1`. Leave them
as is for a same-box test.

## 2. Build and start Sabine

```powershell
dotnet build Sabine.sln
.\start-all.bat
```

`start-all.bat` launches the three servers in sequence. On the first
boot the SQL schema (`sql\main.sql` plus everything under `sql\updates\`)
must be applied to the MySQL database whose credentials live in
`user\conf\database.conf`.

A clean S2500 startup logs:

```text
[Server] Loaded packet table for version 2500.
[Auth]   Listening on 0.0.0.0:7000, 0.0.0.0:6900.
[Char]   Listening on 0.0.0.0:6121.
[Zone]   Listening on 0.0.0.0:5121.
```

## 3. Configure open-midgard

open-midgard's `ClientProfilePreset` enum (`src/gamemode/LoginMode.cpp`)
has explicit Sabine handling for `SabineAlpha` / `SabineBeta1` /
`SabineBeta2`. There is **no** `SabineEP12` preset, but one is not
needed: Sabine S2500's `HC_ACCEPT_ENTER` emits a 24-byte header followed
by 108-byte character entries (see `src/CharServer/Network/Send.cs:63`),
which matches open-midgard's existing `CharListLayout::Legacy`
(`kLegacyCharListHeaderSize` + `sizeof(CHARACTER_INFO)` ≈ 24/108).

Use the default `Auto` profile — it falls through to the `Legacy` /
`PacketVer23` layout that Sabine S2500 expects.

In `clientinfo.xml` (or `settings.ini` `[Client]`):

```xml
<clientinfo>
  <connection>
    <address>127.0.0.1</address>
    <port>7000</port>
  </connection>
  <!-- omit clientprofile to use Auto, or set explicitly: -->
  <clientprofile>auto</clientprofile>
</clientinfo>
```

The login server returns the char-server address from
`char_server_ip` / port; the char server returns the zone-server
address from `zone_server_ip` / `zone_bind_port`. open-midgard reads
both from `SERVER_ADDR` structs (`src/Types.h:73-81`) — no client-side
configuration needed beyond the auth-server endpoint.

## 4. Smoke walkthrough

| Step | Expected | If it fails, look at |
|---|---|---|
| Login screen accepts username/password | AuthServer logs `Account 'foo' logged in.` | `system\conf\auth.conf`; account creation flag |
| Server list appears | Single entry "Sabine" | `char_server_ip` reachability; `HC_NOTIFY_ZONESVR` size |
| Click server → character list | 0–3 characters render | `HC_ACCEPT_ENTER` 24/108 layout; `Send.cs:50-74` |
| Create character | Char appears in slot | `HC_ACCEPT_MAKECHAR`; SQL `characters` insert |
| Click character → enter map | Player visible at start location | `HC_NOTIFY_ZONESVR`; ZoneServer `CZ_ENTER` handler |
| Walk | Movement smooth | `CZ_REQUEST_MOVE` size 11 (S2500 override); `ZC_NOTIFY_PLAYERMOVE` |
| Attack a Poring | Damage numbers appear | `CZ_REQUEST_ACT` size 8; `ZC_NOTIFY_ACT` |
| Save and relog | Position persists | `vars_character` SQL writes; ZoneDb flush |

## 5. Filing bugs

When a packet looks wrong, Sabine logs both the host op and the wire
op of every inbound packet (`Connection.cs:OnMessageReceived`). Capture
the hex dump from the log (`Hex.ToString` output, 16 bytes per line)
plus the rAthena reference in
`C:\Projects\GitHub\open-midgard\Ref\rathena\src\map\clif.cpp` for the
same op + PACKETVER 23 path. The diff is usually obvious — field
order, an off-by-one length, or an unset flag byte.

If a Sabine-specific quirk surfaces that open-midgard's `Auto`
resolution can't accommodate, the resolution is to add a
`SabineEP12` preset in
`open-midgard\src\gamemode\LoginMode.cpp` (mirror the existing
`SabineBeta1` branch in `ResolveCharListLayoutOverride` and
`ResolveClientProfilePreset`).

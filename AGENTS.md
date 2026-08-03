# AGENTS.md — Guidance for AI coding agents

This repository is **RimTaxi**, a RimWorld 1.6 mod. Read this file before changing code.

## Project goal

Civilian taxi shuttle: call from comms console, pay silver, board colonists/cargo, fly to a world-map destination, drop as a **caravan** (do not force settlement map entry / combat).

## Hard rules

1. **Investigate before editing** — read existing `Source/RimTaxi` and Defs; do not invent RimWorld APIs.
2. **Verify APIs** — prefer decompiled game code / installed `Assembly-CSharp.dll` over guesswork.
3. **No drive-by refactors** — only touch files needed for the task.
4. **Save compatibility** — keep Scribe fields stable; add new fields with defaults.
5. **Harmony only when needed** — prefer TransportShip / Defs / comps.
6. **DLC-safe** — do not require Odyssey. Royalty not hard-required (we ship our own TransportShip/WorldObject defs).
7. **Hospitality is out of scope permanently** — no integration code or folders.
8. **After changes** — run `dotnet build -c Release` from `Source/RimTaxi` (or `.\build.ps1`).

## Target environment

| Item | Value |
|------|--------|
| Game | RimWorld **1.6** |
| packageId | `kush.rimtaxi` |
| Namespace | `RimTaxi` |
| TFM | `net472` |
| Required mod | Harmony (`brrainz.harmony`) |
| Local path (dev) | often junctioned to `RimWorld/Mods/RimTaxi` |

## Architecture (current)

```
Call (comms gizmo/right-click)
  → world destination (distance known)
  → pay CALL FEE only (default 200 silver)
  → landing cell on home map
  → TransportShip Ship_RimTaxi + CompRimTaxiTrip booking
  → Wait (default 5h) with gizmo "Depart now"
  → on depart: charge mass×distance×rate, then FlyAway
  → TravellingTransporters with TravelingRimTaxi
  → FORCED world caravan drop (Harmony on Arrived + custom arrival action)
```

### Important types

| Type | Role |
|------|------|
| `TaxiCallService` | Call UX, spawn ship, destination targeting |
| `CompRimTaxiTrip` | **Authoritative** booked dest/distance on shuttle Thing |
| `TaxiTripLookup` | Comp + GameComponent lookup |
| `TaxiTripBilling` / `TaxiFareCalculator` | Call fee vs trip fare |
| `TransportersArrivalAction_RimTaxiWorldDrop` | World caravan drop |
| `TravellingTransporters_Arrival_Patch` | Force WorldDrop; skip vanilla map-drop fallback |
| `ShipJob_Wait_Gizmos_Patch` | Depart now / dismiss (no second world map) |
| `ShipJob_FlyAway_Billing_Patch` | Auto-depart billing after wait |
| `TravellingTransporters_Speed_Patch` | Slower travel for `TravelingRimTaxi` |

### Fare model (do not regress)

- **Call:** `baseFare` silver (dispatch).
- **Depart:** `ceil(massKg × distanceTiles × farePerKgPerTile)`.
- Empty auto-leave after wait: trip fare **0**.

### Arrival model (do not regress)

- **Never** rely on vanilla `StillValid` fallback (can become `LandInSpecificCell` → combat / pawn loss).
- Always world-map caravan; prefer tile **beside** settlements.

## Out of scope (MVP+)

- Hospitality
- Taxi company / NPC contracts / reputation
- Odyssey as hard dependency
- Fancy boarding UI (vanilla transporter load is OK)

## Build

```powershell
cd Source\RimTaxi
dotnet build -c Release
# Output: ../../Assemblies/RimTaxi.dll
```

`RimWorldManaged` and `HarmonyDll` paths are in `RimTaxi.csproj` (override with `-p:`).

## Testing checklist (in-game)

1. Harmony + RimTaxi enabled; log shows `[RimTaxi] Loaded`.
2. Call taxi → pay call fee only → land.
3. Load/unload passengers → booking still present (inspect string).
4. Depart now → trip fare message → no second world-map picker.
5. Wait expire empty → leaves free.
6. Arrive near faction base → **caravan on world map**, no forced attack map.
7. Save/load during wait and during flight.

## Docs map

| File | Contents |
|------|----------|
| `README.md` | Player/dev overview |
| `AGENTS.md` | This file |
| `CONTRIBUTING.md` | Human + AI contribution workflow |
| `docs/ARCHITECTURE.md` | Systems detail |
| `docs/KNOWN_ISSUES.md` | Known bugs / UX traps |
| `docs/ROADMAP.md` | Phases and next work |

## Style

- C# similar to existing RimTaxi files (explicit braces, Verse/RimWorld patterns).
- XML Defs: keep Royalty-independent skyfallers/ship defs.
- Korean + English Keyed strings when adding player-facing text.

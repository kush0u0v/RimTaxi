# Handoff summary (current baseline)

**Date context:** 2026-08 — playable baseline frozen; full GUI deferred.  
**GitHub:** https://github.com/kush0u0v/RimTaxi  
**packageId:** `kush.rimtaxi`

---

## What the mod is

Civilian **call taxi** for RimWorld 1.6:

- Dispatch from **comms console** (gizmo / right-click).
- Taxi arrives after **1–3 hours** (not instant).
- Board at **pickup** (this map, other player settlements, or open field maps with colonists — **no camp required**).
- Choose **destination after landing**, then pay **mass × distance** and fly.
- Trip end: land on **player** destination maps when possible; **foreign settlements always world caravan** (no combat map entry).

Identity: **service / meter**, not player spaceship (not SRTS).

---

## Canonical 5 steps

| # | Step | Player action | Silver |
|---|------|---------------|--------|
| 1 | Call | Comms → call → pick **pickup site** → pick landing cell | **200** from **comms map** |
| 2 | Dispatch | Wait for ETA letter | — |
| 3 | Arrive | Taxi lands at pickup | — |
| 4 | Set destination | Taxi gizmo “목적지 설정” → world map | Preview only |
| 5 | Depart | Taxi gizmo “출발” | **mass×dist×rate** from **taxi map** |

No world map during step 1–3.

---

## Implementation map (where to edit)

| Concern | Files |
|---------|--------|
| Call / pickup / dispatch | `TaxiCallService.cs`, `TaxiPickupSite.cs`, `TaxiPendingDispatch.cs`, `TaxiGameComponent.cs` |
| Destination + depart | `TaxiCallService.BeginSetDestination` / `Depart`, `ShipJob_Wait_Gizmos_Patch.cs` |
| Fare | `TaxiFareCalculator.cs`, `TaxiTripBilling.cs`, `TaxiPayment.cs`, `TaxiSettings.cs` |
| Booking persistence | `CompRimTaxiTrip.cs`, `TaxiTripLookup.cs` |
| Flight arrival | `TaxiArrivalUtility.cs`, `TransportersArrivalAction_RimTaxiMapLand.cs`, `…WorldDrop.cs`, `TravellingTransporters_Arrival_Patch.cs` |
| Auto after boarding wait | `ShipJob_FlyAway_Billing_Patch.cs` |
| Speed | `TravellingTransporters_Speed_Patch.cs` |
| Comms UI | `Building_CommsConsole_Patch.cs` |
| Defs | `Defs/**`, especially `RimTaxi_Shuttle.xml` (`shipDef` must be `Ship_RimTaxi`) |
| Strings | `Languages/English|Korean/Keyed/Keys.xml` |

---

## Defaults (settings)

| Key | Default | Meaning |
|-----|---------|---------|
| baseFare | 200 | Call fee |
| farePerKgPerTile | 0.1 | Trip fare rate |
| dispatchBaseTicks | 2500 | 1h |
| dispatchVarianceTicks | 5000 | +0–2h → **1–3h total ETA** |
| dispatchTicksPerTripTile | 0 | Don’t stretch ETA by trip length at call |
| waitTicks | 12500 | 5h boarding window |
| cooldownTicks | 2500 | 1h between calls |
| landOnSettlementMaps | true | Map land vs caravan at trip end |

Players with old settings may need **Reset to defaults** in mod options.

---

## Deliberately deferred

- Full custom GUI panel (make the same 5 steps clearer later)
- World caravan pickup without any open map
- Hospitality
- Taxi company / multi-company economy

---

## Recent fixes worth knowing

- **Trip booking** stored on shuttle `CompRimTaxiTrip` so load/unload does not wipe destination.
- **Map land NRE:** do not use vanilla `DropShuttle` without `shipDef`; use `DropRimTaxi` + XML `shipDef`.
- **Call** does not open world map; destination only at step 4.

---

## For the next person / AI

1. Read `AGENTS.md`.
2. Prefer bugfixes over new systems.
3. When user asks for GUI: wrap existing steps; don’t invent a parallel call pipeline.
4. Build + in-game smoke (call → wait ETA → land → set dest → depart).

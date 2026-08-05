# Handoff summary (current baseline)

**Date context:** 2026-08 — playable baseline frozen; full GUI deferred.  
**GitHub:** https://github.com/kush0u0v/RimTaxi  
**packageId:** `kush.rimtaxi`

---

## What the mod is

Civilian **call taxi** for RimWorld 1.6:

- Dispatch from **comms console** (radio contact) or **player caravan** top bar (**택시 보내기**: pay call fee → pick world dest → ETA → Depart).
- Taxi arrives after **1–3 hours** (not instant).
- Board at **pickup** (maps, field maps, **or caravan on world map** — no camp required). Map landing: **Q/E rotate** + cell pick; waiting taxi can **reposition**.
- Caravan with taxi waiting: **world icon = taxi** (not yellow circle) until disembark/depart.
- Choose **destination after taxi arrives**, then pay **mass × distance** and fly.
- Trip end: land on **player** maps when possible (unload + **layover wait** — reboard, set new dest, depart); **foreign settlements** world caravan **on tile** + layover boarding (no combat map entry).

Identity: **service / meter**, not player spaceship (not SRTS).

---

## Canonical 5 steps

| # | Step | Player action | Silver |
|---|------|---------------|--------|
| 1 | Call | Comms (pickup site + cell) **or caravan gizmo** | **200** from map stockpile **or caravan silver** |
| 2 | Dispatch | Wait for ETA letter | — |
| 3 | Arrive | Map land **or** caravan “taxi ready” (no map) | — |
| 4 | Set destination | Taxi / caravan gizmo “목적지 설정” | Preview only |
| 5 | Depart | Taxi / caravan gizmo “출발” | **mass×dist×rate** from map or caravan silver |

Comms call: no world map during step 1–3. Caravan call: stays on world map throughout.

---

## Implementation map (where to edit)

| Concern | Files |
|---------|--------|
| Call / pickup / dispatch | `TaxiCallService.cs`, `TaxiPickupSite.cs` (maps + caravans + world pick), `TaxiPendingDispatch.cs`, `TaxiGameComponent.cs` |
| Caravan board | `TaxiCaravanUtility.cs`, `TaxiCaravanBoarding.cs`, `Caravan_Gizmos_Patch.cs` |
| Destination + depart | `TaxiCallService.BeginSetDestination` / `Depart` / caravan variants, `ShipJob_Wait_Gizmos_Patch.cs` |
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
| baseFare | **200 (default)** | Call fee — prepaid; **no refund** on no-board/disembark (default rule) |
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

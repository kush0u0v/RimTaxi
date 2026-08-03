# Architecture

## Layers

```
UI entry
  Building_CommsConsole gizmo + float menu (Harmony)
    → TaxiCallService

Billing
  Call: TaxiFareCalculator.CallFee → TaxiPayment
  Depart: TaxiTripBilling (mass × distance × rate)

Booking
  CompRimTaxiTrip on RimTaxiShuttle (primary)
  TaxiGameComponent trips dict (backup / save)

Ship lifecycle
  TransportShipMaker + Ship_RimTaxi
  WaitTime (boarding window)
  FlyAway → TravellingTransporters (TravelingRimTaxi)

Arrival
  Harmony Prefix TravellingTransporters.Arrived for TravelingRimTaxi
  TransportersArrivalAction_RimTaxiWorldDrop
```

## Defs

| Def | Purpose |
|-----|---------|
| `Ship_RimTaxi` | TransportShipDef |
| `TravelingRimTaxi` | WorldObject (parent ShuttleWorldObjectBase) |
| `RimTaxiShuttle` | Building / ship thing + comps |
| `RimTaxiIncoming` / `RimTaxiLeaving` | Skyfallers + custom shadow |

## Harmony patches

| Patch | Target | Intent |
|-------|--------|--------|
| `Building_CommsConsole_Patch` | Thing.GetGizmos / GetFloatMenuOptions | Call taxi |
| `ShipJob_Wait_Gizmos_Patch` | ShipJob_Wait.GetJobGizmos | Depart now / dismiss |
| `ShipJob_FlyAway_Billing_Patch` | ShipJob_FlyAway.TryStart | Auto trip fare |
| `TravellingTransporters_Arrival_Patch` | TravellingTransporters.Arrived | Force world drop |
| `TravellingTransporters_Speed_Patch` | get_TraveledPctStepPerTick | Slower flight |

## Settings (`TaxiSettings`)

| Field | Default | Meaning |
|-------|---------|---------|
| baseFare | 200 | Call fee |
| farePerKgPerTile | 0.1 | Trip rate |
| waitTicks | 12500 | 5 in-game hours |
| cooldownTicks | 2500 | 1 in-game hour |
| maxLaunchDistance | 70 | World tiles |
| travelSpeedFactor | 0.6 | Flight speed mult |

## Build paths

`Source/RimTaxi/RimTaxi.csproj`:

- `RimWorldDir` → Managed assemblies
- `HarmonyDll` → `0Harmony.dll` (Workshop Harmony `Current/Assemblies`)

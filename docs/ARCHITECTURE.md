# Architecture

## High-level flow

```
Comms (ICommunicable "RimTaxi Dispatch" + radio DiaNode dialog; optional gizmo)
  └─ GetCommTargets → TaxiCommsContact → Dialog_Negotiation
       └─ Request taxi → ShowPickupSiteMenu
            ├─ TaxiPickupSite.GetAll / world pick / caravans
            ├─ landing cell (maps) or caravan dispatch
            ├─ pay CallFee from call map
            └─ TaxiGameComponent.QueueDispatch (ETA 1–3h)
                 └─ GameComponentTick → SpawnTaxi / caravan boarding

Player caravan (world top bar)
  └─ Call taxi → pay CallFee from caravan silver → QueueCaravanDispatch
       └─ tick → TaxiCaravanBoarding ready
            ├─ Set destination (world targeter)
            └─ Depart → pay trip → TaxiCaravanUtility.LaunchCaravanAsTaxi → TravelingRimTaxi

Spawned TransportShip (Ship_RimTaxi) — map path
  ├─ WaitTime (boarding, gizmos)
  │    ├─ Set destination → CompRimTaxiTrip.Book
  │    └─ Depart → charge trip → FlyAway
  └─ queued FlyAway (auto after wait; billing patch)

TravellingTransporters (TravelingRimTaxi)
  └─ Arrival patch → TaxiArrivalUtility.CreateArrivalAction
       ├─ MapLand (player settlement / player map only) → DropRimTaxi
       └─ WorldDrop → caravan (always for foreign settlements; never enter their map)
```

## Layers

| Layer | Responsibility |
|-------|----------------|
| UI entry | Comms Harmony; wait-job gizmos; letters/messages |
| Dispatch | `TaxiPendingDispatch` list on `TaxiGameComponent` |
| Billing | Call fee vs mass×distance at depart |
| Booking | `CompRimTaxiTrip` primary; GC dict backup |
| Ship | Vanilla TransportShip + ShipJob Wait/FlyAway |
| Arrival | Custom actions + Harmony force for our WorldObject def |

## Harmony patches

| Patch | Target | Intent |
|-------|--------|--------|
| `Building_CommsConsole_Patch` | `GetCommTargets`, `Thing.GetGizmos` | Taxi radio contact + gizmo |
| `TaxiCommsContact` / `TaxiDialogMaker` | `ICommunicable` + DiaNode | Faction-style radio UI |
| `Caravan_Gizmos_Patch` | `Caravan.GetGizmos` | Caravan Call / Set dest / Depart |
| `ShipJob_Wait_Gizmos_Patch` | `ShipJob_Wait.GetJobGizmos` | Set dest / Depart / Dismiss |
| `ShipJob_FlyAway_Billing_Patch` | `ShipJob_FlyAway.TryStart` | Auto fare / re-wait / empty leave |
| `TravellingTransporters_Arrival_Patch` | `TravellingTransporters.Arrived` | Force our arrival chooser |
| `TravellingTransporters_Speed_Patch` | `get_TraveledPctStepPerTick` | Slower RimTaxi flights |

## Defs

| Def | Purpose |
|-----|---------|
| `Ship_RimTaxi` | TransportShipDef |
| `TravelingRimTaxi` | In-flight world object |
| `RimTaxiShuttle` | Vehicle; **CompProperties_Shuttle.shipDef = Ship_RimTaxi** (required) |
| `RimTaxiIncoming` / `RimTaxiLeaving` | Skyfallers + `RimTaxiShadow` |

## Settings defaults

| Field | Default | Notes |
|-------|---------|--------|
| baseFare | 200 | Call |
| farePerKgPerTile | 0.1 | Trip |
| dispatchBaseTicks | 2500 | 1h |
| dispatchVarianceTicks | 5000 | 0–2h → **1–3h ETA** |
| dispatchTicksPerTripTile | 0 | Off by default |
| waitTicks | 12500 | 5h board window |
| cooldownTicks | 2500 | 1h |
| maxLaunchDistance | 70 | |
| travelSpeedFactor | 0.6 | |
| landOnSettlementMaps | true | |

## Build paths

`Source/RimTaxi/RimTaxi.csproj`:

- `RimWorldDir` → `...\RimWorldWin64_Data\Managed`
- `HarmonyDll` → Workshop Harmony `Current\Assemblies\0Harmony.dll`

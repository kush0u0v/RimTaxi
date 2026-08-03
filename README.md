# RimTaxi

Civilian **on-demand taxi** mod for **RimWorld 1.6**.

Call a taxi from a **comms console**, wait for dispatch, board at a **pickup** (including other settlements or open field maps), set a destination, pay **mass × distance**, and fly. Not a player-owned spaceship mod.

| | |
|--|--|
| **GitHub** | https://github.com/kush0u0v/RimTaxi |
| **packageId** | `kush.rimtaxi` |
| **Requires** | [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) |
| **DLC** | None required |

## Play loop (baseline)

1. **Call** — pay **200** silver from **comms map** or **caravan inventory**. Comms: pick pickup (this map / other colony / field map / caravan / world map click) then landing cell if needed. Caravan: call from the world-map caravan bar.  
2. **Dispatch** — taxi is en route (**about 1–3 hours**).  
3. **Arrive** — lands on map **or** ready at caravan (world only).  
4. **Set destination** — taxi / caravan gizmo; shows estimated fare.  
5. **Depart** — pay **kg × tiles × rate**, then fly (whole caravan boards if called from caravan). Lands on **your** maps when possible (then **waits** so you can trade/reboard); **foreign settlements** world caravan on tile + layover wait (no combat entry).

## Features

- Comms **radio contact** (like talking to a faction) + optional gizmo
- **Caravan top-bar** call / set destination / depart (no map needed)
- Remote pickup without founding a camp on field maps
- Dual fare: call fee + trip fare at depart
- Destination booking on the vehicle (`CompRimTaxiTrip`)
- Configurable settings (fees, ETA, wait, speed, map land toggle)
- Custom taxi texture / shadow

## Install

1. Put this folder in `RimWorld/Mods/RimTaxi` (or symlink).  
2. Enable **Harmony**, then **RimTaxi**.  
3. Build so `Assemblies/RimTaxi.dll` exists.

## Build

```powershell
.\build.ps1
# or
cd Source\RimTaxi
dotnet build -c Release
```

## Docs (humans & AI)

| Doc | Purpose |
|-----|---------|
| [AGENTS.md](AGENTS.md) | **AI agents start here** |
| [docs/HANDOFF.md](docs/HANDOFF.md) | Current baseline handoff |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Systems + patches |
| [docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md) | Traps / limits |
| [docs/ROADMAP.md](docs/ROADMAP.md) | Freeze + next work |
| [docs/BUG_CHECKLIST.md](docs/BUG_CHECKLIST.md) | In-game bug test checklist |
| [CONTRIBUTING.md](CONTRIBUTING.md) | PR / agent report format |

## License

MIT for this mod’s original code/assets (see `LICENSE`).  
RimWorld © Ludeon Studios — unofficial fan mod.

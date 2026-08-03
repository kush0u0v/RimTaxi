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

1. **Call** — pay **200** silver (from the comms map). Choose **pickup** (this map / other colony / field map with colonists). Choose landing cell. **No world map yet.**  
2. **Dispatch** — taxi is en route (**about 1–3 hours**).  
3. **Arrive** — taxi lands at the pickup.  
4. **Set destination** — taxi gizmo; world map; shows estimated fare.  
5. **Depart** — pay **kg × tiles × rate**, then fly. Lands on **your** maps when possible; **foreign settlements** always drop as a world caravan (no combat entry).

## Features

- Comms **gizmo** + colonist **right-click** (no faction dialog)
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
| [CONTRIBUTING.md](CONTRIBUTING.md) | PR / agent report format |

## License

MIT for this mod’s original code/assets (see `LICENSE`).  
RimWorld © Ludeon Studios — unofficial fan mod.

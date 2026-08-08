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

1. **Call** — pay **call fee (default 400 silver)** from **comms only**. Pick pickup (map landing cell Q/E, or player caravan). Caravans do not call taxis themselves.  
2. **Dispatch** — en route (**about 1–3 hours**). Caravan cannot move while waiting for taxi.  
3. **Arrive** — map land **or** caravan taxi ready (world **taxi icon**, not yellow circle).  
4. **Set destination** — gizmo; fare preview.  
5. **Depart** — **trip fare** (mass × tiles × rate), then fly. Player maps: **pick landing (Q/E)** then wait/reboard. Foreign settlements: caravan on tile + layover (no combat entry). **하차** dismisses waiting taxi without trip fare (call fee kept).

## Features

- Comms **radio contact** (like factions) + gizmo
- **Caravan:** send / immobilize / taxi world icon / depart / disembark
- Map landing **Q/E** at **call** and when **map is visible on arrival**
- Remote pickup (other colonies, field maps, caravans)
- Dual fare: **prepaid call fee** + trip fare at depart
- Settlement trade-beacon / stockpile / carried silver rules
- Custom taxi texture, shadow, world markers

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

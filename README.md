# RimTaxi

Civilian **taxi shuttle** mod for **RimWorld 1.6**.

Call a taxi from the **comms console**, pay a dispatch fee, board colonists and limited cargo, then fly to a world destination. Arrival drops a **caravan on the world map** (does not force-enter settlement combat maps).

| | |
|--|--|
| **packageId** | `kush.rimtaxi` |
| **Requires** | [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077) |
| **DLC** | None required (Odyssey/Royalty not required) |

## Features (current)

- Comms console **gizmo** + colonist **right-click** call (no faction dialog)
- **Call fee** (default 200 silver)
- **Trip fare at depart** = cargo mass (kg) × distance (tiles) × rate (default 0.1)
- Boarding wait (default **5 hours**), or **Depart now** when loaded
- Empty taxi leaves after wait (no trip fare)
- Destination booked on the vehicle (`CompRimTaxiTrip`) so load/unload does not wipe it
- Custom yellow taxi graphics + silhouette shadow
- Configurable speed / cooldown / fares in mod settings

## Install

1. Place this folder in `RimWorld/Mods/RimTaxi` (or junction/symlink).
2. Enable **Harmony**, then **RimTaxi**.
3. Ensure `Assemblies/RimTaxi.dll` exists (build below).

## Build

Requirements: .NET SDK with `net472`, RimWorld install, Harmony `0Harmony.dll`.

```powershell
.\build.ps1
# or
cd Source\RimTaxi
dotnet build -c Release
```

Path overrides:

```powershell
dotnet build -c Release `
  -p:RimWorldDir="C:\Program Files (x86)\Steam\steamapps\common\RimWorld" `
  -p:HarmonyDll="C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\2009463077\Current\Assemblies\0Harmony.dll"
```

## Play loop

1. Comms console → **Call taxi**  
2. Pick **world destination** (call fee charged)  
3. Pick **landing cell** on home map  
4. Load passengers/cargo  
5. **Depart now** (pay trip fare) **or** wait until auto-depart  
6. Arrive as **world caravan** near the destination  

## Docs for contributors & AI

| Document | Purpose |
|----------|---------|
| [AGENTS.md](AGENTS.md) | **Start here if you are an AI agent** |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Contribution workflow |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Systems map |
| [docs/KNOWN_ISSUES.md](docs/KNOWN_ISSUES.md) | Bugs / UX traps |
| [docs/ROADMAP.md](docs/ROADMAP.md) | Next work / non-goals |

## License

Mod code and assets in this repository: see repository license (if none is set, treat as all-rights-reserved by the author until specified).  
RimWorld is © Ludeon Studios — this mod is unofficial fan content.

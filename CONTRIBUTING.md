# Contributing

## For humans

1. Branch from `main`.
2. Keep the **5-step loop** unless the issue is a redesign request.
3. Build: `dotnet build -c Release` in `Source/RimTaxi`.
4. Smoke-test in RimWorld 1.6 + Harmony.
5. PR: summary, test steps, risks.

## For AI agents

1. Read **`AGENTS.md`** and **`docs/HANDOFF.md`** first.
2. Skim `docs/ARCHITECTURE.md` + `docs/KNOWN_ISSUES.md`.
3. Prefer smallest fix; no full GUI unless asked.
4. No Hospitality; no SRTS branding; no Odyssey hard dep.
5. End report: files changed, build result, in-game steps, remaining limits.

## Commit messages

Present tense, e.g.:

- `Fix map land DropShuttle NRE with DropRimTaxi`
- `Add field-map pickup without requiring camp`
- `Set dispatch ETA default to 1–3 hours`

## Review focus

- Call still has **no world map**?
- Call fee vs trip fee still split (comms map vs taxi map)?
- Destination still **after** land?
- Arrival still uses our actions (map land / caravan), not vanilla StillValid wipe?
- `CompProperties_Shuttle.shipDef` still `Ship_RimTaxi`?

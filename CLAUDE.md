# Afterburn — agent guide
Mobile 3D space battle-racer · Veratus Games · Unity 6000.4.4f1 · URP · landscape.
Owner: Seni. Gated build discipline: never start the next phase without his sign-off.

## Read first, in this order
1. `docs/Afterburn_STATE.md` — what exists NOW (refresh it before you finish)
2. `docs/Afterburn_TRACKING.md` — which tracking file gets which loose thread
3. `docs/Afterburn_BUILD.md` — the build contract; §2 is the inviolable law
4. `docs/Afterburn_DesignReview.md` — locked decisions D1–D15.x + the audited economy
For visual work: `docs/Afterburn_UIEnvSpec.md` + `docs/Afterburn_AssetBacklog.md` (WP-01…12).
For prototype parity questions: `docs/Afterburn_PortSpec.md`.

## The laws (violating these is never a judgment call)
- **§2 energy rule**: EnergyCore is the sole spend authority; boost/fire/shield mutually
  exclusive; regen only on coast. Nothing may grant energy except EnergyCore's clamped paths.
  Gates/pickups give SPEED or STATUS only; hazards only DRAIN.
- **Frozen tuning** (BUILD §5–6) is never re-tuned in code — GameTuning assets only.
- **Determinism**: Core runs fixed 60 Hz ticks; no Time/Random in Core sim paths
  (ghost RNG is seeded); recorded ghosts must replay bitwise.
- **Palette roles**: gold = leader only, red = damage only — everywhere, including cosmetics.
- **Monetisation firewall** (DesignReview §6.6): no energy income, no stat power, no ads.
- New Core tunables MUST be zeroed in `ParityTraceTests` (divergence-by-ruling — R-18).

## Tracking protocol (docs/Afterburn_TRACKING.md is the index)
Bug found → `Afterburn_BUGS.md` now. Anything deferred → `Afterburn_REVISIT.md` now.
Before ANY sweep/audit → check `Afterburn_SWEEPLOG.md`; append after.
Session end → CHANGELOG entry + STATE refresh + commit/push. Backup zips at milestones to
`/Volumes/Veratus Group/manual Backups/Afterburn/` ("Afterburn - <Milestone> - YYYYMMDD-HHMM.zip",
exclude UnityProject/{Library,Temp,Logs,UserSettings,obj} AND the vendor packs).

## Verification (headless — fails if the editor has the project open)
```
UNITY="/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -quit -projectPath UnityProject -executeMethod Afterburn.EditorTools.SceneBuilder.BootstrapU1 -logFile /tmp/ab.log
"$UNITY" -batchmode -projectPath UnityProject -runTests -testPlatform EditMode -testResults /tmp/ab.xml -logFile /tmp/abt.log
```
Editor menus live under `Veratus/Afterburn/`. The suite must be green before committing
Core changes; if the editor lock blocks you, say so in STATE and queue it.

## Repo policy
Vendor asset packs are gitignored (USC has a >100 MB file; GitHub hard limit) — fresh clones
reimport from Asset Store (GUIDs stable). Commit messages end with the Claude co-author line.
Push to `origin main` after every committed session.

## Architecture in one breath
`Afterburn.Core` (pure sim: EnergyCore/ShipController/TrackSystem/Combat/Abilities/Contacts/
Gates/Ghosts/RaceDirector/GhostRecording — no View refs) ← `Afterburn.View` (RaceRunner
accumulator drives Core; greybox + vendor-prefab visuals; reads state, NEVER writes) ←
`Afterburn.UI` (tokens + UIFactory runtime screens; HUD reads RaceDirector). The track is
PROCEDURAL (spline = collision = laps = ghosts) — never replace the road with mesh kits.

# Afterburn — Current State
## Veratus Games · Project ALTERNATE · A7 · snapshot 2026-07-10 (Session 12)

> The living snapshot — refresh at the end of every working session. What exists, what's
> wired, what's verified. History lives in the CHANGELOG; this file is only NOW.

## One-liner
Playable 3D space battle-racer: full loop (Boot → loadout → lineup → countdown → race vs 3
ghosts → podium → summary), real PBR ships (USC), warp-gate set pieces, D15.1 gate lane
(collectable/obstacle bridges), tangible ships (D14), complete HUD + touch controls.
U1 ✅ · U2 ✅ (feel signed off) · U3/U4/U5 built+verified · U6/U7 not started.

## Core systems (Assets/Afterburn/Code/Core) — all fixed-tick 60 Hz, deterministic
| System | Status | Notes |
|---|---|---|
| EnergyCore | ✅ | Single spend authority; §2 arbiter lives in ShipController |
| ShipController | ✅ | Track-frame (D6), prototype-parity step order; D14 contact, D15 surge/stall/overdrive/turn-debuff/barrier states |
| TrackSystem | ✅ | Frames, windowed nearest, shortcut allowances, per-race wall state, Spline exposure |
| CombatSystem | ✅ | 40-bullet pool, shield ×0.4, bounty 8/16, siphon capped (#7), barrier absorption |
| PilotAbilitySystem | ✅ | EMP/Phase/Siphon/Decoy, cooldown-only |
| ShipContactSystem | ✅ D14 | Mass-ratio mutual damage, pushout, pair cooldown |
| GateFeatureSystem | ✅ D15.1 | 11 gate types; window+re-arm; deflection by barrier/held shield; player-only (ghosts exempt — R-03) |
| GhostRacer | ✅ | Rail-locked synthetic, rulings #4/#5, seeded RNG |
| RaceDirector | ✅ | Grid→Lineup→Countdown→Racing→Finished; bounty every-tick; lap ±0.5 anti-cheese; stats; lap times |
| GhostRecording | ✅ | Fixed-tick input stream + cosmetic header; bitwise replay determinism PROVEN; not yet in roster (R-27) |

## UI (Assets/Afterburn/Code/UI) — runtime-built, token-driven
HUD (hero energy bar ∝ maxEnergy, frame-true; chips; timer; minimap; toasts; countdown; intel
trailer-only; bounty tag) · TouchControls (steer zone + exclusion-proven wheel + ability btn) ·
RaceFlow (lineup sweep → podium orbit → summary w/ Rematch) · Boot/MainMenu-loadout/Settings ·
font fallback chain (proper TMP import pending — R-13).

## Content & assets
- **Arena01**: star-convex loop (parity fixture) + 5-gate v1 lane on the asset (**reseed to
  v2 10-gate lane pending** — menu exists, Seni hasn't run it yet as of this snapshot)
- **Ships**: USC families — Light=CosmicShark, Medium=StriderOx, Heavy=VoidWhale (+30 variants
  each, swappable); `shipVisualScale` presence knobs
- **Vendor packs** (gitignored, reimport from Asset Store): UltimateSpaceshipsCreator 931 MB ·
  ModularSci-FiWeapons 320 MB (unused yet — D10/mine supply) · WarpGates 294 MB · StarSparrow 91 MB (R-15)
- **Generated pack** `assets/afterburn_assets` (committed): 43 icons, 33 WAVs, 45 env manifests — unwired (R-19)

## Verification
- **Last full green suite: 69/69** (Session 9, pre-D15). Since then +7 tests authored
  (4 gate v1 + 3 gate-lane mechanics) → expected **76**; **NOT yet run headless** (editor lock)
  — BUG-015. Run on next editor close.
- Parity instrument: pre-contact arithmetic exact; energy ≤0.55; fire/boost/shield counts
  exact; wall containment 60 s. D14/D15 fields zeroed in trace (R-18).

## Repo / backup
- GitHub `zeusiam/Afterburn`, main, last commits: `f0771c5` (D15.1), `0848e90` (collider strip)
- Vendor packs gitignored (108 MB file > GitHub limit); GUIDs stable on reimport
- Backups: `/Volumes/Veratus Group/manual Backups/Afterburn/` — last: U5 Frontend (81 MB);
  next zips exclude vendor packs (R-17)

## How to verify (headless, editor closed)
```
UNITY="/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity"
PROJ=".../Afterburn/UnityProject"
"$UNITY" -batchmode -quit -projectPath "$PROJ" -executeMethod Afterburn.EditorTools.SceneBuilder.BootstrapU1 -logFile /tmp/ab.log
"$UNITY" -batchmode -projectPath "$PROJ" -runTests -testPlatform EditMode -testResults /tmp/ab.xml -logFile /tmp/abt.log
```
In-editor menus: `Veratus/Afterburn/` → Create or Update SOs · Build Scenes · Assign USC Hull
Visuals · Reseed Arena01 Gates (v2) · Assign StarSparrow Hull Visuals.

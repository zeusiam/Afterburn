# Afterburn — Milestone Tracker
## Veratus Games · Project ALTERNATE · letter **A**, slot 7

**Git repo:** `https://github.com/zeusiam/Afterburn`
**Manual backup location:** `/Volumes/Veratus Group/manual Backups/Afterburn`
**Backup procedure:** every session → `git commit` + copy working tree to the manual backup location above.

---

## Status legend

| Symbol | Meaning |
|---|---|
| ⬜ | Not started |
| 🔨 | In progress |
| ✅ | Complete + signed off |
| ⛔ | Blocked / awaiting decision |

---

## Stage 1 — Prototype (browser, Claude artifact)

| ID | Milestone | Gate | Status |
|---|---|---|---|
| P0 | Three.js scene, greybox track, chase-cam, ship movement | Lap driven at stable 60fps | ✅ |
| P1 | `EnergyCore` + boost | Drain/regen loop feels meaningful | ✅ |
| P2 | Fire + shield + mutual exclusion | **MAKE-OR-BREAK: the choice is fun** | ✅ **KILL GATE PASSED — Seni approved the concept 2026-07-09** |
| P3 | Three hull sidegrades | Each hull wins a different scenario | ✅ |
| P4 | Pilot abilities + cooldowns | Ability timing swings a race | ✅ |
| P5 | Ghost opponents, bounty, intel | Comebacks without rubber-banding | ✅ |
| P6 | Run summary, restart, tuning panel | "One more run" impulse present | ✅ |

**🚦 Kill gate: P2 — PASSED.** The loop is fun; the project earns its Unity build. Tuning frozen and the gated handoff written (`Afterburn_BUILD.md`). Proceed to Stage 3, U1 → U7, sign-off at every gate.

---

## Stage 2 — Design lock

| ID | Milestone | Status |
|---|---|---|
| D1 | Race mode confirmed as launch mode; battle mode deferred | ✅ concept approved 2026-07-09 |
| D2 | Async ghosts confirmed; real-time PvP deferred | ✅ concept approved 2026-07-09 |
| D3 | Hull count locked at 3 for prototype | ✅ concept approved 2026-07-09 |
| D4 | Tuning values frozen from prototype → `GameTuning` appendix | ✅ frozen in `Afterburn_BUILD.md` §5.3 |

---

## Stage 3 — Unity handoff

| ID | Milestone | Status |
|---|---|---|
| U0 | `Afterburn_BUILD.md` gated handoff written | ✅ 2026-07-09 |
| U1 | Phase 0 inventory + skeleton signed off | 🔨 **built + verified 2026-07-09 — AWAITING SENI SIGN-OFF** (compiles clean, 9 SOs seeded idempotently, 15/15 tests, ship+camera in Race.unity) |
| U2 | Core energy loop in Unity | ⬜ |
| U3 | Hulls + pilots as ScriptableObjects | ⬜ |
| U4 | Ghost system | ⬜ |
| U5 | Front-end, HUD, Safe Area on all roots | ⬜ |
| U6 | Monetisation (cosmetics + pilots, no stat-power in ranked) | ⬜ |
| U7 | Store prep, signed iOS + Android builds | ⬜ |

---

## Risk register

| Risk | Severity | Mitigation |
|---|---|---|
| Scope — biggest build in the portfolio (12–18 mo solo) | 🔴 High | Race mode only at launch; async ghosts; no netcode |
| Pay-to-win kills PvP | 🔴 High | Stats normalised in ranked; upgrades = cooldown only |
| Cannibalises Apex Drift | 🟠 Medium | Positioned as combat *racer*, not shooter |
| Energy loop not fun | 🔴 High | P2 kill gate before any art or Unity spend |
| 3D content cost (ships, arenas) | 🟠 Medium | Greybox until loop proven; one arena at launch |

---

## Session log

| Date | Session outcome | Next action |
|---|---|---|
| 2026-07-09 | Prototype architecture drafted; energy-core mechanic defined; scope reduced to race-mode-first + async ghosts | Get repo + backup path; build P0–P2 artifact |
| 2026-07-09 | **Playable prototype built (P0–P6) in `Afterburn_Prototype.html`** and verified functional (zero console errors, full race loop, boost/energy/combat confirmed). Fixed a start-line track cusp; softened wall collision; nerfed ghost AI | **Seni: play it and rule on the P2 kill gate.** Then either freeze `GameTuning` values + write the Unity handoff, or tune/kill |
| 2026-07-09 | **P2 KILL GATE PASSED — Seni approved the concept.** Design lock (D1–D4) closed; tuning frozen; **gated Unity handoff written (`Afterburn_BUILD.md`, U0 ✅).** | Confirm Unity version + orientation + repo/backup (BUILD §12), then hand `Afterburn_BUILD.md` to the Unity build agent and execute **U1** (skeleton + SO seeder), stopping at its gate |
| 2026-07-09 | **All §12 inputs resolved by Seni** (repo `zeusiam/Afterburn`, backup on Veratus Group volume, Unity **6000.4.4f1**, **landscape**, synthetic+recorded ghosts). **U1 started:** repo initialised, UnityProject scaffolded | Finish U1 skeleton, run gate verification (compile + 8 SOs seeded + ship spawns/camera follows), back up, **stop for Seni's U1 sign-off** |
| 2026-07-09 | **U1 built + verified.** Skeleton complete (asmdefs, SOs + seeder, pool, input, chase cam, greybox Arena01 + ship, 3 scenes); `Afterburn_PortSpec.md` written (16 divergences logged for U2 rulings); zero compile errors, 9 SOs seeded (idempotent re-run 0/9/0), 15/15 edit-mode tests incl. spline parity; adversarial review — 6 findings fixed. Committed + backed up | **Seni: open Race.unity, press Play, sign off U1.** Then U2: EnergyCore + ShipController + mutual exclusion (kill-gate re-validation vs prototype) |

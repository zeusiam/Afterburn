# Afterburn — Changelog
## Veratus Games · Project ALTERNATE · letter **A**, slot 7

**Git repo:** `https://github.com/zeusiam/Afterburn`
**Manual backup location:** `/Volumes/Veratus Group/manual Backups/Afterburn`

**Format:** newest first. Every entry = date · type · what changed · why.
**Types:** `DESIGN` · `PROTOTYPE` · `UNITY` · `TUNING` · `SCOPE` · `FIX` · `SWEEP`

---

## 2026-07-09 — Session 5 (design pass 2)

| Type | Change | Rationale |
|---|---|---|
| `DESIGN` | **Design review & assessment** against Seni's expanded vision (full 3D world, combat strategy, racing excitement, challenge, visual impressiveness) — 4-lens design panel (track/combat/monetisation/art) + synthesis → **`Afterburn_DesignReview.md`** | The validated energy loop stands; the gaps are the flat track, thin feel layer, no challenge ladder, and no owned art direction |
| `DESIGN` | **New locked decisions D5–D8:** D5 launch = 1 arena + 2 full-3D fast-follow · D6 **U2 physics is 3D-track-frame from day one** (flat = degenerate case, parity trace-diff mandatory) · D7 **no ads, ever** · D8 Game Center first (Nibwell GameKit pattern), Play Games at U7 | Seni's calls 2026-07-09; BUILD.md §1/§7.2/§7.6/§10 amended to match |
| `DESIGN` | **IAP catalog defined** (direct-priced cosmetics: liveries incl. energy-reactive tier, boost-visible trails, nameplates, founder pack, pilots; no hard currency/gacha/timers/run-gating) + **ghost records must carry cosmetic loadout** — the status billboard | Cosmetics need an audience in an async-ghost game; competitive integrity guardrails untouched |
| `DESIGN` | **Game Center spec:** per-arena race/lap boards (classic + weekly), career boards, ~20 mechanics-tied achievements | Studio GameKit precedent (Nibwell) makes this low-risk |
| `UNITY` | **`Afterburn_UIEnvSpec.md` written** — the requirements spec for the Claude design/build agent: emissive-first art direction, environment modular kit, 3 arena themes, VFX list, full UI system (landscape 2796×1290), URP mobile budgets, phased deliverables with acceptance criteria | Purpose-built for a code-only builder: procedural meshes, shaders, SDF, uGUI — no hand-painted assets required |
| `SCOPE` | Prototype bug rulings recommended (fix ghost free-lap #5, ghost skill² #4, broken-wall-for-all #3, Siphon minting #7; keep straightened cone #16; add countdown) — **await Seni confirmation at U2 gate** | PortSpec divergences need explicit rulings before the parity build |

**⏭ Next:** U1 gate sign-off still open (open `Race.unity`, press Play). Then U2 with the D6 3D-ready directive.

---

## 2026-07-09 — Session 4

| Type | Change | Rationale |
|---|---|---|
| `DESIGN` | **All BUILD §12 open inputs resolved by Seni:** repo `https://github.com/zeusiam/Afterburn` · backup `/Volumes/Veratus Group/manual Backups/Afterburn` · Unity **6000.4.4f1** · **LANDSCAPE** orientation · ghosts = synthetic day-one + recorded over time | Unblocks U1; nothing gating the Unity build |
| `UNITY` | Unity version chosen to match **all four sibling Veratus projects** (Rune Rouge, Eclipse, Nibwell, Triagles — all 6000.4.4f1, installed) | Studio-convention alignment; shared editor knowledge across the portfolio |
| `DESIGN` | **Orientation: LANDSCAPE** — deliberate deviation from the portrait studio convention; Canvas reference becomes 2796×1290 | Seni's call for the chase-cam racer: classic framing, wider view of the track. Core unaffected |
| `UNITY` | **U1 started:** git repo initialised, UnityProject scaffolded (6000.4.4f1 + URP), assembly split + folders per BUILD §4 | Milestone-gated build begins; stop at U1 gate for sign-off |
| `UNITY` | **U1 built:** 4 asmdefs (`Afterburn.Core/View/UI/Editor` + EditMode tests, nullable on), SO data model (`HullDefinition`/`PilotDefinition`/`GameTuning`+`ShipFeel`/`TrackDefinition`), idempotent seeder `Veratus/Afterburn/Create or Update SOs`, studio `ObjectPool`+`SafeArea` copied per convention, PrimeTween 1.4.8 + TMP Essentials, `Afterburn.inputactions` (prototype keys 1:1), `CatmullRomSpline` (three.js r128-parity), greybox `TrackView`/`ShipGreybox`/`StarfieldView`, `ChaseCamera`, Boot/MainMenu/Race scenes | Everything in BUILD §9's U1 rows, module boundaries per §3 |
| `PROTOTYPE` | **Wrote `Afterburn_PortSpec.md`** — line-by-line extraction of the prototype (track gen, camera, physics order, ghost AI, combat, dressing) + **16 documented divergences** between prototype code and BUILD doc (ghost free-lap bug, broken-wall persistence, Siphon minting energy, cone Euler skew…) | Prototype is the spec of feel; ambiguities now have receipts. Divergence rulings due at the U2 gate |
| `SWEEP` | **U1 verification:** batchmode compile zero errors ×2; seeder produced all 9 data assets with frozen values, second run `0 created / 9 kept` (idempotency proven); **15/15 edit-mode tests pass** incl. spline parity (lap ≈ 1823 u, min corner radius ≈ 58 u); adversarial review workflow (3 reviewers + refuters, 10 agents) found 6 real issues — all fixed (gate torus pose, slab tilt, starfield omission, test dead code, shader-stripping note, cone-skew ruling logged) | Evidence for the U1 acceptance gate before asking Seni to sign off |

**⏭ Next:** **U1 GATE — awaiting Seni's sign-off.** Open `UnityProject` in Unity 6000.4.4f1, open `Race.unity`, press Play: ship spawns at the start line, chase camera follows. On sign-off → U2 (EnergyCore + ShipController + mutual exclusion, the kill-gate re-validation).

---

## 2026-07-09 — Session 3

| Type | Change | Rationale |
|---|---|---|
| `DESIGN` | **P2 kill gate PASSED** — Seni approved the concept after playing the prototype | The shared-energy trade-off is fun; the project earns its Unity build |
| `SCOPE` | Design lock closed: D1 race-mode-first, D2 async ghosts, D3 three hulls, D4 tuning frozen — all ✅ | Stage 2 complete; no open design decisions blocking the build |
| `PROTOTYPE` | **Wrote the gated Unity handoff `Afterburn_BUILD.md` (milestone U0 ✅)** | Single source of truth for a Claude Code Unity build agent to build out the assets, phase-gated U1→U7 |
| `TUNING` | Froze all 12 prototype tuning values + ship-feel reference constants into `GameTuning` spec (BUILD §5.3, §6) | The loop was signed off *at these numbers*; Unity must reproduce the ratios exactly |
| `UNITY` | Handoff specifies assembly split, SO data model (`HullDefinition`/`PilotDefinition`/`GameTuning`), per-system port, asset inventory, and portrait/Safe-Area/uGUI+TMP/PrimeTween conventions | Executable, studio-convention-aligned build plan |
| `UNITY` | Flagged **orientation (portrait per convention vs landscape)** and Unity version as open inputs; Core is orientation-agnostic | Racer-in-portrait is unusual — surfaced for Seni rather than silently assumed |

**⏭ Next:** Seni answers BUILD §12 open inputs (Unity version, orientation, repo, backup), then the Unity build agent runs **U1** and stops at its gate.

---

## 2026-07-09 — Session 2

| Type | Change | Rationale |
|---|---|---|
| `PROTOTYPE` | **Built the playable browser prototype** (`Afterburn_Prototype.html`) — single self-contained file, Three.js r128 (CDN) + vanilla JS modules + DOM HUD | Fastest path to a file Seni can double-click and play; module boundaries map 1:1 to the planned Unity assembly split |
| `PROTOTYPE` | Implemented **all phases P0–P6** in one build: greybox track + chase-cam, EnergyCore, fire/shield/boost with mutual exclusion, 3 hulls, 4 pilot abilities, ghost opponents, bounty + intel, run summary + restart, 12-slider tuning panel | Deliver a complete loop to playtest, not just P0–P2 |
| `PROTOTYPE` | **Core P2 mechanic wired exactly to spec:** EnergyCore is the sole spend-gate; only one of boost/shield/fire active at once; energy regenerates *only* during coast (no action) | This mutual exclusion is the whole game — it must be honest to test the kill gate |
| `UNITY` | Deviation from architecture §3: used **vanilla JS + DOM HUD instead of a React artifact** | Standalone file needs to run at 60fps from disk and load Three from a CDN; the loop is pure-JS via refs either way, and HUD→uGUI in Unity regardless. Module split preserved for handoff |
| `FIX` | Redesigned the track centreline — original hand-placed control points **cusped at the start/finish seam** (path doubled back ~180°, 2.9-unit turn radius). Replaced with a generated star-convex loop (min radius now 57.9, drivable at boost speed) | Original track was un-completable at the start line |
| `TUNING` | Softened wall contact from a hard `×0.82`/frame scrape (compounded to a dead stop) to a `×0.92` scrape + slide-along-wall heading nudge | Wall contact should cost speed, not trap you |
| `TUNING` | Nerfed ghost AI skill band to 0.80–0.90× top speed | Ghosts never scrape walls or mis-corner; needed a handicap so a skilled human can win on energy play |
| `SWEEP` | Verified in-browser: Three loads, scene builds (60 meshes), render never throws, **zero console errors**; full 3-lap race → finish → summary loop confirmed; boost confirmed +40% top speed (62→86); all 4 abilities + combat + bounty reward fire correctly | Evidence the loop is functional before asking Seni to judge the kill gate |

**⏭ Next:** Seni playtests and rules on the **P2 kill gate** — is the boost/fire/shield choice fun? If yes, freeze tuning values and write the Unity handoff. If no, tune (⚙ panel) or kill.

---

## 2026-07-09 — Session 1

| Type | Change | Rationale |
|---|---|---|
| `DESIGN` | Concept defined: 3D space-race arena, ship + pilot dual progression, race mode + battle mode | Seni's original concept for slot A |
| `DESIGN` | **Core mechanic set: single shared Energy pool** driving boost / fire / shield with mutual exclusion | Creates a live trade-off every second — the source of strategic depth |
| `DESIGN` | Ship upgrades redefined as **sidegrades** (Light / Medium / Heavy), never straight power | Linear "+10% speed" creates power, not decisions; also prevents pay-to-win |
| `DESIGN` | Pilot upgrades restricted to **cooldown reduction only** | Protects competitive integrity while keeping monetisation alive |
| `DESIGN` | Added **Bounty** (leader marked, 2× reward) and **Intel catch-up** (trailers see leader's energy) | Comebacks without insulting rubber-banding |
| `DESIGN` | Added **spec-gated shortcuts** (light-only gaps, heavy-only breakable walls) | Loadout choice opens different racing lines |
| `SCOPE` | **Race mode ships first**; battle mode deferred to content update | Last-man-standing is the most crowded, costly, netcode-heavy mode |
| `SCOPE` | **Async ghost opponents** replace real-time PvP at launch | Removes netcode, matchmaking, server cost and cheating — highest-leverage risk cut |
| `SCOPE` | Prototype non-goals fixed: no IAP, no art, no save, no touch, one arena | Prototype exists only to test the energy loop |
| `PROTOTYPE` | Architecture drafted: Three.js (r128) + React single-file artifact; module split defined | Studio pattern; r128 constraints noted (no CapsuleGeometry, no OrbitControls) |
| `PROTOTYPE` | **P2 declared the kill gate** — if boost/fire/shield choice isn't fun with primitives, redesign or kill | Prevents spending art/Unity budget on an unproven loop |
| `TUNING` | 12 parameters exposed as live sliders; defaults set | Studio convention: parameterised, never hard-coded |

---

## Pending / awaiting Seni

- [x] Git repo URL — `https://github.com/zeusiam/Afterburn` (2026-07-09)
- [x] Manual backup location — `/Volumes/Veratus Group/manual Backups/Afterburn` (2026-07-09)
- [x] Confirm race-mode-first — D1 ✅
- [x] Confirm async ghosts — D2 ✅
- [x] Confirm 3 hulls for prototype — D3 ✅
- [x] Confirm keyboard controls for prototype — ✅ (prototype shipped keyboard-only)
- [ ] **U1 gate sign-off** (next action once U1 verification passes)

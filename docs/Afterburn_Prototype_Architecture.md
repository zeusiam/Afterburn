# Afterburn — Prototype Architecture
## Veratus Games · Project ALTERNATE · letter **A**, slot 7

> **Purpose of this document:** a build spec for a **browser prototype** (Claude artifact), not the Unity game. Its only job is to prove or kill the core mechanic before any Unity work begins. Per studio convention: prototype → validate → then write the Claude Code Unity handoff.

---

## 1. What this prototype must prove

| # | Question the prototype answers | Pass condition |
|---|---|---|
| 1 | Is the **energy trade-off** fun? | Player consciously chooses boost vs shoot vs shield, and regrets choices |
| 2 | Does **race mode** work with combat in it? | Shooting the leader feels worth losing speed |
| 3 | Do **hull sidegrades** create real decisions? | Three hulls each win in different situations |
| 4 | Does **pilot ability timing** raise the skill ceiling? | Good timing visibly changes outcomes |
| 5 | Is a **2–4 min session** the right length? | Player wants "one more run" |

**If #1 fails, kill the project.** Everything else is downstream of the energy loop.

---

## 2. Explicit non-goals (do NOT build in prototype)

| Not in prototype | Why |
|---|---|
| Real-time multiplayer / netcode | Highest risk, highest cost — defer. Use ghosts. |
| Battle mode (last man standing) | Content update, not a launch feature |
| IAP, shop, economy, currency | Meaningless until the loop is proven |
| Art, models, VFX polish | Greybox only. Primitives. |
| Progression, save, unlocks | Loadouts are hard-coded and selectable |
| Multiple arenas | One track proves the loop |
| Mobile touch controls | Keyboard first; touch is a Unity concern |

---

## 3. Tech stack (browser prototype)

| Layer | Choice | Note |
|---|---|---|
| Renderer | **Three.js (r128)** | Available in Claude artifacts |
| Type | Single-file **React** artifact | Studio pattern for strategy/systems games |
| Geometry | `BoxGeometry`, `CylinderGeometry`, `SphereGeometry` | ⚠️ `CapsuleGeometry` is r142+ — unavailable |
| Camera | `PerspectiveCamera`, chase-cam | No `OrbitControls` in r128 |
| State | `useRef` for the game loop, `useState` for UI only | Never drive the loop from React state |
| Audio | WebAudio API, synthesised | No external assets |
| Storage | **In-memory only** | No localStorage in artifacts |

---

## 4. Module architecture

Keep every module pure and testable. The game loop reads state, mutates state, renders. No React inside the loop.

| Module | Owns | Depends on |
|---|---|---|
| `EnergyCore` | Energy pool, drain, regen, spend-gating | — |
| `ShipController` | Thrust, steering, speed, collision | `EnergyCore`, `HullSpec` |
| `CombatSystem` | Firing, projectiles (pooled), damage, shields | `EnergyCore` |
| `PilotAbility` | Cooldown, activation, effect | `EnergyCore` |
| `TrackSystem` | Track spline, checkpoints, lap logic, shortcuts | — |
| `GhostSystem` | Records inputs; replays opponents | `ShipController` |
| `BountySystem` | Marks leader, scales rewards, feeds intel to trailers | `TrackSystem` |
| `RaceDirector` | Win/lose, positions, timers, run summary | all |
| `HUD` (React) | Energy bar, position, cooldown, intel | reads state only |

**Rule:** `EnergyCore` is the only thing allowed to say "yes you can spend". Boost, weapons and shields all *ask* it. This is what keeps the trade-off honest.

---

## 5. The core mechanic — one shared energy pool

Every action drains the same pool. This is the whole game.

| Action | Cost | Effect | Trade-off |
|---|---|---|---|
| **Boost** | 25 / sec | +40% top speed | Can't shoot or shield while boosting |
| **Fire** | 20 / shot | Projectile damage | Brief speed dip on fire |
| **Shield** | 15 / sec | −60% incoming damage | Speed capped at 80% while up |
| **Coast** | 0 | Regen 8 / sec | Vulnerable, slow |

**Constraints that make it strategic:**

- Only **one** of boost/fire/shield may be active at a time. Mutual exclusion is the source of all tension.
- Regen only runs while **no** action is active (forces deliberate downtime).
- Energy floor is 0 — no debt, no overdraft.

---

## 6. Data model

### HullSpec (three sidegrades — never a straight upgrade)

| Hull | Max energy | Regen/s | Top speed | Mass | Unique |
|---|---|---|---|---|---|
| **Light** | 80 | 11 | 1.15× | 0.8 | Fits narrow shortcut gaps |
| **Medium** | 100 | 8 | 1.00× | 1.0 | Balanced; no gate access |
| **Heavy** | 130 | 5 | 0.88× | 1.4 | Smashes breakable walls |

### PilotAbility (one per match, cooldown-gated, no raw power)

| Pilot | Ability | Cooldown | Effect |
|---|---|---|---|
| **Vex** | EMP Pulse | 18 s | Drains 30 energy from nearby rivals |
| **Sora** | Phase Shift | 15 s | 1.2 s intangible (pass through walls/shots) |
| **Kade** | Siphon | 20 s | Next hit steals 25 energy |
| **Nyx** | Decoy | 22 s | Spawns a ghost that draws fire for 3 s |

**Upgrades reduce cooldown only. Never damage, never speed.** Preserves competitive integrity.

---

## 7. Systems that create engagement

| System | Rule | Purpose |
|---|---|---|
| **Bounty** | Leader is visibly marked; damaging them yields 2× reward | Punishes running away; creates comebacks |
| **Intel catch-up** | Trailing players see the leader's live energy level | Information, not free speed — feels earned |
| **Spec-gated shortcuts** | Light-only gaps; Heavy-only breakable walls | Loadout choice opens different racing lines |
| **Ghost opponents** | Race recorded runs with their real loadouts | PvP feel, zero netcode |

---

## 8. Prototype build phases (gated)

Build one phase at a time. Stop at each gate.

| Phase | Goal | Acceptance gate |
|---|---|---|
| **P0** | Three.js scene, greybox track, chase-cam, ship moves | Ship drives a lap at stable 60fps |
| **P1** | `EnergyCore` + boost only | Boost drains, coasting regens, running dry feels bad |
| **P2** | Fire + shield, mutual exclusion | Player must consciously pick one. **← the make-or-break gate** |
| **P3** | Three hulls, swappable pre-run | Each hull wins in a different scenario |
| **P4** | Pilot abilities + cooldown HUD | Timing an ability visibly swings a race |
| **P5** | Ghost opponents + bounty + intel | Comebacks happen without rubber-banding |
| **P6** | Run summary, restart loop, tuning panel | "One more run" impulse present |

**🚦 Hard gate at P2.** If the boost/fire/shield choice isn't fun with primitives and no art, more art will not save it. Kill or redesign there.

---

## 9. Tuning appendix (expose ALL of these as live sliders)

Per studio convention, nothing is hard-coded. The prototype ships with an on-screen tuning panel.

| Parameter | Default | Range |
|---|---|---|
| `energy.max` | 100 | 50–200 |
| `energy.regenPerSec` | 8 | 0–25 |
| `boost.drainPerSec` | 25 | 5–60 |
| `boost.speedMult` | 1.40 | 1.0–2.0 |
| `fire.cost` | 20 | 5–50 |
| `fire.speedDip` | 0.90 | 0.5–1.0 |
| `shield.drainPerSec` | 15 | 5–40 |
| `shield.damageMult` | 0.40 | 0.0–1.0 |
| `shield.speedCap` | 0.80 | 0.5–1.0 |
| `bounty.rewardMult` | 2.0 | 1.0–4.0 |
| `ability.cooldownScale` | 1.0 | 0.5–2.0 |
| `race.laps` | 3 | 1–6 |

---

## 10. Path to Unity (after prototype passes)

| Prototype artefact | Becomes in Unity |
|---|---|
| Tuning panel values | `GameTuning` ScriptableObject appendix in the build handoff |
| `HullSpec` table | `HullDefinition` ScriptableObjects, seeded via `Veratus/Afterburn/Create or Update SOs` |
| `PilotAbility` table | `PilotDefinition` ScriptableObjects |
| Module list (§4) | Assembly split: `Afterburn.Core` / `.View` / `.UI` |
| Projectile pooling | Object pool utility (studio convention) |
| — | Canvas 1290×2796 Match 0.5, Safe Area on every screen root, uGUI + TextMeshPro, PrimeTween |

---

## 11. Open decisions for Seni

- [ ] Confirm **race mode ships first**, battle mode deferred to a content update
- [ ] Confirm **async ghosts** at launch (no real-time PvP)
- [ ] Confirm hull count stays at **3** for prototype
- [ ] Confirm **keyboard** controls for prototype (touch deferred to Unity)

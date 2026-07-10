# Afterburn — Design Review & Assessment (Pass 2)
## Veratus Games · Project ALTERNATE · letter **A**, slot 7

> **Purpose:** this document records the **2026-07-09 design review** — the second design pass,
> run as a 4-lens panel (track-3d · combat-systems · monetisation-retention · art-tech) against
> the owner's vision statement: *"a game that takes full advantage of a 3D world, with combat
> strategy, racing excitement, challenging, visually impressive"* + Game Center leaderboards &
> achievements. It records **what stands, what changes, and the new locked decisions D5–D8**.
> `Afterburn_BUILD.md` remains the build contract and was amended to match (§8 below).
> **Status: U1 (Unity skeleton) is built and verified, awaiting sign-off. Nothing in this
> document starts U2.**

> **Standing law is untouched:** the §2 energy rule (EnergyCore sole spend authority, boost/shield
> mutual exclusion, fire blocked while either is active, regen only on coast) and the frozen §5
> tuning values are inviolable. Every recommendation below carries its phase; none re-tunes the
> signed-off loop.

---

## 1. Verdict — the design as it stands

**track-3d lens:** The energy loop is real and validated, but as a track the game is currently a
parking lot with a wavy edge — planar physics, yaw-only heading, world-up hardcoded into the
camera and every pose. It cannot express a single crest, bank, or overpass today. The saving
grace is architectural: the spline sample cache, the analytic lateral clamp, and rail-locked
ghosts are accidentally ~80% of a Wipeout/Redout-style track-relative simulator, and none of the
frozen §5.3/§6 feel constants live in the world-up assumptions. If U2 is written against
per-sample frames that mathematically degenerate to the flat prototype (bitwise-close parity on
Arena01), elevation, banking, layered shortcuts and corkscrews become TrackDefinition **data
purchases**, not a physics rework. Get that one decision right and the "full 3D world" vision
costs a fraction of what it looks like.

**combat-systems lens:** The energy economy is a genuinely strong, validated core — but the
combat layered on it is shallow as a weapon game: one unaimed straight bolt means all depth lives
in WHEN to spend, not in aim, positioning, or counterplay, and the leader (the person the whole
bounty system points at) has zero defensive agency beyond shield-and-pray. Bounty/intel is the
right skeleton for leader-vs-pack drama but is missing hysteresis, leader-side information, and
escalation; ghosts are rail-locked targets rather than rivals; and four prototype bugs
(#3/#4/#5/#7) will silently poison recorded-ghost and Game Center leaderboard integrity if ported
verbatim. The good news: every deepening move fits inside §2 because positioning, information,
and telegraphs are free — they never touch EnergyCore.

**monetisation-retention lens:** This is one of the most monetisation-safe cores handed to an F2P
designer: sidegrade hulls, cooldown-only pilot upgrades, and ranked stat-normalisation mean
competitive integrity is engineered in rather than promised, and the 2–4 min run with a proven
"one more run" impulse is the ideal retention atom. The gap: nothing currently gives cosmetics an
audience — in an async-ghost game nobody sees your livery unless the ghost record format carries
it — and with 3 hulls, 4 pilots and 1 arena the sellable surface at launch is thin. Revenue
expectations should be modest at launch and grow with season cadence; the ghost/leaderboard layer
must be built deliberately as the **status billboard** or cosmetics will not sell.

**art-tech lens:** Mechanically the most disciplined solo-dev handoff seen — the §2 rule, frozen
tuning, and gated phases protect the fun — but visually it is a blank slate with only a palette
and a greybox inventory. The lucky part: everything already locked (void background, emissive
palette, no shadows, spline track, primitive ships) is precisely the brief a code-only art
pipeline executes best, so an emissive vector-light aesthetic can ship looking deliberate rather
than cheap. The two genuine art risks: adding speed-feel VFX without drifting U2 kill-gate
parity, and bloom cost on mobile — both solvable with a parity toggle and a hard performance
contract set at U2.

---

## 2. Assessment against the five pillars

**3D world.** Honest gap: today the game is flat by construction — heading is a scalar yaw,
ships ride y=1.2, bullets y=1.5, the camera wants y=9, all world-up. Nothing in the current build
plan produces a crest or a bank. Headline upgrades: **D6 makes U2 3D-ready now** — track-frame
(Frenet) physics with flat Arena01 as the degenerate case, windowed nearest-sample query, and
level-1 3D rules (ship glued to the ribbon, no gravity on slopes) so the two fast-follow arenas
(D5) are data, not engineering.

**Combat strategy.** Honest gap: one unaimed bolt + instant unreadable abilities = no aim skill,
no counterplay, and a defenceless leader. Headline upgrades at U3/U4: track-space combat, ≤5°
fire-time aim assist with a lead pip, the telegraph & counterplay pass (EMP windup, shield
mitigates drains, Siphon/Phase/Decoy tells), bounty hysteresis + heat, and the rear-drop mine as
the v1.1 loadout choice that finally arms the leader.

**Racing excitement.** Honest gap: the prototype deliberately has no countdown, no FOV kick, no
shake, no draft — it proves the loop, not the thrill. Headline upgrades: countdown +
perfect-launch energy bonus, slipstream as a speed-only draft (the "smart coast"), the
speed-reactive TrackRibbon shader, and the U5 speed-feel package (FOV kick, streaks, sub-pixel
shake, haptics) behind a parity toggle so U2's kill-gate is measured bare.

**Challenge.** Honest gap: difficulty is one synthetic skill band (0.80–0.90) that two ghost bugs
currently distort in opposite directions; there is no ladder above "beat the ghosts once."
Headline upgrades: fix #4/#5 as a pair and re-tune the band at the U4 gate, medal times, leagues
with tighter ghost bands, a daily modifier playlist via the GameTuning runtime copy, and
ghost-verified leaderboards where the replay is both the anti-cheat and the aspirational content.

**Visual impressiveness.** Honest gap: a palette and a greybox inventory is not an art direction.
Headline upgrades: the **SIGNAL VOID** direction ("light is information"), the TrackRibbon and
wall/gate modular kit (D5: designed now), procedural skybox + ThemeDefinition themes, three-tier
thrusters, the pooled combat VFX suite, and the hero frame-true energy bar — all inside a hard
A15/60fps URP performance contract locked at U2.

---

## 3. Decisions locked this pass (D5–D8)

| # | Decision | Locked value |
|---|---|---|
| **D5** | Arena roadmap | Launch arenas = **1** (Arena01, flat, the kill-gate parity fixture) + **2 full-3D arenas as fast-follow content updates**; the environment modular kit is designed now |
| **D6** | 3D timing | **BUILD 3D-READY NOW:** U2's ShipController is engineered as a 3D track-frame (Frenet) system from day one, flat Arena01 as the degenerate case; parity vs the HTML prototype must still hold bitwise-close on Arena01 |
| **D7** | Ads | **NO ADS, ever.** Monetisation = cosmetics + pilots + season pass only. Existing guardrails stand: no stat-power in ranked, pilot upgrades = cooldown only |
| **D8** | Platform services | **Game Center (iOS) first at U5/U6** using the studio's existing Nibwell GameKit pattern (separate platform-gated assembly, Apple.Core + Apple.GameKit); Google Play Games parity at U7 |

> D7 supersedes the panel's "one rewarded slot held in reserve" recommendation — the reserve is
> **deleted**, not deferred. Zero ad SDK ships in any build, ever.

### Pass 3 — locked later the same day (owner's original vision reconciled + economy audited)

| # | Decision | Locked value |
|---|---|---|
| **D9** | Identity + modes | Afterburn is a **"3D space battle-racer."** Three-mode roadmap: **RACE** (ranked, pure, the validated energy loop — launch) → **BATTLE RACE** (update 1: kill scoring + in-match pickups) → **BATTLE MODE** last-man-standing (update 2, as D1 always deferred). **Pickups exist only in Battle Race**, only as in-world spawns obeying §2 (using an item is your action; nothing enables boost+shield), never inventoried, never purchasable |
| **D10** | Ship module slots | Hulls gain **engine / wing / hardpoint slots**; every module is a **sidegrade trade** (e.g. +15 maxEnergy ↔ −15% regen). Modules are **earn-only** via mastery milestones + teaching challenges — the unlock teaches the mechanic it trades on. First module lands in session 1; a fully custom ship exists by ~hour 3 |
| **D11** | Progression | **XP/mastery unlocks CONTENT, never stats** — modules, weapons, contracts, prestige cosmetics. Mastery/teaching challenges progress **only in ranked-normalised contexts** (`abilityCooldownScale = 1.0`) so purchased convenience can never accelerate them |
| **D12** | Economy | The audited sell-vs-earn split in §6 (below) is canon. **The Law: money buys how you LOOK and who you FLY AS; play earns what your ship can DO.** Billboard surfaces (pre-race lineup + podium + ghost cosmetics) are mandatory-flow, not opt-in |
| **D13** ✅ | Ship art | **RESOLVED 2026-07-10 — the Ebal fleet purchased and imported:** **Ultimate Spaceships Creator** (supersedes the Hi-Rez plan — named ship families: Light=CosmicShark, Medium=StriderOx, Heavy=VoidWhale, 562 prefabs, modular parts for D10 slots), **Modular Weapons & Turrets** (D10 hardpoints + the v1.1 mine model), **Modular Warp Gates** (35 gates + animated warp effects — the start line is a real gate now). All URP-native (Ebal Colorize ShaderGraphs = the livery architecture). StarSparrow validated the pipeline at gate 0. World + UI stay code-built. **Repo policy: vendor packs gitignored** (USC contains a >100 MB file; GitHub hard limit) — fresh clones reimport from Asset Store, GUIDs stable. Mobile texture pass (1–2K ASTC) due before device builds (U7). *Galactic Leopard* still deferred |
| **D15** | Gate features | **Gates as TRACK FEATURES (owner ruling 2026-07-10):** SpeedBoost / WarpSurge / Blocker rings placed in `TrackDefinition.gateFeatures` — fixed, deterministic, identical for every racer every lap. **Legality frame:** these are NOT pickups (nothing random, nothing consumed) — same category as spec-gated shortcuts, therefore **ranked-legal**. Laws: boost/warp give **speed only** (cap-max + clamped impulse — never stacks, never energy — the slipstream precedent); blockers **drain only** (D14 hazard family, Phase passes through); **no teleportation** (would break lap logic + ghost replay determinism — "warp" = a violent surge). Recorded ghosts replay gates deterministically (track data). Synthetic ghosts exempt v1 (rail-locked; skill band absorbs — U4 re-tune note). Arena01 ships 5 gates (2 boost, 1 warp on the back straight, 2 blockers on fast lines). Plus the scenic ring: station-scale gates orbiting the arena as backdrop |
| **D14** | Tangible ships | **Ships are TANGIBLE** (owner ruling at the U2 feel pass, overriding the intangible-ghost recommendation): ship↔ship and wall contact **damage the energy pool**. Wall grind drains `wallContactDamagePerSec` (6/s) on top of the ×0.92 scrape; ship contact deals mutual **mass-ratio-scaled** damage (base 10 — `hull.mass` finally has a gameplay consumer: Heavy 1.4 shrugs, Light 0.8 suffers), player pushed out of overlap + speed-scraped, 0.5 s per-pair cooldown, Sora's Phase passes through. §2-safe: contact only drains, never grants. All values in GameTuning; the parity trace zeroes them (divergence-by-ruling). **Balance watch:** mass is now a live sidegrade axis — include in the ±10% parity accounting; ghost skill band may need compensation since rail-locked ghosts never wall-grind |

---

## 4. Upgrade / Add / Change matrix

### U2 — core loop, engineered 3D-ready (D6)

| Change | Pillar | Detail (concrete) | Risk note |
|---|---|---|---|
| Track-frame physics, flat as degenerate case | 3D world | Extend sample cache `{pos, tan, nrm}` → `{pos, tan, nrm, up}`. ShipController step 8 + `resolveWalls` written against the frame, never world-up: `position = sample.pos + nrm·lateral + up·1.2`; scalar yaw → world-space forward vector rotated about `sample.up` by `steer·2.4·bite·dt`, re-projected onto the sample plane per frame (parallel transport, no auto-steer); slide nudge = rotate-toward-tangent `0.18·dh` about up. Camera offsets `−16·fwd + 9·up`, look `+10·fwd + 2·up`. On Arena01 every operation reduces to the prototype's exact arithmetic — assert in edit-mode tests | The one real risk is kill-gate parity drift from the vector-heading refactor — mitigated by the trace-diff below and Arena01 as a permanent regression fixture. §2 untouched: EnergyCore never learns the track has frames |
| Automated parity trace-diff | Challenge (integrity) | Per-frame state-trace diff against `Afterburn_Prototype.html` at frozen tuning — the U2 parity instrument. Trace starts at green light (countdown excluded from the diff window) | Without it, "feels like the prototype" is unfalsifiable |
| Windowed, progress-coherent nearest-sample query | 3D world | Replace global argmin over 700 samples with ±40-sample window around each racer's last index; full scan only on spawn/reset; window keeps advancing through Sora's Phase; detect-and-rescan fallback; unit test on the wrap seam (699→0) | Behaviour-identical on flat by construction; the single prerequisite for any self-intersecting track (overpasses snap to the wrong layer otherwise) |
| Fixed-tick 60 Hz Core simulation | Challenge (tech) | `Afterburn.Core` runs on a fixed 60 Hz accumulator, View interpolates; camera lerp `min(1, dt·4)` → `1−exp(−4·dt)` (logged as a new divergence); HeavyWall break-state joins the deterministic sim state. **Hard prerequisite for U4 recorded ghosts** — the format only replays identically under a fixed timestep | Tiny integration difference vs the prototype's clamped variable step — validate it reads identically at the side-by-side. Retrofitting determinism after ghosts exist invalidates every stored ghost |
| Countdown + perfect-launch bonus | Racing excitement | 3-2-1 countdown (RaceDirector §7.8 already specifies the state; the prototype had none — Divergence #1 is a design upgrade, not a parity bug). Perfect-launch: boost input in the final beat window pays a small energy bonus via EnergyCore's clamped grant (same path as bounty rewards); tuned at U5 | Bonus must route through EnergyCore and never bypass the clamp; keep it small enough that skipping it is viable |
| URP mobile performance contract | Visual | Locked at U2, asserted at every gate through U7: render scale 0.8 (HUD on Screen Space Overlay, native-crisp at 2796×1290); MSAA 4x, Depth/Opaque Texture OFF; draw calls ≤120; visible tris ≤150k; texture memory ≤48MB; bloom half-res, iterations ≤4, threshold 1.1, intensity ~0.6, HDR B10G11R11; zero realtime shadow maps; no other post. Target: A15, 60fps | Bloom is the one negotiable line — thermal throttling → fallback is pre-authored billboard-halo glow, decided by measurement. Setting the contract late is how solo projects end at 45fps |
| SIGNAL VOID art direction lock | Visual | Doc + palette-role constants only at U2, greybox untouched; applied U3–U5. "Light is information": world stays #05070F; cyan #37D0FF = player/track guidance, violet #9D7BFF = shield/phase, orange #FF8A3C = boost/Heavy/heat, gold #FFD23F = leader/bounty only, red #FF4D6D = damage only, wall-blue #2B4D8F = structure. Geometry over texture: zero painted textures, procedural low-poly + vertex color + SDF shaders | Palette-role lock must be enforced everywhere later (cosmetics, themes) or state colors stop meaning anything |

### U3 — hulls, pilots, shortcuts, combat depth, first art pass

| Change | Pillar | Detail (concrete) | Risk note |
|---|---|---|---|
| Track-space combat | Combat strategy | Bullets become `(progress, lateral)` advancing at `170 + 0.4·shooterSpeed` along the shooter's in-plane heading, rendered at surface + 0.3·up; EMP radius 70, aiFireRange 55, decoy targeting evaluated as track-space distance (`Δprogress·LEN`, `Δlateral`). Numerically identical to the prototype on flat — zero parity risk at U3 | Deferring it means the frozen fireCost 20 / damage 25 / bounty 16 economy silently re-tunes the day the first slope ships — a stealth violation of "the prototype is the spec of feel" |
| Aim assist (snap ≤5°, never homing) | Combat strategy | Lead pip + target bracket when a rival is within a 30° cone at ≤55u; at fire-time only, snap the bolt's initial direction ≤5° toward predicted intercept, then perfectly straight. Decoy MUST be a valid snap target. Assist strength is a fixed GameTuning value, identical in ranked, never a pilot upgrade or purchasable | Over-tuned snap turns dodging into dice and the 0.28s cadence into a stunlock engine — pair with the spinout mercy window; verify Decoy still eats assisted shots at the U3 gate |
| Telegraph & counterplay pass | Combat strategy | EMP: 0.6s expanding-ring windup + audio sting (may need 30→35 to compensate); `shieldDamageMult` ×0.40 applies to ability drains via CombatSystem's damage pipeline (NOT a new EnergyCore path); Siphon-armed ships get a visible tether/glow; Phase 0.3s pre-glow; Decoy tell = no thruster flame; spinout mercy window | Damage-side rule only — §2's spend/regen invariants untouched. Mercy window slightly weakens focus-fire on the leader; confirm comebacks still happen at U4 |
| Slipstream draft — LEGAL, speed-only | Racing excitement | Within ~12u behind a racer (lateral ±3u of their tangent line) for ≥0.6s → top-speed cap ×1.08–1.12, **only while coasting or firing; suspended during boost and shield**. Cap logic U3, validated U4 where opponents exist, ship-enabled U5. The "smart coast": regen + near-boost speed + the natural firing line for the unaimed bolt | Keep it under half of boost's +40% or it devalues the core purchase decision. Zero §2 exposure: no energy granted or spent |
| Hull-flavored projectiles — feel only at launch | Combat strategy | Tracer visuals, SFX, muzzle behaviour only. Legality rule recorded for any future stat variance: spinout-seconds-per-100-energy and net-energy-per-hit within ±10% of Medium's baseline (spinout-seconds, not damage, is combat's real currency; every hit pays 8) | Shipping stat variance at U3 silently turns "each hull wins a different scenario" into "Light wins combat, period" |
| Mine SO schema reserved | Combat strategy | Reserve the loadout-weapon enum + SO fields now; the mine itself ships v1.1 post-launch (see that table) | Do not let it bloat the U2–U4 re-proof |
| TrackRibbon speed-reactive shader | Racing / Visual | One ShaderGraph on the ribbon, UV.x = lateral −1..1, UV.y = arc-length: cyan edge rails at ±16.5u brightening with `_PlayerSpeed`; counter-scrolling chevrons; lateral ticks every 20u; shortcut approach tint from ~120u out; ~12 arc chunks for culling; greybox-material toggle kept for parity diffs | Visual-only by contract — the shader never moves geometry; collision stays the analytic clamp. A/B chevron frequency at full boost at the U3 gate |
| Wall/gate modular kit (D5: designed now) | Visual / Challenge | Scrape-reactive wall flash driven by ShipController's wall-contact event; flat SDF ring gates replace the torus; LightGap = cyan pillars + light beam + floor chevrons keyed to the player's own `gateAccess` (information, not power); HeavyWall = orange slab pre-fractured into 12–16 chunks, pooled scripted scatter via PrimeTween (no physics), honouring the #3 ruling | Telegraphs must not alter collision or allowances — `innerAllow` stays in Core untouched. Hull-keyed chevron flagged for U3 gate ruling |
| Skybox + ThemeDefinition SO plumbing | 3D world / Visual | 3-stop gradient skybox, in-shader hash-noise starfield, 1–2 SDF celestial discs as fixed distant landmarks (needed to judge velocity); fog 220–620 color-matched to the horizon; star streaks elongate above ~70 u/s. ThemeDefinition SO wired, Arena01 → 'Deep Void' | Fog/skybox mismatch exposes the track's far edge. No gameplay surface at all |
| Hull meshes + three-tier thrusters | Visual | Dart / arrow (straightened-nose lineage, #16 ruling) / hammerhead, 2–4k tris, one uber-material; silhouettes readable at 100u because hull class is gameplay information. Thrusters read-only from Core: T0 coast, T1 ×1.3, T2 boost ×1.8 + 25u trail, #FF7A3C→#FFE14D (prototype parity) | Thruster flame must cut the exact frame EnergyCore self-cancels at 0 — a lingering flame implies energy that does not exist and misinforms Intel reads |
| Combat/ability VFX suite (into U4) | Combat / Visual | Pooled, shader+particle, no textures: muzzle quad, red #FF4D6D tracer, hit spark + "−25" readout; violet hex-lattice shield; EMP torus expanding to exactly radius 70; Phase screen-door dissolve at 0.4 opacity; Siphon teal #38F5C9 mote stream; Decoy glitching hologram | The core VFX risk: implying untrue state. Shield vanish frame, boost cut, phase window must match Core booleans exactly — test against EnergyCore edit-mode scenarios. VFX never calls TrySpend |

### U4–U5 — ghosts, bounty, front-end, feel layer

| Change | Pillar | Detail (concrete) | Risk note |
|---|---|---|---|
| Ghost record format carries cosmetics | Visual (monetisation) | Extend the record **header** (not the frame stream) with `cosmeticLoadout {liveryId, trailId, plateId}`; ghosts must also serialize whether/when the HeavyWall broke during their run. The single highest-leverage monetisation feature in the plan: every race lost to a ghost is an ad for what that player bought or earned | Fields must land before the U4 gate — a format change after sign-off breaks stored ghosts. Cosmetic IDs stay strictly out of the deterministic input stream or replay parity breaks |
| Ghost bugs #4 + #5 fixed as a pair | Challenge | Single skill application (BUILD §6 documents the intent); seed ghost `laps = −1`, keep the physical grid stagger. The prototype was validated fun WITH both bugs — fixing both yields faster ghosts racing longer distances, so a **skill-band re-tune is an explicit U4 gate item** | The real risk is fixing the code and keeping the old band — the "beatable via energy play" handicap evaporates. Zero §2 / U2-parity exposure |
| Bounty/intel upgrade | Combat strategy | (1) Leader threat-pulse: ★BOUNTY tag pulses when any racer is within 55u behind; (2) 1s mark hysteresis (or +0.02 progress margin) so overtake battles stop strobing the mark; (3) bounty heat: +2 energy per 10s the lead is held unhit, resets on any hit, **capped ≤ fireCost (20)**; (4) leader-mode intel chip | The heat cap is load-bearing: bounty hits must stay energy-negative for the shooter or combat becomes an energy fountain and §2 scarcity collapses. Escalating attention, never rubber-banding |
| Ghost-verified leaderboard times | Challenge | Every submission carries its ghost recording; deterministic replay through Core at fixed tick validates the claimed time; the same file is content ("Race the #1 ghost"). Client-side validation first, server-side later if population justifies | Depends entirely on the U2 fixed-tick job and no cross-device float divergence in Core. The replay IS the anti-cheat |
| Ghost identity holograms + leader beacon | Visual (social) | Recorded ghosts render owner cosmetics through a hologram variant (~0.7 opacity, scanlines); TMP nameplate 28pt fading inside 40u; leader beacon = ~60u gold light pillar; Intel readout renders as a mini energy bar on the leader's screen-edge indicator, **trailer-only**. Synthetic ghosts get generated callsigns | Confirm at the U4 gate that Intel stays trailer-only per §7.7 — rendering it where the leader sees it breaks the information asymmetry |
| Speed-feel package behind a parity toggle | Racing excitement | FOV 62→68 over 0.25s on boost entry/exit; camera-attached speed-line tube above 55 u/s scaling to 90; sub-pixel shake ≤0.05u boost-only; haptics on fire/hit/bounty; audio layer ducks on dry-out. Parity mode (prototype-exact camera, everything OFF) is the default until U2 signs off; package enabled at U5 with explicit sign-off | Highest kill-gate parity risk in the plan: if the FOV kick is live during the U2 diff, "feels like the prototype" cannot be judged. Any handling drift traced to camera effects → toggle stays off |
| HUD art system — frame-true hero bar | Visual / Challenge | Bar width proportional to hull `maxEnergy` (Heavy's bar is physically longer — the sidegrade made visible); tick marks every 20 energy = one shot; primary fill is frame-true with NO tween; trailing loss-ghost fill tweens down over 0.4s after a 0.25s delay; mode colors (regen cyan / boost orange / shield violet), red pulse below 25% | Any lag between `EnergyCore.Ratio` and the fill misinforms the exact decision §2 exists to protect — audit fill = Ratio same-frame at the U5 gate |
| Retention ladder, rungs 1–2 | Racing / Challenge | Summary carries **exactly three** impulse hooks: delta line ("0.4s off your best lap"), Salvage earned with style-bonus breakdown, ONE next-goal chip — plus Rematch (re-races the same ghost seeds). Daily: First Flight (first 3 races pay 2× Salvage) + one Daily Contract from a mechanics-reviewed pool; accumulative "days flown this month", never a reset-to-zero streak | The Summary screen is where "one more run" lives — hook count is a U5 gate criterion. Contract pool gets the same anti-perverse-incentive review as Salvage bonuses (never "boost for 120s") |
| Medal times | Challenge | Per-arena bronze/silver/gold/author lap + race medal targets on Summary and Loadout | Derive from the re-tuned ghost band, not the buggy prototype curve |
| Game Center scaffolding (D8) | Challenge (social) | Platform-gated assembly per the Nibwell GameKit precedent (Apple.Core + Apple.GameKit, define-gated asmdef); achievement/board counters wired into the U5 summary payload; boards + achievements live at U6 (panel had said U7 — D8 pulls it forward) | Keep it behind the platform define so U-gates stay testable everywhere; nothing platform-side may touch Core |

### Post-launch — the 3D ladder and live content

| Change | Pillar | Detail (concrete) | Risk note |
|---|---|---|---|
| **Battle Race mode (D9, update 1)** | Combat strategy | Kill scoring on the racing loop + **in-match pickups** (shield charges, ammo caches, one-time weapons) as world spawns obeying §2 — using an item is your action; nothing enables boost+shield. Pickups never inventoried, never sold. Ranked RACE mode stays pickup-free — the sanctuary of fairness | The chaotic fast-paced fantasy gets its own home instead of eroding the validated ranked loop |
| **Battle Mode — last man standing (D9, update 2)** | Combat strategy | LMS on the same energy loop; shrinking play space; the D1 deferral honoured | Same systems, third mode — no new core surface |
| Rear-drop mine (v1.1) | Combat strategy | Loadout choice **Bolt OR Mine** (one fire input): `EnergyCore.TrySpend(fireCost=20)`, blocked while boost/shield active, arm cadence ~1.0s, max 2 live per racer (pooled), TTL ~7s, damage 25 + spinout 0.7 (bolt parity), visible emissive pulse so it's dodgeable. Arms the leader; area-denies the Light gap; the natural counterplay to draft | Rail-locked ghosts can't dodge — requires a ghost lateral-dodge behaviour (lane-offset lerp when a mine is <25u ahead) before shipping. §2-safe: shares the existing fire spend path |
| **Module slots system (D10/D11)** | Challenge / 3D world | Engine/wing/hardpoint slots; every module a sidegrade trade; earn-only via teaching challenges (§6.3 curve); XP reveals, challenge unlocks; slot housings are the cosmetic surface (§6.1) | Sidegrade legality per module uses the same ±10% parity accounting as hull projectiles; each module passes a mini balance gate before ranked |
| Coast-amplifier pickups — flat orbs REJECTED | Challenge / 3D world | Ruling: flat +energy orbs violate the spirit of §2 (income must require disengaged vulnerability). Allowed form: pickup banks +50% regen for the next 4s of coasting — no energy on contact. 2–3 per lap, exclusively on risk lines (wall-scrape apexes, off-camber lines, inside shortcut zones); no same-lap respawn; GameTuning-exposed, **default off**; tuning flag reserved at U4 so recorded ghosts capture pickup lines | The recommendation most likely to erode the validated loop — judged against the U2 "running dry feels bad" criterion, never added to launch scope |
| Level-1 3D: banking + elevation (D5 fast-follow tech; test scene pre-launch) | 3D world | TrackDefinition control points gain Y + per-sample roll baked into the up vector. Ship stays **glued to the ribbon** (`surface + 1.2·up`) so the longitudinal pipeline (thrust 55, drag 0.6, caps, boost ×1.40) runs unchanged in 1D — every frozen constant survives verbatim. **Slopes do NOT accelerate/decelerate grounded ships — no gravity in the speed pipeline; slopes are route and spectacle, not physics** (`ShipFeel.slopeAssist` constant, default 0, for future experiments only). Banking lets corner radii tighten below the flat 58u minimum. Camera up-vector smoothed separately, slerp ~2/s (vs 4/s position) | Gravity-on-slopes is the trap: it makes hull mass (0.8/1.0/1.4) a downhill speed stat — breaking frozen handling and ranked fairness. Glue + no-slope-force sidesteps all of it. Arena01 parity fixture protects the U2 gate forever |
| Airtime: crests, ramps, gap jumps | 3D / Racing | Grounded/airborne state machine; launch only via ramp-lip zones flagged in TrackDefinition (explicit data, deterministic); airborne = ballistic under `ShipFeel.gravityAccel` ~35 u/s² (standard hop 0.6–1.0s); steering bite ×0.3 in air; landing misalignment reuses the ×0.92 scrape. **Boost entry refused while airborne** (ShipController-side gate, EnergyCore untouched) — air is a natural coast/regen window; the skill move is pre-lip boost for distance. Fire and shield stay legal midair | The largest §2-adjacent surface in the plan. Wrong rulings: boost-drains-midair-with-no-effect punishes held input; boost-thrusts-midair breaks the racing frame. Grounded-only entry keeps EnergyCore's authority intact |
| 3D shortcut grammar | 3D / Challenge | (1) `verticalClearance` on the zone struct — LightGap's 3D-native form is a low duct only Light fits through; gating stays the `gateAccess` enum, never ad-hoc radius math. (2) Branch splines `{entryFraction f0, exitFraction f1, spline, gating}`; on a branch, `progress = lerp(f0, f1, branchT)` so laps, bounty leader, and Intel keep working unchanged. HeavyWall's 3D form: breakable floor grate dropping Heavy one layer down | Branches touch nearest-query (query per-branch in range), ghost recording (branch choice joins deterministic state), and the minimap. Layer shortcuts stay distance/line advantage only or the sidegrade promise breaks |
| Arena ladder (D5: two fast-follow, then a third) | 3D world / Visual | **Arena02 'Redline Canyon'** (banking + elevation + one jump): 55° wall-carved banked sweepers dropping ~40u; signature = The Overrun, a blind crest gap — pre-lip boost clears it, coasting drops to a safe mid-ledge rejoin; LightGap = slot-canyon fissure, HeavyWall = smashed ore-conveyor bore. **Arena03 'Anchor Station'** (over/under + tunnels + branches): orbital shipyard figure-8; signature = the Gantry Cross — the lap crossing over itself, rivals visible through the grating (Intel made physical). **'Helix Foundry'** follows. Themes: 'Ember Belt', 'Reactor Violet' (red #FF4D6D stays reserved for damage) | Each arena is a content-update-sized deliverable even greyboxed — the ladder is ordered so no arena waits on tech it doesn't use. Redline Canyon's greybox doubles as the pre-launch elevation test scene |
| Leagues + daily modifier | Challenge | Leagues = progressively tighter synthetic-ghost skill bands per tier above the launch 0.80–0.90; daily modifier = a labeled unranked playlist mutating a **runtime copy of GameTuning** (the §5.3 asset is never touched), reusing the dev-overlay mechanism | Modifiers never enter ranked; ranked always runs frozen values + `cooldownScale = 1.0` |
| Season structure: 'Circuits' | (monetisation) | See §6 — first Circuit ~4–6 weeks post-launch, once retention data exists | Treadmill risk: if the cadence slips, go to 8–10 week seasons rather than cutting the anti-FOMO rule |
| Hull projectile stat profiles | Combat strategy | Only after the ±10% parity accounting (spinout-seconds and net-energy-per-hit) exists and is enforced | Any monetised hull would inherit a pay-adjacent power axis without it |

---

## 5. Prototype bug rulings

> **Status: RECOMMENDED — each ruling awaits Seni's confirmation at the U2 gate.** Numbers refer
> to `Afterburn_PortSpec.md` DIVERGENCES.

| # | Prototype behaviour | Ruling (recommended) | Phase |
|---|---|---|---|
| **1** | No countdown — live the frame after START | **Not a parity bug — design upgrade.** The game adds a 3-2-1 countdown (+ perfect-launch bonus); the U2 trace-diff starts at green light | U2 |
| **3** | Broken HeavyWall opens for ALL hulls and **persists across "one more run"** | **Split ruling.** (a) KEEP within-race open-for-all: readable, makes Heavy a track-altering pick, creates pack dynamics — update BUILD §7.5 wording to make it canon. (b) FIX persistence: reset all `zone.broken` flags in RaceDirector race-start — state leakage makes recorded ghosts and leaderboard times incomparable. Recorded ghosts serialize whether/when the wall broke so replays render the slab correctly | Wording now; code with U3 shortcuts |
| **4** | Ghost boost applies skill **twice** → boosting cap = `baseTop·1.40·skill²` (ghosts slower on straights than BUILD documents) | **FIX** to single application (BUILD §6 already documents the intent). Must land paired with #5 | U4, with band re-tune at the gate |
| **5** | Ghosts start at `ai.t ≈ 0.99`, `laps = 0` → first wrap gifts a **free lap**; player starts ranked P4; ghosts finish after ~2.01 laps of travel | **FIX:** seed ghost `laps = −1` (PortSpec's own repair), keep the physical grid stagger. Caveat: the prototype was fun WITH #4+#5 — fixing both makes ghosts faster over longer distances, so the 0.80–0.90 skill band is **re-tuned as an explicit U4 gate item** | U4, paired with #4 |
| **7** | Siphon is flat −25/+25 regardless of the victim's pool — **mints energy from nothing** | **FIX:** `steal = min(25, victim.energy)`; attacker gains exactly what the victim lost, via EnergyCore's clamped grant; delete the prototype's dead capped-value code. Makes Kade a timing read (strike a FAT target via Intel) instead of a guaranteed +25. If he underperforms at the U3 "visibly swings a race" gate, shave `cooldownSec` 20→18 — cooldown-only compensation keeps the monetisation guardrail | U3 |
| **16** | Cone Euler-order skew — rendered nose points 45° off travel | **KEEP the straightened nose** (apex +z, base rolled 45°) — the skew is an authoring bug, not feel | U2 greybox / U3 art |

---

## 6. The economy — sell vs earn (AUDITED, pass 3 canon)

> **Audit record (2026-07-09):** this section was adversarially audited by a 3-lens panel
> (pay-to-win leak hunter · economy pacing auditor · revenue realist). The matrix held
> structurally; **two catalog-breaking findings** (season-pass pilot timing; the week-2 spender
> wall) and a set of drift-shaped leaks were found and are **fixed below**. This section
> supersedes the pass-2 catalog.

**THE LAW: money buys how you LOOK and who you FLY AS — play earns what your ship can DO.**
Store page carries the literal, checkable claim: **"nothing you can buy makes you faster."**

> **Billboard hard requirements:** cosmetics need a mandatory audience, not an opt-in one.
> (1) Ghost record header carries `{liveryId, trailId, plateId, housingIds}` (blocking, U4).
> (2) **Pre-race lineup screen** — all 4 ships full-size, trails idling, plates showing (~4–8 s,
> Mario-Kart-intro pattern) — and (3) **post-race podium** rendering the top-3 finishers' ships:
> every cosmetic is seen 3+ times per race *by construction* (U5 screens; see UIEnvSpec).
> Victory-effect SKUs gate on the podium existing; hangar themes gate on hangar inspection (U6+).

### 6.1 SOLD — money-only (pure identity; `com.veratusgames.afterburn.*`; Unity Purchasing at U6)

| SKU family | Item | Price | Audit-hardened terms |
|---|---|---|---|
| `livery.*` | Standard livery | $1.99 | Per-hull tints/patterns |
| `livery.*` | **Reactive livery** | $3.99 | Energy-state-responsive trim. **Non-owner views (rivals, ghosts) are quantised to already-public states** (boost = thruster T2, shield = lattice, fire = muzzle); the sub-25% "gutter" renders **owner + post-race replay only** — it would otherwise leak fuel-gauge intel the leader is denied (§7.7) and adversely select top ghosts back to stock |
| `livery.*` | Prestige livery | $5.99 | Animated + unique trim, one per hull per season |
| `housing.*` | **Slot housings** | $1.99–$3.99 | **Per-SLOT finishes (engine/wing/hardpoint), apply to stock loadouts too** — market exists day one. Legality contract: bounded silhouette envelope, mandatory module identifier glyph, never alters hull-class signature |
| `trail.*` | Thruster trails | $0.99–$2.99 | Render only while boosting — the purchase advertises energy spend |
| `plate.*` | Nameplates / badges | $0.99 | Ghost tags + leaderboard rows |
| `audio.*` | Announcer packs | $2.99 | Highest-attachment racing audio SKU; **identical callout triggers/timing/info across packs**. Engine audio skins $0.99 (re-timbre only; state stings preserved) |
| `fx.*` | Victory/podium effects | $0.99–$1.99 | **Ships only after the podium screen exists** — until then it sells to an empty theater |
| `theme.*` | Front-end/hangar themes | $1.99 | Menus + hangar only. **In-race HUD themes are REJECTED as a category, permanently** — HUD colors are information; no purchasable pixel renders between Countdown and Finished |
| `bundle.*` | **Collections** | $12.99–$14.99 | Reactive set across 3 hulls (~$16.94 itemised → $12.99); seasonal Prestige collection ($17.97 → $14.99). Bundle rules: pure cosmetics, every component also sold separately, never contain Salvage or cooldown levels |
| `pack.supporter` | **Supporter pack** | $9.99 | Plate + badge + hangar banner, described verbatim as "this funds development; it contains nothing else" — the tip jar the fairness contract earns |
| `pack.founder` | Founder pack | $4.99 | Cosmetics + **Salvage grant fixed at 1200–1500** (~half a pilot — never substitutes for one). Permanently retired after launch window; honoured forever |
| `pass.s01` | Season pass 'Circuits' | $4.99 | See 6.4 — **never expires**, pilot parity on both tracks |

### 6.2 DUAL-PATH — money = time saved, never exclusivity, never a skipped skill gate

- **Pilots: fixed 3000 Salvage (~3 hours at median rates) or $2.99–$3.99.** Written law: **money
  waives the Salvage price, never a challenge** — no pilot is ever mastery-gated behind a wall
  money can skip. Every new pilot passes a §2 mini kill-gate before art; **any tuning buff to a
  purchasable pilot re-runs the mini-gate** (paid-power-by-patch is the drift mode).
- **Standard colorways: 400 / 700 / 1000 Salvage** (one / one-and-a-half / two sessions) — the
  whole band stays below ⅓ of a pilot so the two sinks never share a mental account.
- **Measured invariant, re-verified every season via telemetry:** every money-purchasable gameplay
  item is earnable in **≤ 4 hours at the median player earn rate**.

### 6.3 EARN-ONLY — collectable/unlockable; cannot be bought at any price

- **Hull modules (D10):** mastery milestones *reveal* (Summary next-goal chip), teaching
  challenges *unlock* — every challenge completable inside normal racing. Curve: **M1 hour 0.5**
  (engine 'Deep Cycle', +15 maxEnergy ↔ −15% regen — challenge: "spend 40%+ of one race
  coasting"), M2 hour 1.5 (wing 'Feather'), M3 hour 3 (hardpoint 'Long Bolt') → **fully custom
  ship by hour 3**; M4–M8 through ~hour 12. Slot order engine→wing→hardpoint.
- **Alternate weapons** (rear-mine v1.1, future): mastery milestones.
- **Battle Race pickups (D9):** in-match world spawns only — never inventoried, never sold, no
  consumable packs, not even once.
- **Achievement plates/titles** (Kingslayer…), league emblems, **medal-gated cosmetics** (beat
  the author time → exclusive livery), per-hull prestige mastery skins.
- **Arena access: free for everyone, always** — paid arenas would split the ghost pool.
- **Season pass FREE track** carries every gameplay-relevant item at the same tier as paid (6.4).

### 6.4 Season structure — 'Circuits' (audit-corrected)

30 tiers / 6 weeks / $4.99. **The pass never expires** — once bought, its objectives remain
completable in any later season (one pass active at a time). This deletes the genre's last hidden
timer; there is no catch-up SKU because none is needed (tier skips are banned anyway).
**The season pilot sits at the SAME tier on BOTH tracks (~tier 25)** — the paid track carries
that pilot's *cosmetics* (prestige skin, plate) instead of early access; tier-8-paid access was a
3–4 week ranked head start across zero-sum weekly boards and is the one true P2W break the audit
found. 10 weekly objectives (retroactively completable; two lapsed weeks recoverable); **any
single race can advance ≥ 3 objectives** so 3-session/week players finish the free pilot before
season end. Anti-FOMO rule stands: pass cosmetics enter the direct store two seasons later; only
the season plate stays exclusive.

### 6.5 Salvage economics (audit-corrected numbers)

Canonical loop: **2.5 min door-to-door** (~22–26 races/hr) → decent play earns **~900–1400/hr**
(the pass-2 sheet was calibrated 30–40% low). Per race: **base 20** + placement (P1 +30 / P2 +20
/ P3 +10) + style bonuses:

| Bonus | Value | Audit fix |
|---|---|---|
| Bounty hits | +5 each, **capped at 3/race** | Uncapped, vulturing the rail-locked leader out-earned winning (10–14 hits ≈ +50–70) — the exact "pay for raw activity" failure §6 bans |
| Spec-gated shortcut | +5, **once per race, only via your own hull's `gateAccess`** (Sora phase counts as LightGap) | Was structurally unequal: Light could earn +15, Medium 0 forever |
| 'Racing Line' | +5 — no shortcut taken, every checkpoint above 55 u/s | **New**: gives Medium an equal style-bonus ceiling |
| 'Cold Blood' | +10 — hit 0 energy **before the final lap** and still win | Was satisfiable by ritual dry-out at the finish line; now it's a recovery story |
| 'Clean Coast' | +5 — zero wall scrapes | Unchanged |

No bonus for boost-time or shots-fired, ever. **'First Flight' daily (first 3 races ×2) applies
to base + placement only** — never style bonuses. From Arena02 onward, base + placement scale
with **arena par time** (base = 8 × par-minutes) so the shortest arena never becomes the farm.
**FTUE:** a **free-pick colorway token** (any tier — a gift, not exact change) delivered on the
first Summary screen, plus a 250 Salvage seed so the wallet shows progress toward the next item.

### 6.6 The pay-to-win firewall (mechanical, not editorial — each line is a U6/U7 acceptance test)

Pilot cooldown upgrades: 5 levels, −4%/level (max −20%; Sora floors at 12 s), **Salvage-only**,
ranked-normalised via the RaceContext flag (`abilityCooldownScale = 1.0`, base `cooldownSec`).
Plus, verbatim, permanently:

- No hard currency · no gacha/loot boxes · no timers · no run-limiting energy · **no ads (D7)** ·
  no tier skips · no premium+ pass · cooldown levels never in any IAP, bundle, or pass track.
- **No earn-only item (module, weapon, mastery cosmetic) ever carries a Salvage price; Salvage
  never appears on a paid pass track.** (Closes: money → founder Salvage → earn-only items.)
- **Mastery/teaching challenges progress only in ranked-normalised contexts** — bought cooldown
  convenience cannot accelerate module unlocks.
- **The frame-true VFX contract binds every purchasable effect**: state-bound on/off with zero
  linger; occlusion envelope capped at the stock T2 trail; gameplay telegraphs (mine pulse, Decoy
  no-flame tell, EMP windup) always render above cosmetic VFX.
- **Reserved palette roles (gold = leader, red = damage) are banned in all purchasable cosmetics**
  (hue-distance validator); housings keep the module identifier glyph.
- **No purchasable pixel renders during race states** (Countdown → Finished).
- **Store-catalog lint at every release:** no SKU/bundle references cooldown or stat tables; no
  Salvage price on earn-only item IDs; no Salvage on paid pass tracks.
- Gifting: deferred until recorded-ghost sharing exists (no social graph, no server, no native
  App Store gifting — disproportionate fraud surface for a solo dev pre-U7).

---

## 7. Game Center (D8: iOS first at U5/U6 · Play Games parity at U7)

**Implementation pattern — the Nibwell precedent:** a separate platform-gated assembly
(define-gated asmdef) wrapping Apple.Core + Apple.GameKit; Core never references platform
services; counters feed from the U5 summary payload. Scaffolding + counters at U5, boards and
achievements live at U6. Google Play Games achieves parity at U7 alongside store prep.

**Leaderboards — 7 at launch, expanding per-arena.** All time boards submit seconds ×100
(Game Center integer scores); the bounty board submits raw count. Ghost-verified submission
(§4, U4 format) is the anti-cheat pairing — an unvalidated time board on iOS is a cheat-magnet.

| Board | Type | Notes |
|---|---|---|
| Arena 01 — Best Race | Classic | 3-lap total; the headline board |
| Arena 01 — Fastest Lap | Classic | |
| Fastest Lap — Light / Medium / Heavy | Classic ×3 | Per-hull boards make the sidegrade doctrine visible and triple every player's chance of ranking somewhere |
| Weekly Time Trial | Recurring weekly | Resets Monday; runs under ranked normalisation (`cooldownScale = 1.0`); the spine of the weekly retention loop |
| Weekly Bounty Hunter | Recurring weekly | Most bounty hits this week — a board for combat-first players that isn't lap time |
| Per-arena race + lap boards (career) | Classic, post-launch | Added per hull per arena as Redline Canyon / Anchor Station ship; top-N rows carry their recorded ghost — "race the #1 line through the Gantry Cross" with zero netcode |

Defer any ranked-rating board until a rating actually exists post-launch.

**Achievements — 20, named to the mechanics, not to grind.** Each maps to telemetry already
tracked (shots / hits / bountyHits / boostTime / energy events / leader flag).

| Category | Achievement | Trigger |
|---|---|---|
| Onboarding | Ignition | Finish first race |
| Onboarding | Checkered Flare | First win |
| Energy | Dry Tank | Hit 0 energy **before the final lap** and still win (aligned with the 'Cold Blood' Salvage bonus — same recovery story, §6.5) |
| Energy | Perpetual Motion | Win without dropping below the 25% red-line |
| Energy | Feathered Throttle | Win with <10s total boost |
| Energy | The Long Coast | Regen from <10% to full in one race |
| Energy | Pacifist Podium | Finish top-2 without firing |
| Energy | Shieldwright | Absorb 500 career damage while shielding |
| Hulls & shortcuts | Threading the Needle | Take the Light Gap 3× in one race |
| Hulls & shortcuts | Wrecking Ball | First Heavy Wall smash (depends on the #3 reset ruling) |
| Hulls & shortcuts | Triple Crown | Win with all 3 hulls |
| Abilities | Ghost in the Walls | Phase Shift through a wall (Sora) |
| Abilities | Blackout | EMP 3 racers at once (Vex) |
| Abilities | Static Thief | Steal 250 career energy (Kade) |
| Abilities | Look Over There | Draw 25 career shots onto Decoys (Nyx) |
| Bounty & comebacks | Kingslayer | First bounty hit on the marked leader |
| Bounty & comebacks | Wire-to-Wire | Lead every frame of a race (uses the per-frame leader re-evaluation) |
| Bounty & comebacks | The Long Game | Win without ever leading until the final lap |
| Bounty & comebacks | Paid in Full | 100 career bounty hits |
| Bounty & comebacks | Photo Finish | Win from P4 entering the final lap |

Post-launch achievements author against the 3D vocabulary: clear The Overrun at full boost, smash
the HeavyWall with rivals in draft range. Two mechanics dependencies flagged by the panel:
Wrecking Ball requires the #3 per-race reset ruling; Wire-to-Wire depends on the every-frame
leader re-evaluation (and interacts with the U4 hysteresis change — rule at the U4 gate).
Achievements reward mastery only — none may grant anything touching energy, cooldowns, or speed.

---

## 8. Amendments applied to BUILD.md

- **§1 (design lock):** arena roadmap note added — D5: one launch arena (Arena01, parity
  fixture) + two full-3D arenas as fast-follow content updates; environment modular kit designed
  now.
- **§1 (monetisation row):** "**No ads, ever**" appended per D7.
- **§7.2 (ShipController):** the D6 3D-ready directive added verbatim — per-sample track frames
  `{pos, tan, nrm, up}`, pose/steering/camera written against the frame, flat-degeneracy asserted
  in edit-mode tests; windowed progress-coherent nearest-sample query; fixed-tick 60 Hz Core.
- **§10 (U2 goal):** extended per D6 — ShipController built as a 3D track-frame system with flat
  Arena01 as the degenerate case; acceptance gate now includes the automated per-frame parity
  trace-diff against the HTML prototype at frozen tuning.
- **§7.6 (GhostSystem):** note added — fixed-tick 60 Hz Core simulation (built at U2) is a hard
  prerequisite for the recorded-ghost format; the format only replays deterministically under a
  fixed timestep.

---

*Review written 2026-07-09. Companion build spec: `Afterburn_UIEnvSpec.md`. U1 gate remains
open; none of this starts U2.*

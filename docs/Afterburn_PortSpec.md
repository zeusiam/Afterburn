# Afterburn — Prototype Port Spec (extracted 2026-07-09)
## Veratus Games · Project ALTERNATE · letter **A**, slot 7

> **Purpose:** the line-by-line extraction of `Afterburn_Prototype.html` that the Unity build
> executes against. `Afterburn_BUILD.md` is the contract; this file is the measured behaviour of
> the prototype — "the spec of feel". Where BUILD is ambiguous, this document is the tiebreaker.
> Every constant here was read out of the prototype source, not from memory.

**Conventions used throughout the prototype:** heading `h` is a yaw angle where
`forward = (sin h, 0, cos h)` (h = 0 → +Z). Track normal `nrm = normalize(cross(tangent, up))`;
for this loop **+nrm points toward the loop interior**. Ships ride at `y = 1.2`, bullets at
`y = 1.5`. Main loop: `dt` clamped to `min(dt, 0.05)`. Game only updates in state `'racing'`.

---

## 1. Track generation (`Track`)

**Control points — deterministic, NO noise, NO seed.** Fixed 14-entry radius-multiplier table:

```
R0 = 300
shape = [1.00, 0.94, 0.78, 0.74, 0.92, 1.06, 1.10, 0.86, 0.72, 0.82, 1.02, 1.08, 0.92, 0.84]
for i in 0..13:
    a = (i / 14) * 2π
    point[i] = ( cos(a) * R0 * shape[i],  0,  sin(a) * R0 * shape[i] )
```
Star-convex by construction (radius-varying polar loop → can never cusp). Dips = corners,
peaks = straights.

**Spline:** `CatmullRomCurve3(points, closed=true, type='catmullrom', tension=0.5)` — *uniform*
Catmull-Rom, tension 0.5 (NOT centripetal). `LEN = curve.getLength()` ≈ 1823 u (as frozen);
min corner radius ≈ 58 u (as frozen). Arc-length mapping uses a 200-division LUT (three.js
`arcLengthDivisions` default) with linear interpolation — reproduce for parity.

**Sampling:** `N = 700` samples at `t = i/N` via arc-length-parameterised `getPointAt` /
`getTangentAt` (tangent = numeric differentiation, delta 0.0001, clamped [0,1]). Each sample
caches `{pos, tan (normalized), nrm = cross(tan, up).normalized}`.

**Half-width:** `HALF = 17`. Road ribbon spans lateral −17…+17 at y = 0.02. Centre stripe spans
−0.6…+0.6 at y = 0.05.

**Walls:** extruded ribbons at lateral `±(HALF+0.4) = ±17.4`, y 0→3.2, double-sided. Walls are
**visual only** — collision is the analytic lateral clamp (§3). The LightGap has no visual hole,
only a collision allowance + pillar markers.

**Start/finish:** sample index 0 (world ≈ (300, 0, 0)). White torus gate. No physical trigger —
lap detection is fraction-based (§9).

**Checkpoints: NONE in the prototype** (BUILD §7.5 mentions checkpoint indices; prototype defines
zero — see Divergence #2).

**Shortcut zones** (both on the inside, `side = +1` = +normal):

| Zone | from idx | to idx | fraction | extra allowance | gating |
|---|---|---|---|---|---|
| LightGap | 140 | 168 | 0.20–0.24 | +20 | hull == light |
| HeavyWall | 427 | 462 | 0.61–0.66 | +22 | hull == heavy, **or zone already broken (any hull)** |

```
innerAllow(idx, hullKey):
    a = 0
    for each zone z:  # range test supports wrap: from<=to ? (from<=idx<=to) : (idx>=from || idx<=to)
        if idx in range:
            if z.type=='light' and hullKey=='light': a = max(a, z.extra)
            if z.type=='heavy' and (hullKey=='heavy' or z.broken): a = max(a, z.extra)
    return a
```
`breakHeavy(idx)`: when a **heavy** hull's lateral offset exceeds `HALF − hull.radius` inside the
heavy zone (plain range test, no wrap), sets `broken=true`, hides the slab, toast "WALL SMASHED".
Zones are NOT reset by `Game.start()` — broken persists across reruns (Divergence #3).

**Zone markers** (at midpoint sample `floor((from+to)/2)`):
- LightGap: 2× cylinder (r 1.1, h 7, 10 segs), color #37D0FF, emissive #0A3A55 @0.7; at
  `mid.pos + nrm*(HALF+2)*side ± tan*3.4`, y 3.5.
- HeavyWall: box slab 10×6×1.4, color #FF8A3C, emissive #5A2400 @0.6; at
  `mid.pos + nrm*(HALF+3)*side`, y 3, `lookAt(mid.pos)`. Hidden when broken (no shatter VFX).

## 2. Chase camera

- **FOV 62°** vertical, near 0.5, far 2000.
- Per frame:
```
want = playerPos + forward * (−16);  want.y = 9      // 16 u behind, 9 u up (world-up)
camera.position.lerp(want, min(1, dt * 4))            // exponential follow, rate 4/s
look = playerPos + forward * (+10);  look.y = 2       // look-ahead 10 u at y=2
camera.lookAt(look)                                   // rotation snaps — no rotational smoothing
```
- **No FOV kick, no shake, no boost camera effects.** Menu camera: pos (0,120,−160), lookAt (0,0,60).

## 3. Ship physics — `updatePlayer(dt, input)` exact order

`baseTop = 62 × hull.topSpeedMult` (Light 71.3 / Medium 62 / Heavy 54.56).
`maxEnergy = hull.maxEnergy × (energyMaxScale/100)`; starts full.

1. **Mode resolution** (mutual exclusion; EnergyCore is the arbiter). Reset flags, then:
   `if input.boost and energy>=1: boosting` — `elif input.shield and energy>=1: shielding`.
   **Boost strictly beats shield when both held** (in-code comment says "last pressed" — it lies;
   BUILD §7.2 documents the code. Divergence #6).
2. **Fire (discrete):** iff `input.fire` held (autofire) AND `!boosting` AND `!shielding` AND
   `fireTimer<=0` AND `energy>=20`: `energy−=20; fireTimer=0.28; firing=true; speed*=0.90 (once);
   Combat.fire(this, siphonArmed)`.
3. `fireTimer = max(0, fireTimer − dt)`.
4. **Drain/regen (exclusive):** boosting → `energy−=25·dt` (self-cancel at 0); elif shielding →
   `energy−=15·dt`; elif `!firing` → `energy += hull.regenPerSec × (regenScale/8) × dt`.
   Clamp [0, max]. Regen is skipped on the frame a shot fires.
5. **Speed cap:** `cap = baseTop`; boosting → ×1.40; elif shielding → ×0.80; if spinout>0 →
   `cap ×= 0.55` (stacks multiplicatively on top — Divergence #12).
6. **Longitudinal:** `thrust: speed += 55·dt`; `brake: speed −= 80·dt`; drag `speed −= speed·0.6·dt`;
   clamp [0, cap]. (No boost acceleration bonus — boost only raises the cap.)
7. **Steering:** `steer = left−right`; `bite = min(1, speed/25 + 0.2)`;
   `heading += steer × 2.4 × dt × bite`.
8. **Integrate + walls:** `np = pos + forward·speed·dt`; `resolveWalls(np)`; y forced 1.2;
   `mesh.rotation.y = heading`.
9. Timers: spinout −= dt; intangible −= dt (0.4 opacity while phased); ability cd −= dt.
10. Thruster: `scale = (boosting?1.8:1) × (0.6 + speed/baseTop×0.6)`; color #FFE14D boosting else #FF7A3C.

**`resolveWalls(np)`:**
```
if intangible > 0: return                    // Sora skips walls entirely
i = nearest(np);  s = samples[i]
off = dot(np − s.pos, s.nrm)                 // signed lateral, + = inside
limInner =  HALF + innerAllow(i, hull) − hull.radius
limOuter = −(HALF − hull.radius)
if off > HALF − hull.radius and hull == heavy: breakHeavy(i)
clamped = clamp(off, limOuter, limInner)
if clamped != off:
    speed *= 0.92                            // scrape
    dh = shortestSignedAngle(atan2(s.tan.x, s.tan.z) − heading)
    heading += dh * 0.18                     // slide nudge
    np = s.pos + s.nrm * clamped  (xz only)
```

## 4. Energy core

- Per-frame order: mode resolution → discrete fire spend → drain OR regen (never both) → clamp.
- Priority: **boost > shield** (held); fire blocked while either is active; regen only when none
  acted this frame.
- Fire gating: 0.28 s tap cooldown, cost check `energy ≥ 20`, hold = autofire at 0.28 s cadence
  (Divergence #13).
- Boost/shield activation gate: `energy ≥ 1`; mode self-cancels when drained to 0.
- Abilities cost **zero energy** — cooldown-gated only.
- Tuning sliders write live; `energy.max` recomputes player max without refilling.

## 5. Spawn layout

- `place()`: `samples[0].pos + samples[0].nrm × (lane × 5)`, y 1.2, heading = along +tangent.
- Lanes: **player = 0**; ghosts = **−1, +1, +2**.
- Roster: `[heavy+Kade, lane −1, tint #FF5A4D] [light+Sora, +1, #38F5C9] [medium+Nyx, +2, #FFD23F]`
  (tints override hull colors).
- Ghost curve params at start: `ai.t = 0.994 / 0.988 / 0.982`, `laps = 0` → see Divergence #5.
- Ghost lane spacing while racing is **×4** (vs ×5 at placement).
- **No countdown** — live the frame after START (Divergence #1).

## 6. Ghost AI — synthetic curve-follower (no physics, no walls)

State: `{t, skill: 0.80+rand·0.10, aggro: rand}` re-rolled each race.
1. Straight test: `curv = 1 − |tan[idx]·tan[(idx+8)%700]| < 0.02` (≈20.8 u lookahead).
2. Boost iff `energy > 0.35·max` AND straight: `cap = baseTop·1.40·skill; energy −= 25·dt`;
   else coast + regen. Then `cap ×= skill` **again** → boosting cap = `baseTop·1.4·skill²`
   (Divergence #4).
3. Speed approach: `speed += (cap − speed) × min(1, 3·dt)` (no thrust/drag).
4. `t += speed·dt/LEN`, wrap at 1.
5. Fire iff `aggro > 0.4` AND leader exists AND ≠ self AND refire ≤ 0 AND dist < 55 AND
   `energy > 20`; refire = 1.4+rand() s; target = `decoy ?? bountyLeader` (range check only);
   bullets are unaimed — straight along ghost heading (Divergence #9).
6. Pose: `curve.getPointAt(t) + nrm·(lane·4)`, y 1.2; heading = atan2(tan.x, tan.z). Rail-locked;
   never shields, brakes, or uses abilities. Can be hit/spun/EMP'd/siphoned.
7. Progress: `laps + t`; forward wrap only. Finish → freeze in place.
8. Thruster scale 1.6 boosting / 0.9 not.

## 7. Combat

- **Pool:** 40 bullets pre-created (sphere r 0.6, unlit #FF4D6D), y 1.5. Pool-dry = shot silently dropped.
- **Spawn:** `pos = racerPos + dir·3` (y 1.5); `vel = dir × (170 + 0.4·shooterSpeed)`; `ttl = 1.6 s`.
- **Hit:** racer ≠ owner, not finished, not intangible; `dist < hull.radius + 1.6`. First hit kills bullet.
- **onHit(from, target, siphon):**
  ```
  dmg = 25;  if target.shielding: dmg *= 0.40
  target.energy = max(0, energy − dmg);  target.spinout = max(spinout, 0.7)
  reward = 8;  if target == bountyLeader: reward *= 2.0   // → 16
  from.energy = min(max, energy + reward)                  // non-leader hits still pay 8 (Div #8)
  if siphon: target.energy −= 25 (floor 0); from.energy += 25 (flat — can mint energy, Div #7)
  ```
- **Abilities** (no energy cost; cd = pilot.cd × abilityCooldownScale):
  - **Vex — EMP (18 s):** all racers within 70 lose 30. Toast #37D0FF.
  - **Sora — Phase (15 s):** intangible 1.2 s — skips walls AND hits; 0.4 opacity. Toast #9D7BFF.
  - **Kade — Siphon (20 s):** arms next hit, flat −25/+25. Toast "SIPHON ARMED" #38F5C9.
  - **Nyx — Decoy (22 s):** static wireframe octahedron r 2 #FFD23F at owner pos, ttl 3 s, spins
    4 rad/s; replaces leader as ghost fire-range target; cannot itself be hit; re-cast replaces.

## 8. Bounty / Intel

- Leader = strictly greatest unwrapped `progress`, re-evaluated every frame, no hysteresis;
  includes finished racers; ties keep earlier racer (player is seed).
- Player leads → "★ BOUNTY" HUD tag; minimap leader dot gold + ring.
- Intel: shown ONLY when player is NOT leader — leader's live energy bar. Leader sees nothing.
- Payout in onHit (§7): 8 base / 16 vs leader.

## 9. Race flow

- **No countdown** (Divergence #1). Laps default 3.
- Player lap counting per frame: `frac = nearest(pos)/700`; `d = frac − prevFrac`;
  `d < −0.5 → laps++`; `d > 0.5 → laps−−` (anti-cheese, player only);
  `progress = laps + frac`; finish at `progress ≥ raceLaps`.
- Race ends the frame the **player** finishes. Earlier-finishing ghosts freeze with their stats;
  unfinished ghosts ranked by live progress. Standings = progress descending.
- HUD: position n/4; lap `min(raceLaps, floor(progress)+1)`; time m:ss;
  speed display `round(speed × 3.0) km/h` (display-only, Div #14); energy bar red below 25%;
  ability fill %; toast lifetime 1.1 s.
- Player stats: `{shots, hits, bountyHits, boostTime}` + place, time, hull/pilot.

## 10. Scene dressing (greybox inventory)

- Background #05070F. Fog linear 220→620, same color. `pixelRatio ≤ 2`, antialias on.
- HemisphereLight(sky #9FC4FF, ground #0A0F1E, 0.9); DirectionalLight(#FFFFFF, 0.9) at (120,200,80). No shadows.
- Starfield: 1400 points, `r = 700+rand·500`, uniform sphere angles, `y = |r·cos b|·0.6 + 40`;
  color #6F8BD0, size 2, no attenuation.
- Ground grid: 1600 u, 80 divisions, centre #11305F, grid #0A1C3A, y −0.4.
- Road: ±17 @ y 0.02, #12203C rough 0.9 metal 0.1. Stripe: ±0.6 @ y 0.05, #1C2F57 emissive #0A1830.
- Walls: ±17.4, h 3.2, #2B4D8F emissive #0D2350 rough 0.5, double-sided.
- Start gate: torus r 18, tube 0.5 (8×24 segs), white emissive 0.6, at samples[0], **y = 15**,
  ring plane perpendicular to travel.
- Ship: cone (r 1.5, h 5, 4 segs) rot x π/2, z π/4, hull color emissive 0.25 metal 0.4 rough 0.4;
  wing box 5×0.4×1.6 #000223 metal 0.5 at z −1.2; thruster sphere r 0.9 unlit #FF7A3C at z −2.6.
- Decoy: wireframe octahedron r 2 #FFD23F. Bullets: sphere r 0.6 #FF4D6D.
- Minimap: 150×150, world scale 0.14, origin centre; outline 60 segments rgba(120,170,255,.5);
  player #38F5C9 r4, leader #FFD23F r4 + ring r6, others #FF5A4D r3.

---

## DIVERGENCES & quirks vs BUILD.md §5–6 (rulings needed at U2 gate)

All §5 tables and §6 feel constants **match the prototype code exactly** — verified line by line.
The differences:

1. **No countdown** in prototype; BUILD §7.8 specifies one. Duration is a port decision.
2. **No checkpoints** in prototype; BUILD §7.5 lists them in `TrackDefinition`. Progress integrity
   relies on the ±0.5 wrap test alone.
3. **Broken HeavyWall opens for ALL hulls** and persists across "one more run" (reset only on page
   reload). BUILD says allowance only for matching `gateAccess` (or Sora phasing — which in code
   skips walls entirely rather than using the allowance). Likely unintended; needs a ruling.
4. **Ghost boost applies skill twice** → boosting cap = `baseTop·1.40·skill²` (≈0.64–0.81×1.4).
   BUILD documents a single 0.80–0.90 band.
5. **Ghosts start with ~1 free lap of progress** (`ai.t ≈ 0.99`, laps 0, first wrap → laps 1).
   Player starts ranked P4; ghosts finish after ~2.01 laps of travel. Almost certainly a bug —
   ruling: reproduce verbatim or seed ghost `laps = −1`.
6. **"Last pressed wins" comment is false** — code is strict boost-over-shield. Port the code.
7. **Siphon can mint energy** — flat −25/+25 regardless of victim's remaining pool (+ dead code
   computing an unused capped value).
8. **Every hit pays the shooter 8 energy** — not just leader hits (leader = 16).
9. **Ghost fire preconditions** beyond range 55: `aggro > 0.4` (~60 % of ghosts ever shoot),
   refire 1.4–2.4 s, `energy > 20` (strict), decoy substitutes in range check, shots unaimed.
10. **Ghost movement constants not in frozen tables:** boost threshold 0.35·max; straight test
    `1−dot(tan,tan+8) < 0.02`; speed approach rate 3/s; racing lane spacing ×4 vs placement ×5.
11. **Camera numbers absent from BUILD:** −16 back / +9 up, lerp rate 4/s (frame-rate-dependent),
    look +10 fwd @ y 2, FOV 62, no boost kick.
12. **Spinout ×0.55 stacks multiplicatively** with boost/shield cap (boost-while-spun-out is legal).
13. **Fire is held-input autofire** at 0.28 s cadence, not press-per-shot.
14. **Speed HUD = `round(speed × 3.0)` km/h** — display-only conversion.
15. **Standings double-sort quirk** — net effect is plain progress-descending; port it simply.
16. **Ship cone Euler-order skew** — the prototype sets `body.rotation.x=π/2; body.rotation.z=π/4`
    under three.js default XYZ Euler order, which applies the z-roll about the cone's *side* axis
    first: the rendered nose points 45° off the ship's travel direction (verified numerically).
    Almost certainly an authoring bug. The Unity port straightens the nose (apex +z, base rolled
    45°) — ruling due at the U2 side-by-side gate: keep the straightened nose (recommended) or
    reproduce the skew verbatim.

*Extracted 2026-07-09 from `Afterburn_Prototype.html` (Track lines 317–424, Combat 426–467,
Racer/physics 469–651, RaceDirector 653–734, HUD 758–829).*

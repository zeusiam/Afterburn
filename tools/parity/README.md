# Afterburn parity harness (`tools/parity/`)

Executes the **actual physics code** from `docs/Afterburn_Prototype.html` (the
Three.js r128 browser prototype) under Node and emits `reference_trace.json` —
the ground-truth trace the Unity C# port (`Afterburn.Core`) is diffed against.

Nothing physics-relevant is retyped: `extract.js` slices the `T` tuning store,
`HULLS`/`PILOTS`, the `Track` IIFE and the `Racer` class **verbatim** out of the
HTML and runs them on a THREE shim whose math paths are ported line-for-line
from three.js r128.

## Regenerate

```sh
node gen-trace.js
```

No npm dependencies — Node built-ins only (tested on Node 24). The generator
re-runs `extract.js` first, so the trace can never drift from the HTML source;
if the prototype changes, regenerating produces the new ground truth. Exit code
is non-zero if any sanity check fails.

Files:

| file | role |
|---|---|
| `three-shim.js` | THREE r128 shim: real `Vector3` + `CatmullRomCurve3`, inert scene-graph stubs |
| `extract.js` | slices the prototype source → writes `prototype-slices.js` |
| `prototype-slices.js` | **auto-generated, do not edit** — verbatim prototype code + harness globals |
| `gen-trace.js` | builds Track, steps one player Racer 3600 × (1/60 s), writes the trace |
| `reference_trace.json` | header + 3600 frames, one frame per line for diffability |

## Simulation & trace format

One `Racer` — hull `medium`, pilot `vex`, `isPlayer=true`, `_lane=0` —
`place()`d at the start line, then `updatePlayer(dt, input)` stepped at exactly
`dt = 1/60` for 3600 frames (60 s). Double precision throughout; **only the
emitted values are rounded** (6 decimals). Intermediate state is never rounded.

Header: `{trackLen, sample0:{pos,tan}, baseTop, maxEnergy, hull, dt, frames}`.

Per frame (recorded *after* the step):
`{f, in:{t,b,l,r,bo,fi,sh}, px, pz, h, s, e, fl}` where `px/pz` = mesh position,
`h` = heading (radians, unwrapped), `s` = speed, `e` = energy, and `fl` is a
bitfield: `1` boosting, `2` shielding, `4` fired this frame.

## Input schedule (evaluated BEFORE each step, t = f/60)

| input | active |
|---|---|
| thrust | t ∈ [0,20) except [8,9) |
| left | [3,5) ∪ ([9,15) with ⌊t⌋ odd) |
| right | [6.5,8) ∪ ([9,15) with ⌊t⌋ even) |
| boost | [5,6.5) ∪ [15,18) |
| fire | [6.5,8) |
| shield | [8,9) ∪ [18,19) |
| brake | [8,9) |
| abilityEdge | never |

Coverage: cruise ramp + drag/cap; cornering + steering bite; boost drain (exact:
e = 62.5 at t = 6.5); autofire cadence (0.28 s timer → 17-frame period; 3 shots
land, the 4th is blocked by `canSpend`); fire speed-dip; brake + shield with the
shield `canSpend(1)` cutout sawtooth; slalom wall scrapes (0.92 speed cut +
heading nudge + cross-section snap); a long boost that **exhausts the pool**
(~t = 16.9 s) and the boost low-energy sawtooth; shield-after-boost; coast regen
back to full in [19,20); dead-input decay to standstill for t ≥ 20.

### Known behavior notes (real code, verified — the Unity port must reproduce these)

- **Wall pinning**: with no steering during [0,3), the ship runs straight while
  the track bends and contacts the outer wall at **f = 102 (t = 1.70 s)**.
  `resolveWalls` snaps position to the *nearest sample's cross-section*
  (`corrected = s.pos + nrm·clampedOff`), discarding along-track motion, so a
  pinned ship freezes in place at the 0.92-scrape equilibrium speed ≈ 9.36
  until steering frees it. This is verbatim prototype behavior, not a harness
  artifact.
- Consequently the schedule spec's estimates "speed ≈ 55–62 at t = 3" and
  "[15,18) boost approaches 86.8" do not occur: the cruise peaks at 58.19
  (f = 101) *before* wall contact, and both boost windows happen while pinned
  (boost still drains energy — drain is speed-independent).
- `fireTimer` quirk: the constructor never initialises `fireTimer`, so the
  verbatim `this.fireTimer <= 0` check is false (`undefined <= 0`) until the
  decay line seeds it to 0 on frame 0. Invisible in this trace (fire starts at
  t = 6.5) but ports should match the seeding, not "fix" it.

## Shim fidelity notes (`three-shim.js`)

Ported line-for-line from three.js r128, double precision:

- `Vector3`: `clone, copy, set, add, sub, subVectors, addScaledVector,
  multiplyScalar, normalize, length, lengthSq, distanceTo, dot, crossVectors,
  lerp` — including r128's exact `normalize() = divideScalar(length()||1)` and
  `divideScalar = multiplyScalar(1/s)` (reciprocal multiply, matters for ULPs).
- `CatmullRomCurve3` (closed, uniform `'catmullrom'`, tension 0.5): the r128
  `CubicPoly` algorithm — `initCatmullRom` tangents `t0 = tension·(x2−x0)`,
  `t1 = tension·(x3−x1)`; the closed-curve `intPoint` wrap (`intPoint += l` when
  `intPoint ≤ 0`, so t = 0 evaluates the p13→p0 segment via wrap indices);
  module-scoped shared `CubicPoly`/`tmp` instances as in r128.
- Arc-length machinery from r128 `Curve`: `getLengths(200)` cumulative Euclidean
  LUT (cached), binary-search `getUtoTmapping`, `getPointAt/getTangentAt` via
  the LUT, numeric tangent delta 1e-4 clamped to [0,1],
  `getLength() = lengths[200]`.
- Everything else (geometries, materials, `Mesh`, `Group`, `GridHelper`,
  `BufferGeometry`…) is inert — just enough surface for the Track IIFE's mesh
  building to run. Object3D stubs carry a **real `Vector3`** as `.position`
  because Track's `gateAt()`/shortcut code calls `.copy().addScaledVector()` on
  mesh positions.

Validation: `Track.LEN = 1822.735409` vs the expected ≈ 1823 (±20) from the real
prototype — agreement to 0.3 units end-to-end through getPoint → LUT → getLength.

## Stub audit trail (everything beyond the sliced prototype code)

Harness globals (in generated `prototype-slices.js`): `scene {add, remove}`,
`HUD {toast}`, `Combat {fire}` (no-ops), and
`Game {decoy, bountyLeader, spawnDecoy, time, state, stats}`.

Stubs that needed **more** than the bare spec, and why:

1. `Game.stats` — the sliced `updatePlayer` reads `Game.stats.shots++` and
   `Game.stats.boostTime += dt` (not a bare `stats` global), so the stats object
   is attached to `Game` as well as exported standalone.
2. `mesh.children: []` — `tickCommon()` runs `this.mesh.children.forEach(...)`
   **every frame**, not only when intangible.
3. `mesh.traverse()` no-op — referenced in `tickCommon()`'s intangible branch
   (never hit in this trace since `abilityEdge` is always false; defensive).
4. `thruster.scale.setScalar` **returns `this`** and `scale` also carries
   `multiplyScalar` — `updatePlayer` chains
   `.scale.setScalar(...).multiplyScalar(...)`; a bare `setScalar(){}` would
   throw.
5. Shim graphics stubs actually exercised by the Track IIFE at require time:
   `BufferGeometry.setAttribute/computeVertexNormals`, `BufferAttribute`,
   `Mesh` (real-Vector3 `.position`, `lookAt`, `rotateX`, `visible`),
   `Group.add(...)` (variadic — `group.add(wOut, wIn)`), `GridHelper`
   (`.position.y=`), `TorusGeometry`, `CylinderGeometry`, `BoxGeometry`,
   `MeshStandardMaterial`, and the `THREE.DoubleSide` constant.
6. `document` — **not** referenced by any sliced section; no stub needed.

## Determinism

`Racer`'s constructor rolls `this.ai = {t: Math.random()…}` **only for
non-players**; the player path is random-free. `gen-trace.js` proves it by
trapping `Math.random` before the slices load (the Track IIFE builds at require
time) through all 3600 steps and asserting **0 calls**.

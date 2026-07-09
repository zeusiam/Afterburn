'use strict';
/*
 * gen-trace.js — executes the ACTUAL Afterburn prototype physics (sliced by
 * extract.js, running on three-shim.js) and emits reference_trace.json: the
 * ground-truth trace the Unity C# port is diffed against.
 *
 * Run: node gen-trace.js     (re-slices the prototype first, so the trace can
 *                             never drift from docs/Afterburn_Prototype.html)
 *
 * Simulation: one player Racer (hull 'medium', pilot 'vex', _lane 0), place(),
 * then 3600 steps of updatePlayer(dt, input) at EXACTLY dt = 1/60 (60 s).
 * Double precision throughout; only the EMITTED values are rounded (6 dp).
 */

const fs = require('fs');
const path = require('path');

/* Determinism audit: trap Math.random BEFORE the slices load (the Track IIFE
 * builds at require time) and keep the trap through all 3600 steps. The Racer
 * constructor rolls this.ai only for non-players; the player path must be
 * random-free. Any call in the physics path is counted and reported. */
let randCalls = 0;
const _origRandom = Math.random;
Math.random = function () { randCalls++; return _origRandom(); };

require('./extract.js').extract();                  // regenerate slices from the HTML
const P = require('./prototype-slices.js');
const { Track, Racer } = P;

const DT = 1 / 60;
const FRAMES = 3600;

/* ------------------------- Input schedule ------------------------------- */
// Evaluated BEFORE each step, t = f/60. All window edges are multiples of half
// a second, so every edge lands exactly on a frame (f/60 is exact there).
function inputAt(f) {
  const t = f / 60;
  const win = (a, b) => t >= a && t < b;
  const slalom = win(9, 15);
  const sec = Math.floor(t);
  return {
    thrust: win(0, 20) && !win(8, 9),
    left: win(3, 5) || (slalom && sec % 2 === 1),
    right: win(6.5, 8) || (slalom && sec % 2 === 0),
    boost: win(5, 6.5) || win(15, 18),
    fire: win(6.5, 8),
    shield: win(8, 9) || win(18, 19),
    brake: win(8, 9),
    abilityEdge: false,
  };
}

/* ----------------------------- Simulate --------------------------------- */
const racer = new Racer('medium', 'vex', true);
racer._lane = 0;
racer.place();

if (racer.ai !== null) throw new Error('player Racer unexpectedly has an AI block');

const r6 = (x) => Number(x.toFixed(6));
const s0 = Track.samples[0];
const header = {
  trackLen: r6(Track.LEN),
  sample0: {
    pos: [r6(s0.pos.x), r6(s0.pos.y), r6(s0.pos.z)],
    tan: [r6(s0.tan.x), r6(s0.tan.y), r6(s0.tan.z)],
  },
  baseTop: r6(racer.baseTop),
  maxEnergy: r6(racer.maxEnergy),
  hull: 'medium',
  dt: '1/60',
  frames: FRAMES,
};

const frames = [];       // emitted (rounded) records
const rawS = new Float64Array(FRAMES);   // raw copies for sanity math only
const rawE = new Float64Array(FRAMES);
const inputs = [];

for (let f = 0; f < FRAMES; f++) {
  const inp = inputAt(f);
  racer.updatePlayer(DT, inp);           // the prototype's real code path

  rawS[f] = racer.speed;
  rawE[f] = racer.energy;
  inputs.push(inp);

  const fl = (racer.boosting ? 1 : 0) | (racer.shielding ? 2 : 0) | (racer.firing ? 4 : 0);
  frames.push({
    f,
    in: {
      t: inp.thrust ? 1 : 0, b: inp.brake ? 1 : 0, l: inp.left ? 1 : 0, r: inp.right ? 1 : 0,
      bo: inp.boost ? 1 : 0, fi: inp.fire ? 1 : 0, sh: inp.shield ? 1 : 0,
    },
    px: r6(racer.mesh.position.x),
    pz: r6(racer.mesh.position.z),
    h: r6(racer.heading),
    s: r6(racer.speed),
    e: r6(racer.energy),
    fl,
  });
}

/* ------------------------------ Emit JSON ------------------------------- */
// Compact, one frame per line, for line-diffability against the Unity trace.
const OUT = path.join(__dirname, 'reference_trace.json');
const json =
  '{\n"header": ' + JSON.stringify(header) + ',\n"frames": [\n' +
  frames.map((fr) => JSON.stringify(fr)).join(',\n') +
  '\n]\n}\n';
fs.writeFileSync(OUT, json);

/* --------------------------- Sanity checks ------------------------------ */
// HARD checks assert harness integrity (they'd catch a shim/slicing bug).
// NOTEs report where the schedule-spec's a-priori estimates diverge from the
// prototype's ACTUAL behavior — the trace is ground truth; the estimates
// assumed a wall-free cruise, but with no steering during [0,3) the real code
// pins the ship against the outer wall at t ~= 1.70 s (resolveWalls snaps the
// position to the nearest sample's cross-section, so a pinned ship holds the
// 0.92-scrape equilibrium speed ~= 9.36 until steering input frees it).
const failures = [];
function check(name, ok, detail) {
  console.log(`${ok ? 'PASS' : 'FAIL'}  ${name}  ${detail}`);
  if (!ok) failures.push(name);
}
function note(msg) { console.log(`NOTE  ${msg}`); }

// 1. Track length (validates the CatmullRom + arc-length shim end to end)
check('trackLen ~= 1823 +/- 20', Math.abs(Track.LEN - 1823) <= 20,
  `LEN = ${Track.LEN.toFixed(6)}`);

// 2. Cruise speed. Spec estimate for t=3 was 55-62 (assumed wall-free cruise);
// the real code hits the outer wall first. Assert the wall-free portion of the
// ramp peaked in a sane band, and report both numbers.
let firstScrapeF = -1;
for (let f = 1; f < FRAMES; f++) {
  if (inputs[f].thrust && !(frames[f].fl & 4) && rawS[f] < 0.95 * rawS[f - 1]) { firstScrapeF = f; break; }
}
let preContactPeak = 0, preContactPeakF = 0;
const cruiseEnd = firstScrapeF > 0 ? firstScrapeF : 180;
for (let f = 0; f < cruiseEnd; f++) if (rawS[f] > preContactPeak) { preContactPeak = rawS[f]; preContactPeakF = f; }
check('pre-contact cruise peak in [50, 62]', preContactPeak >= 50 && preContactPeak <= 62 + 1e-9,
  `peak s = ${preContactPeak.toFixed(6)} at f = ${preContactPeakF}; first wall contact f = ${firstScrapeF} (t = ${(firstScrapeF / 60).toFixed(3)} s)`);
note(`speed @ t=3 (f=180) = ${rawS[180].toFixed(6)} — spec estimated 55-62 assuming no wall contact before t=3; ` +
  `actual: ship wall-pinned from t ~= ${(firstScrapeF / 60).toFixed(2)} s at scrape-equilibrium speed`);

// 3. Energy after 1.5 s of boost from full (last boost frame, f = 389).
// Boost drains 25/s regardless of speed, so this holds even while wall-pinned.
check('energy @ t=6.5 ~= 62.5 +/- 2', Math.abs(rawE[389] - 62.5) <= 2,
  `e = ${rawE[389].toFixed(6)} (expected 100 - 25*1.5 = 62.5)`);

// 4. Energy low right after the fire window (fire ends t=8, shield covers [8,9)):
// assert a deep local minimum (< 2 energy) inside t in [6.5, 9.5].
let postFireMin = Infinity, postFireMinF = -1;
for (let f = 390; f <= 570; f++) if (rawE[f] < postFireMin) { postFireMin = rawE[f]; postFireMinF = f; }
check('energy low right after fire window (< 2 within t in [6.5, 9.5])', postFireMin < 2,
  `local min e = ${postFireMin.toFixed(6)} at f = ${postFireMinF} (t = ${(postFireMinF / 60).toFixed(3)} s)`);
let minE = Infinity, minEf = -1;
for (let f = 0; f < FRAMES; f++) if (rawE[f] < minE) { minE = rawE[f]; minEf = f; }
if (minEf !== postFireMinF) {
  note(`global energy min = ${minE.toFixed(6)} at f = ${minEf} (t = ${(minEf / 60).toFixed(3)} s) — ` +
    'the long boost window [15,18) exhausts the pool slightly deeper than the post-fire low');
}

// 5. Speed cap invariant + max speed. Spec expected the [15,18) boost to
// approach 86.8 (= 62 * 1.4); actually both boost windows occur while the ship
// is wall-pinned, so the boost cap is never approached. The cap must still
// never be exceeded.
let maxS = -Infinity, maxSf = -1;
for (let f = 0; f < FRAMES; f++) if (rawS[f] > maxS) { maxS = rawS[f]; maxSf = f; }
let maxBoostS = -Infinity, maxBoostSf = -1;
for (let f = 900; f < 1080; f++) if (rawS[f] > maxBoostS) { maxBoostS = rawS[f]; maxBoostSf = f; }
check('speed never exceeds boost cap 86.8', maxS <= 62 * 1.4 + 1e-9,
  `max s = ${maxS.toFixed(6)} at f = ${maxSf} (t = ${(maxSf / 60).toFixed(3)} s)`);
note(`max speed during boost window [15,18) = ${maxBoostS.toFixed(6)} at f = ${maxBoostSf} — spec expected ` +
  'approach to 86.8, but the ship is wall-pinned there (boost still drains: pool empties at t ~= 16.9 s)');

// 6. Wall contacts during slalom (t in [9,15)): speed drops > 5% in one frame
// while thrusting without a shot fired that frame.
let scrapes = 0;
for (let f = 541; f < 900; f++) {
  if (inputs[f].thrust && !(frames[f].fl & 4) && rawS[f] < 0.95 * rawS[f - 1]) scrapes++;
}
check('wall contact during slalom (>= 1 scrape frame)', scrapes >= 1,
  `scrape frames in [9,15) = ${scrapes}`);

// 7. Determinism: no Math.random anywhere in the player physics path
check('determinism: Math.random calls = 0', randCalls === 0,
  `calls = ${randCalls} (Racer ctor rolls this.ai only when !isPlayer)`);

Math.random = _origRandom;

console.log(`\nwrote ${OUT} (${FRAMES} frames, ${(fs.statSync(OUT).size / 1024).toFixed(0)} KiB)`);
if (failures.length) {
  console.error('\nSANITY FAILURES: ' + failures.join(' | '));
  process.exit(1);
}

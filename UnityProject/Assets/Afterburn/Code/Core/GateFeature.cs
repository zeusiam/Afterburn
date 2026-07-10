using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// D15 (owner ruling, 2026-07-10): gates as TRACK FEATURES — fixed, deterministic, identical
    /// for every racer every lap. NOT pickups (no randomness, nothing consumed, nothing granted
    /// to a pool): SpeedBoost/WarpSurge give SPEED only (never energy — the slipstream precedent);
    /// Blocker only DRAINS (D14 hazard family). Ranked-legal by determinism; recorded ghosts stay
    /// replayable because features live in TrackDefinition.
    /// </summary>
    public enum GateFeatureType
    {
        // ---- Collectables (ring gates, full-opening fields) --------------------
        /// <summary>Fly-through speed surge — cap raise + impulse, short.</summary>
        SpeedBoost = 0,

        /// <summary>The rare, violent version — "warp speed". Speed only, bigger, longer.</summary>
        WarpSurge = 1,

        /// <summary>Legacy harmful ring (superseded by the richer obstacles; kept for data compat).</summary>
        Blocker = 2,

        /// <summary>Shield gate: grants a BARRIER charge — absorbs the next obstacle/projectile hit.</summary>
        Barrier = 3,

        /// <summary>Photon tractor: building acceleration surge — reels you toward the ship ahead.</summary>
        Photon = 4,

        /// <summary>Overdrive: raw thrust-acceleration buff (distinct feel from cap-raising boost).</summary>
        Overdrive = 5,

        // ---- Obstacles (partial hazard fields inside the ring opening) ---------
        /// <summary>Electricity: stalls all input for stallDuration. Deflected by barrier/active shield.</summary>
        Electric = 6,

        /// <summary>Reverse warp: dumps speed + shoves the ship backwards. Deflected by barrier/shield.</summary>
        ReverseWarp = 7,

        /// <summary>Shredder: damage + a piece of your ship visibly tears off + clipped-wing handling. Deflectable.</summary>
        Shredder = 8,

        // ---- Small gates (floating markers, not bridges) -----------------------
        /// <summary>Mine: damage + spinout. Deflected by barrier/shield.</summary>
        Mine = 9,

        /// <summary>Armor: small barrier pickup (same as the Shield ring, smaller telegraph).</summary>
        Armor = 10,
    }

    /// <summary>One gate feature on the track (authored in TrackDefinition, baked by TrackSystem).</summary>
    [Serializable]
    public sealed class GateFeature
    {
        public GateFeatureType type = GateFeatureType.SpeedBoost;

        [Tooltip("Track fraction [0..1) where the gate stands.")]
        public float fraction;

        [Tooltip("Lateral centre of the gate opening (0 = centreline, + = inside).")]
        public float lateralOffset;

        [Tooltip("Half-width of the trigger opening (u). 8 ≈ half the track; blockers are narrower.")]
        public float halfSpan = 8f;

        [Tooltip("SpeedBoost/WarpSurge: cap multiplier while the surge lasts (speed only, never energy).")]
        public float surgeCapMult = 1.25f;

        [Tooltip("SpeedBoost/WarpSurge: one-time speed impulse on entry (clamped to the surged cap).")]
        public float surgeImpulse = 12f;

        [Tooltip("SpeedBoost/WarpSurge: surge duration in seconds.")]
        public float surgeDuration = 1.2f;

        [Tooltip("Obstacles/Mine: energy drained on contact (D14 hazard family — drains only).")]
        public float blockerDamage = 15f;

        [Tooltip("Obstacles: ×speed on contact (ReverseWarp uses this as the dump, e.g. 0.05).")]
        public float blockerSpeedMult = 0.85f;

        [Tooltip("Barrier/Armor: charges granted (each absorbs one obstacle or projectile hit).")]
        public int barrierCharges = 1;

        [Tooltip("Electric: input-stall duration in seconds.")]
        public float stallDuration = 0.3f;

        [Tooltip("Shredder: ×turnRate while clipped.")]
        public float debuffTurnMult = 0.85f;

        [Tooltip("Shredder: clipped-wing duration in seconds.")]
        public float debuffDuration = 8f;

        /// <summary>Collectable ring types touch anywhere in the opening; obstacles are partial fields.</summary>
        public bool IsObstacle => type is GateFeatureType.Blocker or GateFeatureType.Electric
            or GateFeatureType.ReverseWarp or GateFeatureType.Shredder or GateFeatureType.Mine;

        /// <summary>Small floating markers rather than track-wrapping bridges.</summary>
        public bool IsSmallGate => type is GateFeatureType.Mine or GateFeatureType.Armor;
    }

    /// <summary>
    /// Detects racers crossing gate features and applies effects. One trigger per gate pass —
    /// re-arms when the racer leaves the gate's index window. Synthetic ghosts are exempt in v1
    /// (rail-locked — they can't steer for or around gates; the skill band absorbs it; flagged
    /// for the U4 re-tune). Replayed ghosts run ShipController and get gates deterministically.
    /// </summary>
    public sealed class GateFeatureSystem
    {
        private sealed class Baked
        {
            public GateFeature Feature = null!;
            public int Index;                    // sample index of the gate plane
            public int Window;                   // ± samples counted as "at the gate"
        }

        private readonly TrackSystem _track;
        private readonly List<Baked> _gates = new();
        private readonly Dictionary<ShipController, bool[]> _inside = new();

        /// <summary>(racer, feature, deflected) — fired once per pass. HUD/VFX hook.</summary>
        public event Action<ShipController, GateFeature, bool>? OnGateTriggered;

        public GateFeatureSystem(TrackSystem track, TrackDefinition def)
        {
            _track = track ?? throw new ArgumentNullException(nameof(track));
            int n = track.SampleCount;
            foreach (GateFeature f in def.gateFeatures ?? Array.Empty<GateFeature>())
            {
                _gates.Add(new Baked
                {
                    Feature = f,
                    Index = Mathf.FloorToInt(Mathf.Repeat(f.fraction, 1f) * n),
                    Window = 3,                   // ±3 samples ≈ ±7.8 u — generous at 60 Hz
                });
            }
        }

        public int GateCount => _gates.Count;

        public void Register(ShipController racer)
        {
            _inside[racer] = new bool[_gates.Count];
        }

        /// <summary>Per fixed tick, after the racer stepped.</summary>
        public void Tick(ShipController racer)
        {
            if (_gates.Count == 0 || !_inside.TryGetValue(racer, out bool[]? inside)) return;

            int n = _track.SampleCount;
            int at = racer.NearestIndex;

            for (int g = 0; g < _gates.Count; g++)
            {
                Baked gate = _gates[g];
                int delta = Mathf.Abs(at - gate.Index);
                bool inWindow = Mathf.Min(delta, n - delta) <= gate.Window;

                if (!inWindow)
                {
                    inside[g] = false;            // left the window — re-arm
                    continue;
                }
                if (inside[g]) continue;          // already triggered this pass
                inside[g] = true;

                // Lateral check: must pass through the opening.
                TrackSample s = _track[gate.Index];
                float lateral = Vector3.Dot(racer.Position - s.Pos, s.Nrm);
                if (Mathf.Abs(lateral - gate.Feature.lateralOffset) > gate.Feature.halfSpan) continue;

                bool deflected = Apply(racer, gate.Feature);
                OnGateTriggered?.Invoke(racer, gate.Feature, deflected);
            }
        }

        /// <summary>Returns true when an obstacle was DEFLECTED (barrier charge or active shield).</summary>
        private static bool Apply(ShipController racer, GateFeature f)
        {
            // Obstacles first check the deflection stack: an armed barrier charge is consumed,
            // or an actively held (energy-paying) shield deflects for free — Seni's rule.
            if (f.IsObstacle)
            {
                if (racer.Shielding) return true;
                if (racer.TryConsumeBarrier()) return true;
            }

            switch (f.type)
            {
                case GateFeatureType.SpeedBoost:
                case GateFeatureType.WarpSurge:
                    racer.ApplyGateSurge(f.surgeCapMult, f.surgeImpulse, f.surgeDuration);
                    break;

                case GateFeatureType.Photon:
                    // Tractor lock: acceleration + modest cap raise, building feel via duration.
                    racer.ApplyOverdrive(1.5f, f.surgeDuration);
                    racer.ApplyGateSurge(Mathf.Max(1.15f, f.surgeCapMult), 0f, f.surgeDuration);
                    break;

                case GateFeatureType.Overdrive:
                    racer.ApplyOverdrive(f.surgeCapMult, f.surgeDuration);
                    break;

                case GateFeatureType.Barrier:
                case GateFeatureType.Armor:
                    racer.ApplyBarrier(f.barrierCharges);
                    break;

                case GateFeatureType.Blocker:
                    racer.ApplyHazard(f.blockerDamage, f.blockerSpeedMult);
                    break;

                case GateFeatureType.Electric:
                    racer.ApplyStall(f.stallDuration);
                    break;

                case GateFeatureType.ReverseWarp:
                    racer.ApplyHazard(0f, f.blockerSpeedMult);
                    racer.ApplyReverseShove(f.surgeImpulse);
                    break;

                case GateFeatureType.Shredder:
                    racer.ApplyHazard(f.blockerDamage, 0.9f);
                    racer.ApplyTurnDebuff(f.debuffTurnMult, f.debuffDuration);
                    racer.RaiseShredded();
                    break;

                case GateFeatureType.Mine:
                    racer.ApplyHazard(f.blockerDamage, f.blockerSpeedMult);
                    racer.ApplySpinout(0.7f);
                    break;
            }
            return false;
        }
    }
}

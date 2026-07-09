using System;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// Synthetic curve-follower ghost (BUILD §7.6 / PortSpec §6): rail-locked on the centreline
    /// (never scrapes walls), simulated energy loop → PvP feel, zero netcode. Boosts on straights
    /// when the pool is fat, fires unaimed bolts at the bounty leader (or a decoy) in range.
    ///
    /// Rulings applied (DesignReview §5):
    ///   #4 — skill applies ONCE (prototype multiplied it into the boost cap twice).
    ///   #5 — laps seeds at −1 with ai.t just behind the line (prototype gifted ~1 free lap).
    /// The 0.80–0.90 skill band re-tune is an explicit U4 gate item (the prototype was fun WITH
    /// both bugs — fixing them makes ghosts faster over longer distances).
    /// </summary>
    public sealed class GhostRacer : IRacer
    {
        private readonly TrackSystem _track;
        private readonly GameTuning _tuning;
        private readonly System.Random _rng;
        private readonly int _lane;

        private float _t;              // curve fraction [0,1)
        private float _prevFrac;
        private float _fireCd;

        public GhostRacer(TrackSystem track, HullDefinition hull, GameTuning tuning,
            int lane, float startBehindFraction, System.Random rng)
        {
            _track = track ?? throw new ArgumentNullException(nameof(track));
            Hull = hull != null ? hull : throw new ArgumentNullException(nameof(hull));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _lane = lane;

            Energy = new EnergyCore(hull.maxEnergy, tuning.energyMaxScale);

            // PortSpec §5/§6: ai.t just behind the line; ruling #5: laps = −1 (no free lap).
            _t = 1f - startBehindFraction;
            _prevFrac = _t;
            Laps = -1;

            // Skill band + aggro rolled per race (prototype re-rolls each Game.start()).
            float band = _tuning.shipFeel.aiSkillBandMax - _tuning.shipFeel.aiSkillBandMin;
            Skill = _tuning.shipFeel.aiSkillBandMin + (float)_rng.NextDouble() * band;
            Aggro = (float)_rng.NextDouble();

            UpdatePose();
        }

        // ---- IRacer ----------------------------------------------------------
        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; }
        public float Speed { get; private set; }
        public HullDefinition Hull { get; }
        public EnergyCore Energy { get; }
        public bool Shielding => false;                     // ghosts never shield (PortSpec §6)
        public float IntangibleTimer => 0f;                 // …or phase
        public float SpinoutTimer { get; private set; }

        public void ApplySpinout(float duration) => SpinoutTimer = Mathf.Max(SpinoutTimer, duration);
        public void ApplyIntangible(float duration) { /* ghosts never phase */ }

        // ---- Ghost state ------------------------------------------------------
        public float Skill { get; }
        public float Aggro { get; }
        public bool Boosting { get; private set; }
        public int Laps { get; private set; }
        public float Progress => Laps + _t;
        public bool Finished { get; private set; }
        public float FinishTime { get; private set; }

        /// <summary>Ghost wants to fire this tick — RaceDirector routes to CombatSystem.</summary>
        public event Action<GhostRacer>? OnFired;

        /// <summary>PortSpec §6 exact order (with #4 single-skill fix), at the fixed tick.</summary>
        public void Step(float dt, IRacer? bountyLeader, PilotAbilitySystem.DecoyState? decoy, float raceTime)
        {
            if (Finished) return;

            ShipFeel feel = _tuning.shipFeel;
            float baseTop = feel.baseTopSpeed * Hull.topSpeedMult * feel.worldScale;
            int n = _track.SampleCount;

            // 1. Straight detection: probe 8 samples ahead (≈ 20.8 u).
            int idx = ((int)(_t * n)) % n;
            float curv = 1f - Mathf.Abs(Vector3.Dot(_track[idx].Tan, _track[(idx + 8) % n].Tan));
            bool straight = curv < 0.02f;

            // 2. Boost/coast + energy sim. Ruling #4: skill applied exactly once, at the end.
            float cap = baseTop;
            if (SpinoutTimer > 0f)
            {
                cap *= feel.spinoutSpeedCapMult;
                Boosting = false;
            }
            else if (Energy.Energy > 0.35f * Energy.Max && straight)
            {
                Boosting = true;
                cap *= _tuning.boostSpeedMult;
                Energy.Drain(_tuning.boostDrainPerSec, dt);
            }
            else
            {
                Boosting = false;
                Energy.Regen(Hull.regenPerSec, _tuning.regenScale, dt);
            }
            cap *= Skill;

            // 3. Exponential speed approach (no thrust/drag model for ghosts).
            Speed += (cap - Speed) * Mathf.Min(1f, dt * 3f);

            // 4. Advance along the rail.
            _t += Speed * dt / _track.Length;
            if (_t >= 1f) _t -= 1f;

            // 5. Fire decision — unaimed, at the leader (decoy hijacks the range check).
            _fireCd -= dt;
            if (Aggro > 0.4f && bountyLeader != null && !ReferenceEquals(bountyLeader, this)
                && _fireCd <= 0f && Energy.Energy > _tuning.fireCost)
            {
                Vector3 targetPos = decoy?.Position ?? bountyLeader.Position;
                if (Vector3.Distance(Position, targetPos) < feel.aiFireRange * feel.worldScale)
                {
                    Energy.Damage(_tuning.fireCost);           // ghost sim spends directly
                    _fireCd = 1.4f + (float)_rng.NextDouble(); // 1.4–2.4 s refire
                    OnFired?.Invoke(this);
                }
            }

            // 6. Pose from the rail (+ lane × 4 while racing — PortSpec §5 note).
            UpdatePose();

            // 7. Progress, lap wrap (forward only for ghosts), finish.
            if (_t < _prevFrac - 0.5f) Laps++;
            _prevFrac = _t;
            if (Progress >= _tuning.raceLaps && !Finished)
            {
                Finished = true;
                FinishTime = raceTime;
            }

            if (SpinoutTimer > 0f) SpinoutTimer = Mathf.Max(0f, SpinoutTimer - dt);
        }

        private void UpdatePose()
        {
            Vector3 pos = _track.Spline.GetPointAt(_t);
            Vector3 tan = _track.Spline.GetTangentAt(_t);
            Vector3 nrm = Vector3.Cross(tan, Vector3.up).normalized;
            pos += nrm * (_lane * 4f);
            pos.y = ShipController.RideHeight;
            Position = pos;
            Vector3 flat = new Vector3(tan.x, 0f, tan.z);
            Forward = flat.sqrMagnitude > 1e-12f ? flat.normalized : tan;
        }
    }
}

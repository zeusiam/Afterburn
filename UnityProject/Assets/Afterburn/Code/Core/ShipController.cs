using System;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// The ship simulation (BUILD §7.2). Reproduces the prototype's <c>updatePlayer()</c> in the
    /// EXACT step order (PortSpec §3) — this is the loop the P2 kill gate validated:
    ///   1 mode resolution (boost &gt; shield for held inputs)  2 discrete fire  3 fire timer
    ///   4 drain OR regen (never both; regen skipped the tick a shot fires)  5 speed cap
    ///   6 thrust/brake/drag  7 steering bite  8 integrate + wall resolve  9 timers.
    ///
    /// D6 directive: written against per-sample track frames — pose and steering use
    /// <see cref="TrackSample.Up"/>, never world-up. On flat Arena01 every operation reduces to
    /// the prototype's planar arithmetic (asserted by FlatDegeneracyTests + the parity trace).
    /// Runs at a FIXED tick (see <see cref="Tick"/>) — determinism is a hard prerequisite for
    /// U4 recorded ghosts.
    /// </summary>
    public sealed class ShipController : IRacer
    {
        /// <summary>Fixed simulation tick — 60 Hz. View interpolates between states.</summary>
        public const float Tick = 1f / 60f;

        /// <summary>Ships ride this far along the frame Up (prototype y = 1.2).</summary>
        public const float RideHeight = 1.2f;

        private readonly TrackSystem _track;
        private readonly HullDefinition _hull;
        private readonly GameTuning _tuning;

        private float _fireTimer;

        public ShipController(TrackSystem track, HullDefinition hull, GameTuning tuning, int lane = 0)
        {
            _track = track ?? throw new ArgumentNullException(nameof(track));
            _hull = hull != null ? hull : throw new ArgumentNullException(nameof(hull));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            Energy = new EnergyCore(hull.maxEnergy, tuning.energyMaxScale);
            Lane = lane;
            SpawnAtStart();
        }

        // ---- Sim state (View reads, never writes) ----------------------------
        public EnergyCore Energy { get; }
        public Vector3 Position { get; private set; }
        public Vector3 Forward { get; private set; }
        public float Speed { get; private set; }
        public bool Boosting { get; private set; }
        public bool Shielding { get; private set; }
        public bool FiredThisTick { get; private set; }
        public float SpinoutTimer { get; private set; }
        public float IntangibleTimer { get; private set; }
        public int NearestIndex { get; private set; } = -1;
        public int Lane { get; }
        public HullDefinition Hull => _hull;

        /// <summary>Scalar yaw for trace parity / View rotation: atan2(Forward.x, Forward.z).</summary>
        public float Heading => Mathf.Atan2(Forward.x, Forward.z);

        /// <summary>Prototype baseTop = shipFeel.baseTopSpeed × hull.topSpeedMult (× worldScale).</summary>
        public float BaseTopSpeed => _tuning.shipFeel.baseTopSpeed * _hull.topSpeedMult * _tuning.shipFeel.worldScale;

        /// <summary>A shot left the barrel this tick — U3 CombatSystem consumes (spawns the projectile).</summary>
        public event Action<ShipController>? OnFired;

        /// <summary>Wall contact this tick (sample index, pre-clamp signed lateral). View scrape-flash hook.</summary>
        public event Action<int, float>? OnWallContact;

        /// <summary>Prototype place(): start line + lane × 5 along the normal, facing along the tangent.</summary>
        public void SpawnAtStart()
        {
            TrackSample s0 = _track[0];
            Vector3 pos = s0.Pos + s0.Nrm * (Lane * 5f) + s0.Up * RideHeight;
            Position = pos;
            Forward = FlattenToFrame(s0.Tan, s0.Up);
            Speed = 0f;
            SpinoutTimer = 0f;
            IntangibleTimer = 0f;
            _fireTimer = 0f;
            NearestIndex = _track.NearestGlobal(pos);
        }

        /// <summary>Advance one fixed tick. `dt` is a parameter for testability but callers pass <see cref="Tick"/>.</summary>
        public void Step(in ShipInputState input, float dt)
        {
            ShipFeel feel = _tuning.shipFeel;
            float baseTop = BaseTopSpeed;

            // -- 1. Mode resolution (EnergyCore is the arbiter; boost strictly beats shield) ----
            Boosting = false;
            Shielding = false;
            FiredThisTick = false;
            if (input.Boost && Energy.CanSpend(1f)) Boosting = true;
            else if (input.Shield && Energy.CanSpend(1f)) Shielding = true;

            // -- 2. Discrete fire: held autofire, blocked by either active mode ----------------
            if (input.Fire && !Boosting && !Shielding && _fireTimer <= 0f
                && Energy.TrySpend(_tuning.fireCost))
            {
                _fireTimer = feel.fireTapCooldown;
                FiredThisTick = true;
                Speed *= _tuning.fireSpeedDip;      // once per shot, on current speed
                OnFired?.Invoke(this);
            }

            // -- 3. Fire timer ------------------------------------------------------------------
            _fireTimer = Mathf.Max(0f, _fireTimer - dt);

            // -- 4. Drain OR regen (never both; no regen the tick a shot fired) ----------------
            if (Boosting)
            {
                if (!Energy.Drain(_tuning.boostDrainPerSec, dt)) Boosting = false;
            }
            else if (Shielding)
            {
                if (!Energy.Drain(_tuning.shieldDrainPerSec, dt)) Shielding = false;
            }
            else if (!FiredThisTick)
            {
                Energy.Regen(_hull.regenPerSec, _tuning.regenScale, dt);
            }

            // -- 5. Speed cap (spinout stacks multiplicatively — PortSpec div #12) --------------
            float cap = baseTop;
            if (Boosting) cap = baseTop * _tuning.boostSpeedMult;
            else if (Shielding) cap = baseTop * _tuning.shieldSpeedCap;
            if (SpinoutTimer > 0f) cap *= feel.spinoutSpeedCapMult;

            // -- 6. Longitudinal ----------------------------------------------------------------
            float worldScale = feel.worldScale;
            if (input.Thrust) Speed += feel.thrustAccel * worldScale * dt;
            if (input.Brake) Speed -= feel.brakeDecel * worldScale * dt;
            Speed -= Speed * feel.dragPerSec * dt;
            Speed = Mathf.Clamp(Speed, 0f, cap);

            // -- 7. Steering: rotate Forward about the local frame Up (D6) ----------------------
            float steer = (input.Left ? 1f : 0f) - (input.Right ? 1f : 0f);
            if (steer != 0f)
            {
                float bite = Mathf.Min(1f, Speed / (feel.turnBiteSpeedDivisor * worldScale) + feel.turnBiteFloor);
                Vector3 up = CurrentFrame().Up;
                float angleRad = steer * feel.turnRate * dt * bite;
                Forward = Quaternion.AngleAxis(angleRad * Mathf.Rad2Deg, up) * Forward;
            }

            // -- 8. Integrate + walls -----------------------------------------------------------
            Vector3 np = Position + Forward * (Speed * dt);
            ResolveWalls(ref np);
            Position = np;

            // -- 9. Timers ----------------------------------------------------------------------
            if (SpinoutTimer > 0f) SpinoutTimer = Mathf.Max(0f, SpinoutTimer - dt);
            if (IntangibleTimer > 0f) IntangibleTimer = Mathf.Max(0f, IntangibleTimer - dt);
        }

        /// <summary>Combat entry points (U3): spinout on hit; Sora's phase.</summary>
        public void ApplySpinout(float duration) => SpinoutTimer = Mathf.Max(SpinoutTimer, duration);
        public void ApplyIntangible(float duration) => IntangibleTimer = Mathf.Max(IntangibleTimer, duration);

        /// <summary>
        /// Prototype resolveWalls: nearest-centreline lateral clamp + scrape + slide-toward-tangent
        /// + hard positional snap onto the sample's cross-section. Sora's phase skips entirely
        /// (the window keeps advancing so re-attachment stays on-layer — D6 note).
        /// </summary>
        private void ResolveWalls(ref Vector3 np)
        {
            if (IntangibleTimer > 0f)
            {
                NearestIndex = _track.Nearest(np, NearestIndex);
                return;
            }

            int i = _track.Nearest(np, NearestIndex);
            NearestIndex = i;
            TrackSample s = _track[i];

            float half = _track.HalfWidth;
            float radius = _hull.collisionRadius;
            float off = Vector3.Dot(np - s.Pos, s.Nrm);
            float allow = _track.InnerAllow(i, _hull.gateAccess);
            float limInner = half + allow - radius;
            float limOuter = -(half - radius);

            // Heavy smashes the slab when pressing the inner limit inside the heavy zone.
            if (off > half - radius && _hull.gateAccess == GateAccess.HeavyWall)
            {
                _track.TryBreakHeavy(i);
                // Allowance may have opened this very tick — prototype recomputes implicitly next
                // frame; it clamps against the PRE-break limits this frame. Match that exactly.
            }

            // Exact comparison, prototype parity: Clamp returns `off` bitwise when in range.
            float clamped = Mathf.Clamp(off, limOuter, limInner);
            if (clamped != off)
            {
                ShipFeel feel = _tuning.shipFeel;
                Speed *= feel.wallScrapeSpeedMult;

                float tanH = Mathf.Atan2(s.Tan.x, s.Tan.z);
                float h = Heading;
                float dh = Mathf.Repeat(tanH - h + 3f * Mathf.PI, 2f * Mathf.PI) - Mathf.PI;
                Forward = Quaternion.AngleAxis(dh * feel.wallSlideHeadingNudge * Mathf.Rad2Deg, s.Up) * Forward;

                np = s.Pos + s.Nrm * clamped + s.Up * RideHeight;   // prototype hard snap
                OnWallContact?.Invoke(i, off);
            }
            else
            {
                // Keep the ride height pinned along the local up (prototype: y forced to 1.2).
                float upComponent = Vector3.Dot(np - s.Pos, s.Up);
                np += s.Up * (RideHeight - upComponent);
            }
        }

        private TrackSample CurrentFrame() =>
            _track[NearestIndex < 0 ? _track.NearestGlobal(Position) : NearestIndex];

        /// <summary>Project a direction into the frame plane and normalise (flat: strips y).</summary>
        private static Vector3 FlattenToFrame(Vector3 dir, Vector3 up)
        {
            Vector3 flat = dir - up * Vector3.Dot(dir, up);
            return flat.sqrMagnitude > 1e-12f ? flat.normalized : dir.normalized;
        }
    }
}

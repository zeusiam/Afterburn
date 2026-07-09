using Afterburn.Core;
using NUnit.Framework;
using UnityEngine;

namespace Afterburn.Tests
{
    /// <summary>
    /// BUILD §11 U2: the mutual-exclusion resolver and the flat-degeneracy assertions (D6 —
    /// on flat Arena01 the track-frame math must reduce to the prototype's planar arithmetic).
    /// Builds the sim from code-constructed SOs — no scene, no assets.
    /// </summary>
    [TestFixture]
    public sealed class ShipControllerTests
    {
        private const float Dt = ShipController.Tick;

        private static readonly float[] Arena01Shape =
            { 1.00f, 0.94f, 0.78f, 0.74f, 0.92f, 1.06f, 1.10f, 0.86f, 0.72f, 0.82f, 1.02f, 1.08f, 0.92f, 0.84f };

        private static TrackDefinition MakeArena01()
        {
            var track = ScriptableObject.CreateInstance<TrackDefinition>();
            track.baseRadius = 300f;
            track.radiusShape = (float[])Arena01Shape.Clone();
            track.controlPoints = TrackSampler.GenerateStarConvexPoints(300f, Arena01Shape);
            track.catmullTension = 0.5f;
            track.sampleCount = 700;
            track.halfWidth = 17f;
            track.shortcuts = new[]
            {
                new ShortcutZone { access = GateAccess.LightGap, fromFraction = 0.20f, toFraction = 0.24f, extraInnerAllowance = 20f, side = 1 },
                new ShortcutZone { access = GateAccess.HeavyWall, fromFraction = 0.61f, toFraction = 0.66f, extraInnerAllowance = 22f, side = 1 },
            };
            return track;
        }

        private static HullDefinition MakeMedium()
        {
            var hull = ScriptableObject.CreateInstance<HullDefinition>();
            hull.displayName = "Medium";
            hull.maxEnergy = 100f;
            hull.regenPerSec = 8f;
            hull.topSpeedMult = 1f;
            hull.collisionRadius = 2f;
            hull.gateAccess = GateAccess.None;
            return hull;
        }

        private static (ShipController ship, TrackSystem track) MakeShip()
        {
            var trackSystem = new TrackSystem(MakeArena01());
            var tuning = ScriptableObject.CreateInstance<GameTuning>();   // frozen defaults
            var ship = new ShipController(trackSystem, MakeMedium(), tuning);
            return (ship, trackSystem);
        }

        /// <summary>
        /// A wall-free measuring straightaway: giant circle (R 5000, halfWidth 500) so pure
        /// longitudinal feel can be measured without Arena01's curvature forcing wall scrapes
        /// (an unsteered ship grinding the wall at ~9 u/s is correct prototype behaviour —
        /// but it is not what these constants-tests measure).
        /// </summary>
        private static ShipController MakeStraightawayShip()
        {
            var def = ScriptableObject.CreateInstance<TrackDefinition>();
            var shape = new float[14];
            for (int i = 0; i < shape.Length; i++) shape[i] = 1f;
            def.controlPoints = TrackSampler.GenerateStarConvexPoints(5000f, shape);
            def.catmullTension = 0.5f;
            def.sampleCount = 700;
            def.halfWidth = 500f;
            def.shortcuts = System.Array.Empty<ShortcutZone>();
            var tuning = ScriptableObject.CreateInstance<GameTuning>();
            return new ShipController(new TrackSystem(def), MakeMedium(), tuning);
        }

        // ---- Mutual exclusion (§2 — the inviolable rule) ---------------------

        [Test]
        public void BoostBeatsShield_WhenBothHeld()
        {
            (ShipController ship, _) = MakeShip();
            ship.Step(new ShipInputState { Boost = true, Shield = true }, Dt);
            Assert.That(ship.Boosting, Is.True);
            Assert.That(ship.Shielding, Is.False, "boost strictly beats shield (PortSpec div #6)");
        }

        [Test]
        public void FireBlocked_WhileBoostingOrShielding()
        {
            (ShipController shipA, _) = MakeShip();
            shipA.Step(new ShipInputState { Boost = true, Fire = true }, Dt);
            Assert.That(shipA.FiredThisTick, Is.False, "fire is blocked while boosting");
            Assert.That(shipA.Energy.Energy, Is.EqualTo(100f - 25f * Dt).Within(1e-4f),
                "only the boost drain may have spent");

            (ShipController shipB, _) = MakeShip();
            shipB.Step(new ShipInputState { Shield = true, Fire = true }, Dt);
            Assert.That(shipB.FiredThisTick, Is.False, "fire is blocked while shielding");
        }

        [Test]
        public void RegenOnlyOnCoast_AndSkippedOnFireTick()
        {
            (ShipController ship, _) = MakeShip();
            ship.Energy.Damage(50f);                            // 50 left

            ship.Step(new ShipInputState { Fire = true }, Dt);  // fires: -20, NO regen this tick
            Assert.That(ship.FiredThisTick, Is.True);
            Assert.That(ship.Energy.Energy, Is.EqualTo(30f).Within(1e-4f),
                "the tick a shot fires must not regen (PortSpec §3 step 4)");

            ship.Step(ShipInputState.None, Dt);                 // pure coast: regen 8 × dt
            Assert.That(ship.Energy.Energy, Is.EqualTo(30f + 8f * Dt).Within(1e-4f));
        }

        [Test]
        public void FireAutorepeats_AtTapCooldownCadence()
        {
            (ShipController ship, _) = MakeShip();
            int shots = 0;
            ship.OnFired += _ => shots++;

            var holdFire = new ShipInputState { Fire = true };
            int ticks = Mathf.CeilToInt(1f / Dt);               // 1 second held
            for (int i = 0; i < ticks; i++) ship.Step(holdFire, Dt);

            // 0.28 s cadence → shots at t=0, 0.283…, 0.566…, 0.85 → 4 in the first second.
            Assert.That(shots, Is.EqualTo(4), "held fire autorepeats at the 0.28 s cadence (div #13)");
        }

        [Test]
        public void BoostSelfCancels_WhenPoolEmpties()
        {
            (ShipController ship, _) = MakeShip();
            var boost = new ShipInputState { Boost = true, Thrust = true };
            // 100 energy / 25 per s = 4 s of boost; run 4.5 s.
            int ticks = Mathf.CeilToInt(4.5f / Dt);
            bool sawCancel = false;
            for (int i = 0; i < ticks; i++)
            {
                ship.Step(boost, Dt);
                if (!ship.Boosting) sawCancel = true;
            }
            Assert.That(sawCancel, Is.True, "boost must self-cancel at 0 (dry tank)");
            // After cancel, ticks where CanSpend(1) fails coast+regen until boost re-engages —
            // the pool flaps just above zero exactly like the prototype's canSpend(1) gate.
            Assert.That(ship.Energy.Energy, Is.LessThan(2f), "pool must stay pinned near 0 under held boost");
        }

        // ---- Feel constants through the pipeline ------------------------------

        [Test]
        public void TerminalVelocity_MatchesPrototypeCruise()
        {
            ShipController ship = MakeStraightawayShip();
            var thrust = new ShipInputState { Thrust = true };
            for (int i = 0; i < 60 * 10; i++) ship.Step(thrust, Dt);   // 10 s — settle
            // Terminal ≈ min(cap, accel/drag) — Medium cap 62 binds first (55/0.6 ≈ 91.7).
            Assert.That(ship.Speed, Is.EqualTo(62f).Within(0.5f));
        }

        [Test]
        public void BoostCap_Is1_4x_WhileEnergyLasts()
        {
            // Reach cruise first: from a standing start one 100-energy pool (4 s of boost)
            // cannot climb to the 86.8 cap — the prototype's own accel/drag math. Cruise at 62,
            // then boost: v crosses the cap ≈ 3 s in, with ~1 s of pool left to hold it.
            ShipController ship = MakeStraightawayShip();
            var thrust = new ShipInputState { Thrust = true };
            for (int i = 0; i < 60 * 6; i++) ship.Step(thrust, Dt);

            var boost = new ShipInputState { Thrust = true, Boost = true };
            float maxSeen = 0f;
            for (int i = 0; i < 60 * 4; i++)
            {
                ship.Step(boost, Dt);
                maxSeen = Mathf.Max(maxSeen, ship.Speed);
            }
            Assert.That(maxSeen, Is.EqualTo(86.8f).Within(1.5f), "boost cap = 62 × 1.40");
        }

        // ---- Flat degeneracy (D6) ---------------------------------------------

        [Test]
        public void FlatTrack_ForwardStaysInPlane_AndRideHeightHolds()
        {
            (ShipController ship, _) = MakeShip();
            var input = new ShipInputState { Thrust = true, Left = true };
            for (int i = 0; i < 60 * 20; i++)
            {
                ship.Step(input, Dt);
                Assert.That(ship.Forward.y, Is.EqualTo(0f).Within(1e-5f),
                    $"flat degeneracy broken at tick {i}: Forward left the plane");
                Assert.That(ship.Position.y, Is.EqualTo(ShipController.RideHeight).Within(1e-4f),
                    $"ride height drifted at tick {i}");
            }
        }

        [Test]
        public void SteeringMatchesScalarHeadingArithmetic()
        {
            // One steering tick from rest must equal the prototype's heading += steer·2.4·dt·bite.
            (ShipController ship, _) = MakeShip();
            float h0 = ship.Heading;
            ship.Step(new ShipInputState { Left = true }, Dt);
            float expectedBite = Mathf.Min(1f, 0f / 25f + 0.2f);            // speed 0 → bite 0.2
            float expected = h0 + 1f * 2.4f * Dt * expectedBite;
            Assert.That(Mathf.DeltaAngle(ship.Heading * Mathf.Rad2Deg, expected * Mathf.Rad2Deg),
                Is.EqualTo(0f).Within(1e-3f),
                "vector steering must reduce to the prototype's scalar heading arithmetic on flat");
        }

        // ---- Walls -------------------------------------------------------------

        [Test]
        public void WallClamp_ScrapesAndSnapsToLimit()
        {
            (ShipController ship, TrackSystem track) = MakeShip();
            // Drive hard toward the outer wall: steer right until contact.
            var input = new ShipInputState { Thrust = true, Right = true };
            bool contact = false;
            ship.OnWallContact += (_, _) => contact = true;
            for (int i = 0; i < 60 * 15 && !contact; i++) ship.Step(input, Dt);

            Assert.That(contact, Is.True, "sustained steer into the wall must produce contact");
            TrackSample s = track[ship.NearestIndex];
            float off = Vector3.Dot(ship.Position - s.Pos, s.Nrm);
            // Medium radius 2, half 17 → clamp band is [-15, +15].
            Assert.That(Mathf.Abs(off), Is.LessThanOrEqualTo(15f + 1e-3f),
                "post-clamp lateral offset must respect half − radius");
        }
    }
}

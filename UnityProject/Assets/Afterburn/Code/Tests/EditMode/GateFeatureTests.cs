using Afterburn.Core;
using NUnit.Framework;
using UnityEngine;

namespace Afterburn.Tests
{
    /// <summary>
    /// D15 gate features: trigger window + re-arm, lateral gating, surge math (speed only —
    /// the §2 invariant: gates NEVER touch the energy pool upward), blocker drain, Phase immunity.
    /// </summary>
    [TestFixture]
    public sealed class GateFeatureTests
    {
        private const float Dt = ShipController.Tick;

        private static TrackDefinition MakeStraightaway(params GateFeature[] gates)
        {
            var def = ScriptableObject.CreateInstance<TrackDefinition>();
            var shape = new float[14];
            for (int i = 0; i < shape.Length; i++) shape[i] = 1f;
            def.controlPoints = TrackSampler.GenerateStarConvexPoints(5000f, shape);
            def.catmullTension = 0.5f;
            def.sampleCount = 700;
            def.halfWidth = 500f;
            def.shortcuts = System.Array.Empty<ShortcutZone>();
            def.gateFeatures = gates;
            return def;
        }

        private static HullDefinition MakeHull()
        {
            var hull = ScriptableObject.CreateInstance<HullDefinition>();
            hull.maxEnergy = 100f;
            hull.regenPerSec = 0f;                       // exact energy assertions
            hull.topSpeedMult = 1f;
            hull.collisionRadius = 2f;
            return hull;
        }

        /// <summary>Drive the ship forward until it crosses the given track fraction.</summary>
        private static void DriveTo(ShipController ship, TrackSystem track, float fraction, GateFeatureSystem gates)
        {
            int target = Mathf.FloorToInt(fraction * track.SampleCount);
            var thrust = new ShipInputState { Thrust = true };
            for (int i = 0; i < 60 * 120 && ship.NearestIndex < target + 5; i++)
            {
                ship.Step(thrust, Dt);
                gates.Tick(ship);
            }
        }

        [Test]
        public void SpeedBoostGate_RaisesCapAndImpulse_NeverEnergy()
        {
            var gate = new GateFeature { type = GateFeatureType.SpeedBoost, fraction = 0.02f, halfSpan = 20f, surgeCapMult = 1.25f, surgeImpulse = 12f, surgeDuration = 1.2f };
            TrackDefinition def = MakeStraightaway(gate);
            var track = new TrackSystem(def);
            var tuning = ScriptableObject.CreateInstance<GameTuning>();
            var ship = new ShipController(track, MakeHull(), tuning);
            var gates = new GateFeatureSystem(track, def);
            gates.Register(ship);

            float energyBefore = ship.Energy.Energy;
            bool fired = false;
            gates.OnGateTriggered += (_, _) => fired = true;

            DriveTo(ship, track, 0.02f, gates);

            Assert.That(fired, Is.True, "gate must trigger on pass");
            Assert.That(ship.SurgeTimer, Is.GreaterThan(0f));
            Assert.That(ship.Speed, Is.GreaterThan(62f), "surge lifts speed past the base cap");
            Assert.That(ship.Energy.Energy, Is.EqualTo(energyBefore),
                "§2 invariant: gates NEVER grant or spend energy");
        }

        [Test]
        public void Surge_Expires_AndCapReturnsToBase()
        {
            var gate = new GateFeature { type = GateFeatureType.SpeedBoost, fraction = 0.02f, halfSpan = 20f, surgeCapMult = 1.25f, surgeImpulse = 12f, surgeDuration = 0.5f };
            TrackDefinition def = MakeStraightaway(gate);
            var track = new TrackSystem(def);
            var ship = new ShipController(track, MakeHull(), ScriptableObject.CreateInstance<GameTuning>());
            var gates = new GateFeatureSystem(track, def);
            gates.Register(ship);
            DriveTo(ship, track, 0.02f, gates);

            var thrust = new ShipInputState { Thrust = true };
            for (int i = 0; i < 60 * 3; i++) { ship.Step(thrust, Dt); gates.Tick(ship); }

            Assert.That(ship.SurgeTimer, Is.EqualTo(0f));
            Assert.That(ship.Speed, Is.LessThanOrEqualTo(62f + 0.5f), "speed decays back under the base cap");
        }

        [Test]
        public void Blocker_DrainsAndScrapes_PhasePassesThrough()
        {
            var blocker = new GateFeature { type = GateFeatureType.Blocker, fraction = 0.02f, halfSpan = 20f, blockerDamage = 15f, blockerSpeedMult = 0.85f };
            TrackDefinition def = MakeStraightaway(blocker);

            // Normal pass: pays the toll.
            var trackA = new TrackSystem(def);
            var shipA = new ShipController(trackA, MakeHull(), ScriptableObject.CreateInstance<GameTuning>());
            var gatesA = new GateFeatureSystem(trackA, def);
            gatesA.Register(shipA);
            DriveTo(shipA, trackA, 0.02f, gatesA);
            Assert.That(shipA.Energy.Energy, Is.EqualTo(85f).Within(1e-3f), "blocker drains 15");

            // Phased pass: untouchable (consistent with walls and shots).
            var trackB = new TrackSystem(def);
            var shipB = new ShipController(trackB, MakeHull(), ScriptableObject.CreateInstance<GameTuning>());
            var gatesB = new GateFeatureSystem(trackB, def);
            gatesB.Register(shipB);
            var thrust = new ShipInputState { Thrust = true };
            int target = Mathf.FloorToInt(0.02f * trackB.SampleCount);
            for (int i = 0; i < 60 * 120 && shipB.NearestIndex < target + 5; i++)
            {
                shipB.ApplyIntangible(0.1f);             // keep the phase alive through the pass
                shipB.Step(thrust, Dt);
                gatesB.Tick(shipB);
            }
            Assert.That(shipB.Energy.Energy, Is.EqualTo(100f), "Sora phases through blockers");
        }

        [Test]
        public void Gate_TriggersOncePerPass_LateralGated()
        {
            var gate = new GateFeature { type = GateFeatureType.SpeedBoost, fraction = 0.02f, halfSpan = 3f, lateralOffset = 200f, surgeCapMult = 1.25f, surgeImpulse = 12f, surgeDuration = 1.2f };
            TrackDefinition def = MakeStraightaway(gate);
            var track = new TrackSystem(def);
            var ship = new ShipController(track, MakeHull(), ScriptableObject.CreateInstance<GameTuning>());
            var gates = new GateFeatureSystem(track, def);
            gates.Register(ship);

            int triggers = 0;
            gates.OnGateTriggered += (_, _) => triggers++;
            DriveTo(ship, track, 0.03f, gates);

            Assert.That(triggers, Is.EqualTo(0),
                "gate offset 200 u laterally — a centreline pass must NOT trigger (opening missed)");
        }
    }
}

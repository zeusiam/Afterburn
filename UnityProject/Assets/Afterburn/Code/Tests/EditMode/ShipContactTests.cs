using Afterburn.Core;
using NUnit.Framework;
using UnityEngine;

namespace Afterburn.Tests
{
    /// <summary>
    /// D14 (ships are tangible): mass-scaled mutual contact damage, per-pair cooldown, pushout,
    /// Phase immunity, and the wall-grind energy drain. §2 invariant: contact only drains.
    /// </summary>
    [TestFixture]
    public sealed class ShipContactTests
    {
        private const float Dt = ShipController.Tick;

        private static readonly float[] Arena01Shape =
            { 1.00f, 0.94f, 0.78f, 0.74f, 0.92f, 1.06f, 1.10f, 0.86f, 0.72f, 0.82f, 1.02f, 1.08f, 0.92f, 0.84f };

        private static TrackDefinition MakeTrackDef()
        {
            var def = ScriptableObject.CreateInstance<TrackDefinition>();
            def.controlPoints = TrackSampler.GenerateStarConvexPoints(300f, Arena01Shape);
            def.catmullTension = 0.5f;
            def.sampleCount = 700;
            def.halfWidth = 17f;
            def.shortcuts = System.Array.Empty<ShortcutZone>();
            return def;
        }

        private static HullDefinition MakeHull(string name, float mass, float radius)
        {
            var hull = ScriptableObject.CreateInstance<HullDefinition>();
            hull.displayName = name;
            hull.maxEnergy = 100f;
            hull.regenPerSec = 0f;                              // exact energy assertions
            hull.topSpeedMult = 1f;
            hull.mass = mass;
            hull.collisionRadius = radius;
            return hull;
        }

        private static (ShipController player, GhostRacer ghost, ShipContactSystem contacts, GameTuning tuning)
            Overlapping(float playerMass, float ghostMass)
        {
            var track = new TrackSystem(MakeTrackDef());
            var tuning = ScriptableObject.CreateInstance<GameTuning>();
            var player = new ShipController(track, MakeHull("P", playerMass, 2f), tuning, lane: 0);
            // Ghost at lane 0, t just behind the line — spawn pose lands within contact range.
            var ghost = new GhostRacer(track, MakeHull("G", ghostMass, 2f), tuning, lane: 0, 0.001f,
                new System.Random(1));
            return (player, ghost, new ShipContactSystem(tuning), tuning);
        }

        [Test]
        public void Contact_DealsMutualMassScaledDamage()
        {
            (ShipController player, GhostRacer ghost, ShipContactSystem contacts, _) =
                Overlapping(playerMass: 0.8f, ghostMass: 1.4f);      // Light player vs Heavy ghost

            contacts.Tick(Dt, player, new[] { ghost });

            Assert.That(player.Energy.Energy, Is.EqualTo(100f - 10f * (1.4f / 0.8f)).Within(1e-3f),
                "Light takes base × (heavyMass/lightMass) = 17.5");
            Assert.That(ghost.Energy.Energy, Is.EqualTo(100f - 10f * (0.8f / 1.4f)).Within(1e-3f),
                "Heavy takes base × (lightMass/heavyMass) ≈ 5.71");
        }

        [Test]
        public void Contact_PushesPlayerOutOfOverlap_AndScrapesSpeed()
        {
            (ShipController player, GhostRacer ghost, ShipContactSystem contacts, _) =
                Overlapping(1f, 1f);

            contacts.Tick(Dt, player, new[] { ghost });

            float separation = Vector3.Distance(
                new Vector3(player.Position.x, 0f, player.Position.z),
                new Vector3(ghost.Position.x, 0f, ghost.Position.z));
            Assert.That(separation, Is.GreaterThanOrEqualTo(4f - 1e-3f),
                "player pushed out to at least the combined radii");
        }

        [Test]
        public void Contact_CooldownStopsPerTickDrain()
        {
            (ShipController player, GhostRacer ghost, ShipContactSystem contacts, _) =
                Overlapping(1f, 1f);

            contacts.Tick(Dt, player, new[] { ghost });
            float afterFirst = player.Energy.Energy;

            // Immediately re-overlap and tick again inside the cooldown window.
            player.ApplyContactPush(ghost.Position, 1f);
            for (int i = 0; i < 10; i++) contacts.Tick(Dt, player, new[] { ghost });

            Assert.That(player.Energy.Energy, Is.EqualTo(afterFirst),
                "no second hit inside the 0.5 s per-pair cooldown");
        }

        [Test]
        public void PhasedPlayer_PassesThroughContact()
        {
            (ShipController player, GhostRacer ghost, ShipContactSystem contacts, _) =
                Overlapping(1f, 1f);
            player.ApplyIntangible(1.2f);

            contacts.Tick(Dt, player, new[] { ghost });

            Assert.That(player.Energy.Energy, Is.EqualTo(100f), "Sora phases through contact");
            Assert.That(ghost.Energy.Energy, Is.EqualTo(100f));
        }

        [Test]
        public void ZeroTuning_DisablesContactEntirely()
        {
            (ShipController player, GhostRacer ghost, ShipContactSystem contacts, GameTuning tuning) =
                Overlapping(1f, 1f);
            tuning.shipFeel.shipContactDamage = 0f;              // prototype-parity mode

            contacts.Tick(Dt, player, new[] { ghost });

            Assert.That(player.Energy.Energy, Is.EqualTo(100f));
            Assert.That(ghost.Energy.Energy, Is.EqualTo(100f));
        }

        [Test]
        public void WallGrind_DrainsEnergy_D14()
        {
            var track = new TrackSystem(MakeTrackDef());
            var tuning = ScriptableObject.CreateInstance<GameTuning>();
            var ship = new ShipController(track, MakeHull("P", 1f, 2f), tuning);

            // Drive into the wall and grind for 5 s (regen frozen at 0 for exactness).
            var input = new ShipInputState { Thrust = true, Right = true };
            for (int i = 0; i < 60 * 5; i++) ship.Step(input, Dt);

            Assert.That(ship.Energy.Energy, Is.LessThan(100f),
                "grinding the wall must drain the pool (6/s while in contact)");
            Assert.That(ship.Energy.Energy, Is.GreaterThan(0f),
                "5 s of partial contact must not empty a 100 pool at 6/s");
        }
    }
}

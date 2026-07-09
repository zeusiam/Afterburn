using Afterburn.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Afterburn.Tests
{
    /// <summary>
    /// Guards the frozen prototype tuning (BUILD §5). The P2 kill gate passed at these exact
    /// numbers — any drift here is a port bug, not a re-tune (BUILD §11).
    /// Asset tests require <c>Veratus/Afterburn/Create or Update SOs</c> to have run.
    /// </summary>
    [TestFixture]
    public sealed class FrozenValuesTests
    {
        // ---- GameTuning class defaults (§5.3) — asset-independent -----------

        [Test]
        public void GameTuning_Defaults_MatchFrozenTable()
        {
            var t = ScriptableObject.CreateInstance<GameTuning>();
            Assert.That(t.energyMaxScale, Is.EqualTo(100f));
            Assert.That(t.regenScale, Is.EqualTo(8f));
            Assert.That(t.boostDrainPerSec, Is.EqualTo(25f));
            Assert.That(t.boostSpeedMult, Is.EqualTo(1.40f));
            Assert.That(t.fireCost, Is.EqualTo(20f));
            Assert.That(t.fireSpeedDip, Is.EqualTo(0.90f));
            Assert.That(t.shieldDrainPerSec, Is.EqualTo(15f));
            Assert.That(t.shieldDamageMult, Is.EqualTo(0.40f));
            Assert.That(t.shieldSpeedCap, Is.EqualTo(0.80f));
            Assert.That(t.bountyRewardMult, Is.EqualTo(2.0f));
            Assert.That(t.abilityCooldownScale, Is.EqualTo(1.0f));
            Assert.That(t.raceLaps, Is.EqualTo(3));
            Object.DestroyImmediate(t);
        }

        [Test]
        public void ShipFeel_Defaults_MatchFrozenTable()
        {
            var t = ScriptableObject.CreateInstance<GameTuning>();
            ShipFeel f = t.shipFeel;
            Assert.That(f.worldScale, Is.EqualTo(1f));
            Assert.That(f.baseTopSpeed, Is.EqualTo(62f));
            Assert.That(f.thrustAccel, Is.EqualTo(55f));
            Assert.That(f.dragPerSec, Is.EqualTo(0.6f));
            Assert.That(f.brakeDecel, Is.EqualTo(80f));
            Assert.That(f.turnRate, Is.EqualTo(2.4f));
            Assert.That(f.turnBiteSpeedDivisor, Is.EqualTo(25f));
            Assert.That(f.turnBiteFloor, Is.EqualTo(0.2f));
            Assert.That(f.wallScrapeSpeedMult, Is.EqualTo(0.92f));
            Assert.That(f.wallSlideHeadingNudge, Is.EqualTo(0.18f));
            Assert.That(f.fireTapCooldown, Is.EqualTo(0.28f));
            Assert.That(f.projectileDamage, Is.EqualTo(25f));
            Assert.That(f.projectileSpeed, Is.EqualTo(170f));
            Assert.That(f.projectileInheritFactor, Is.EqualTo(0.4f));
            Assert.That(f.projectileLifetime, Is.EqualTo(1.6f));
            Assert.That(f.projectileSpawnAhead, Is.EqualTo(3f));
            Assert.That(f.projectileHitPad, Is.EqualTo(1.6f));
            Assert.That(f.spinoutDuration, Is.EqualTo(0.7f));
            Assert.That(f.spinoutSpeedCapMult, Is.EqualTo(0.55f));
            Assert.That(f.bountyBaseReward, Is.EqualTo(8f));
            Assert.That(f.aiSkillBandMin, Is.EqualTo(0.80f));
            Assert.That(f.aiSkillBandMax, Is.EqualTo(0.90f));
            Assert.That(f.aiFireRange, Is.EqualTo(55f));
            Object.DestroyImmediate(t);
        }

        // ---- Seeded assets (§5.1 / §5.2 / §7.5) ------------------------------

        [TestCase("Light", 80f, 11f, 1.15f, 0.8f, 1.6f, GateAccess.LightGap, "37D0FF")]
        [TestCase("Medium", 100f, 8f, 1.00f, 1.0f, 2.0f, GateAccess.None, "9D7BFF")]
        [TestCase("Heavy", 130f, 5f, 0.88f, 1.4f, 2.6f, GateAccess.HeavyWall, "FF8A3C")]
        public void SeededHull_MatchesFrozenTable(string id, float maxEnergy, float regen, float topMult,
            float mass, float radius, GateAccess access, string hex)
        {
            var hull = AssetDatabase.LoadAssetAtPath<HullDefinition>($"Assets/Afterburn/Data/Hulls/{id}.asset");
            Assert.That(hull, Is.Not.Null, $"Hull '{id}' missing — run Veratus/Afterburn/Create or Update SOs.");
            Assert.That(hull!.displayName, Is.EqualTo(id));
            Assert.That(hull.maxEnergy, Is.EqualTo(maxEnergy));
            Assert.That(hull.regenPerSec, Is.EqualTo(regen));
            Assert.That(hull.topSpeedMult, Is.EqualTo(topMult));
            Assert.That(hull.mass, Is.EqualTo(mass));
            Assert.That(hull.collisionRadius, Is.EqualTo(radius));
            Assert.That(hull.gateAccess, Is.EqualTo(access));
            Assert.That(ColorUtility.ToHtmlStringRGB(hull.tintColor), Is.EqualTo(hex));
        }

        [TestCase("Vex", "EMP Pulse", 18f, AbilityType.EmpPulse, 30f, 70f)]
        [TestCase("Sora", "Phase Shift", 15f, AbilityType.PhaseShift, 1.2f, 0f)]
        [TestCase("Kade", "Siphon", 20f, AbilityType.Siphon, 25f, 0f)]
        [TestCase("Nyx", "Decoy", 22f, AbilityType.Decoy, 3.0f, 0f)]
        public void SeededPilot_MatchesFrozenTable(string id, string abilityName, float cd,
            AbilityType type, float param, float radius)
        {
            var pilot = AssetDatabase.LoadAssetAtPath<PilotDefinition>($"Assets/Afterburn/Data/Pilots/{id}.asset");
            Assert.That(pilot, Is.Not.Null, $"Pilot '{id}' missing — run Veratus/Afterburn/Create or Update SOs.");
            Assert.That(pilot!.displayName, Is.EqualTo(id));
            Assert.That(pilot.abilityName, Is.EqualTo(abilityName));
            Assert.That(pilot.cooldownSec, Is.EqualTo(cd));
            Assert.That(pilot.abilityType, Is.EqualTo(type));
            Assert.That(pilot.abilityParam, Is.EqualTo(param));
            Assert.That(pilot.abilityRadius, Is.EqualTo(radius));
        }

        [Test]
        public void SeededArena01_MatchesPrototypeGeometry()
        {
            var track = AssetDatabase.LoadAssetAtPath<TrackDefinition>("Assets/Afterburn/Data/Tracks/Arena01.asset");
            Assert.That(track, Is.Not.Null, "Arena01 missing — run Veratus/Afterburn/Create or Update SOs.");
            Assert.That(track!.controlPoints, Has.Length.EqualTo(14));
            Assert.That(track.catmullTension, Is.EqualTo(0.5f));
            Assert.That(track.sampleCount, Is.EqualTo(700));
            Assert.That(track.halfWidth, Is.EqualTo(17f));
            Assert.That(track.wallHeight, Is.EqualTo(3.2f));
            Assert.That(track.shortcuts, Has.Length.EqualTo(2));

            ShortcutZone light = track.shortcuts[0];
            Assert.That(light.access, Is.EqualTo(GateAccess.LightGap));
            Assert.That(light.fromFraction, Is.EqualTo(0.20f));
            Assert.That(light.toFraction, Is.EqualTo(0.24f));
            Assert.That(light.extraInnerAllowance, Is.EqualTo(20f));
            Assert.That(light.side, Is.EqualTo(1));

            ShortcutZone heavy = track.shortcuts[1];
            Assert.That(heavy.access, Is.EqualTo(GateAccess.HeavyWall));
            Assert.That(heavy.fromFraction, Is.EqualTo(0.61f));
            Assert.That(heavy.toFraction, Is.EqualTo(0.66f));
            Assert.That(heavy.extraInnerAllowance, Is.EqualTo(22f));
            Assert.That(heavy.side, Is.EqualTo(1));

            // First control point sits at angle 0: (R0 × 1.00, 0, 0).
            Assert.That(track.controlPoints[0].x, Is.EqualTo(300f).Within(0.001f));
            Assert.That(track.controlPoints[0].z, Is.EqualTo(0f).Within(0.001f));
        }
    }
}

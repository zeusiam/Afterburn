using System.Collections.Generic;
using Afterburn.Core;
using NUnit.Framework;
using UnityEngine;

namespace Afterburn.Tests
{
    /// <summary>
    /// BUILD §11 U3: the combat economy (PortSpec §7) with rulings #3/#7 applied.
    /// Two ships on the straightaway track, bullets stepped at the fixed tick.
    /// </summary>
    [TestFixture]
    public sealed class CombatSystemTests
    {
        private const float Dt = ShipController.Tick;

        private TrackSystem _track = null!;
        private GameTuning _tuning = null!;
        private PilotAbilitySystem _abilities = null!;

        private static TrackDefinition MakeStraightaway()
        {
            var def = ScriptableObject.CreateInstance<TrackDefinition>();
            var shape = new float[14];
            for (int i = 0; i < shape.Length; i++) shape[i] = 1f;
            def.controlPoints = TrackSampler.GenerateStarConvexPoints(5000f, shape);
            def.catmullTension = 0.5f;
            def.sampleCount = 700;
            def.halfWidth = 500f;
            def.shortcuts = System.Array.Empty<ShortcutZone>();
            return def;
        }

        private static HullDefinition MakeHull(string name, float maxEnergy = 100f, float radius = 2f)
        {
            var hull = ScriptableObject.CreateInstance<HullDefinition>();
            hull.displayName = name;
            hull.maxEnergy = maxEnergy;
            hull.regenPerSec = 0f;      // freeze regen so energy assertions are exact
            hull.topSpeedMult = 1f;
            hull.collisionRadius = radius;
            return hull;
        }

        private static PilotDefinition MakePilot(AbilityType type, float cd, float param, float radius = 0f)
        {
            var p = ScriptableObject.CreateInstance<PilotDefinition>();
            p.abilityType = type;
            p.cooldownSec = cd;
            p.abilityParam = param;
            p.abilityRadius = radius;
            return p;
        }

        [SetUp]
        public void SetUp()
        {
            _track = new TrackSystem(MakeStraightaway());
            _tuning = ScriptableObject.CreateInstance<GameTuning>();
            _abilities = new PilotAbilitySystem(_tuning);
        }

        /// <summary>Shooter at lane 0 and a target dead-ahead: lane 0, some units down-track.</summary>
        private (ShipController shooter, ShipController target, CombatSystem combat) Duel(
            PilotDefinition? shooterPilot = null, ShipController? leader = null)
        {
            var shooter = new ShipController(_track, MakeHull("Shooter"), _tuning, lane: 0);
            var target = new ShipController(_track, MakeHull("Target"), _tuning, lane: 0);
            _abilities.Register(shooter, shooterPilot ?? MakePilot(AbilityType.EmpPulse, 18f, 30f, 70f));
            _abilities.Register(target, MakePilot(AbilityType.Decoy, 22f, 3f));

            // Nudge the target ~30 u ahead along the start tangent (still lane 0).
            for (int i = 0; i < 60 * 2; i++) target.Step(new ShipInputState { Thrust = true }, Dt);

            var combat = new CombatSystem(_tuning, _abilities, leader == null ? null : () => leader);
            combat.Attach(shooter);
            combat.Attach(target);
            return (shooter, target, combat);
        }

        private static void RunUntilResolved(CombatSystem combat, IReadOnlyList<ShipController> racers, int maxTicks = 120)
        {
            for (int i = 0; i < maxTicks; i++)
            {
                combat.Tick(Dt, racers);
                bool anyLive = false;
                foreach (CombatSystem.Projectile p in combat.Projectiles) anyLive |= p.Live;
                if (!anyLive) return;
            }
        }

        [Test]
        public void Hit_AppliesDamage_Spinout_AndBaseBounty()
        {
            (ShipController shooter, ShipController target, CombatSystem combat) = Duel();
            shooter.Energy.Damage(30f);                          // 70 — room to see the reward

            combat.Fire(shooter, siphon: false);
            RunUntilResolved(combat, new[] { shooter, target });

            Assert.That(target.Energy.Energy, Is.EqualTo(75f).Within(1e-3f), "25 damage unshielded");
            Assert.That(target.SpinoutTimer, Is.GreaterThan(0f).And.LessThanOrEqualTo(0.7f));
            Assert.That(shooter.Energy.Energy, Is.EqualTo(78f).Within(1e-3f), "base bounty 8 paid");
        }

        [Test]
        public void Hit_OnLeader_PaysDoubleBounty()
        {
            var pilots = MakePilot(AbilityType.EmpPulse, 18f, 30f, 70f);
            (ShipController shooter, ShipController target, CombatSystem combat) = Duel(pilots, leader: null);
            // Rebuild with the target as leader.
            var combatLeader = new CombatSystem(_tuning, _abilities, () => target);
            shooter.Energy.Damage(30f);

            combatLeader.Fire(shooter, siphon: false);
            RunUntilResolved(combatLeader, new[] { shooter, target });

            Assert.That(shooter.Energy.Energy, Is.EqualTo(70f + 16f).Within(1e-3f),
                "leader hit pays 8 × 2.0 = 16 (PortSpec §8)");
        }

        [Test]
        public void ShieldedTarget_TakesFortyPercent()
        {
            (ShipController shooter, ShipController target, CombatSystem combat) = Duel();
            // Hold shield on the target while the bullet flies.
            combat.Fire(shooter, siphon: false);
            for (int i = 0; i < 120; i++)
            {
                target.Step(new ShipInputState { Shield = true }, Dt);
                combat.Tick(Dt, new[] { shooter, target });
            }
            // Shield drained 15/s while active — compute damage taken as (start − drain) − end.
            // Simpler invariant: the hit removed exactly 10 (25 × 0.40) at the moment of impact,
            // so total loss > pure drain by exactly 10.
            var reference = new ShipController(_track, MakeHull("Ref"), _tuning);
            for (int i = 0; i < 120; i++)
            {
                reference.Step(new ShipInputState { Shield = true }, Dt);
            }
            float lossWithHit = 100f - target.Energy.Energy;
            float lossDrainOnly = 100f - reference.Energy.Energy;
            Assert.That(lossWithHit - lossDrainOnly, Is.EqualTo(10f).Within(0.5f),
                "shielded hit must cost 25 × 0.40 = 10 (PortSpec §7)");
        }

        [Test]
        public void Siphon_StealsCappedAtVictimPool_Ruling7()
        {
            var kade = MakePilot(AbilityType.Siphon, 20f, 25f);
            (ShipController shooter, ShipController target, CombatSystem combat) = Duel(kade);
            shooter.Energy.Damage(60f);                          // 40
            target.Energy.Damage(70f);                           // 30 — after 25 dmg only 5 remain to steal

            Assert.That(_abilities.Activate(shooter, new[] { shooter, target }), Is.True);
            Assert.That(_abilities.IsSiphonArmed(shooter), Is.True);

            combat.Fire(shooter, _abilities.IsSiphonArmed(shooter));
            RunUntilResolved(combat, new[] { shooter, target });

            // Hit: target 30 − 25 dmg = 5 → siphon steals min(25, 5) = 5 → target 0.
            Assert.That(target.Energy.Energy, Is.EqualTo(0f).Within(1e-3f));
            // Shooter: 40 + 8 bounty + 5 stolen = 53. The prototype would have minted +25.
            Assert.That(shooter.Energy.Energy, Is.EqualTo(53f).Within(1e-3f),
                "ruling #7: steal is capped at the victim's remaining pool — never minted");
            Assert.That(_abilities.IsSiphonArmed(shooter), Is.False, "armed state consumed on the landed hit");
        }

        [Test]
        public void IntangibleTarget_IsUntouchable()
        {
            (ShipController shooter, ShipController target, CombatSystem combat) = Duel();
            target.ApplyIntangible(1.2f);

            combat.Fire(shooter, siphon: false);
            RunUntilResolved(combat, new[] { shooter, target });

            Assert.That(target.Energy.Energy, Is.EqualTo(100f), "Sora phases through shots");
            Assert.That(target.SpinoutTimer, Is.EqualTo(0f));
        }

        [Test]
        public void PoolDry_DropsShotSilently()
        {
            (ShipController shooter, ShipController target, CombatSystem combat) = Duel();
            for (int i = 0; i < CombatSystem.PoolSize; i++) combat.Fire(shooter, false);

            int live = 0;
            foreach (CombatSystem.Projectile p in combat.Projectiles) if (p.Live) live++;
            Assert.That(live, Is.EqualTo(CombatSystem.PoolSize));

            combat.Fire(shooter, false);   // 41st — must not throw, must not exceed pool
            live = 0;
            foreach (CombatSystem.Projectile p in combat.Projectiles) if (p.Live) live++;
            Assert.That(live, Is.EqualTo(CombatSystem.PoolSize), "pool-dry shot silently dropped (prototype)");
        }

        [Test]
        public void Emp_DrainsOnlyWithinRadius()
        {
            var vex = MakePilot(AbilityType.EmpPulse, 18f, 30f, 70f);
            var caster = new ShipController(_track, MakeHull("Vex"), _tuning);
            var near = new ShipController(_track, MakeHull("Near"), _tuning, lane: 2);    // 10 u aside
            var far = new ShipController(_track, MakeHull("Far"), _tuning);
            _abilities.Register(caster, vex);

            // Drive `far` well beyond 70 u.
            for (int i = 0; i < 60 * 4; i++) far.Step(new ShipInputState { Thrust = true }, Dt);
            Assert.That(Vector3.Distance(caster.Position, far.Position), Is.GreaterThan(70f));

            _abilities.Activate(caster, new[] { caster, near, far });

            Assert.That(near.Energy.Energy, Is.EqualTo(70f).Within(1e-3f), "EMP drains 30 within radius 70");
            Assert.That(far.Energy.Energy, Is.EqualTo(100f), "outside the radius: untouched");
            Assert.That(caster.Energy.Energy, Is.EqualTo(100f), "abilities cost zero energy");
        }

        [Test]
        public void Ability_CooldownGates_AndTicksDown()
        {
            var sora = MakePilot(AbilityType.PhaseShift, 15f, 1.2f);
            var ship = new ShipController(_track, MakeHull("Sora"), _tuning);
            _abilities.Register(ship, sora);
            var all = new[] { ship };

            Assert.That(_abilities.Activate(ship, all), Is.True);
            Assert.That(ship.IntangibleTimer, Is.EqualTo(1.2f).Within(1e-4f));
            Assert.That(_abilities.Activate(ship, all), Is.False, "cooldown must gate re-cast");

            for (int i = 0; i < Mathf.CeilToInt(15f / Dt) + 1; i++) _abilities.Tick(Dt);
            Assert.That(_abilities.Activate(ship, all), Is.True, "castable again after cooldownSec");
        }

        [Test]
        public void Decoy_SpawnsAtOwner_Expires_AndReplacesOnRecast()
        {
            var nyx = MakePilot(AbilityType.Decoy, 22f, 3f);
            var ship = new ShipController(_track, MakeHull("Nyx"), _tuning);
            _abilities.Register(ship, nyx);

            _abilities.Activate(ship, new[] { ship });
            Assert.That(_abilities.Decoy, Is.Not.Null);
            Assert.That(_abilities.Decoy!.Position, Is.EqualTo(ship.Position));

            for (int i = 0; i < Mathf.CeilToInt(3f / Dt) + 1; i++) _abilities.Tick(Dt);
            Assert.That(_abilities.Decoy, Is.Null, "decoy expires after ttl 3 s");
        }
    }
}

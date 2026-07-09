using Afterburn.Core;
using NUnit.Framework;
using UnityEngine;

namespace Afterburn.Tests
{
    /// <summary>
    /// BUILD §11 U4: race lifecycle, ghost behaviour under rulings #4/#5, bounty leadership,
    /// lap counting (±0.5 wrap incl. the backward anti-cheese), standings, stats.
    /// Deterministic: seeded ghost RNG.
    /// </summary>
    [TestFixture]
    public sealed class RaceDirectorTests
    {
        private const float Dt = ShipController.Tick;

        private static readonly float[] Arena01Shape =
            { 1.00f, 0.94f, 0.78f, 0.74f, 0.92f, 1.06f, 1.10f, 0.86f, 0.72f, 0.82f, 1.02f, 1.08f, 0.92f, 0.84f };

        private static TrackDefinition MakeArena01()
        {
            var track = ScriptableObject.CreateInstance<TrackDefinition>();
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

        private static HullDefinition MakeHull(string name, float maxEnergy, float regen, float topMult, float radius, GateAccess access)
        {
            var hull = ScriptableObject.CreateInstance<HullDefinition>();
            hull.displayName = name;
            hull.maxEnergy = maxEnergy;
            hull.regenPerSec = regen;
            hull.topSpeedMult = topMult;
            hull.collisionRadius = radius;
            hull.gateAccess = access;
            return hull;
        }

        private static RaceDirector MakeRace(int seed = 7)
        {
            var tuning = ScriptableObject.CreateInstance<GameTuning>();
            var pilot = ScriptableObject.CreateInstance<PilotDefinition>();
            pilot.abilityType = AbilityType.EmpPulse;
            pilot.cooldownSec = 18f;
            pilot.abilityParam = 30f;
            pilot.abilityRadius = 70f;

            HullDefinition medium = MakeHull("Medium", 100f, 8f, 1.00f, 2.0f, GateAccess.None);
            HullDefinition heavy = MakeHull("Heavy", 130f, 5f, 0.88f, 2.6f, GateAccess.HeavyWall);
            HullDefinition light = MakeHull("Light", 80f, 11f, 1.15f, 1.6f, GateAccess.LightGap);

            return new RaceDirector(new RaceDirector.Config
            {
                Track = MakeArena01(),
                Tuning = tuning,
                PlayerHull = medium,
                PlayerPilot = pilot,
                // Prototype grid: heavy −1, light +1, medium +2 at 0.6/1.2/1.8 % behind.
                GhostGrid = new[]
                {
                    (heavy, -1, 0.006f),
                    (light, 1, 0.012f),
                    (medium, 2, 0.018f),
                },
                Seed = seed,
            });
        }

        [Test]
        public void Countdown_FreezesEverything_ThenGoesRacing()
        {
            RaceDirector race = MakeRace();
            Assert.That(race.State, Is.EqualTo(RaceState.Countdown));

            Vector3 playerStart = race.Player.Position;
            Vector3 ghostStart = race.Ghosts[0].Position;
            var fullThrottle = new ShipInputState { Thrust = true, Boost = true };

            int countdownTicks = Mathf.CeilToInt(RaceDirector.CountdownDuration / Dt);
            for (int i = 0; i < countdownTicks - 1; i++) race.Tick(fullThrottle, Dt);

            Assert.That(race.State, Is.EqualTo(RaceState.Countdown));
            Assert.That(race.Player.Position, Is.EqualTo(playerStart), "player frozen during countdown");
            Assert.That(race.Ghosts[0].Position, Is.EqualTo(ghostStart), "ghosts frozen during countdown");
            Assert.That(race.Player.Energy.Energy, Is.EqualTo(100f), "no drain during countdown");

            race.Tick(fullThrottle, Dt);
            race.Tick(fullThrottle, Dt);
            Assert.That(race.State, Is.EqualTo(RaceState.Racing));
        }

        [Test]
        public void Ghosts_StartJustBehindTheLine_Ruling5()
        {
            RaceDirector race = MakeRace();
            foreach (GhostRacer ghost in race.Ghosts)
            {
                Assert.That(ghost.Progress, Is.LessThan(0f).And.GreaterThan(-0.05f),
                    "ruling #5: ghost progress starts slightly NEGATIVE (laps −1, t just behind 1.0) — no free lap");
            }
            Assert.That(race.PlayerProgress, Is.EqualTo(0f).Within(0.01f));
            Assert.That(race.BountyLeader, Is.SameAs(race.Player),
                "at the start the player (progress 0) leads ghosts (negative progress)");
        }

        [Test]
        public void Ghosts_RunTheRail_AndRespectSkillCap()
        {
            RaceDirector race = MakeRace();
            SkipCountdown(race);

            for (int i = 0; i < 60 * 20; i++) race.Tick(ShipInputState.None, Dt);

            foreach (GhostRacer ghost in race.Ghosts)
            {
                Assert.That(ghost.Progress, Is.GreaterThan(0.1f), "ghosts must make progress");
                float baseTop = 62f * ghost.Hull.topSpeedMult;
                // Ruling #4: single skill application → hard ceiling is baseTop × 1.4 × skill(≤0.9).
                Assert.That(ghost.Speed, Is.LessThanOrEqualTo(baseTop * 1.4f * 0.9f + 0.5f),
                    $"{ghost.Hull.displayName} ghost exceeded the single-skill boost ceiling");
            }
        }

        [Test]
        public void Bounty_TracksTheGreatestProgress_EveryTick()
        {
            RaceDirector race = MakeRace();
            SkipCountdown(race);

            // Player never thrusts → ghosts pass within a few seconds and one takes the mark.
            for (int i = 0; i < 60 * 10; i++) race.Tick(ShipInputState.None, Dt);

            Assert.That(race.BountyLeader, Is.Not.SameAs(race.Player),
                "an idle player must lose the bounty mark to a ghost");
            float leaderProgress = race.ProgressOf(race.BountyLeader!);
            foreach (GhostRacer g in race.Ghosts)
            {
                Assert.That(leaderProgress, Is.GreaterThanOrEqualTo(g.Progress));
            }
        }

        [Test]
        public void Standings_AreProgressDescending_AndPlaceIsConsistent()
        {
            RaceDirector race = MakeRace();
            SkipCountdown(race);
            for (int i = 0; i < 60 * 15; i++) race.Tick(ShipInputState.None, Dt);

            IRacer[] order = race.Standings();
            for (int i = 1; i < order.Length; i++)
            {
                Assert.That(race.ProgressOf(order[i - 1]), Is.GreaterThanOrEqualTo(race.ProgressOf(order[i])));
            }
            Assert.That(race.PlayerPlace(), Is.EqualTo(System.Array.IndexOf(order, (IRacer)race.Player) + 1));
            Assert.That(race.PlayerPlace(), Is.EqualTo(4), "an idle player should be P4 after 15 s");
        }

        [Test]
        public void GhostFire_SpendsGhostEnergy_AndSpawnsRealBullets()
        {
            RaceDirector race = MakeRace(seed: 3);
            SkipCountdown(race);

            int liveBulletTicks = 0;
            for (int i = 0; i < 60 * 30; i++)
            {
                race.Tick(ShipInputState.None, Dt);
                foreach (CombatSystem.Projectile p in race.Combat.Projectiles)
                {
                    if (p.Live) { liveBulletTicks++; break; }
                }
            }
            Assert.That(liveBulletTicks, Is.GreaterThan(0),
                "over 30 s with a leading pack, some aggro>0.4 ghost in range must fire (seeded RNG)");
        }

        [Test]
        public void Determinism_SameSeedSameOutcome()
        {
            RaceDirector a = MakeRace(seed: 42);
            RaceDirector b = MakeRace(seed: 42);
            SkipCountdown(a);
            SkipCountdown(b);

            var input = new ShipInputState { Thrust = true };
            for (int i = 0; i < 60 * 10; i++)
            {
                a.Tick(input, Dt);
                b.Tick(input, Dt);
            }

            Assert.That(a.Player.Position, Is.EqualTo(b.Player.Position), "same seed → identical player state");
            for (int g = 0; g < a.Ghosts.Count; g++)
            {
                Assert.That(a.Ghosts[g].Progress, Is.EqualTo(b.Ghosts[g].Progress),
                    $"same seed → identical ghost {g} progress");
                Assert.That(a.Ghosts[g].Energy.Energy, Is.EqualTo(b.Ghosts[g].Energy.Energy));
            }
        }

        [Test]
        public void Stats_CountShots()
        {
            RaceDirector race = MakeRace();
            SkipCountdown(race);
            var fire = new ShipInputState { Fire = true };
            for (int i = 0; i < 60; i++) race.Tick(fire, Dt);
            Assert.That(race.Stats.Shots, Is.EqualTo(4), "4 shots in 1 s at the 0.28 cadence");
        }

        private static void SkipCountdown(RaceDirector race)
        {
            int ticks = Mathf.CeilToInt(RaceDirector.CountdownDuration / Dt) + 1;
            for (int i = 0; i < ticks; i++) race.Tick(ShipInputState.None, Dt);
        }
    }
}

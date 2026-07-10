using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>Race lifecycle states (BUILD §7.8). The prototype had no countdown — its addition
    /// is a design upgrade (DesignReview ruling #1); the parity trace-diff window starts at green.
    /// Grid = pre-countdown hold for the U5 lineup sweep (the mandatory cosmetic billboard).</summary>
    public enum RaceState
    {
        Grid = 0,
        Countdown = 1,
        Racing = 2,
        Finished = 3,
    }

    /// <summary>Per-race player stats (PortSpec §9).</summary>
    public struct RaceStats
    {
        public int Shots;
        public int Hits;
        public int BountyHits;
        public float BoostTime;
    }

    /// <summary>
    /// The race orchestrator (BUILD §7.8): owns the roster (player + 3 synthetic ghosts per the
    /// prototype's grid), the fixed-tick pipeline (player → ghosts → abilities → combat →
    /// progress → bounty → finish), lap counting with the ±0.5 wrap test (anti-cheese backward
    /// case for the player only), the every-frame bounty leader, standings, and stats.
    /// Deterministic given (config, seed) — the ghost RNG is seeded per race.
    /// </summary>
    public sealed class RaceDirector
    {
        public const float CountdownDuration = 3f;

        public sealed class Config
        {
            public TrackDefinition Track = null!;
            public GameTuning Tuning = null!;
            public HullDefinition PlayerHull = null!;
            public PilotDefinition PlayerPilot = null!;
            /// <summary>Prototype roster: (hull, lane, startBehindFraction) — heavy −1, light +1, medium +2.</summary>
            public (HullDefinition hull, int lane, float behind)[] GhostGrid =
                Array.Empty<(HullDefinition, int, float)>();
            public int Seed = 1;
        }

        private readonly Config _config;
        private readonly List<IRacer> _racers = new();
        private readonly List<GhostRacer> _ghosts = new();
        private RaceStats _stats;

        public RaceDirector(Config config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            Track = new TrackSystem(config.Track);
            Track.ResetRaceState();                            // ruling #3

            Abilities = new PilotAbilitySystem(config.Tuning);
            Combat = new CombatSystem(config.Tuning, Abilities, () => BountyLeader);
            Contacts = new ShipContactSystem(config.Tuning);
            Gates = new GateFeatureSystem(Track, config.Track);   // D15 track features

            Player = new ShipController(Track, config.PlayerHull, config.Tuning, lane: 0);
            Abilities.Register(Player, config.PlayerPilot);
            Combat.Attach(Player);
            Gates.Register(Player);
            _racers.Add(Player);

            var rng = new System.Random(config.Seed);
            foreach ((HullDefinition hull, int lane, float behind) in config.GhostGrid)
            {
                var ghost = new GhostRacer(Track, hull, config.Tuning, lane, behind, rng);
                ghost.OnFired += g => Combat.Fire(g, siphon: false);
                _ghosts.Add(ghost);
                _racers.Add(ghost);
            }

            Player.OnFired += _ => _stats.Shots++;
            Combat.OnHitLanded += (from, _, _, wasLeader) =>
            {
                if (!ReferenceEquals(from, Player)) return;
                _stats.Hits++;
                if (wasLeader) _stats.BountyHits++;
            };

            State = RaceState.Grid;
            CountdownRemaining = CountdownDuration;
            BountyLeader = Player;                             // prototype: player seeds the reduce
        }

        /// <summary>Called by the flow when the lineup sweep ends (or immediately in dev/tests).</summary>
        public void BeginCountdown()
        {
            if (State == RaceState.Grid) State = RaceState.Countdown;
        }

        // ---- Read surface (View/HUD/tests) ------------------------------------
        public TrackSystem Track { get; }
        public PilotAbilitySystem Abilities { get; }
        public CombatSystem Combat { get; }
        public ShipContactSystem Contacts { get; }
        public GateFeatureSystem Gates { get; }
        public ShipController Player { get; }
        public IReadOnlyList<IRacer> Racers => _racers;
        public IReadOnlyList<GhostRacer> Ghosts => _ghosts;
        public RaceState State { get; private set; }
        public float CountdownRemaining { get; private set; }
        public float RaceTime { get; private set; }
        public IRacer? BountyLeader { get; private set; }
        public RaceStats Stats => _stats;

        // Player progress (prototype updatePlayerProgress).
        public int PlayerLaps { get; private set; }
        public float PlayerFrac { get; private set; }
        public float PlayerProgress => PlayerLaps + PlayerFrac;
        public bool PlayerFinished { get; private set; }
        public float PlayerFinishTime { get; private set; }

        /// <summary>Completed lap times (U5 summary delta / future medals). Forward crossings only.</summary>
        public List<float> PlayerLapTimes { get; } = new();
        private float _lapMark;

        /// <summary>Advance one fixed tick. Player input is ignored outside Racing.</summary>
        public void Tick(ShipInputState input, float dt)
        {
            switch (State)
            {
                case RaceState.Grid:
                    return;

                case RaceState.Countdown:
                    CountdownRemaining -= dt;
                    if (CountdownRemaining <= 0f) State = RaceState.Racing;
                    return;

                case RaceState.Finished:
                    return;

                case RaceState.Racing:
                    RaceTime += dt;

                    Player.Step(input, dt);
                    Gates.Tick(Player);                          // D15: gate features fire post-step
                    if (Player.Boosting) _stats.BoostTime += dt;
                    if (input.AbilityEdge) Abilities.Activate(Player, _racers);

                    var finishedFlags = new bool[_racers.Count];
                    finishedFlags[0] = PlayerFinished;
                    for (int g = 0; g < _ghosts.Count; g++)
                    {
                        GhostRacer ghost = _ghosts[g];
                        if (!ghost.Finished) ghost.Step(dt, BountyLeader, Abilities.Decoy, RaceTime);
                        finishedFlags[g + 1] = ghost.Finished;
                    }

                    Abilities.Tick(dt);
                    Contacts.Tick(dt, Player, _ghosts);           // D14: tangible ships
                    Combat.Tick(dt, _racers, finishedFlags);

                    UpdatePlayerProgress();
                    UpdateBounty();

                    if (PlayerFinished) State = RaceState.Finished;
                    return;
            }
        }

        /// <summary>Prototype: frac = nearest/700 with the ±0.5 wrap test (backward case anti-cheese).</summary>
        private void UpdatePlayerProgress()
        {
            int i = Track.Nearest(Player.Position, Player.NearestIndex);
            float frac = i / (float)Track.SampleCount;
            float d = frac - PlayerFrac;
            if (d < -0.5f)
            {
                PlayerLaps++;
                PlayerLapTimes.Add(RaceTime - _lapMark);
                _lapMark = RaceTime;
            }
            else if (d > 0.5f) PlayerLaps--;
            PlayerFrac = frac;

            if (!PlayerFinished && PlayerProgress >= _config.Tuning.raceLaps)
            {
                PlayerFinished = true;
                PlayerFinishTime = RaceTime;
            }
        }

        /// <summary>Prototype updateBounty: strictly greatest progress, re-evaluated every tick,
        /// ties keep the earlier racer (player seeds). Includes finished racers.</summary>
        private void UpdateBounty()
        {
            IRacer best = Player;
            float bestProgress = PlayerProgress;
            foreach (GhostRacer ghost in _ghosts)
            {
                if (ghost.Progress > bestProgress)
                {
                    best = ghost;
                    bestProgress = ghost.Progress;
                }
            }
            BountyLeader = best;
        }

        /// <summary>Standings: progress descending (PortSpec §9 — the double-sort's net effect).</summary>
        public IRacer[] Standings()
        {
            var order = _racers.ToArray();
            Array.Sort(order, (a, b) => ProgressOf(b).CompareTo(ProgressOf(a)));
            return order;
        }

        public float ProgressOf(IRacer racer) =>
            ReferenceEquals(racer, Player) ? PlayerProgress : ((GhostRacer)racer).Progress;

        /// <summary>Player place, 1-based (prototype: order.indexOf(player) + 1).</summary>
        public int PlayerPlace()
        {
            IRacer[] order = Standings();
            for (int i = 0; i < order.Length; i++)
            {
                if (ReferenceEquals(order[i], Player)) return i + 1;
            }
            return order.Length;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterburn.Core
{
    /// <summary>
    /// Pilot abilities (BUILD §7.4 / PortSpec §7): one per pilot, cooldown-gated, ZERO energy
    /// cost — the monetisation guardrail's mechanical root (upgrades touch cooldownSec only).
    ///   Vex/EmpPulse: drain abilityParam energy from every racer within abilityRadius.
    ///   Sora/PhaseShift: intangible for abilityParam seconds (skips walls AND hits).
    ///   Kade/Siphon: arm the next landed hit (flat steal, capped by ruling #7 in CombatSystem).
    ///   Nyx/Decoy: spawn a static decoy for abilityParam seconds; ghosts range-check against it.
    /// </summary>
    public sealed class PilotAbilitySystem
    {
        public sealed class RacerAbility
        {
            public PilotDefinition Pilot = null!;
            public float Cooldown;
            public float CooldownMax;
            public bool SiphonArmed;
        }

        public sealed class DecoyState
        {
            public Vector3 Position;
            public float Ttl;
            public IRacer Owner = null!;
        }

        private readonly GameTuning _tuning;
        private readonly Dictionary<IRacer, RacerAbility> _state = new();

        public PilotAbilitySystem(GameTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>The live decoy, if any (prototype: re-cast replaces the old one).</summary>
        public DecoyState? Decoy { get; private set; }

        public event Action<IRacer, AbilityType>? OnAbilityActivated;
        public event Action<IRacer>? OnDecoySpawned;

        public void Register(IRacer ship, PilotDefinition pilot)
        {
            _state[ship] = new RacerAbility { Pilot = pilot };
        }

        public RacerAbility StateOf(IRacer ship) => _state[ship];

        public bool IsSiphonArmed(IRacer ship) =>
            _state.TryGetValue(ship, out RacerAbility? s) && s.SiphonArmed;

        /// <summary>Consumed by CombatSystem on a landed siphon-flagged hit (prototype onHit).</summary>
        public void ConsumeSiphon(IRacer ship)
        {
            if (_state.TryGetValue(ship, out RacerAbility? s)) s.SiphonArmed = false;
        }

        /// <summary>The steal magnitude for a racer's siphon — the pilot's abilityParam (Kade: 25).</summary>
        public float SiphonAmountOf(IRacer ship) =>
            _state.TryGetValue(ship, out RacerAbility? s) ? s.Pilot.abilityParam : 0f;

        /// <summary>
        /// Prototype activateAbility: blocked while cooling; cd = pilot.cooldownSec × cooldownScale.
        /// Returns true when the ability fired.
        /// </summary>
        public bool Activate(IRacer ship, IReadOnlyList<IRacer> allRacers)
        {
            if (!_state.TryGetValue(ship, out RacerAbility? s) || s.Cooldown > 0f) return false;

            s.CooldownMax = s.Pilot.cooldownSec * _tuning.abilityCooldownScale;
            s.Cooldown = s.CooldownMax;

            switch (s.Pilot.abilityType)
            {
                case AbilityType.EmpPulse:
                    foreach (IRacer other in allRacers)
                    {
                        if (other == ship) continue;
                        if (Vector3.Distance(other.Position, ship.Position) < s.Pilot.abilityRadius)
                        {
                            other.Energy.Damage(s.Pilot.abilityParam);
                        }
                    }
                    break;

                case AbilityType.PhaseShift:
                    ship.ApplyIntangible(s.Pilot.abilityParam);
                    break;

                case AbilityType.Siphon:
                    s.SiphonArmed = true;
                    break;

                case AbilityType.Decoy:
                    Decoy = new DecoyState
                    {
                        Position = ship.Position,
                        Ttl = s.Pilot.abilityParam,
                        Owner = ship,
                    };
                    OnDecoySpawned?.Invoke(ship);
                    break;
            }

            OnAbilityActivated?.Invoke(ship, s.Pilot.abilityType);
            return true;
        }

        /// <summary>Per fixed tick: cooldowns and the decoy lifetime.</summary>
        public void Tick(float dt)
        {
            foreach (RacerAbility s in _state.Values)
            {
                if (s.Cooldown > 0f) s.Cooldown = Mathf.Max(0f, s.Cooldown - dt);
            }
            if (Decoy != null)
            {
                Decoy.Ttl -= dt;
                if (Decoy.Ttl <= 0f) Decoy = null;
            }
        }
    }
}

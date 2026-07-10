using UnityEngine;

namespace Afterburn.UI
{
    /// <summary>
    /// The energy-action wheel state machine (UIEnvSpec §4.3) — pure logic, no Unity objects,
    /// unit-tested. Hub = FIRE (hold, autofires at the sim's cadence). Upper 100° arc = BOOST
    /// (hold). Lower 100° arc = SHIELD (hold). A held touch may slide between zones —
    /// slide-to-switch releases the old intent the same frame the new engages. STRUCTURALLY at
    /// most one held-mode intent per frame: a position maps to exactly one zone. This enforces
    /// boost/shield exclusion at the input layer; Core re-arbitrates regardless (§2).
    /// </summary>
    public sealed class WheelLogic
    {
        public enum Zone
        {
            None = 0,
            Hub = 1,
            Boost = 2,
            Shield = 3,
        }

        public struct Intents
        {
            public bool Fire;
            public bool Boost;
            public bool Shield;
        }

        private readonly float _hubRadius;
        private readonly float _outerRadius;
        private readonly float _arcHalfAngle;

        public WheelLogic(float hubRadius = 120f, float outerRadius = 300f, float arcDegrees = 100f)
        {
            _hubRadius = hubRadius;
            _outerRadius = outerRadius;
            _arcHalfAngle = arcDegrees * 0.5f;
        }

        public Zone ActiveZone { get; private set; }

        /// <summary>Classify a position relative to the wheel centre (canvas units).</summary>
        public Zone Classify(Vector2 rel)
        {
            float r = rel.magnitude;
            if (r <= _hubRadius) return Zone.Hub;
            if (r > _outerRadius) return Zone.None;

            // Angle from straight-up; upper arc = Boost, mirrored lower arc = Shield.
            float angleFromUp = Vector2.Angle(Vector2.up, rel);
            if (angleFromUp <= _arcHalfAngle) return Zone.Boost;
            if (angleFromUp >= 180f - _arcHalfAngle) return Zone.Shield;
            return Zone.None;                                  // dead sectors left/right
        }

        /// <summary>
        /// Advance one frame. `touchHeld` false = released (all intents drop).
        /// Returns this frame's intents — never Boost and Shield together, by construction.
        /// </summary>
        public Intents Update(bool touchHeld, Vector2 rel)
        {
            if (!touchHeld)
            {
                ActiveZone = Zone.None;
                return default;
            }

            ActiveZone = Classify(rel);
            return new Intents
            {
                Fire = ActiveZone == Zone.Hub,
                Boost = ActiveZone == Zone.Boost,
                Shield = ActiveZone == Zone.Shield,
            };
        }
    }
}

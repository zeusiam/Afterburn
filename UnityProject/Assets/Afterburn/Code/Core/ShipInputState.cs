namespace Afterburn.Core
{
    /// <summary>
    /// One fixed tick's worth of racer intent (PortSpec: the prototype's Input.state).
    /// Pure data — View/UI fill it (keyboard, touch wheel, ghost playback), Core consumes it.
    /// The touch wheel additionally enforces boost/shield exclusion at the input layer, but Core
    /// NEVER relies on that: <see cref="ShipController"/> re-arbitrates every tick (§2).
    /// </summary>
    public struct ShipInputState
    {
        public bool Thrust;
        public bool Brake;
        public bool Left;
        public bool Right;
        public bool Boost;
        public bool Shield;

        /// <summary>Held — autofires at the tap-cooldown cadence (PortSpec div #13).</summary>
        public bool Fire;

        /// <summary>Edge-triggered: consumed by the sim the tick it is seen.</summary>
        public bool AbilityEdge;

        public static ShipInputState None => default;
    }
}

namespace Afterburn.Core
{
    /// <summary>
    /// The player's selected loadout, carried across scene loads (MainMenu → Race).
    /// Null = the scene's serialized defaults (dev flow straight into Race.unity still works).
    /// </summary>
    public static class RaceLoadout
    {
        public static HullDefinition? Hull;
        public static PilotDefinition? Pilot;
    }

    /// <summary>Race session context: seed control for rematch (same ghosts) vs fresh run.</summary>
    public static class RaceContext
    {
        public static int Seed = 1;
        public static bool UseFixedSeed;

        /// <summary>Session-best lap for the summary delta line (per track id).</summary>
        public static float BestLapSeconds = float.PositiveInfinity;
    }
}

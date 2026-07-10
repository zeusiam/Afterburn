using UnityEngine;

namespace Afterburn.UI
{
    /// <summary>
    /// SIGNAL VOID color tokens (UIEnvSpec §1.3) — the single source. No hex literal appears in
    /// any component; everything references these. Core roles are LOCKED: gold = leader/bounty
    /// ONLY, red = damage/danger ONLY, everywhere including future cosmetics.
    /// </summary>
    public static class AfterburnPalette
    {
        // Core roles (locked)
        public static readonly Color Void = Hex("#05070F");
        public static readonly Color Cyan = Hex("#37D0FF");     // player/track guidance, regen/coast
        public static readonly Color Violet = Hex("#9D7BFF");   // shield/phase
        public static readonly Color Orange = Hex("#FF8A3C");   // boost/heat
        public static readonly Color Gold = Hex("#FFD23F");     // leader/bounty/reward ONLY
        public static readonly Color Red = Hex("#FF4D6D");      // damage/danger ONLY
        public static readonly Color WallBlue = Hex("#2B4D8F"); // passive structure
        public static readonly Color Teal = Hex("#38F5C9");     // self-marker, siphon
        public static readonly Color Star = Hex("#6F8BD0");     // tertiary/ambient

        // Mode tokens
        public static readonly Color ModeBoost = Orange;
        public static readonly Color ModeShield = Violet;
        public static readonly Color ModeCoast = Cyan;
        public static readonly Color ModeFire = Red;            // flash only

        // UI surfaces / text tiers
        public static readonly Color Surface0 = Hex("#05070F");
        public static readonly Color Surface1 = Hex("#0A1C3A");
        public static readonly Color Surface2 = Hex("#12203C");
        public static readonly Color Surface3 = Hex("#1C2F57");
        public static readonly Color Stroke = Hex("#2B4D8F");
        public static readonly Color TextHi = Hex("#EAF2FF");
        public static readonly Color TextMid = Hex("#9FC4FF");
        public static readonly Color TextLow = Hex("#6F8BD0");

        public static Color WithAlpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        private static Color Hex(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
    }

    /// <summary>Type scale tokens (UIEnvSpec §1.4) — pt at the 2796×1290 reference canvas.</summary>
    public static class AfterburnType
    {
        public const float Countdown = 220f;
        public const float Display = 96f;
        public const float H1 = 72f;
        public const float H2 = 56f;
        public const float Body = 40f;
        public const float Label = 34f;
        public const float Caption = 28f;
        public const float Micro = 22f;
    }

    /// <summary>Motion tokens (UIEnvSpec §4.6). LAW: the primary energy fill is NEVER tweened.</summary>
    public static class AfterburnMotion
    {
        public const float ScreenIn = 0.22f;
        public const float ScreenOut = 0.16f;
        public const float ButtonPress = 0.08f;
        public const float ButtonRelease = 0.12f;
        public const float CardSelect = 0.15f;
        public const float ChipSwap = 0.09f;
        public const float ToastIn = 0.12f;
        public const float ToastOut = 0.18f;
        public const float ToastHold = 1.1f;                    // prototype parity
        public const float LossGhostDelay = 0.25f;
        public const float LossGhostDuration = 0.4f;
        public const float CooldownReadyPulse = 0.3f;
        public const float DenialShake = 0.06f;
    }
}

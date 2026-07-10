using Afterburn.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Afterburn.UI
{
    /// <summary>
    /// Touch layout (UIEnvSpec §4.3): left 38% = thrust-hold + drag-steer (dead zone 24 px, full
    /// deflection ±220 px, release = coast — regen is a physical release of the thumb); right =
    /// the energy-action wheel (hub FIRE / upper arc BOOST / lower arc SHIELD, slide-to-switch,
    /// exclusion enforced by <see cref="WheelLogic"/>) + ability button above it.
    /// Produces a <see cref="ShipInputState"/> each frame; the runner ORs it with keyboard.
    /// Mouse drives it in-editor for dev.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TouchControls : MonoBehaviour
    {
        private const float SteerZoneFraction = 0.38f;
        private const float SteerDeadZone = 24f;
        private const float WheelMargin = 96f;

        private readonly WheelLogic _wheel = new();
        private RectTransform? _root;
        private Canvas? _canvas;
        private RectTransform? _wheelRt;
        private Image? _hub;
        private Image? _boostArc;
        private Image? _shieldArc;
        private Vector2 _wheelCentreScreen;
        private float _canvasScale = 1f;

        private Vector2 _steerOrigin;
        private int _steerTouchId = -1;
        private int _wheelTouchId = -1;
        private bool _abilityEdge;

        public ShipInputState Current { get; private set; }

        private void Awake()
        {
            _root = UIFactory.NewScreenCanvas("TouchControls", sortOrder: 6);
            _canvas = _root.GetComponentInParent<Canvas>();

            // Wheel visuals (bottom-right): outer ring zones + hub. Pivot = centre so the
            // anchored position IS the wheel centre (matches the touch-classification math).
            RectTransform wheelRt = UIFactory.Fixed(_root, "Wheel",
                new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-WheelMargin - 300f, WheelMargin + 300f), new Vector2(600f, 600f));
            _wheelRt = wheelRt;

            _boostArc = ZoneArc(wheelRt, "BoostZone", new Vector2(0f, 150f), AfterburnPalette.ModeBoost, "BOOST");
            _shieldArc = ZoneArc(wheelRt, "ShieldZone", new Vector2(0f, -150f), AfterburnPalette.ModeShield, "SHIELD");

            RectTransform hubRt = UIFactory.Fixed(wheelRt, "Hub", new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 240f));
            _hub = UIFactory.Rect(hubRt, "Bg", AfterburnPalette.WithAlpha(AfterburnPalette.ModeFire, 0.4f));
            UIFactory.Outline(_hub.gameObject);
            UIFactory.Label(hubRt, "Text", "FIRE", AfterburnType.Label, AfterburnPalette.TextHi,
                TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);

            // Ability tap region — sits exactly under the HUD's cooldown radial (above the wheel).
            RectTransform abilityTap = UIFactory.Fixed(_root, "AbilityTap",
                new Vector2(1f, 0f), new Vector2(0.5f, 0.5f),
                new Vector2(-WheelMargin - 300f, WheelMargin + 300f + 385f), new Vector2(170f, 170f));
            var tapImg = abilityTap.gameObject.AddComponent<Image>();
            tapImg.color = Color.clear;
            tapImg.raycastTarget = true;
            abilityTap.gameObject.AddComponent<Button>().onClick.AddListener(TriggerAbility);

            // Steer zone hint (left).
            RectTransform steerHint = UIFactory.Panel(_root, "SteerZone",
                Vector2.zero, new Vector2(SteerZoneFraction, 1f), Vector2.zero, Vector2.zero);
            Image hint = UIFactory.Rect(steerHint, "Tint", AfterburnPalette.WithAlpha(AfterburnPalette.Cyan, 0.03f));
            hint.raycastTarget = false;
            UIFactory.Label(
                UIFactory.Fixed(steerHint, "Hint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 40f), new Vector2(700f, 50f)),
                "Text", "HOLD = THRUST · DRAG = STEER", AfterburnType.Micro, AfterburnPalette.TextLow);
        }

        private static Image ZoneArc(RectTransform wheel, string zoneName, Vector2 offset, Color color, string label)
        {
            RectTransform rt = UIFactory.Fixed(wheel, zoneName, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), offset, new Vector2(420f, 190f));
            Image img = UIFactory.Rect(rt, "Bg", AfterburnPalette.WithAlpha(color, 0.16f));
            UIFactory.Label(rt, "Text", label, AfterburnType.Caption, color,
                TMPro.TextAlignmentOptions.Center, TMPro.FontStyles.Bold);
            return img;
        }

        private void Update()
        {
            // Wheel centre in screen space — read from the ACTUAL visual rect, never re-derived:
            // the SafeArea inset shifts the visuals inboard (notch ~140 px on iPhone), and any
            // assumption about the corner desynchronises touch from what the player sees.
            // Overlay-canvas world positions ARE screen pixels.
            _canvasScale = _canvas != null ? _canvas.scaleFactor : Screen.width / UIFactory.RefWidth;
            if (_wheelRt != null)
            {
                Vector3 wheelWorld = _wheelRt.position;
                _wheelCentreScreen = new Vector2(wheelWorld.x, wheelWorld.y);
            }

            var input = new ShipInputState();
            bool steerActive = false, wheelActive = false;
            Vector2 steerPos = default, wheelPos = default;

            Touchscreen? ts = Touchscreen.current;
            if (ts != null)
            {
                foreach (var touch in ts.touches)
                {
                    if (!touch.press.isPressed) continue;
                    Vector2 pos = touch.position.ReadValue();
                    int id = touch.touchId.ReadValue();
                    if (pos.x < Screen.width * SteerZoneFraction && (_steerTouchId == -1 || _steerTouchId == id))
                    {
                        if (_steerTouchId == -1) { _steerTouchId = id; _steerOrigin = pos; }
                        steerActive = true;
                        steerPos = pos;
                    }
                    else if (_wheelTouchId == -1 || _wheelTouchId == id)
                    {
                        if (_wheelTouchId == -1) _wheelTouchId = id;
                        wheelActive = true;
                        wheelPos = pos;
                    }
                }
            }

            // Editor/dev fallback: mouse acts as a single touch.
            Mouse? mouse = Mouse.current;
            if (!steerActive && !wheelActive && mouse != null && mouse.leftButton.isPressed)
            {
                Vector2 pos = mouse.position.ReadValue();
                if (pos.x < Screen.width * SteerZoneFraction)
                {
                    if (_steerTouchId == -1) { _steerTouchId = 0; _steerOrigin = pos; }
                    steerActive = true;
                    steerPos = pos;
                }
                else
                {
                    wheelActive = true;
                    wheelPos = pos;
                }
            }

            if (!steerActive) _steerTouchId = -1;
            if (!wheelActive) _wheelTouchId = -1;

            // Steer: hold = thrust; drag past the dead zone = steer.
            if (steerActive)
            {
                input.Thrust = true;
                float dx = (steerPos.x - _steerOrigin.x) / _canvasScale;
                if (dx < -SteerDeadZone) input.Left = true;
                else if (dx > SteerDeadZone) input.Right = true;
            }

            // Wheel: classify relative position in canvas units.
            Vector2 rel = (wheelPos - _wheelCentreScreen) / _canvasScale;
            WheelLogic.Intents intents = _wheel.Update(wheelActive, rel);
            input.Fire = intents.Fire;
            input.Boost = intents.Boost;
            input.Shield = intents.Shield;

            if (_abilityEdge)
            {
                input.AbilityEdge = true;
                _abilityEdge = false;
            }

            Current = input;
            UpdateVisuals();
        }

        /// <summary>Wired to the HUD ability button / external tap region.</summary>
        public void TriggerAbility() => _abilityEdge = true;

        private void UpdateVisuals()
        {
            if (_hub == null || _boostArc == null || _shieldArc == null) return;
            WheelLogic.Zone zone = _wheel.ActiveZone;
            _hub.color = AfterburnPalette.WithAlpha(AfterburnPalette.ModeFire,
                zone == WheelLogic.Zone.Hub ? 0.85f : 0.4f);
            _boostArc.color = AfterburnPalette.WithAlpha(AfterburnPalette.ModeBoost,
                zone == WheelLogic.Zone.Boost ? 0.55f : 0.16f);
            _shieldArc.color = AfterburnPalette.WithAlpha(AfterburnPalette.ModeShield,
                zone == WheelLogic.Zone.Shield ? 0.55f : 0.16f);
        }
    }
}

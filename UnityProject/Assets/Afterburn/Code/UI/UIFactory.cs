using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Afterburn.UI
{
    /// <summary>
    /// The component library as code (UIEnvSpec §4.4, runtime-constructed): every screen composes
    /// these constructors — zero one-off styled controls. Canvas 2796×1290 Match 0.5 landscape,
    /// SafeArea on every screen root, tokens only (no literals in screens).
    /// </summary>
    public static class UIFactory
    {
        public const float RefWidth = 2796f;
        public const float RefHeight = 1290f;

        /// <summary>Screen Space Overlay canvas + scaler + raycaster + SafeArea root. Ensures an EventSystem exists.</summary>
        public static RectTransform NewScreenCanvas(string name, int sortOrder = 0)
        {
            EnsureEventSystem();

            var canvasGo = new GameObject(name);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var rootGo = new GameObject("SafeRoot");
            rootGo.transform.SetParent(canvasGo.transform, false);
            var root = rootGo.AddComponent<RectTransform>();
            Stretch(root);
            rootGo.AddComponent<SafeArea>();
            return root;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>Anchored empty rect.</summary>
        public static RectTransform Panel(RectTransform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        /// <summary>Anchored rect of fixed size at an anchor point.</summary>
        public static RectTransform Fixed(RectTransform parent, string name,
            Vector2 anchor, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            RectTransform rt = Panel(parent, name, anchor, anchor, Vector2.zero, Vector2.zero);
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Rect(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            Stretch((RectTransform)go.transform);
            return img;
        }

        private static TMP_FontAsset? _font;

        /// <summary>
        /// Robust default-font resolution: the copied TMP Essentials asset → TMP Settings →
        /// a dynamic font asset built from Unity's built-in typeface. Text must never silently
        /// fail to render because TMP's settings lookup misfired.
        /// </summary>
        public static TMP_FontAsset DefaultFont()
        {
            if (_font != null) return _font;
            _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (_font == null) _font = TMP_Settings.defaultFontAsset;
            if (_font == null)
            {
                Font legacy = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                _font = TMP_FontAsset.CreateFontAsset(legacy);
                Debug.LogWarning("[Afterburn] TMP Essentials font not found — using a dynamic fallback font. " +
                                 "Run Window → TextMeshPro → Import TMP Essential Resources to fix properly.");
            }
            return _font;
        }

        public static TextMeshProUGUI Label(RectTransform parent, string name, string text,
            float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center,
            FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            // Inactive during construction: TMP's Awake would warn about the missing default
            // font before we assign ours — defer Awake until the font is set.
            go.SetActive(false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = DefaultFont();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            Stretch((RectTransform)go.transform);
            go.SetActive(true);
            return tmp;
        }

        /// <summary>Primary/secondary button: Surface fill + stroke frame + TMP label + press scale.</summary>
        public static Button TextButton(RectTransform parent, string name, string label,
            UnityAction onClick, bool primary = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var bg = go.AddComponent<Image>();
            bg.color = primary ? AfterburnPalette.Cyan : AfterburnPalette.Surface2;

            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            button.colors = colors;
            button.onClick.AddListener(onClick);

            if (!primary)
            {
                Image frame = Rect(rt, "Frame", Color.clear);
                Outline(frame.gameObject);
            }

            Label(rt, "Text", label, AfterburnType.Label,
                primary ? AfterburnPalette.Void : AfterburnPalette.TextHi,
                TextAlignmentOptions.Center, FontStyles.Bold);
            return button;
        }

        /// <summary>Horizontal bar: background + left-anchored fill. Set fill via <see cref="SetBarRatio"/>.</summary>
        public static (Image bg, Image fill) Bar(RectTransform parent, string name, Color bgColor, Color fillColor)
        {
            var bgRt = Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var bg = bgRt.gameObject.AddComponent<Image>();
            bg.color = bgColor;
            bg.raycastTarget = false;

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgRt, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fill = fillGo.AddComponent<Image>();
            fill.color = fillColor;
            fill.raycastTarget = false;
            return (bg, fill);
        }

        /// <summary>Frame-true ratio set — direct anchor write, NEVER tweened (the §4.6 law).</summary>
        public static void SetBarRatio(Image fill, float ratio)
        {
            var rt = (RectTransform)fill.transform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }

        public static void Outline(GameObject go)
        {
            var outline = go.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = AfterburnPalette.Stroke;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterburn.UI
{
    /// <summary>
    /// Settings overlay (UIEnvSpec §4.1): own canvas at sortOrder 10, centered Surface1 panel.
    /// Master volume slider (drives <see cref="AudioListener.volume"/>, persisted to
    /// PlayerPrefs "ab.volume"), FOV-kick / shake ON-OFF toggles (persisted, consumed by the
    /// speed-feel package later), disabled RESTORE PURCHASES placeholder, CLOSE.
    /// Built lazily on first <see cref="Show"/>; <see cref="Hide"/> just deactivates.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        private const string VolumeKey = "ab.volume";
        private const string FovKickKey = "ab.fovkick";
        private const string ShakeKey = "ab.shake";

        private GameObject? _canvasGo;
        private Slider? _volumeSlider;
        private TextMeshProUGUI? _fovKickValue;
        private TextMeshProUGUI? _shakeValue;

        public bool IsVisible => _canvasGo != null && _canvasGo.activeSelf;

        public void Show()
        {
            if (_canvasGo == null)
            {
                Build();
            }
            LoadState();
            _canvasGo!.SetActive(true);
        }

        public void Hide()
        {
            if (_canvasGo != null)
            {
                _canvasGo.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_canvasGo != null)
            {
                Destroy(_canvasGo);
            }
        }

        // ---------------------------------------------------------------- build

        private void Build()
        {
            RectTransform root = UIFactory.NewScreenCanvas("SettingsCanvas", sortOrder: 10);
            _canvasGo = root.parent.gameObject;

            // Dim scrim blocking clicks to the screen underneath while the overlay is open.
            Image dim = UIFactory.Rect(root, "Dim",
                AfterburnPalette.WithAlpha(AfterburnPalette.Void, 0.7f));
            dim.raycastTarget = true;

            RectTransform panel = UIFactory.Fixed(root, "Panel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1200f, 800f));
            var panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.color = AfterburnPalette.Surface1;
            UIFactory.Outline(panel.gameObject);

            RectTransform title = UIFactory.Fixed(panel, "Title",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -32f), new Vector2(800f, 90f));
            UIFactory.Label(title, "Text", "SETTINGS",
                AfterburnType.H1, AfterburnPalette.TextHi,
                TextAlignmentOptions.Center, FontStyles.Bold);

            RowLabel(panel, "VolumeLabel", "MASTER VOLUME", -180f);
            _volumeSlider = BuildSlider(panel, new Vector2(500f, -186f), new Vector2(640f, 48f));
            _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

            RowLabel(panel, "FovKickLabel", "FOV KICK", -320f);
            _fovKickValue = BuildToggle(panel, "FovKickToggle", -314f, FovKickKey);

            RowLabel(panel, "ShakeLabel", "SCREEN SHAKE", -440f);
            _shakeValue = BuildToggle(panel, "ShakeToggle", -434f, ShakeKey);

            RectTransform chip = UIFactory.Fixed(panel, "RestoreChip",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, -570f), new Vector2(560f, 80f));
            UIFactory.Rect(chip, "Bg", AfterburnPalette.Surface2);
            UIFactory.Label(chip, "Text", "RESTORE PURCHASES — U6",
                AfterburnType.Caption, AfterburnPalette.TextLow);

            Button closeButton = UIFactory.TextButton(panel, "CloseButton",
                "CLOSE", Hide, primary: false);
            Place((RectTransform)closeButton.transform,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(360f, 110f));
        }

        /// <summary>
        /// Manual uGUI slider from primitives: Stroke track, Cyan fill, 48 px TextHi handle
        /// (square Image standing in for the circle until the icon atlas lands).
        /// </summary>
        private static Slider BuildSlider(RectTransform panel, Vector2 pos, Vector2 size)
        {
            RectTransform sliderRt = UIFactory.Fixed(panel, "VolumeSlider",
                new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);

            RectTransform trackRt = UIFactory.Panel(sliderRt, "Background",
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, -6f), new Vector2(0f, 6f));
            var trackImg = trackRt.gameObject.AddComponent<Image>();
            trackImg.color = AfterburnPalette.Stroke;
            // raycastTarget stays true: tapping the track jumps the handle.

            RectTransform fillAreaRt = UIFactory.Panel(sliderRt, "FillArea",
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, -6f), new Vector2(0f, 6f));
            RectTransform fillRt = UIFactory.Panel(fillAreaRt, "Fill",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var fillImg = fillRt.gameObject.AddComponent<Image>();
            fillImg.color = AfterburnPalette.Cyan;
            fillImg.raycastTarget = false;

            RectTransform handleAreaRt = UIFactory.Panel(sliderRt, "HandleSlideArea",
                Vector2.zero, Vector2.one,
                new Vector2(24f, 0f), new Vector2(-24f, 0f));
            RectTransform handleRt = UIFactory.Panel(handleAreaRt, "Handle",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                Vector2.zero, Vector2.zero);
            handleRt.sizeDelta = new Vector2(48f, 0f);
            var handleImg = handleRt.gameObject.AddComponent<Image>();
            handleImg.color = AfterburnPalette.TextHi;

            var slider = sliderRt.gameObject.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        /// <summary>Small secondary button flipping an ON/OFF PlayerPrefs int (default ON).</summary>
        private static TextMeshProUGUI BuildToggle(RectTransform panel, string name, float y, string prefKey)
        {
            TextMeshProUGUI? valueLabel = null;
            Button button = UIFactory.TextButton(panel, name, "ON", () =>
            {
                bool on = PlayerPrefs.GetInt(prefKey, 1) == 1;
                on = !on;
                PlayerPrefs.SetInt(prefKey, on ? 1 : 0);
                if (valueLabel != null)
                {
                    valueLabel.text = on ? "ON" : "OFF";
                }
            }, primary: false);
            Place((RectTransform)button.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-48f, y), new Vector2(220f, 84f));
            valueLabel = button.GetComponentInChildren<TextMeshProUGUI>();
            return valueLabel;
        }

        // ---------------------------------------------------------------- behaviour

        private void LoadState()
        {
            float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            AudioListener.volume = volume;
            if (_volumeSlider != null)
            {
                _volumeSlider.SetValueWithoutNotify(volume);
            }
            if (_fovKickValue != null)
            {
                _fovKickValue.text = PlayerPrefs.GetInt(FovKickKey, 1) == 1 ? "ON" : "OFF";
            }
            if (_shakeValue != null)
            {
                _shakeValue.text = PlayerPrefs.GetInt(ShakeKey, 1) == 1 ? "ON" : "OFF";
            }
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(VolumeKey, value);
        }

        // ---------------------------------------------------------------- helpers

        private static void RowLabel(RectTransform panel, string name, string text, float y)
        {
            RectTransform rt = UIFactory.Fixed(panel, name,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(48f, y), new Vector2(420f, 60f));
            UIFactory.Label(rt, "Text", text,
                AfterburnType.Label, AfterburnPalette.TextMid,
                TextAlignmentOptions.Left);
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}

using System;
using System.Globalization;
using Afterburn.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Afterburn.UI
{
    /// <summary>
    /// MainMenu / Loadout screen (UIEnvSpec §4.1): hull picker (3 cards, sidegrade copy + stat
    /// compare bars normalized across the roster), pilot picker (4 cards), START RACE primary,
    /// disabled U6 placeholder chips and a Settings toggle. Runtime-constructed from
    /// <see cref="UIFactory"/>; hull/pilot SO arrays are wired by the scene builder via the
    /// serialized fields / public setters below.
    /// </summary>
    public sealed class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private HullDefinition[] hulls = Array.Empty<HullDefinition>();
        public HullDefinition[] Hulls { get => hulls; set => hulls = value; }

        [SerializeField] private PilotDefinition[] pilots = Array.Empty<PilotDefinition>();
        public PilotDefinition[] Pilots { get => pilots; set => pilots = value; }

        private const int DefaultHullIndex = 1;   // Medium
        private const int DefaultPilotIndex = 0;

        private int _selectedHull;
        private int _selectedPilot;

        private Image[] _hullBackgrounds = Array.Empty<Image>();
        private Image[] _hullGlows = Array.Empty<Image>();
        private Image[] _pilotBackgrounds = Array.Empty<Image>();
        private Image[] _pilotGlows = Array.Empty<Image>();

        private SettingsScreen? _settings;

        private void Awake()
        {
            _selectedHull = Mathf.Clamp(DefaultHullIndex, 0, Mathf.Max(0, hulls.Length - 1));
            _selectedPilot = Mathf.Clamp(DefaultPilotIndex, 0, Mathf.Max(0, pilots.Length - 1));

            RectTransform root = UIFactory.NewScreenCanvas("MainMenuCanvas");
            UIFactory.Rect(root, "Background", AfterburnPalette.Surface0);

            RectTransform title = UIFactory.Fixed(root, "Title",
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(64f, -40f), new Vector2(1200f, 110f));
            UIFactory.Label(title, "Text", "AFTERBURN",
                AfterburnType.Display, AfterburnPalette.TextHi,
                TextAlignmentOptions.Left, FontStyles.Bold);

            SectionHeader(root, "HullHeader", "SELECT HULL", new Vector2(64f, -170f));
            BuildHullCards(root);

            SectionHeader(root, "PilotHeader", "SELECT PILOT", new Vector2(64f, -706f));
            BuildPilotCards(root);

            BuildBottomRow(root);
            RefreshSelectionVisuals();
        }

        // ---------------------------------------------------------------- hull cards

        private void BuildHullCards(RectTransform root)
        {
            _hullBackgrounds = new Image[hulls.Length];
            _hullGlows = new Image[hulls.Length];

            float maxEnergy = 0f;
            float maxRegen = 0f;
            float maxSpeed = 0f;
            for (int i = 0; i < hulls.Length; i++)
            {
                maxEnergy = Mathf.Max(maxEnergy, hulls[i].maxEnergy);
                maxRegen = Mathf.Max(maxRegen, hulls[i].regenPerSec);
                maxSpeed = Mathf.Max(maxSpeed, hulls[i].topSpeedMult);
            }

            for (int i = 0; i < hulls.Length; i++)
            {
                HullDefinition hull = hulls[i];
                int index = i;

                RectTransform card = UIFactory.Fixed(root, "HullCard_" + i,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(64f + i * 600f, -230f), new Vector2(560f, 420f));
                (_hullBackgrounds[i], _hullGlows[i]) = CardChrome(card,
                    AfterburnPalette.Cyan, () => SelectHull(index));

                RectTransform name = UIFactory.Fixed(card, "Name",
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f, -16f), new Vector2(512f, 64f));
                UIFactory.Label(name, "Text", hull.displayName,
                    AfterburnType.H2, hull.tintColor,
                    TextAlignmentOptions.Left, FontStyles.Bold);

                RectTransform tag = UIFactory.Fixed(card, "Tag",
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f, -88f), new Vector2(512f, 34f));
                UIFactory.Label(tag, "Text", hull.tag,
                    AfterburnType.Caption, AfterburnPalette.TextMid,
                    TextAlignmentOptions.Left);

                RectTransform desc = UIFactory.Fixed(card, "Description",
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(24f, -130f), new Vector2(512f, 126f));
                TextMeshProUGUI descTmp = UIFactory.Label(desc, "Text", hull.description,
                    AfterburnType.Body, AfterburnPalette.TextLow,
                    TextAlignmentOptions.TopLeft);
                descTmp.overflowMode = TextOverflowModes.Ellipsis;

                StatRow(card, "PWR", hull.maxEnergy, maxEnergy, -270f);
                StatRow(card, "RGN", hull.regenPerSec, maxRegen, -316f);
                StatRow(card, "SPD", hull.topSpeedMult, maxSpeed, -362f);
            }
        }

        private static void StatRow(RectTransform card, string statName, float value, float max, float y)
        {
            RectTransform label = UIFactory.Fixed(card, "Stat_" + statName,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(24f, y), new Vector2(96f, 34f));
            UIFactory.Label(label, "Text", statName,
                AfterburnType.Caption, AfterburnPalette.TextMid,
                TextAlignmentOptions.Left);

            RectTransform barRt = UIFactory.Fixed(card, "StatBar_" + statName,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(136f, y - 10f), new Vector2(400f, 14f));
            (_, Image fill) = UIFactory.Bar(barRt, "Bar",
                AfterburnPalette.Stroke, AfterburnPalette.Cyan);
            UIFactory.SetBarRatio(fill, max > 0f ? Mathf.Clamp01(value / max) : 0f);
        }

        // ---------------------------------------------------------------- pilot cards

        private void BuildPilotCards(RectTransform root)
        {
            _pilotBackgrounds = new Image[pilots.Length];
            _pilotGlows = new Image[pilots.Length];

            for (int i = 0; i < pilots.Length; i++)
            {
                PilotDefinition pilot = pilots[i];
                int index = i;

                RectTransform card = UIFactory.Fixed(root, "PilotCard_" + i,
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(64f + i * 432f, -766f), new Vector2(400f, 330f));
                (_pilotBackgrounds[i], _pilotGlows[i]) = CardChrome(card,
                    AfterburnPalette.Violet, () => SelectPilot(index));

                RectTransform name = UIFactory.Fixed(card, "Name",
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(20f, -14f), new Vector2(360f, 60f));
                UIFactory.Label(name, "Text", pilot.displayName,
                    AfterburnType.H2, AfterburnPalette.Violet,
                    TextAlignmentOptions.Left, FontStyles.Bold);

                RectTransform ability = UIFactory.Fixed(card, "Ability",
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(20f, -80f), new Vector2(360f, 40f));
                UIFactory.Label(ability, "Text", pilot.abilityName,
                    AfterburnType.Label, AfterburnPalette.TextHi,
                    TextAlignmentOptions.Left);

                RectTransform cooldown = UIFactory.Fixed(card, "Cooldown",
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(20f, -126f), new Vector2(360f, 32f));
                UIFactory.Label(cooldown, "Text",
                    string.Format(CultureInfo.InvariantCulture, "CD {0:0}s", pilot.cooldownSec),
                    AfterburnType.Caption, AfterburnPalette.TextMid,
                    TextAlignmentOptions.Left);

                RectTransform desc = UIFactory.Fixed(card, "Description",
                    new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(20f, -166f), new Vector2(360f, 148f));
                TextMeshProUGUI descTmp = UIFactory.Label(desc, "Text", pilot.description,
                    AfterburnType.Caption, AfterburnPalette.TextLow,
                    TextAlignmentOptions.TopLeft);
                descTmp.overflowMode = TextOverflowModes.Ellipsis;
            }
        }

        // ---------------------------------------------------------------- bottom row

        private void BuildBottomRow(RectTransform root)
        {
            float x = 64f;
            x = Chip(root, "ChipStore", "STORE — U6", x);
            x = Chip(root, "ChipSeason", "SEASON — U6", x);
            x = Chip(root, "ChipGameCenter", "GAME CENTER — U6", x);

            Button settingsButton = UIFactory.TextButton(root, "SettingsButton",
                "SETTINGS", ToggleSettings, primary: false);
            Place((RectTransform)settingsButton.transform,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(x, 40f), new Vector2(300f, 90f));

            Button startButton = UIFactory.TextButton(root, "StartRaceButton",
                "START RACE", StartRace, primary: true);
            Place((RectTransform)startButton.transform,
                new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-64f, 40f), new Vector2(500f, 130f));
        }

        /// <summary>Disabled placeholder chip (Surface2, Caption TextLow). Returns the next x.</summary>
        private static float Chip(RectTransform parent, string name, string text, float x)
        {
            const float width = 380f;
            RectTransform rt = UIFactory.Fixed(parent, name,
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(x, 45f), new Vector2(width, 80f));
            UIFactory.Rect(rt, "Bg", AfterburnPalette.Surface2);
            UIFactory.Label(rt, "Text", text,
                AfterburnType.Caption, AfterburnPalette.TextLow);
            return x + width + 24f;
        }

        // ---------------------------------------------------------------- behaviour

        private void SelectHull(int index)
        {
            _selectedHull = index;
            RefreshSelectionVisuals();
        }

        private void SelectPilot(int index)
        {
            _selectedPilot = index;
            RefreshSelectionVisuals();
        }

        private void RefreshSelectionVisuals()
        {
            for (int i = 0; i < _hullBackgrounds.Length; i++)
            {
                bool selected = i == _selectedHull;
                _hullBackgrounds[i].color = selected ? AfterburnPalette.Surface3 : AfterburnPalette.Surface1;
                _hullGlows[i].enabled = selected;
            }

            for (int i = 0; i < _pilotBackgrounds.Length; i++)
            {
                bool selected = i == _selectedPilot;
                _pilotBackgrounds[i].color = selected ? AfterburnPalette.Surface3 : AfterburnPalette.Surface1;
                _pilotGlows[i].enabled = selected;
            }
        }

        private void StartRace()
        {
            if (hulls.Length > 0)
            {
                RaceLoadout.Hull = hulls[Mathf.Clamp(_selectedHull, 0, hulls.Length - 1)];
            }
            if (pilots.Length > 0)
            {
                RaceLoadout.Pilot = pilots[Mathf.Clamp(_selectedPilot, 0, pilots.Length - 1)];
            }
            RaceContext.UseFixedSeed = false;
            SceneManager.LoadScene("Race");
        }

        private void ToggleSettings()
        {
            if (_settings == null)
            {
                var go = new GameObject("Settings");
                go.transform.SetParent(transform, false);
                _settings = go.AddComponent<SettingsScreen>();
            }

            if (_settings.IsVisible)
            {
                _settings.Hide();
            }
            else
            {
                _settings.Show();
            }
        }

        // ---------------------------------------------------------------- helpers

        private static void SectionHeader(RectTransform root, string name, string text, Vector2 pos)
        {
            RectTransform rt = UIFactory.Fixed(root, name,
                new Vector2(0f, 1f), new Vector2(0f, 1f),
                pos, new Vector2(700f, 44f));
            UIFactory.Label(rt, "Text", text,
                AfterburnType.Label, AfterburnPalette.TextMid,
                TextAlignmentOptions.Left);
        }

        /// <summary>
        /// Shared card chrome: raycastable background Image (Button target), Outline, and a
        /// 4 px underglow strip along the bottom edge toggled by selection (§4.4 selected state).
        /// </summary>
        private static (Image bg, Image glow) CardChrome(RectTransform card, Color glowColor,
            UnityEngine.Events.UnityAction onClick)
        {
            var bg = card.gameObject.AddComponent<Image>();
            bg.color = AfterburnPalette.Surface1;
            UIFactory.Outline(card.gameObject);

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(onClick);

            RectTransform glowRt = UIFactory.Panel(card, "Underglow",
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                Vector2.zero, new Vector2(0f, 4f));
            var glow = glowRt.gameObject.AddComponent<Image>();
            glow.color = glowColor;
            glow.raycastTarget = false;
            glow.enabled = false;

            return (bg, glow);
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

using System.Globalization;
using Afterburn.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterburn.UI
{
    /// <summary>Payload for <see cref="SummaryScreen.Show"/> — filled by the race flow.</summary>
    public struct SummaryData
    {
        public int Place;
        public int RacerCount;
        public float RaceTime;
        public float BestLapThisRace;
        public int Shots, Hits, BountyHits;
        public float BoostTime;
        public System.Action? OnOneMoreRun, OnRematch, OnChangeLoadout;
    }

    /// <summary>
    /// Post-race summary overlay (UIEnvSpec §4.1): own canvas at sortOrder 20, dim scrim,
    /// centered Surface1 panel. Exactly THREE impulse hooks (hard cap, U5 gate criterion):
    /// (1) best-lap delta line, (2) informational stat strip, (3) ONE next-goal chip.
    /// <see cref="Show"/> rebuilds the layout from data; <see cref="Hide"/> destroys the
    /// canvas children and deactivates.
    /// </summary>
    public sealed class SummaryScreen : MonoBehaviour
    {
        private GameObject? _canvasGo;
        private RectTransform? _root;

        public void Show(SummaryData data)
        {
            if (_root == null || _canvasGo == null)
            {
                _root = UIFactory.NewScreenCanvas("SummaryCanvas", sortOrder: 20);
                _canvasGo = _root.parent.gameObject;
            }
            ClearChildren(_root);
            _canvasGo.SetActive(true);
            Build(_root, data);
        }

        public void Hide()
        {
            if (_root != null)
            {
                ClearChildren(_root);
            }
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

        private static void Build(RectTransform root, SummaryData data)
        {
            Image dim = UIFactory.Rect(root, "Dim",
                AfterburnPalette.WithAlpha(AfterburnPalette.Void, 0.7f));
            dim.raycastTarget = true;

            RectTransform panel = UIFactory.Fixed(root, "Panel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1600f, 900f));
            var panelBg = panel.gameObject.AddComponent<Image>();
            panelBg.color = AfterburnPalette.Surface1;
            UIFactory.Outline(panel.gameObject);

            bool wonRace = data.Place == 1;

            RectTransform place = UIFactory.Fixed(panel, "Place",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -36f), new Vector2(800f, 120f));
            UIFactory.Label(place, "Text", "P" + data.Place,
                AfterburnType.Display,
                wonRace ? AfterburnPalette.Gold : AfterburnPalette.TextHi,
                TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform finished = UIFactory.Fixed(panel, "Finished",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -162f), new Vector2(800f, 66f));
            UIFactory.Label(finished, "Text", "FINISHED",
                AfterburnType.H2, AfterburnPalette.TextMid);

            RectTransform time = UIFactory.Fixed(panel, "Time",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -244f), new Vector2(900f, 90f));
            UIFactory.Label(time, "Text", FormatTime(data.RaceTime),
                AfterburnType.H1, AfterburnPalette.TextHi);

            // Hook 1 — best-lap delta line.
            string deltaText;
            Color deltaColor;
            bool newBest = data.BestLapThisRace > 0f
                && data.BestLapThisRace < RaceContext.BestLapSeconds;
            if (newBest)
            {
                RaceContext.BestLapSeconds = data.BestLapThisRace;
                deltaText = string.Format(CultureInfo.InvariantCulture,
                    "NEW BEST LAP — {0:0.00}s", data.BestLapThisRace);
                deltaColor = AfterburnPalette.Cyan;
            }
            else if (!float.IsInfinity(RaceContext.BestLapSeconds))
            {
                float delta = data.BestLapThisRace - RaceContext.BestLapSeconds;
                deltaText = string.Format(CultureInfo.InvariantCulture,
                    "{0:+0.00;-0.00}s off your best lap", delta);
                deltaColor = AfterburnPalette.TextMid;
            }
            else
            {
                // No lap recorded this race and no prior best — nothing to compare.
                deltaText = "—";
                deltaColor = AfterburnPalette.TextMid;
            }
            RectTransform deltaRt = UIFactory.Fixed(panel, "BestLapDelta",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -352f), new Vector2(1200f, 52f));
            UIFactory.Label(deltaRt, "Text", deltaText, AfterburnType.Body, deltaColor);

            // Hook 2 — informational stat strip.
            RectTransform stats = UIFactory.Fixed(panel, "StatStrip",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -424f), new Vector2(1200f, 36f));
            UIFactory.Label(stats, "Text",
                string.Format(CultureInfo.InvariantCulture,
                    "shots {0} · hits {1} · bounty {2} · boost {3:0.0}s",
                    data.Shots, data.Hits, data.BountyHits, data.BoostTime),
                AfterburnType.Caption, AfterburnPalette.TextLow);

            // Hook 3 — the ONE next-goal chip.
            string goalText = data.Place > 1
                ? string.Format(CultureInfo.InvariantCulture, "NEXT: finish P{0}", data.Place - 1)
                : "NEXT: beat your best lap";
            RectTransform chip = UIFactory.Fixed(panel, "NextGoalChip",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -492f), new Vector2(640f, 80f));
            UIFactory.Rect(chip, "Bg", AfterburnPalette.Surface2);
            UIFactory.Label(chip, "Text", goalText,
                AfterburnType.Caption, AfterburnPalette.TextHi);

            // Buttons row.
            System.Action? onOneMoreRun = data.OnOneMoreRun;
            System.Action? onRematch = data.OnRematch;
            System.Action? onChangeLoadout = data.OnChangeLoadout;

            Button oneMore = UIFactory.TextButton(panel, "OneMoreRunButton",
                "ONE MORE RUN", () => onOneMoreRun?.Invoke(), primary: true);
            Place((RectTransform)oneMore.transform,
                new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                new Vector2(-732f, 48f), new Vector2(520f, 130f));

            Button rematch = UIFactory.TextButton(panel, "RematchButton",
                "REMATCH", () => onRematch?.Invoke(), primary: false);
            Place((RectTransform)rematch.transform,
                new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                new Vector2(-180f, 58f), new Vector2(400f, 110f));

            Button changeLoadout = UIFactory.TextButton(panel, "ChangeLoadoutButton",
                "CHANGE LOADOUT", () => onChangeLoadout?.Invoke(), primary: false);
            Place((RectTransform)changeLoadout.transform,
                new Vector2(0.5f, 0f), new Vector2(0f, 0f),
                new Vector2(252f, 58f), new Vector2(480f, 110f));
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>m:ss.ff — integer math so 59.999 never rounds up to "0:60.00".</summary>
        private static string FormatTime(float seconds)
        {
            if (seconds < 0f)
            {
                seconds = 0f;
            }
            int hundredths = Mathf.FloorToInt(seconds * 100f);
            int m = hundredths / 6000;
            int s = (hundredths % 6000) / 100;
            int ff = hundredths % 100;
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}.{2:00}", m, s, ff);
        }

        private static void ClearChildren(RectTransform rt)
        {
            for (int i = rt.childCount - 1; i >= 0; i--)
            {
                Destroy(rt.GetChild(i).gameObject);
            }
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

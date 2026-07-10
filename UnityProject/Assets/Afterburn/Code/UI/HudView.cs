using System.Collections.Generic;
using Afterburn.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterburn.UI
{
    /// <summary>
    /// The Race HUD (UIEnvSpec §4.2) — runtime-built on a Screen Space Overlay canvas.
    /// THE LAW: the primary energy fill is frame-true, equal to EnergyCore.Ratio the same frame,
    /// NEVER tweened. The loss-ghost trail dramatizes damage on a delay. Bar width is physically
    /// proportional to the hull's pool (9 px per energy point — the sidegrade made visible).
    /// Reads Core every frame; writes nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HudView : MonoBehaviour
    {
        private const float PxPerEnergy = 9f;
        private const float BarHeight = 44f;
        private const float BarBottomOffset = 72f;

        private RaceDirector? _race;

        // Energy bar
        private Image? _fill;
        private Image? _lossGhost;
        private Image? _barRim;
        private float _lossShown = 1f;
        private float _lossDelayTimer;

        // Chips / labels
        private readonly Dictionary<string, (Image bg, TextMeshProUGUI text)> _chips = new();
        private TextMeshProUGUI? _position;
        private TextMeshProUGUI? _lap;
        private TextMeshProUGUI? _timer;
        private TextMeshProUGUI? _speed;
        private TextMeshProUGUI? _bounty;
        private TextMeshProUGUI? _countdown;
        private RectTransform? _intelRoot;
        private Image? _intelFill;
        private Image? _abilityRadial;

        // Minimap
        private RectTransform? _minimapRoot;
        private readonly List<RectTransform> _mapDots = new();
        private RectTransform? _leaderRing;
        private Vector2 _mapCentre;
        private float _mapScale;

        // Toasts
        private RectTransform? _toastRoot;
        private readonly Queue<(TextMeshProUGUI label, float dieAt)> _toasts = new();

        public void Bind(RaceDirector race)
        {
            _race = race;
            Build();

            race.Combat.OnHitLanded += (from, target, reward, wasLeader) =>
            {
                if (ReferenceEquals(from, race.Player))
                    Toast(wasLeader ? $"BOUNTY HIT +{reward:0}" : "HIT", wasLeader ? AfterburnPalette.Gold : AfterburnPalette.TextHi);
                else if (ReferenceEquals(target, race.Player))
                    Toast("TAKING FIRE", AfterburnPalette.Red);
            };
            race.Contacts.OnShipContact += (_, _, dmg) => Toast($"CONTACT −{dmg:0}", AfterburnPalette.Red);
            race.Track.OnWallBroken += _ => Toast("WALL SMASHED", AfterburnPalette.Orange);
            race.Abilities.OnAbilityActivated += (who, type) =>
            {
                if (!ReferenceEquals(who, race.Player)) return;
                Toast(type switch
                {
                    AbilityType.EmpPulse => "EMP PULSE",
                    AbilityType.PhaseShift => "PHASE SHIFT",
                    AbilityType.Siphon => "SIPHON ARMED",
                    _ => "DECOY OUT",
                }, AfterburnPalette.Violet);
            };
        }

        private void Build()
        {
            RectTransform root = UIFactory.NewScreenCanvas("HUD", sortOrder: 5);
            ShipController player = _race!.Player;

            // ---- Hero energy bar (bottom-centre) --------------------------------
            float barWidth = player.Energy.Max * PxPerEnergy;
            RectTransform bar = UIFactory.Fixed(root, "EnergyBar",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, BarBottomOffset), new Vector2(barWidth, BarHeight));
            Image barBg = UIFactory.Rect(bar, "Bg", AfterburnPalette.Surface2);
            UIFactory.Outline(barBg.gameObject);
            _barRim = barBg;

            (Image _, Image loss) = UIFactory.Bar(bar, "LossGhost",
                Color.clear, AfterburnPalette.WithAlpha(AfterburnPalette.Red, 0.35f));
            _lossGhost = loss;
            (Image _, Image fill) = UIFactory.Bar(bar, "Fill", Color.clear, AfterburnPalette.ModeCoast);
            _fill = fill;

            int ticks = Mathf.FloorToInt(player.Energy.Max / 20f);
            for (int t = 1; t <= ticks; t++)
            {
                float x = barWidth * (t * 20f / player.Energy.Max);
                UIFactory.Fixed(bar, $"Tick{t}", new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(x, 0f), new Vector2(3f, BarHeight))
                    .gameObject.AddComponent<Image>().color = AfterburnPalette.Void;
            }

            _speed = UIFactory.Label(
                UIFactory.Fixed(root, "Speed", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(barWidth / 2f + 160f, BarBottomOffset), new Vector2(280f, BarHeight)),
                "Value", "0 km/h", AfterburnType.Caption, AfterburnPalette.TextMid);

            // ---- State chips above the bar --------------------------------------
            string[] chipNames = { "BOOST", "FIRE", "SHIELD", "COAST" };
            for (int i = 0; i < chipNames.Length; i++)
            {
                RectTransform chip = UIFactory.Fixed(root, $"Chip{chipNames[i]}",
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2((i - 1.5f) * 240f, BarBottomOffset + BarHeight + 26f),
                    new Vector2(220f, 56f));
                Image bg = UIFactory.Rect(chip, "Bg", AfterburnPalette.Surface2);
                TextMeshProUGUI text = UIFactory.Label(chip, "Text", chipNames[i],
                    AfterburnType.Caption, AfterburnPalette.TextLow, TextAlignmentOptions.Center, FontStyles.Bold);
                _chips[chipNames[i]] = (bg, text);
            }

            // ---- Top-left: position + lap; top-centre-left: timer ----------------
            _position = UIFactory.Label(
                UIFactory.Fixed(root, "Position", new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(180f, -90f), new Vector2(320f, 110f)),
                "Value", "P4/4", AfterburnType.H1, AfterburnPalette.TextHi,
                TextAlignmentOptions.Left, FontStyles.Bold);
            _lap = UIFactory.Label(
                UIFactory.Fixed(root, "Lap", new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(180f, -190f), new Vector2(320f, 80f)),
                "Value", "LAP 1/3", AfterburnType.H2, AfterburnPalette.TextMid, TextAlignmentOptions.Left);
            _timer = UIFactory.Label(
                UIFactory.Fixed(root, "Timer", new Vector2(0.35f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -90f), new Vector2(420f, 90f)),
                "Value", "0:00.0", AfterburnType.H2, AfterburnPalette.TextHi);

            // ---- Bounty tag + intel ----------------------------------------------
            _bounty = UIFactory.Label(
                UIFactory.Fixed(root, "Bounty", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -84f), new Vector2(560f, 76f)),
                "Value", "BOUNTY — YOU LEAD", AfterburnType.Label, AfterburnPalette.Gold,
                TextAlignmentOptions.Center, FontStyles.Bold);

            _intelRoot = UIFactory.Fixed(root, "Intel", new Vector2(0.62f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -96f), new Vector2(300f, 60f));
            UIFactory.Label(_intelRoot, "Tag", "LEADER", AfterburnType.Micro, AfterburnPalette.Gold,
                TextAlignmentOptions.Top);
            RectTransform intelBar = UIFactory.Fixed(_intelRoot, "BarHolder",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(280f, 18f));
            (Image _, Image intelFill) = UIFactory.Bar(intelBar, "Bar", AfterburnPalette.Surface2, AfterburnPalette.Gold);
            _intelFill = intelFill;

            // ---- Ability radial (bottom-right, centred above the touch wheel) ------
            RectTransform ability = UIFactory.Fixed(root, "Ability",
                new Vector2(1f, 0f), new Vector2(0.5f, 0.5f), new Vector2(-396f, 781f), new Vector2(150f, 150f));
            Image abilityBg = UIFactory.Rect(ability, "Bg", AfterburnPalette.Surface2);
            abilityBg.raycastTarget = false;
            var radialGo = new GameObject("Radial");
            radialGo.transform.SetParent(ability, false);
            _abilityRadial = radialGo.AddComponent<Image>();
            UIFactory.Stretch((RectTransform)radialGo.transform);
            _abilityRadial.sprite = SolidSprite();
            _abilityRadial.type = Image.Type.Filled;
            _abilityRadial.fillMethod = Image.FillMethod.Radial360;
            _abilityRadial.color = AfterburnPalette.Violet;
            _abilityRadial.raycastTarget = false;
            UIFactory.Label(ability, "Tag", "Q", AfterburnType.Label, AfterburnPalette.TextHi);

            // ---- Minimap (top-right) ----------------------------------------------
            BuildMinimap(root);

            // ---- Toast feed + countdown -------------------------------------------
            _toastRoot = UIFactory.Fixed(root, "Toasts", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -190f), new Vector2(900f, 200f));
            _countdown = UIFactory.Label(
                UIFactory.Panel(root, "Countdown", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero),
                "Value", "3", AfterburnType.Countdown, AfterburnPalette.TextHi,
                TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private void BuildMinimap(RectTransform root)
        {
            _minimapRoot = UIFactory.Fixed(root, "Minimap", new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-190f, -190f), new Vector2(300f, 300f));
            Image bg = UIFactory.Rect(_minimapRoot, "Bg", AfterburnPalette.WithAlpha(AfterburnPalette.Surface1, 0.7f));
            UIFactory.Outline(bg.gameObject);

            // Bake the outline once: 60 segments of the spline into a small texture.
            TrackSystem track = _race!.Track;
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var clear = new Color32[256 * 256];
            tex.SetPixels32(clear);

            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            for (int i = 0; i < track.SampleCount; i++)
            {
                min = Vector3.Min(min, track[i].Pos);
                max = Vector3.Max(max, track[i].Pos);
            }
            Vector3 centre = (min + max) * 0.5f;
            float extent = Mathf.Max(max.x - min.x, max.z - min.z) * 0.62f;
            _mapCentre = new Vector2(centre.x, centre.z);
            _mapScale = 130f / extent;                          // canvas px per world unit

            Color line = AfterburnPalette.WithAlpha(AfterburnPalette.TextMid, 0.9f);
            for (int s = 0; s < 60; s++)
            {
                Vector3 a = track.Spline.GetPointAt(s / 60f);
                Vector3 b = track.Spline.GetPointAt((s + 1) / 60f);
                DrawLine(tex, ToTexel(a, centre, extent), ToTexel(b, centre, extent), line);
            }
            tex.Apply();

            var outlineGo = new GameObject("Outline");
            outlineGo.transform.SetParent(_minimapRoot, false);
            var raw = outlineGo.AddComponent<RawImage>();
            raw.texture = tex;
            raw.raycastTarget = false;
            UIFactory.Stretch((RectTransform)outlineGo.transform);

            // Dots: leader ring + player (teal) + 3 rivals (red). Order: ring under dots.
            _leaderRing = Dot(_minimapRoot, "LeaderRing", AfterburnPalette.Gold, 34f, hollow: true);
            _mapDots.Add(Dot(_minimapRoot, "Player", AfterburnPalette.Teal, 18f));
            for (int g = 0; g < 3; g++) _mapDots.Add(Dot(_minimapRoot, $"Rival{g}", AfterburnPalette.Red, 13f));
        }

        private void LateUpdate()
        {
            if (_race == null || _fill == null) return;
            ShipController p = _race.Player;

            // ---- Frame-true fill (the law), mode color, loss ghost ---------------
            float ratio = p.Energy.Ratio;
            UIFactory.SetBarRatio(_fill, ratio);
            Color mode = p.Boosting ? AfterburnPalette.ModeBoost
                : p.Shielding ? AfterburnPalette.ModeShield
                : AfterburnPalette.ModeCoast;
            if (ratio < 0.25f)
            {
                float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 4f);
                mode = Color.Lerp(AfterburnPalette.Red, mode, pulse * 0.4f);
            }
            _fill.color = mode;

            if (ratio < _lossShown - 1e-4f)
            {
                _lossDelayTimer += Time.deltaTime;
                if (_lossDelayTimer >= AfterburnMotion.LossGhostDelay)
                {
                    _lossShown = Mathf.MoveTowards(_lossShown, ratio,
                        Time.deltaTime / AfterburnMotion.LossGhostDuration);
                }
            }
            else
            {
                _lossShown = ratio;
                _lossDelayTimer = 0f;
            }
            if (_lossGhost != null) UIFactory.SetBarRatio(_lossGhost, _lossShown);

            // ---- Chips ------------------------------------------------------------
            SetChip("BOOST", p.Boosting, AfterburnPalette.ModeBoost);
            SetChip("SHIELD", p.Shielding, AfterburnPalette.ModeShield);
            SetChip("FIRE", p.FiredThisTick, AfterburnPalette.ModeFire);
            SetChip("COAST", !p.Boosting && !p.Shielding && !p.FiredThisTick, AfterburnPalette.ModeCoast);

            // ---- Labels -------------------------------------------------------------
            if (_position != null) _position.text = $"P{_race.PlayerPlace()}/{_race.Racers.Count}";
            if (_lap != null)
                _lap.text = $"LAP {Mathf.Clamp((int)_race.PlayerProgress + 1, 1, 3)}/3";
            if (_timer != null)
            {
                float t = _race.RaceTime;
                _timer.text = $"{(int)(t / 60f)}:{t % 60f:00.0}";
            }
            if (_speed != null) _speed.text = $"{Mathf.RoundToInt(p.Speed * 3f)} km/h";   // div #14

            // ---- Bounty / intel (§7.7 asymmetry: leaders see NOTHING extra) --------
            bool playerLeads = ReferenceEquals(_race.BountyLeader, p);
            if (_bounty != null) _bounty.gameObject.SetActive(playerLeads && _race.State == RaceState.Racing);
            if (_intelRoot != null)
            {
                bool showIntel = !playerLeads && _race.BountyLeader != null && _race.State == RaceState.Racing;
                _intelRoot.gameObject.SetActive(showIntel);
                if (showIntel && _intelFill != null)
                {
                    UIFactory.SetBarRatio(_intelFill, _race.BountyLeader!.Energy.Ratio);
                }
            }

            // ---- Ability radial ------------------------------------------------------
            if (_abilityRadial != null)
            {
                PilotAbilitySystem.RacerAbility state = _race.Abilities.StateOf(p);
                float readiness = state.CooldownMax <= 0f ? 1f : 1f - state.Cooldown / state.CooldownMax;
                _abilityRadial.fillAmount = readiness;
                _abilityRadial.color = readiness >= 1f
                    ? AfterburnPalette.Violet
                    : AfterburnPalette.WithAlpha(AfterburnPalette.Violet, 0.35f);
            }

            UpdateMinimap();
            UpdateToasts();
            UpdateCountdown();
        }

        private void UpdateMinimap()
        {
            if (_minimapRoot == null) return;
            for (int i = 0; i < _race!.Racers.Count && i < _mapDots.Count; i++)
            {
                IRacer racer = _race.Racers[i];
                Vector2 mapPos = (new Vector2(racer.Position.x, racer.Position.z) - _mapCentre) * _mapScale;
                _mapDots[i].anchoredPosition = mapPos;
                if (ReferenceEquals(racer, _race.BountyLeader) && _leaderRing != null)
                {
                    _leaderRing.anchoredPosition = mapPos;
                }
            }
        }

        private void UpdateToasts()
        {
            while (_toasts.Count > 0 && Time.unscaledTime >= _toasts.Peek().dieAt)
            {
                (TextMeshProUGUI dead, _) = _toasts.Dequeue();
                if (dead != null) Destroy(dead.transform.parent.gameObject);
                RestackToasts();
            }
        }

        private void UpdateCountdown()
        {
            if (_countdown == null) return;
            switch (_race!.State)
            {
                case RaceState.Countdown:
                    _countdown.gameObject.SetActive(true);
                    _countdown.text = Mathf.CeilToInt(_race.CountdownRemaining).ToString();
                    break;
                case RaceState.Racing when _race.RaceTime < 0.8f:
                    _countdown.gameObject.SetActive(true);
                    _countdown.text = "GO";
                    _countdown.color = AfterburnPalette.Cyan;
                    break;
                default:
                    _countdown.gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>Prototype toast: 1.1 s hold, max 2 stacked.</summary>
        public void Toast(string message, Color color)
        {
            if (_toastRoot == null) return;
            if (_toasts.Count >= 2)
            {
                (TextMeshProUGUI dead, _) = _toasts.Dequeue();
                if (dead != null) Destroy(dead.transform.parent.gameObject);
            }
            RectTransform holder = UIFactory.Fixed(_toastRoot, "Toast",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(860f, 64f));
            UIFactory.Rect(holder, "Bg", AfterburnPalette.WithAlpha(AfterburnPalette.Surface2, 0.9f));
            TextMeshProUGUI label = UIFactory.Label(holder, "Text", message, AfterburnType.Body, color,
                TextAlignmentOptions.Center, FontStyles.Bold);
            _toasts.Enqueue((label, Time.unscaledTime + AfterburnMotion.ToastHold));
            RestackToasts();
        }

        private void RestackToasts()
        {
            int i = 0;
            foreach ((TextMeshProUGUI label, _) in _toasts)
            {
                if (label != null)
                    ((RectTransform)label.transform.parent).anchoredPosition = new Vector2(0f, -i * 72f);
                i++;
            }
        }

        private void SetChip(string chipName, bool active, Color activeColor)
        {
            (Image bg, TextMeshProUGUI text) = _chips[chipName];
            bg.color = active ? AfterburnPalette.WithAlpha(activeColor, 0.25f) : AfterburnPalette.Surface2;
            text.color = active ? activeColor : AfterburnPalette.TextLow;
        }

        private static RectTransform Dot(RectTransform parent, string dotName, Color color, float size, bool hollow = false)
        {
            RectTransform rt = UIFactory.Fixed(parent, dotName, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            var img = rt.gameObject.AddComponent<Image>();
            img.color = hollow ? AfterburnPalette.WithAlpha(color, 0.5f) : color;
            img.raycastTarget = false;
            return rt;
        }

        private static Sprite? _solid;
        private static Sprite SolidSprite()
        {
            if (_solid != null) return _solid;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply();
            _solid = Sprite.Create(tex, new UnityEngine.Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            return _solid;
        }

        private static Vector2Int ToTexel(Vector3 world, Vector3 centre, float extent)
        {
            float u = (world.x - centre.x) / extent * 0.5f + 0.5f;
            float v = (world.z - centre.z) / extent * 0.5f + 0.5f;
            return new Vector2Int(Mathf.Clamp((int)(u * 255f), 0, 255), Mathf.Clamp((int)(v * 255f), 0, 255));
        }

        private static void DrawLine(Texture2D tex, Vector2Int a, Vector2Int b, Color color)
        {
            int steps = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y), 1);
            for (int s = 0; s <= steps; s++)
            {
                float t = s / (float)steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(a.x, b.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(a.y, b.y, t));
                tex.SetPixel(x, y, color);
                tex.SetPixel(Mathf.Min(x + 1, 255), y, color);   // 2 px stroke
            }
        }
    }
}

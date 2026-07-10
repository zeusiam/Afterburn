using System.Collections;
using Afterburn.Core;
using Afterburn.View;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Afterburn.UI
{
    /// <summary>
    /// Race-scene orchestration (UIEnvSpec §4.1 + §8½.2): the mandatory cosmetic billboard —
    /// a diegetic pre-race LINEUP sweep over the grid (skippable after 3 s, hard cap 8 s) →
    /// countdown → racing (HUD + touch live) → PODIUM framing on finish → Summary overlay.
    /// Rematch reruns the same seed (same ghosts); One More Run rolls fresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceFlowController : MonoBehaviour
    {
        private const float LineupDuration = 6f;
        private const float LineupSkippableAfter = 3f;
        private const float PodiumDuration = 3.5f;

        [SerializeField] private RaceRunner? runner;
        [SerializeField] private Camera? raceCamera;

        private HudView? _hud;
        private TouchControls? _touch;
        private SummaryScreen? _summary;
        private ChaseCamera? _chase;
        private bool _summaryShown;

        public RaceRunner? Runner { get => runner; set => runner = value; }
        public Camera? RaceCamera { get => raceCamera; set => raceCamera = value; }

        private void Start()
        {
            if (runner == null || runner.Race == null || raceCamera == null)
            {
                Debug.LogError("[Afterburn] RaceFlowController missing runner/camera.", this);
                enabled = false;
                return;
            }

            _chase = raceCamera.GetComponent<ChaseCamera>();

            var hudGo = new GameObject("HudView");
            hudGo.transform.SetParent(transform, false);
            _hud = hudGo.AddComponent<HudView>();
            _hud.Bind(runner.Race);

            var touchGo = new GameObject("TouchControls");
            touchGo.transform.SetParent(transform, false);
            _touch = touchGo.AddComponent<TouchControls>();

            var summaryGo = new GameObject("SummaryScreen");
            summaryGo.transform.SetParent(transform, false);
            _summary = summaryGo.AddComponent<SummaryScreen>();

            StartCoroutine(LineupSweep());
        }

        private void Update()
        {
            if (runner == null || runner.Race == null) return;

            if (_touch != null) runner.ExternalInput = _touch.Current;

            if (runner.Race.State == RaceState.Finished && !_summaryShown)
            {
                _summaryShown = true;
                StartCoroutine(PodiumThenSummary());
            }
        }

        /// <summary>The billboard: dolly across the grid, every ship (and later, every cosmetic) on screen.</summary>
        private IEnumerator LineupSweep()
        {
            RaceDirector race = runner!.Race!;
            if (_chase != null) _chase.enabled = false;

            // Grid framing: pack centre + the start tangent.
            Vector3 centre = Vector3.zero;
            foreach (IRacer r in race.Racers) centre += r.Position;
            centre /= race.Racers.Count;
            Vector3 fwd = race.Player.Forward;
            Vector3 side = Vector3.Cross(Vector3.up, fwd).normalized;

            // Lineup name overlay.
            RectTransform lineupRoot = UIFactory.NewScreenCanvas("Lineup", sortOrder: 8);
            UIFactory.Label(
                UIFactory.Fixed(lineupRoot, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -110f), new Vector2(1200f, 100f)),
                "Text", "THE GRID", AfterburnType.H1, AfterburnPalette.TextHi,
                TextAlignmentOptions.Center, FontStyles.Bold);
            string names = $"YOU — {race.Player.Hull.displayName}";
            foreach (GhostRacer g in race.Ghosts) names += $"   ·   {g.Hull.displayName}";
            UIFactory.Label(
                UIFactory.Fixed(lineupRoot, "Names", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -210f), new Vector2(2000f, 70f)),
                "Text", names, AfterburnType.Label, AfterburnPalette.TextMid);
            UIFactory.Label(
                UIFactory.Fixed(lineupRoot, "Skip", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 60f), new Vector2(800f, 50f)),
                "Text", "TAP TO SKIP", AfterburnType.Micro, AfterburnPalette.TextLow);

            float t = 0f;
            while (t < LineupDuration)
            {
                t += Time.deltaTime;
                float u = Mathf.SmoothStep(0f, 1f, t / LineupDuration);
                Vector3 from = centre + side * -18f - fwd * 6f + Vector3.up * 4f;
                Vector3 to = centre + side * 18f - fwd * 10f + Vector3.up * 7f;
                raceCamera!.transform.position = Vector3.Lerp(from, to, u);
                raceCamera.transform.LookAt(centre + Vector3.up * 1.2f);

                if (t >= LineupSkippableAfter && AnyTap()) break;
                yield return null;
            }

            Destroy(lineupRoot.parent.gameObject);              // the canvas GO
            if (_chase != null)
            {
                _chase.enabled = true;
                _chase.SnapToTarget();
            }
            race.BeginCountdown();
        }

        private IEnumerator PodiumThenSummary()
        {
            RaceDirector race = runner!.Race!;
            if (_chase != null) _chase.enabled = false;

            // Podium framing: slow orbit around the player's parked ship.
            Vector3 focus = race.Player.Position;
            RectTransform podiumRoot = UIFactory.NewScreenCanvas("Podium", sortOrder: 8);
            int place = race.PlayerPlace();
            UIFactory.Label(
                UIFactory.Fixed(podiumRoot, "Place", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -140f), new Vector2(900f, 160f)),
                "Text", $"P{place}", AfterburnType.Display,
                place == 1 ? AfterburnPalette.Gold : AfterburnPalette.TextHi,
                TextAlignmentOptions.Center, FontStyles.Bold);

            float t = 0f;
            while (t < PodiumDuration)
            {
                t += Time.deltaTime;
                float ang = t * 0.5f;
                raceCamera!.transform.position = focus
                    + new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang)) * 14f + Vector3.up * 5f;
                raceCamera.transform.LookAt(focus + Vector3.up * 1.2f);
                if (t >= 1.2f && AnyTap()) break;
                yield return null;
            }
            Destroy(podiumRoot.parent.gameObject);

            float bestLap = float.PositiveInfinity;
            foreach (float lap in race.PlayerLapTimes) bestLap = Mathf.Min(bestLap, lap);

            _summary!.Show(new SummaryData
            {
                Place = place,
                RacerCount = race.Racers.Count,
                RaceTime = race.PlayerFinishTime,
                BestLapThisRace = bestLap,
                Shots = race.Stats.Shots,
                Hits = race.Stats.Hits,
                BountyHits = race.Stats.BountyHits,
                BoostTime = race.Stats.BoostTime,
                OnOneMoreRun = () => { RaceContext.UseFixedSeed = false; Reload(); },
                OnRematch = () => { RaceContext.UseFixedSeed = true; Reload(); },
                OnChangeLoadout = () => SceneManager.LoadScene("MainMenu"),
            });
        }

        private static void Reload() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        private static bool AnyTap()
        {
            bool key = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
            bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool touch = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            return key || mouse || touch;
        }
    }
}

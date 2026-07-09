using Afterburn.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterburn.View
{
    /// <summary>
    /// Drives the full race sim in the Race scene (U2–U4 playable greybox). Owns the fixed-tick
    /// accumulator over <see cref="RaceDirector"/>: Core steps at exactly
    /// <see cref="ShipController.Tick"/>; racer transforms interpolate between sim states.
    /// Keyboard is the prototype's dev scheme 1:1 (W/↑ thrust · S/↓ brake · A/D steer ·
    /// Shift boost · Space fire · E shield · Q ability). Touch + HUD land at U5 — until then a
    /// minimal IMGUI dev overlay shows countdown/energy/lap/place. View reads Core, never writes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceRunner : MonoBehaviour
    {
        /// <summary>Prototype ghost tints (roster order: heavy, light, medium).</summary>
        private static readonly string[] GhostTints = { "#FF5A4D", "#38F5C9", "#FFD23F" };

        [SerializeField] private TrackDefinition? track;
        [SerializeField] private GameTuning? tuning;
        [SerializeField] private HullDefinition? playerHull;
        [SerializeField] private PilotDefinition? playerPilot;
        [SerializeField] private HullDefinition? heavyHull;
        [SerializeField] private HullDefinition? lightHull;
        [SerializeField] private HullDefinition? mediumHull;
        [SerializeField] private Transform? shipTransform;
        [SerializeField] private TrackView? trackView;

        private RaceDirector? _race;
        private float _accumulator;

        // Interpolation state: player + 3 ghosts.
        private Transform?[] _transforms = new Transform?[4];
        private Vector3[] _prevPos = new Vector3[4];
        private Vector3[] _currPos = new Vector3[4];
        private Quaternion[] _prevRot = new Quaternion[4];
        private Quaternion[] _currRot = new Quaternion[4];

        public TrackDefinition? Track { get => track; set => track = value; }
        public GameTuning? Tuning { get => tuning; set => tuning = value; }
        public HullDefinition? PlayerHull { get => playerHull; set => playerHull = value; }
        public PilotDefinition? PlayerPilot { get => playerPilot; set => playerPilot = value; }
        public HullDefinition? HeavyHull { get => heavyHull; set => heavyHull = value; }
        public HullDefinition? LightHull { get => lightHull; set => lightHull = value; }
        public HullDefinition? MediumHull { get => mediumHull; set => mediumHull = value; }
        public Transform? ShipTransform { get => shipTransform; set => shipTransform = value; }
        public TrackView? Greybox { get => trackView; set => trackView = value; }

        /// <summary>The live race — U5's HUD reads everything from here.</summary>
        public RaceDirector? Race => _race;

        private void Awake()
        {
            if (track == null || tuning == null || playerHull == null || playerPilot == null
                || heavyHull == null || lightHull == null || mediumHull == null || shipTransform == null)
            {
                Debug.LogError("[Afterburn] RaceRunner is missing references.", this);
                enabled = false;
                return;
            }

            _race = new RaceDirector(new RaceDirector.Config
            {
                Track = track,
                Tuning = tuning,
                PlayerHull = playerHull,
                PlayerPilot = playerPilot,
                GhostGrid = new[]
                {
                    (heavyHull, -1, 0.006f),   // prototype roster: heavy+Kade lane −1
                    (lightHull, 1, 0.012f),    // light+Sora lane +1
                    (mediumHull, 2, 0.018f),   // medium+Nyx lane +2
                },
                Seed = System.Environment.TickCount,
            });

            // Player transform is the scene ship; ghosts get greybox ships with roster tints.
            _transforms[0] = shipTransform;
            for (int g = 0; g < _race.Ghosts.Count && g < GhostTints.Length; g++)
            {
                var ghostGo = new GameObject($"Ghost{g}");
                ghostGo.SetActive(false);
                var greybox = ghostGo.AddComponent<ShipGreybox>();
                greybox.Hull = _race.Ghosts[g].Hull;
                greybox.TintOverride = GreyboxMaterials.Hex(GhostTints[g]);
                greybox.HologramMode = true;               // ghosts READ intangible (UIEnvSpec §3.1)
                ghostGo.SetActive(true);
                _transforms[g + 1] = ghostGo.transform;
            }

            var combatViewGo = new GameObject("CombatView");
            combatViewGo.transform.SetParent(transform, false);
            combatViewGo.AddComponent<CombatView>().Bind(_race.Combat, _race.Abilities);

            if (trackView != null)
            {
                _race.Track.OnWallBroken += _ => trackView.HideHeavySlab();
            }

            SnapshotPoses();
            for (int i = 0; i < _transforms.Length; i++)
            {
                _prevPos[i] = _currPos[i];
                _prevRot[i] = _currRot[i];
                _transforms[i]?.SetPositionAndRotation(_currPos[i], _currRot[i]);
            }
        }

        private void Update()
        {
            if (_race == null) return;

            ShipInputState input = ReadKeyboard();

            _accumulator += Time.deltaTime;
            _accumulator = Mathf.Min(_accumulator, 0.25f);   // dt clamp, prototype-style

            while (_accumulator >= ShipController.Tick)
            {
                System.Array.Copy(_currPos, _prevPos, _currPos.Length);
                System.Array.Copy(_currRot, _prevRot, _currRot.Length);

                _race.Tick(input, ShipController.Tick);
                input.AbilityEdge = false;                    // edge consumed on the first tick

                SnapshotPoses();
                _accumulator -= ShipController.Tick;
            }

            float alpha = _accumulator / ShipController.Tick;
            for (int i = 0; i < _transforms.Length; i++)
            {
                _transforms[i]?.SetPositionAndRotation(
                    Vector3.Lerp(_prevPos[i], _currPos[i], alpha),
                    Quaternion.Slerp(_prevRot[i], _currRot[i], alpha));
            }
        }

        private void SnapshotPoses()
        {
            if (_race == null) return;
            _currPos[0] = _race.Player.Position;
            _currRot[0] = Quaternion.LookRotation(_race.Player.Forward, Vector3.up);
            for (int g = 0; g < _race.Ghosts.Count; g++)
            {
                _currPos[g + 1] = _race.Ghosts[g].Position;
                _currRot[g + 1] = Quaternion.LookRotation(_race.Ghosts[g].Forward, Vector3.up);
            }
        }

        private static ShipInputState ReadKeyboard()
        {
            Keyboard? kb = Keyboard.current;
            if (kb == null) return ShipInputState.None;
            return new ShipInputState
            {
                Thrust = kb.wKey.isPressed || kb.upArrowKey.isPressed,
                Brake = kb.sKey.isPressed || kb.downArrowKey.isPressed,
                Left = kb.aKey.isPressed || kb.leftArrowKey.isPressed,
                Right = kb.dKey.isPressed || kb.rightArrowKey.isPressed,
                Boost = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed,
                Fire = kb.spaceKey.isPressed,
                Shield = kb.eKey.isPressed,
                AbilityEdge = kb.qKey.wasPressedThisFrame,
            };
        }

        /// <summary>Dev overlay only — the real HUD is U5. Deleted the day HUD.prefab lands.</summary>
        private void OnGUI()
        {
            if (_race == null) return;
            GUILayout.BeginArea(new Rect(12, 12, 460, 200));
            GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 22 };

            switch (_race.State)
            {
                case RaceState.Countdown:
                    GUILayout.Label($"COUNTDOWN  {Mathf.CeilToInt(_race.CountdownRemaining)}", style);
                    break;
                case RaceState.Racing:
                case RaceState.Finished:
                    ShipController p = _race.Player;
                    string mode = p.Boosting ? "BOOST" : p.Shielding ? "SHIELD" : "COAST";
                    GUILayout.Label(
                        $"E {p.Energy.Energy:F0}/{p.Energy.Max:F0}  [{mode}]   " +
                        $"v {p.Speed * 3f:F0} km/h   P{_race.PlayerPlace()}/4   " +
                        $"Lap {Mathf.Min(_race.Player != null ? (int)_race.PlayerProgress + 1 : 1, 3)}/3   " +
                        $"t {_race.RaceTime:F1}s", style);
                    if (_race.State == RaceState.Finished)
                    {
                        GUILayout.Label($"FINISHED — P{_race.PlayerPlace()}  {_race.PlayerFinishTime:F1}s  " +
                                        $"shots {_race.Stats.Shots} hits {_race.Stats.Hits} " +
                                        $"bounty {_race.Stats.BountyHits} boost {_race.Stats.BoostTime:F1}s", style);
                    }
                    break;
            }
            GUILayout.EndArea();
        }
    }
}

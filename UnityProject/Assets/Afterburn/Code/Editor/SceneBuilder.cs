using Afterburn.Core;
using Afterburn.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterburn.EditorTools
{
    /// <summary>
    /// Builds the three U1 scenes deterministically (BUILD §4/§10 U1): Boot, MainMenu, and Race
    /// with the greybox Arena01, a spawned greybox ship at the start line and the chase camera.
    /// Scene flow / front-end code is U5 — these are skeletons. Re-running overwrites the scenes
    /// (they are generated artefacts; data lives in the SOs).
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenesDir = "Assets/Afterburn/Scenes";
        public const string BootPath = ScenesDir + "/Boot.unity";
        public const string MainMenuPath = ScenesDir + "/MainMenu.unity";
        public const string RacePath = ScenesDir + "/Race.unity";

        /// <summary>Batchmode entry: seed SOs then build scenes (U1 bootstrap, one -executeMethod call).</summary>
        public static void BootstrapU1()
        {
            AfterburnMenu.CreateOrUpdateSOs();
            BuildScenes();
        }

        [MenuItem("Veratus/Afterburn/Setup/Build Scenes", priority = 20)]
        public static void BuildScenes()
        {
            AfterburnMenu.EnsureFolder(ScenesDir);

            BuildBoot();
            BuildMainMenu();
            BuildRace();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootPath, true),
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene(RacePath, true),
            };
            Debug.Log("[Afterburn] Scenes built: Boot, MainMenu, Race (+ EditorBuildSettings list).");
        }

        private static void BuildBoot()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMenuCamera("Main Camera");
            new GameObject("BootRoot");   // U5 wires the studio splash + MainMenu load here
            EditorSceneManager.SaveScene(scene, BootPath);
        }

        private static void BuildMainMenu()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMenuCamera("Main Camera");
            new GameObject("MenuRoot");   // U5 wires the loadout screen here
            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        private static void BuildRace()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var arena = AssetDatabase.LoadAssetAtPath<TrackDefinition>(AfterburnMenu.TracksDir + "/Arena01.asset");
            var mediumHull = AssetDatabase.LoadAssetAtPath<HullDefinition>(AfterburnMenu.HullsDir + "/Medium.asset");
            if (arena == null || mediumHull == null)
            {
                Debug.LogError("[Afterburn] Run 'Veratus/Afterburn/Create or Update SOs' before building scenes.");
                return;
            }

            // Scene dressing per PortSpec §10 (greybox): fog + trilight ambient, sun at (120,200,80).
            Color bg = Hex("#05070F");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = bg;
            RenderSettings.fogStartDistance = 220f;
            RenderSettings.fogEndDistance = 620f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            Color sky = Hex("#9FC4FF") * 0.9f;
            Color ground = Hex("#0A0F1E") * 0.9f;
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = Color.Lerp(sky, ground, 0.5f);
            RenderSettings.ambientGroundColor = ground;

            var sunGo = new GameObject("Directional Light");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = Color.white;
            sun.intensity = 0.9f;
            sunGo.transform.position = new Vector3(120f, 200f, 80f);
            sunGo.transform.rotation = Quaternion.LookRotation(-sunGo.transform.position.normalized);

            // Track (greybox built by TrackView on Awake).
            var trackGo = new GameObject("Track");
            var trackView = trackGo.AddComponent<TrackView>();
            trackView.Track = arena;
            EditorUtility.SetDirty(trackView);

            // Starfield backdrop (PortSpec §10, built on Awake).
            new GameObject("Starfield").AddComponent<StarfieldView>();

            // Ship at the start line, lane 0 (PortSpec §5), y 1.2, facing along +tangent.
            TrackSample start = TrackSampler.BuildSamples(arena)[0];
            var shipGo = new GameObject("Ship");
            var ship = shipGo.AddComponent<ShipGreybox>();
            ship.Hull = mediumHull;
            EditorUtility.SetDirty(ship);
            shipGo.transform.SetPositionAndRotation(
                new Vector3(start.Pos.x, 1.2f, start.Pos.z),
                Quaternion.LookRotation(new Vector3(start.Tan.x, 0f, start.Tan.z), Vector3.up));

            // U2–U4: the race driver — full roster (player Medium+Vex vs heavy/light/medium ghosts),
            // prototype keyboard scheme, combat + abilities + bounty live. HUD is a dev overlay until U5.
            var tuning = AssetDatabase.LoadAssetAtPath<GameTuning>(AfterburnMenu.GameTuningPath);
            var vex = AssetDatabase.LoadAssetAtPath<PilotDefinition>(AfterburnMenu.PilotsDir + "/Vex.asset");
            var heavyHull = AssetDatabase.LoadAssetAtPath<HullDefinition>(AfterburnMenu.HullsDir + "/Heavy.asset");
            var lightHull = AssetDatabase.LoadAssetAtPath<HullDefinition>(AfterburnMenu.HullsDir + "/Light.asset");
            var runnerGo = new GameObject("RaceRunner");
            var runner = runnerGo.AddComponent<RaceRunner>();
            runner.Track = arena;
            runner.Tuning = tuning;
            runner.PlayerHull = mediumHull;
            runner.PlayerPilot = vex;
            runner.HeavyHull = heavyHull;
            runner.LightHull = lightHull;
            runner.MediumHull = mediumHull;
            runner.ShipTransform = shipGo.transform;
            runner.Greybox = trackView;
            EditorUtility.SetDirty(runner);

            // Chase camera (PortSpec §2), parked at its rest pose behind the ship.
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 2000f;
            camGo.AddComponent<AudioListener>();
            var chase = camGo.AddComponent<ChaseCamera>();
            chase.Target = shipGo.transform;
            EditorUtility.SetDirty(chase);
            Vector3 fwd = shipGo.transform.forward;
            Vector3 restPos = shipGo.transform.position - fwd * 16f;
            restPos.y = 9f;
            camGo.transform.position = restPos;
            Vector3 look = shipGo.transform.position + fwd * 10f;
            look.y = 2f;
            camGo.transform.LookAt(look);

            EditorSceneManager.SaveScene(scene, RacePath);
        }

        private static void CreateMenuCamera(string cameraName)
        {
            var camGo = new GameObject(cameraName) { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("#05070F");
            camGo.AddComponent<AudioListener>();
            // Prototype menu camera pose: (0, 120, −160) looking at (0, 0, 60).
            camGo.transform.position = new Vector3(0f, 120f, -160f);
            camGo.transform.LookAt(new Vector3(0f, 0f, 60f));
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
        }
    }
}

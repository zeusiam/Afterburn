using System.IO;
using Afterburn.Core;
using UnityEditor;
using UnityEngine;

namespace Afterburn.EditorTools
{
    /// <summary>
    /// Single idempotent entry point that creates or updates every ScriptableObject data asset
    /// (BUILD §5, §9). Running this menu repeatedly never duplicates assets nor overwrites
    /// hand-edited values — the Veratus Games studio-standard seeder contract (EnsureSO pattern,
    /// mirrored from Rune Rouge / Eclipse / Nibwell).
    ///
    /// Values below are FROZEN from the prototype (BUILD §5.1–5.3; the P2 kill gate passed at
    /// these numbers). Do not retune here — retuning happens on the asset via the dev overlay.
    /// </summary>
    public static class AfterburnMenu
    {
        public const string DataDir = "Assets/Afterburn/Data";
        public const string HullsDir = DataDir + "/Hulls";
        public const string PilotsDir = DataDir + "/Pilots";
        public const string TracksDir = DataDir + "/Tracks";
        public const string GameTuningPath = DataDir + "/GameTuning.asset";

        /// <summary>Prototype star-convex radius multipliers (PortSpec §1 — deterministic, no seed).</summary>
        private static readonly float[] Arena01Shape =
            { 1.00f, 0.94f, 0.78f, 0.74f, 0.92f, 1.06f, 1.10f, 0.86f, 0.72f, 0.82f, 1.02f, 1.08f, 0.92f, 0.84f };

        [MenuItem("Veratus/Afterburn/Create or Update SOs", priority = 0)]
        public static void CreateOrUpdateSOs()
        {
            EnsureFolder(DataDir);
            EnsureFolder(HullsDir);
            EnsureFolder(PilotsDir);
            EnsureFolder(TracksDir);

            int created = 0, kept = 0;

            // GameTuning — field initializers are the frozen §5.3 defaults.
            EnsureSO<GameTuning>(GameTuningPath, ref created, ref kept);

            // Hulls (§5.1) — sidegrades, never straight power.
            EnsureHull("Light", 80f, 11f, 1.15f, 0.8f, 1.6f, GateAccess.LightGap, "#37D0FF",
                "Fits narrow gaps", "Fast, fragile, thirsty pool. Squeezes through the Light Gap shortcut.",
                ref created, ref kept);
            EnsureHull("Medium", 100f, 8f, 1.00f, 1.0f, 2.0f, GateAccess.None, "#9D7BFF",
                "Balanced", "No gate access, no weakness. The honest default.",
                ref created, ref kept);
            EnsureHull("Heavy", 130f, 5f, 0.88f, 1.4f, 2.6f, GateAccess.HeavyWall, "#FF8A3C",
                "Smashes walls", "Slow but a huge pool. Rams through the Heavy Wall shortcut.",
                ref created, ref kept);

            // Pilots (§5.2) — upgrades reduce cooldownSec only. Never damage, never speed.
            EnsurePilot("Vex", "EMP Pulse", 18f, AbilityType.EmpPulse, 30f, 70f,
                "Drains 30 energy from nearby rivals.", ref created, ref kept);
            EnsurePilot("Sora", "Phase Shift", 15f, AbilityType.PhaseShift, 1.2f, 0f,
                "1.2s intangible — pass through walls & shots.", ref created, ref kept);
            EnsurePilot("Kade", "Siphon", 20f, AbilityType.Siphon, 25f, 0f,
                "Your next hit steals 25 energy.", ref created, ref kept);
            EnsurePilot("Nyx", "Decoy", 22f, AbilityType.Decoy, 3.0f, 0f,
                "Spawns a decoy that draws enemy fire 3s.", ref created, ref kept);

            // Arena01 (§7.5 / PortSpec §1) — the prototype's star-convex loop, reproduced exactly.
            EnsureArena01(ref created, ref kept);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Afterburn] Create or Update SOs complete — {created} created, {kept} kept, 0 overwritten " +
                      "(existing assets and hand-edited values are preserved).");
        }

        private static void EnsureHull(string id, float maxEnergy, float regenPerSec, float topSpeedMult,
            float mass, float collisionRadius, GateAccess gateAccess, string tintHex,
            string tag, string description, ref int created, ref int kept)
        {
            string path = $"{HullsDir}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<HullDefinition>(path);
            if (existing != null) { kept++; return; }   // never overwrite hand-edited values

            var hull = ScriptableObject.CreateInstance<HullDefinition>();
            hull.displayName = id;
            hull.maxEnergy = maxEnergy;
            hull.regenPerSec = regenPerSec;
            hull.topSpeedMult = topSpeedMult;
            hull.mass = mass;
            hull.collisionRadius = collisionRadius;
            hull.gateAccess = gateAccess;
            hull.tintColor = ParseHex(tintHex);
            hull.tag = tag;
            hull.description = description;
            AssetDatabase.CreateAsset(hull, path);
            created++;
            Debug.Log($"[Afterburn] Created HullDefinition '{id}' at {path}.");
        }

        private static void EnsurePilot(string id, string abilityName, float cooldownSec, AbilityType type,
            float abilityParam, float abilityRadius, string description, ref int created, ref int kept)
        {
            string path = $"{PilotsDir}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PilotDefinition>(path);
            if (existing != null) { kept++; return; }

            var pilot = ScriptableObject.CreateInstance<PilotDefinition>();
            pilot.displayName = id;
            pilot.abilityName = abilityName;
            pilot.cooldownSec = cooldownSec;
            pilot.abilityType = type;
            pilot.abilityParam = abilityParam;
            pilot.abilityRadius = abilityRadius;
            pilot.description = description;
            AssetDatabase.CreateAsset(pilot, path);
            created++;
            Debug.Log($"[Afterburn] Created PilotDefinition '{id}' at {path}.");
        }

        private static void EnsureArena01(ref int created, ref int kept)
        {
            string path = $"{TracksDir}/Arena01.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TrackDefinition>(path);
            if (existing != null) { kept++; return; }

            var track = ScriptableObject.CreateInstance<TrackDefinition>();
            track.displayName = "Arena 01";
            track.baseRadius = 300f;
            track.radiusShape = (float[])Arena01Shape.Clone();
            track.controlPoints = TrackSampler.GenerateStarConvexPoints(track.baseRadius, track.radiusShape);
            track.catmullTension = 0.5f;
            track.sampleCount = 700;
            track.halfWidth = 17f;
            track.wallHeight = 3.2f;
            track.checkpointFractions = System.Array.Empty<float>();   // prototype defines none (PortSpec div #2)
            track.shortcuts = new[]
            {
                new ShortcutZone { access = GateAccess.LightGap, fromFraction = 0.20f, toFraction = 0.24f, extraInnerAllowance = 20f, side = 1 },
                new ShortcutZone { access = GateAccess.HeavyWall, fromFraction = 0.61f, toFraction = 0.66f, extraInnerAllowance = 22f, side = 1 },
            };
            AssetDatabase.CreateAsset(track, path);
            created++;
            Debug.Log($"[Afterburn] Created TrackDefinition 'Arena01' at {path}.");
        }

        /// <summary>
        /// D13 gate 0: wire three distinct StarSparrow example ships onto the hull assets
        /// (only where empty — idempotent, never overwrites a hand-picked prefab).
        /// </summary>
        [MenuItem("Veratus/Afterburn/Setup/Assign StarSparrow Hull Visuals", priority = 30)]
        public static void AssignStarSparrowVisuals()
        {
            (string hull, string prefab)[] picks =
            {
                ("Light", "Assets/StarSparrow/Prefabs/Examples/StarSparrow2.prefab"),
                ("Medium", "Assets/StarSparrow/Prefabs/Examples/StarSparrow15.prefab"),
                ("Heavy", "Assets/StarSparrow/Prefabs/Examples/StarSparrow28.prefab"),
            };
            int assigned = 0;
            foreach ((string hullName, string prefabPath) in picks)
            {
                var hull = AssetDatabase.LoadAssetAtPath<HullDefinition>($"{HullsDir}/{hullName}.asset");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (hull == null) { Debug.LogError($"[Afterburn] Hull '{hullName}' missing — run Create or Update SOs."); continue; }
                if (prefab == null) { Debug.LogError($"[Afterburn] Prefab not found: {prefabPath}"); continue; }
                if (hull.shipPrefab != null) continue;      // hand-picked — keep
                hull.shipPrefab = prefab;
                EditorUtility.SetDirty(hull);
                assigned++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Afterburn] StarSparrow visuals assigned to {assigned} hull(s). " +
                      "Clear a hull's Ship Prefab field to return it to greybox.");
        }

        /// <summary>Creates the asset with its default field values only if missing (studio contract).</summary>
        public static T EnsureSO<T>(string path, ref int created, ref int kept) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                kept++;
                return existing;   // never overwrite hand-edited values
            }

            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, path);
            created++;
            Debug.Log($"[Afterburn] Created {typeof(T).Name} at {path}.");
            return so;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        private static Color ParseHex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
        }
    }
}

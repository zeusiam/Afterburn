using Afterburn.Core;
using UnityEngine;

namespace Afterburn.View
{
    /// <summary>
    /// Greybox track renderer (BUILD §7.5 View half). Builds the prototype's arena visuals from a
    /// <see cref="TrackDefinition"/> at runtime: road ribbon, centre stripe, inner/outer walls,
    /// ground grid, start/finish gate and shortcut markers — geometry and colors per PortSpec §1/§10.
    /// Reads Core, never writes game state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrackView : MonoBehaviour
    {
        /// <summary>Walls sit at ±(halfWidth + this) — prototype 0.4.</summary>
        private const float WallInset = 0.4f;

        [SerializeField] private TrackDefinition? track;

        [Tooltip("Optional warp-gate prefab for the start/finish line (Ebal Modular Warp Gates). " +
                 "Null = procedural SDF-style ring. Auto-fitted to span the track width.")]
        [SerializeField] private GameObject? startGatePrefab;

        [Tooltip("World width the gate's bounds are scaled to span (track is 34 u wide + clearance).")]
        [SerializeField] private float gateSpanWidth = 46f;

        [Tooltip("Max world height/depth after fitting — station-scale gate assemblies get clamped " +
                 "instead of towering over the arena.")]
        [SerializeField] private float gateMaxExtent = 55f;

        [Tooltip("Gate parked this far AHEAD of the start line along the tangent, so the grid " +
                 "spawns outside it and flies through on launch (0 = centred on the line).")]
        [SerializeField] private float gateForwardOffset = 22f;

        [Tooltip("D15 gate-feature ring visual (small warp gate) — auto-fitted per feature. " +
                 "Null = procedural rings.")]
        [SerializeField] private GameObject? featureGatePrefab;

        [Tooltip("Scenic gates orbiting the arena at distance (pure dressing). Empty = none.")]
        [SerializeField] private GameObject[] scenicGatePrefabs = System.Array.Empty<GameObject>();

        public GameObject? FeatureGatePrefab { get => featureGatePrefab; set => featureGatePrefab = value; }
        public GameObject[] ScenicGatePrefabs { get => scenicGatePrefabs; set => scenicGatePrefabs = value; }

        private TrackSample[]? _samples;
        private GameObject? _heavySlab;

        public GameObject? StartGatePrefab { get => startGatePrefab; set => startGatePrefab = value; }

        public TrackDefinition? Track
        {
            get => track;
            set => track = value;
        }

        /// <summary>The dense sample cache — built on demand so Core systems (U2) can share it later.</summary>
        public TrackSample[] Samples
        {
            get
            {
                _samples ??= TrackSampler.BuildSamples(track!);
                return _samples;
            }
        }

        private void Awake()
        {
            if (track == null)
            {
                Debug.LogError("[Afterburn] TrackView has no TrackDefinition assigned.", this);
                return;
            }
            BuildGreybox();
        }

        /// <summary>Prototype: the slab hides when the Heavy smashes it (no shatter VFX in greybox).</summary>
        public void HideHeavySlab()
        {
            if (_heavySlab != null) _heavySlab.SetActive(false);
        }

        private void BuildGreybox()
        {
            TrackSample[] s = Samples;
            float half = track!.halfWidth;

            // Road ribbon ±half at y 0.02 (PortSpec §10: #12203C rough .9 metal .1).
            AddMesh("Road", BuildRibbon(s, -half, half, 0.02f),
                GreyboxMaterials.Lit(GreyboxMaterials.Hex("#12203C"), 0.9f, 0.1f, doubleSided: true));

            // Centre stripe ±0.6 at y 0.05 (#1C2F57, emissive #0A1830).
            AddMesh("CentreStripe", BuildRibbon(s, -0.6f, 0.6f, 0.05f),
                GreyboxMaterials.Lit(GreyboxMaterials.Hex("#1C2F57"), 0.6f, 0f,
                    GreyboxMaterials.Hex("#0A1830"), doubleSided: true));

            // Walls at ±(half+0.4), height wallHeight (#2B4D8F, emissive #0D2350, double-sided).
            Material wallMat = GreyboxMaterials.Lit(GreyboxMaterials.Hex("#2B4D8F"), 0.5f, 0f,
                GreyboxMaterials.Hex("#0D2350"), doubleSided: true);
            AddMesh("WallOuter", BuildWall(s, -(half + WallInset), track.wallHeight), wallMat);
            AddMesh("WallInner", BuildWall(s, half + WallInset, track.wallHeight), wallMat);

            BuildGroundGrid();
            BuildStartGate(s, half);
            BuildShortcutMarkers(s, half);
            BuildFeatureGates(s);
            BuildScenicRing();
        }

        /// <summary>
        /// D15 gate features: a ring per feature at its track fraction, color-coded by type —
        /// boost = cyan, warp surge = violet, blocker = red (the palette's damage color, honestly
        /// earned: it hurts). Trigger logic lives in Core; these are the telegraphs.
        /// </summary>
        private void BuildFeatureGates(TrackSample[] s)
        {
            if (track!.gateFeatures == null) return;
            int n = s.Length;
            foreach (GateFeature f in track.gateFeatures)
            {
                TrackSample sample = s[Mathf.FloorToInt(Mathf.Repeat(f.fraction, 1f) * n) % n];
                Vector3 centre = sample.Pos + sample.Nrm * f.lateralOffset;

                string hex = f.type switch
                {
                    GateFeatureType.WarpSurge => "#9D7BFF",
                    GateFeatureType.Blocker => "#FF4D6D",
                    _ => "#37D0FF",
                };
                Color tint = GreyboxMaterials.Hex(hex);

                GameObject ring;
                float targetWidth = f.halfSpan * 2f;
                if (featureGatePrefab != null)
                {
                    ring = ViewPrefabs.InstantiateWithoutColliders(featureGatePrefab, transform);
                    Renderer[] renderers = ring.GetComponentsInChildren<Renderer>();
                    if (renderers.Length > 0)
                    {
                        Bounds b = renderers[0].bounds;
                        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
                        float scale = targetWidth / Mathf.Max(b.size.x, 0.01f);
                        scale = Mathf.Min(scale, 30f / Mathf.Max(b.size.y, 0.01f));
                        ring.transform.localScale = Vector3.one * scale;
                    }
                }
                else
                {
                    ring = new GameObject();
                    ring.transform.SetParent(transform, false);
                    ring.AddComponent<MeshFilter>().sharedMesh = BuildTorus(f.halfSpan, 0.45f, 8, 24);
                    ring.AddComponent<MeshRenderer>().sharedMaterial =
                        GreyboxMaterials.Lit(tint, 0.5f, 0f, tint, f.type == GateFeatureType.Blocker ? 0.9f : 0.6f);
                }
                ring.name = $"GateFeature_{f.type}";
                ring.transform.position = new Vector3(centre.x, 5f, centre.z);
                ring.transform.rotation = Quaternion.LookRotation(sample.Tan, Vector3.up);
            }
        }

        /// <summary>D15 "surround the stage": station-scale gates orbiting the arena — pure backdrop.</summary>
        private void BuildScenicRing()
        {
            if (scenicGatePrefabs == null || scenicGatePrefabs.Length == 0) return;
            const int count = 5;
            const float radius = 620f;
            for (int i = 0; i < count; i++)
            {
                GameObject prefab = scenicGatePrefabs[i % scenicGatePrefabs.Length];
                if (prefab == null) continue;
                float a = i / (float)count * Mathf.PI * 2f + 0.4f;
                Vector3 pos = new Vector3(Mathf.Cos(a) * radius, 25f + (i % 3) * 30f, Mathf.Sin(a) * radius);

                GameObject gate = ViewPrefabs.InstantiateWithoutColliders(prefab, transform);
                gate.name = $"ScenicGate{i}";
                gate.transform.position = pos;
                gate.transform.rotation = Quaternion.LookRotation((-pos).normalized, Vector3.up)
                                          * Quaternion.Euler(0f, (i * 47f) % 90f - 45f, 0f);
                gate.transform.localScale = Vector3.one * 3f;   // distant + fog: silhouettes, not detail
            }
        }

        private void AddMesh(string childName, Mesh mesh, Material mat)
        {
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        /// <summary>Prototype buildRibbon: quads between lateral offsets [offInner, offOuter] at height y.</summary>
        private static Mesh BuildRibbon(TrackSample[] s, float offInner, float offOuter, float y)
        {
            int n = s.Length;
            var verts = new Vector3[(n + 1) * 2];
            for (int i = 0; i <= n; i++)
            {
                TrackSample sm = s[i % n];
                Vector3 a = sm.Pos + sm.Nrm * offOuter; a.y = y;
                Vector3 b = sm.Pos + sm.Nrm * offInner; b.y = y;
                verts[i * 2] = a;
                verts[i * 2 + 1] = b;
            }
            return StripToMesh("Ribbon", verts, n);
        }

        /// <summary>Prototype buildWall: vertical quads at lateral offset off, from y 0 to height.</summary>
        private static Mesh BuildWall(TrackSample[] s, float off, float height)
        {
            int n = s.Length;
            var verts = new Vector3[(n + 1) * 2];
            for (int i = 0; i <= n; i++)
            {
                TrackSample sm = s[i % n];
                Vector3 basePos = sm.Pos + sm.Nrm * off;
                verts[i * 2] = new Vector3(basePos.x, 0f, basePos.z);
                verts[i * 2 + 1] = new Vector3(basePos.x, height, basePos.z);
            }
            return StripToMesh("Wall", verts, n);
        }

        /// <summary>Triangulate a 2-wide vertex strip exactly like the prototype's index pattern.</summary>
        private static Mesh StripToMesh(string meshName, Vector3[] verts, int segmentCount)
        {
            var tris = new int[segmentCount * 6];
            for (int i = 0; i < segmentCount; i++)
            {
                int a = i * 2, b = i * 2 + 1, c = i * 2 + 2, d = i * 2 + 3;
                int o = i * 6;
                tris[o] = a; tris[o + 1] = b; tris[o + 2] = c;
                tris[o + 3] = b; tris[o + 4] = d; tris[o + 5] = c;
            }
            var mesh = new Mesh
            {
                name = meshName,
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = verts,
                triangles = tris,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Prototype GridHelper(1600, 80): line grid at y −0.4, centre cross #11305F, rest #0A1C3A.</summary>
        private void BuildGroundGrid()
        {
            const float size = 1600f;
            const int divisions = 80;
            const float y = -0.4f;
            float halfSize = size / 2f;
            float step = size / divisions;

            var centreVerts = new System.Collections.Generic.List<Vector3>(4);
            var gridVerts = new System.Collections.Generic.List<Vector3>(divisions * 4);
            for (int i = 0; i <= divisions; i++)
            {
                float k = -halfSize + i * step;
                var target = Mathf.Approximately(k, 0f) ? centreVerts : gridVerts;
                target.Add(new Vector3(-halfSize, y, k));
                target.Add(new Vector3(halfSize, y, k));
                target.Add(new Vector3(k, y, -halfSize));
                target.Add(new Vector3(k, y, halfSize));
            }

            AddLineMesh("GridLines", gridVerts.ToArray(), GreyboxMaterials.Hex("#0A1C3A"));
            AddLineMesh("GridCentre", centreVerts.ToArray(), GreyboxMaterials.Hex("#11305F"));
        }

        private void AddLineMesh(string childName, Vector3[] verts, Color color)
        {
            var indices = new int[verts.Length];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            var mesh = new Mesh { name = childName, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.SetIndices(indices, MeshTopology.Lines, 0);
            mesh.RecalculateBounds();

            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = GreyboxMaterials.Unlit(color);
        }

        /// <summary>
        /// Prototype gateAt(0): white emissive torus r half+1, tube 0.5, at y half−2 — a vertical
        /// drive-through ring, plane perpendicular to travel. (The prototype's lookAt target sits
        /// ~15 u below the gate, so its rotateX(π/2) cancels the downward look; LookRotation along
        /// the tangent alone reproduces the net pose to within its ~3.8° lean.)
        /// </summary>
        private void BuildStartGate(TrackSample[] s, float half)
        {
            TrackSample s0 = s[0];

            if (startGatePrefab != null)
            {
                // D13 fleet: a real warp gate spans the start line. Auto-fit its bounds to the
                // configured span, base resting on the road, opening facing the direction of travel.
                GameObject gate = ViewPrefabs.InstantiateWithoutColliders(startGatePrefab, transform);
                gate.name = "StartGate";

                Renderer[] renderers = gate.GetComponentsInChildren<Renderer>();
                Vector3 anchor = new Vector3(s0.Pos.x, 0f, s0.Pos.z) + s0.Tan * gateForwardOffset;
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                    // Fit to span the track — but clamp against station-scale assemblies whose
                    // bounds dwarf their ring opening (they'd otherwise swallow the whole grid).
                    float scale = gateSpanWidth / Mathf.Max(bounds.size.x, 0.01f);
                    scale = Mathf.Min(scale,
                        gateMaxExtent / Mathf.Max(bounds.size.y, 0.01f),
                        gateMaxExtent / Mathf.Max(bounds.size.z, 0.01f));
                    gate.transform.localScale = Vector3.one * scale;

                    // Recentre laterally/longitudinally on the anchor; rest the base at y 0.
                    Vector3 centre = bounds.center * scale;
                    float bottom = (bounds.min.y - bounds.center.y) * scale;
                    gate.transform.rotation = Quaternion.LookRotation(s0.Tan, Vector3.up);
                    gate.transform.position = anchor
                        - gate.transform.rotation * new Vector3(centre.x, 0f, centre.z)
                        + Vector3.up * (-bottom);
                }
                else
                {
                    gate.transform.SetPositionAndRotation(anchor,
                        Quaternion.LookRotation(s0.Tan, Vector3.up));
                }
                return;
            }

            var go = new GameObject("StartGate");
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = BuildTorus(half + 1f, 0.5f, 8, 24);
            go.AddComponent<MeshRenderer>().sharedMaterial =
                GreyboxMaterials.Lit(Color.white, 0.5f, 0f, Color.white, 0.6f);
            go.transform.position = new Vector3(s0.Pos.x, half - 2f, s0.Pos.z);
            go.transform.rotation = Quaternion.LookRotation(s0.Tan, Vector3.up);
        }

        /// <summary>Prototype shortcut markers: cyan pillar pair (LightGap), orange slab (HeavyWall).</summary>
        private void BuildShortcutMarkers(TrackSample[] s, float half)
        {
            if (track!.shortcuts == null) return;
            int n = s.Length;
            foreach (ShortcutZone zone in track.shortcuts)
            {
                int from = Mathf.FloorToInt(n * zone.fromFraction);
                int to = Mathf.FloorToInt(n * zone.toFraction);
                TrackSample mid = s[((from + to) / 2) % n];

                if (zone.access == GateAccess.LightGap)
                {
                    Material pillarMat = GreyboxMaterials.Lit(GreyboxMaterials.Hex("#37D0FF"), 0.5f, 0f,
                        GreyboxMaterials.Hex("#0A3A55"), 0.7f);
                    for (int sgn = -1; sgn <= 1; sgn += 2)
                    {
                        var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        pillar.name = $"LightGapPillar{(sgn < 0 ? "A" : "B")}";
                        pillar.transform.SetParent(transform, false);
                        Object.Destroy(pillar.GetComponent<Collider>());
                        // Unity cylinder is 2 u tall, r 0.5 → scale to r 1.1, h 7.
                        pillar.transform.localScale = new Vector3(2.2f, 3.5f, 2.2f);
                        Vector3 p = mid.Pos + mid.Nrm * ((half + 2f) * zone.side) + mid.Tan * (sgn * 3.4f);
                        pillar.transform.position = new Vector3(p.x, 3.5f, p.z);
                        pillar.GetComponent<MeshRenderer>().sharedMaterial = pillarMat;
                    }
                }
                else if (zone.access == GateAccess.HeavyWall)
                {
                    var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _heavySlab = slab;
                    slab.name = "HeavyWallSlab";
                    slab.transform.SetParent(transform, false);
                    Object.Destroy(slab.GetComponent<Collider>());
                    slab.transform.localScale = new Vector3(10f, 6f, 1.4f);
                    Vector3 p = mid.Pos + mid.Nrm * ((half + 3f) * zone.side);
                    slab.transform.position = new Vector3(p.x, 3f, p.z);
                    // Prototype: lookAt(mid.pos) with mid.pos.y ≈ 0 → keeps its ~8.5° downward tilt.
                    slab.transform.LookAt(mid.Pos);
                    slab.GetComponent<MeshRenderer>().sharedMaterial =
                        GreyboxMaterials.Lit(GreyboxMaterials.Hex("#FF8A3C"), 0.5f, 0f,
                            GreyboxMaterials.Hex("#5A2400"), 0.6f);
                }
            }
        }

        /// <summary>Procedural torus in the XY plane (axis +Z), matching three.js TorusGeometry.</summary>
        private static Mesh BuildTorus(float radius, float tube, int radialSegments, int tubularSegments)
        {
            var verts = new Vector3[(radialSegments + 1) * (tubularSegments + 1)];
            var tris = new int[radialSegments * tubularSegments * 6];

            for (int j = 0; j <= radialSegments; j++)
            {
                for (int i = 0; i <= tubularSegments; i++)
                {
                    float u = i / (float)tubularSegments * Mathf.PI * 2f;
                    float v = j / (float)radialSegments * Mathf.PI * 2f;
                    verts[j * (tubularSegments + 1) + i] = new Vector3(
                        (radius + tube * Mathf.Cos(v)) * Mathf.Cos(u),
                        (radius + tube * Mathf.Cos(v)) * Mathf.Sin(u),
                        tube * Mathf.Sin(v));
                }
            }

            int t = 0;
            for (int j = 1; j <= radialSegments; j++)
            {
                for (int i = 1; i <= tubularSegments; i++)
                {
                    int a = (tubularSegments + 1) * j + i - 1;
                    int b = (tubularSegments + 1) * (j - 1) + i - 1;
                    int c = (tubularSegments + 1) * (j - 1) + i;
                    int d = (tubularSegments + 1) * j + i;
                    tris[t++] = a; tris[t++] = b; tris[t++] = d;
                    tris[t++] = b; tris[t++] = c; tris[t++] = d;
                }
            }

            var mesh = new Mesh { name = "Torus", vertices = verts, triangles = tris };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

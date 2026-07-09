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

        private TrackSample[]? _samples;

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

using UnityEngine;

namespace Afterburn.View
{
    /// <summary>
    /// Starfield backdrop (PortSpec §10): 1400 stars on a far shell, r = 700 + rand·500,
    /// uniform sphere angles, y = |r·cos b|·0.6 + 40, color #6F8BD0, constant ~2px screen size.
    /// The prototype uses THREE.Points (sizeAttenuation off); without a point-size shader this
    /// builds one mesh of small origin-facing quads whose world size scales with distance so the
    /// angular size stays constant. Deterministic seed (the prototype rolls Math.random() each
    /// load — visual equivalence, reproducible builds).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StarfieldView : MonoBehaviour
    {
        private const int StarCount = 1400;
        private const int Seed = 20260709;

        /// <summary>2px at a 1290px-tall viewport, FOV 62 → world size ≈ 0.00186 × distance.</summary>
        private const float AngularSizeFactor = 0.00186f;

        private void Awake()
        {
            var rng = new System.Random(Seed);
            var verts = new Vector3[StarCount * 4];
            var tris = new int[StarCount * 6];

            for (int i = 0; i < StarCount; i++)
            {
                float r = 700f + (float)rng.NextDouble() * 500f;
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float b = Mathf.Acos(2f * (float)rng.NextDouble() - 1f);
                var pos = new Vector3(
                    r * Mathf.Sin(b) * Mathf.Cos(a),
                    Mathf.Abs(r * Mathf.Cos(b)) * 0.6f + 40f,
                    r * Mathf.Sin(b) * Mathf.Sin(a));

                // Quad facing the arena centre, constant angular size.
                Vector3 dir = -pos.normalized;
                Vector3 right = Mathf.Abs(dir.y) > 0.99f
                    ? Vector3.right
                    : Vector3.Cross(Vector3.up, dir).normalized;
                Vector3 up = Vector3.Cross(dir, right).normalized;
                float half = pos.magnitude * AngularSizeFactor * 0.5f;

                int v = i * 4;
                verts[v] = pos - right * half - up * half;
                verts[v + 1] = pos - right * half + up * half;
                verts[v + 2] = pos + right * half + up * half;
                verts[v + 3] = pos + right * half - up * half;

                int t = i * 6;
                tris[t] = v; tris[t + 1] = v + 1; tris[t + 2] = v + 2;
                tris[t + 3] = v; tris[t + 4] = v + 2; tris[t + 5] = v + 3;
            }

            var mesh = new Mesh
            {
                name = "Starfield",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = verts,
                triangles = tris,
            };
            mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial =
                GreyboxMaterials.Unlit(GreyboxMaterials.Hex("#6F8BD0"));
        }
    }
}

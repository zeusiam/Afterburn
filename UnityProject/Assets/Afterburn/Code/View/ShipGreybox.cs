using Afterburn.Core;
using UnityEngine;

namespace Afterburn.View
{
    /// <summary>
    /// Greybox ship visual, matching the prototype mesh (PortSpec §10): 4-sided cone body
    /// (apex forward, base rolled 45°), dark wing box at z −1.2, unlit thruster glow sphere
    /// at z −2.6. Tint driven by <see cref="HullDefinition.tintColor"/> — never hard-coded.
    /// Visual only; ShipController logic lands at U2.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShipGreybox : MonoBehaviour
    {
        [SerializeField] private HullDefinition? hull;

        private Transform? _thruster;
        private Color? _tintOverride;

        public HullDefinition? Hull
        {
            get => hull;
            set => hull = value;
        }

        /// <summary>Roster tint override (prototype: ghost colors trump hull colors). Set before Awake.</summary>
        public Color? TintOverride
        {
            get => _tintOverride;
            set => _tintOverride = value;
        }

        /// <summary>
        /// Ghost-hologram treatment (UIEnvSpec §3.1): semi-transparent body/wing so intangibility
        /// READS — racers pass through ghosts by design (async opponents, no ship-ship collision,
        /// prototype parity). The thruster stays opaque-bright for trackability at range.
        /// Set before Awake.
        /// </summary>
        public bool HologramMode { get; set; }

        /// <summary>The thruster glow — U2's ShipView will scale/tint it with boost state.</summary>
        public Transform? Thruster => _thruster;

        /// <summary>
        /// D15.1 Shredder feedback: tear a small piece off the hull — it tumbles away and fades.
        /// Purely visual (the sim's damage/debuff already applied); parts return on next spawn.
        /// </summary>
        public void DetachRandomPart()
        {
            Renderer[] parts = GetComponentsInChildren<Renderer>();
            if (parts.Length <= 1) return;                       // never strip the last piece
            Renderer victim = parts[Random.Range(1, parts.Length)];
            Transform t = victim.transform;
            t.SetParent(null, true);
            StartCoroutine(TumbleAway(t));
        }

        private System.Collections.IEnumerator TumbleAway(Transform part)
        {
            Vector3 spin = new Vector3(Random.value, Random.value, Random.value) * 540f;
            Vector3 drift = (Vector3.up * 2f + Random.insideUnitSphere * 4f);
            float life = 1.5f;
            for (float e = 0f; e < life && part != null; e += Time.deltaTime)
            {
                part.position += drift * Time.deltaTime;
                part.Rotate(spin * Time.deltaTime);
                part.localScale *= 1f - 0.9f * (Time.deltaTime / life);
                yield return null;
            }
            if (part != null) Destroy(part.gameObject);
        }

        private void Awake()
        {
            Color tint = _tintOverride ?? (hull != null ? hull.tintColor : GreyboxMaterials.Hex("#9D7BFF"));
            if (hull != null && hull.shipPrefab != null)
            {
                BuildFromPrefab(hull.shipPrefab);
            }
            else
            {
                BuildBody(tint);
                BuildWing();
                BuildThruster();
            }
        }

        /// <summary>
        /// D13 hybrid path: instantiate the PBR ship and auto-fit it to sim scale — the sim's
        /// collision radius and camera framing assume a ~5 u ship with +z forward (prototype).
        /// </summary>
        private void BuildFromPrefab(GameObject prefab)
        {
            GameObject visual = ViewPrefabs.InstantiateWithoutColliders(prefab, transform);
            visual.name = "HullVisual";

            // Auto-fit: uniform scale so the forward (z) length ≈ 5 u, recentred on the sim origin.
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

                float length = Mathf.Max(bounds.size.z, 0.01f);
                float presence = hull != null ? Mathf.Max(hull.shipVisualScale, 0.1f) : 1f;
                float scale = 5f * presence / length;
                visual.transform.localScale = Vector3.one * scale;
                Vector3 worldCentre = bounds.center;
                Vector3 localCentre = transform.InverseTransformPoint(worldCentre) * scale;
                visual.transform.localPosition = -localCentre;
            }
        }

        /// <summary>
        /// ConeGeometry(1.5, 5, 4) with the apex straight forward (+z) and the square base rolled 45°.
        /// DELIBERATE DEVIATION (PortSpec divergence #16): the prototype's literal render is skewed —
        /// three.js applies `rotation.z=π/4` BEFORE `rotation.x=π/2` (XYZ Euler order), pointing the
        /// cone nose 45° off the ship's travel direction. Almost certainly an authoring bug; this port
        /// straightens the nose. Ruling due at the U2 side-by-side gate.
        /// </summary>
        private void BuildBody(Color tint)
        {
            const float radius = 1.5f;
            const float height = 5f;
            const int segments = 4;
            const float rollOffset = Mathf.PI / 4f;

            Vector3 apex = new Vector3(0f, 0f, height / 2f);
            var baseVerts = new Vector3[segments];
            for (int i = 0; i < segments; i++)
            {
                // Base ring in the XY plane at z = −h/2; π/4 roll makes the square read as a diamond.
                float a = i / (float)segments * Mathf.PI * 2f + rollOffset;
                baseVerts[i] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, -height / 2f);
            }

            // Flat-shaded: duplicated verts per face.
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int b = verts.Count;
                verts.Add(apex);
                verts.Add(baseVerts[next]);
                verts.Add(baseVerts[i]);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            }
            int cap = verts.Count;
            for (int i = 0; i < segments; i++) verts.Add(baseVerts[i]);
            tris.Add(cap); tris.Add(cap + 1); tris.Add(cap + 2);
            tris.Add(cap); tris.Add(cap + 2); tris.Add(cap + 3);

            var mesh = new Mesh { name = "ShipCone" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            body.AddComponent<MeshFilter>().sharedMesh = mesh;
            body.AddComponent<MeshRenderer>().sharedMaterial = HologramMode
                ? GreyboxMaterials.LitTransparent(tint, 0.55f, 0.4f, 0.4f, tint, 0.6f)
                : GreyboxMaterials.Lit(tint, 0.4f, 0.4f, tint, 0.25f);
        }

        /// <summary>Prototype: Box(5, 0.4, 1.6), color #000223, metalness .5, at z −1.2.</summary>
        private void BuildWing()
        {
            var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wing.name = "Wing";
            wing.transform.SetParent(transform, false);
            Destroy(wing.GetComponent<Collider>());
            wing.transform.localScale = new Vector3(5f, 0.4f, 1.6f);
            wing.transform.localPosition = new Vector3(0f, 0f, -1.2f);
            wing.GetComponent<MeshRenderer>().sharedMaterial = HologramMode
                ? GreyboxMaterials.LitTransparent(GreyboxMaterials.Hex("#000223"), 0.45f, 0.5f, 0.5f)
                : GreyboxMaterials.Lit(GreyboxMaterials.Hex("#000223"), 0.5f, 0.5f);
        }

        /// <summary>Prototype: unlit sphere r 0.9 at z −2.6, color #FF7A3C.</summary>
        private void BuildThruster()
        {
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "Thruster";
            glow.transform.SetParent(transform, false);
            Destroy(glow.GetComponent<Collider>());
            glow.transform.localScale = Vector3.one * 1.8f;   // Unity sphere r 0.5 → r 0.9
            glow.transform.localPosition = new Vector3(0f, 0f, -2.6f);
            glow.GetComponent<MeshRenderer>().sharedMaterial =
                GreyboxMaterials.Unlit(GreyboxMaterials.Hex("#FF7A3C"));
            _thruster = glow.transform;
        }
    }
}

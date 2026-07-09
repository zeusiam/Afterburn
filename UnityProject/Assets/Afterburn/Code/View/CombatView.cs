using Afterburn.Core;
using UnityEngine;

namespace Afterburn.View
{
    /// <summary>
    /// Greybox rendering for the combat sim: 40 pooled bullet spheres (r 0.6, unlit #FF4D6D,
    /// PortSpec §10) synced from <see cref="CombatSystem.Projectiles"/> each frame, plus the
    /// Nyx decoy (wireframe-ish octahedron stand-in, #FFD23F, spinning 4 rad/s).
    /// Pure presentation — reads Core, never writes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatView : MonoBehaviour
    {
        private CombatSystem? _combat;
        private PilotAbilitySystem? _abilities;
        private Transform[]? _bulletPool;
        private Transform? _decoy;

        public void Bind(CombatSystem combat, PilotAbilitySystem abilities)
        {
            _combat = combat;
            _abilities = abilities;
            BuildPools();
        }

        private void BuildPools()
        {
            Material bulletMat = GreyboxMaterials.Unlit(GreyboxMaterials.Hex("#FF4D6D"));
            _bulletPool = new Transform[CombatSystem.PoolSize];
            for (int i = 0; i < CombatSystem.PoolSize; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"Bullet{i:00}";
                go.transform.SetParent(transform, false);
                Destroy(go.GetComponent<Collider>());
                go.transform.localScale = Vector3.one * 1.2f;   // r 0.6
                go.GetComponent<MeshRenderer>().sharedMaterial = bulletMat;
                go.SetActive(false);
                _bulletPool[i] = go.transform;
            }

            // Decoy stand-in: gold octahedron (two stacked pyramids ≈ a rotated cube is close
            // enough for greybox — we use a diamond-rotated cube).
            var decoyGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            decoyGo.name = "Decoy";
            decoyGo.transform.SetParent(transform, false);
            Destroy(decoyGo.GetComponent<Collider>());
            decoyGo.transform.localScale = Vector3.one * 2.8f;
            decoyGo.transform.rotation = Quaternion.Euler(45f, 0f, 45f);
            decoyGo.GetComponent<MeshRenderer>().sharedMaterial =
                GreyboxMaterials.Lit(GreyboxMaterials.Hex("#FFD23F"), 0.5f, 0f,
                    GreyboxMaterials.Hex("#FFD23F"), 0.4f);
            decoyGo.SetActive(false);
            _decoy = decoyGo.transform;
        }

        private void LateUpdate()
        {
            if (_combat == null || _bulletPool == null) return;

            var projectiles = _combat.Projectiles;
            for (int i = 0; i < projectiles.Count; i++)
            {
                CombatSystem.Projectile p = projectiles[i];
                Transform t = _bulletPool[i];
                if (p.Live)
                {
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                    t.position = p.Position;
                }
                else if (t.gameObject.activeSelf)
                {
                    t.gameObject.SetActive(false);
                }
            }

            if (_decoy != null && _abilities != null)
            {
                PilotAbilitySystem.DecoyState? decoy = _abilities.Decoy;
                if (decoy != null)
                {
                    if (!_decoy.gameObject.activeSelf) _decoy.gameObject.SetActive(true);
                    _decoy.position = decoy.Position;
                    _decoy.Rotate(0f, 4f * Mathf.Rad2Deg * Time.deltaTime, 0f, Space.World);
                }
                else if (_decoy.gameObject.activeSelf)
                {
                    _decoy.gameObject.SetActive(false);
                }
            }
        }
    }
}

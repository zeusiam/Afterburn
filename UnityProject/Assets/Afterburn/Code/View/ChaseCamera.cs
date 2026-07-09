using UnityEngine;

namespace Afterburn.View
{
    /// <summary>
    /// Chase camera (BUILD §7.9), reproducing the prototype exactly (PortSpec §2):
    /// position = target − forward·16 at world y 9, exponential lerp at rate 4/s;
    /// look-at = target + forward·10 at y 2, rotation snapped every frame (no smoothing);
    /// FOV 62, no boost kick, no shake. Framing values are View-layer presentation
    /// (retunable for landscape per BUILD §8) — game balance stays in GameTuning.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class ChaseCamera : MonoBehaviour
    {
        [SerializeField] private Transform? target;

        [Header("Prototype framing (PortSpec §2)")]
        [SerializeField] private float followBehind = 16f;
        [SerializeField] private float followHeight = 9f;
        [SerializeField] private float positionLerpRate = 4f;
        [SerializeField] private float lookAhead = 10f;
        [SerializeField] private float lookHeight = 2f;
        [SerializeField] private float fieldOfView = 62f;

        private Camera? _camera;

        public Transform? Target
        {
            get => target;
            set => target = value;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.fieldOfView = fieldOfView;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 fwd = target.forward;

            Vector3 want = target.position - fwd * followBehind;
            want.y = followHeight;
            float k = Mathf.Min(1f, Time.deltaTime * positionLerpRate);
            transform.position = Vector3.Lerp(transform.position, want, k);

            Vector3 look = target.position + fwd * lookAhead;
            look.y = lookHeight;
            transform.LookAt(look);
        }

        /// <summary>Teleport the camera straight to its rest pose (spawn / restart — avoids a long first lerp).</summary>
        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 fwd = target.forward;
            Vector3 want = target.position - fwd * followBehind;
            want.y = followHeight;
            transform.position = want;
            Vector3 look = target.position + fwd * lookAhead;
            look.y = lookHeight;
            transform.LookAt(look);
        }
    }
}

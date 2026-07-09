using UnityEngine;

namespace Afterburn.UI
{
    /// <summary>
    /// Insets a full-stretch <see cref="RectTransform"/> to the device safe area (notch,
    /// Dynamic Island, home indicator, rounded corners). Attach to the single root child of a
    /// Canvas; every screen's content parents under it so nothing renders beneath system UI
    /// (BUILD §8: Safe Area component on every screen root).
    ///
    /// Re-applies automatically whenever the reported safe area, resolution or orientation
    /// changes (rotation, iPad multitasking, simulator device swap). In the editor it also
    /// re-applies in edit mode so the layout previews correctly per device profile.
    ///
    /// Copied verbatim (namespace only) from the Veratus Games studio standard (Eclipse Run).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform? _rect;
        private Rect _lastSafeArea = new Rect(0f, 0f, 0f, 0f);
        private Vector2Int _lastScreen = Vector2Int.zero;
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void OnEnable()
        {
            // Force a recompute next tick (covers prefab instantiation and domain reloads).
            _lastScreen = Vector2Int.zero;
            Apply();
        }

        private void Update()
        {
            // Cheap guard — only recompute when something actually changed.
            if (Screen.safeArea != _lastSafeArea
                || Screen.width != _lastScreen.x
                || Screen.height != _lastScreen.y
                || Screen.orientation != _lastOrientation)
            {
                Apply();
            }
        }

        private void Apply()
        {
            if (_rect == null)
            {
                _rect = (RectTransform)transform;
            }

            int w = Screen.width;
            int h = Screen.height;
            if (w <= 0 || h <= 0)
            {
                return;
            }

            Rect safe = Screen.safeArea;
            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= w;
            anchorMin.y /= h;
            anchorMax.x /= w;
            anchorMax.y /= h;

            // Reject obviously invalid values that can appear on the very first frame.
            if (anchorMin.x >= 0f && anchorMin.y >= 0f
                && anchorMax.x <= 1.0001f && anchorMax.y <= 1.0001f
                && anchorMax.x > anchorMin.x && anchorMax.y > anchorMin.y)
            {
                _rect.anchorMin = anchorMin;
                _rect.anchorMax = anchorMax;
                _rect.offsetMin = Vector2.zero;
                _rect.offsetMax = Vector2.zero;
            }

            _lastSafeArea = safe;
            _lastScreen = new Vector2Int(w, h);
            _lastOrientation = Screen.orientation;
        }
    }
}

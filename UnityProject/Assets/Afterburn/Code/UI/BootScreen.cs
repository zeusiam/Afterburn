using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Afterburn.UI
{
    /// <summary>
    /// Boot screen (UIEnvSpec §4.1): Surface0 background, studio mark above the title, and a
    /// progress hairline (Stroke → Cyan) that fills over a fixed beat before auto-advancing to
    /// MainMenu. No interaction. Runtime-constructed; composes <see cref="UIFactory"/> only.
    /// </summary>
    public sealed class BootScreen : MonoBehaviour
    {
        /// <summary>Boot fill beat — spec-directed one-off, not a §4.6 motion token.</summary>
        private const float FillSeconds = 1.4f;
        private const string NextScene = "MainMenu";

        private Image? _fill;
        private float _elapsed;
        private bool _loaded;

        private void Awake()
        {
            RectTransform root = UIFactory.NewScreenCanvas("BootCanvas");
            UIFactory.Rect(root, "Background", AfterburnPalette.Surface0);

            RectTransform studio = UIFactory.Fixed(root, "StudioMark",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 150f), new Vector2(1200f, 60f));
            UIFactory.Label(studio, "Text", "VERATUS GAMES",
                AfterburnType.Label, AfterburnPalette.TextMid);

            RectTransform title = UIFactory.Fixed(root, "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(1600f, 130f));
            UIFactory.Label(title, "Text", "AFTERBURN",
                AfterburnType.Display, AfterburnPalette.TextHi,
                TextAlignmentOptions.Center, FontStyles.Bold);

            RectTransform hairline = UIFactory.Fixed(root, "ProgressHairline",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -140f), new Vector2(600f, 6f));
            (_, _fill) = UIFactory.Bar(hairline, "Bar",
                AfterburnPalette.Stroke, AfterburnPalette.Cyan);
            UIFactory.SetBarRatio(_fill, 0f);
        }

        private void Update()
        {
            if (_loaded || _fill == null)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(_elapsed / FillSeconds);
            UIFactory.SetBarRatio(_fill, ratio);

            if (ratio >= 1f)
            {
                _loaded = true;
                SceneManager.LoadScene(NextScene);
            }
        }
    }
}

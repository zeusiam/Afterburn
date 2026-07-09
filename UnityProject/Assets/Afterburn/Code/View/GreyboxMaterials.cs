using UnityEngine;

namespace Afterburn.View
{
    /// <summary>
    /// URP material factory for the greybox pass (Art/Greybox rule: primitives + placeholder
    /// materials only until the loop is re-proven at the U2 gate). Colors/roughness values come
    /// from the prototype scene dressing (PortSpec §10).
    ///
    /// ⚠ DEVICE-BUILD NOTE: Shader.Find only works when the shader ships in the build; runtime-only
    /// references get stripped from device builds. Fine for the U1/U2 in-editor gates — before any
    /// device build (U7), greybox materials must become serialized .mat assets (U2's prefab pass
    /// does this for Ship/Projectile anyway) or the shaders added to Always Included Shaders.
    /// </summary>
    internal static class GreyboxMaterials
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int CullId = Shader.PropertyToID("_Cull");

        /// <summary>URP Lit standard surface. roughness/metalness use the prototype's convention.</summary>
        public static Material Lit(Color baseColor, float roughness, float metalness,
            Color? emission = null, float emissionIntensity = 1f, bool doubleSided = false)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = $"Greybox_Lit_{ColorUtility.ToHtmlStringRGB(baseColor)}",
            };
            mat.SetColor(BaseColorId, baseColor);
            mat.SetFloat(SmoothnessId, 1f - roughness);
            mat.SetFloat(MetallicId, metalness);
            if (emission.HasValue)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor(EmissionColorId, emission.Value * emissionIntensity);
            }
            if (doubleSided)
            {
                mat.SetFloat(CullId, (float)UnityEngine.Rendering.CullMode.Off);
            }
            return mat;
        }

        /// <summary>
        /// Transparent URP Lit — the ghost-hologram treatment (UIEnvSpec §3.1: ghosts read as
        /// intangible, ~0.55 alpha in greybox; the player is always the most solid object on track).
        /// </summary>
        public static Material LitTransparent(Color baseColor, float alpha, float roughness, float metalness,
            Color? emission = null, float emissionIntensity = 1f)
        {
            Material mat = Lit(new Color(baseColor.r, baseColor.g, baseColor.b, alpha),
                roughness, metalness, emission, emissionIntensity);
            mat.SetFloat(Shader.PropertyToID("_Surface"), 1f);   // URP: Transparent surface
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat(Shader.PropertyToID("_SrcBlend"), (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat(Shader.PropertyToID("_DstBlend"), (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat(Shader.PropertyToID("_ZWrite"), 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return mat;
        }

        /// <summary>URP Unlit flat color (thruster glow, bullets, grid lines).</summary>
        public static Material Unlit(Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"))
            {
                name = $"Greybox_Unlit_{ColorUtility.ToHtmlStringRGB(color)}",
            };
            mat.SetColor(BaseColorId, color);
            return mat;
        }

        /// <summary>Parse "#RRGGBB" — greybox colors are specified as hex in the PortSpec.</summary>
        public static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
        }
    }
}

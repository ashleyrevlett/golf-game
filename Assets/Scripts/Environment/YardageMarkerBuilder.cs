using UnityEngine;
using TMPro;
using GolfGame.Core;

namespace GolfGame.Environment
{
    /// <summary>
    /// Creates TMP 3D text yardage markers and translucent yard lines
    /// at 25-yard intervals along the fairway.
    /// </summary>
    public static class YardageMarkerBuilder
    {
        private const float MarkerY = 1.2f;
        private const float LineY = 0.03f;
        private const float LineHeight = 0.05f;
        private const int YardageInterval = 25;
        private const float FontSize = 24f;

        private static readonly Color MarkerColor = new Color(1f, 0.757f, 0.027f); // #FFC107
        private static readonly Color LineColor = new Color(1f, 0.843f, 0f, 0.18f); // translucent gold

        /// <summary>
        /// Build yardage markers and lines as children of parent.
        /// </summary>
        /// <param name="parent">Parent transform (course root).</param>
        /// <param name="fairwayWidth">Width of the fairway in units.</param>
        /// <param name="courseLength">Total course length in units.</param>
        public static void Build(Transform parent, float fairwayWidth, float courseLength)
        {
            Material lineMaterial = CreateLineMaterial();

            for (int yards = YardageInterval; yards <= courseLength; yards += YardageInterval)
            {
                float z = yards;

                // TMP 3D text marker
                CreateTextMarker(parent, yards, fairwayWidth, z);

                // Yard line across fairway
                CreateYardLine(parent, fairwayWidth, z, lineMaterial);
            }
        }

        private static void CreateTextMarker(Transform parent, int yards, float fairwayWidth, float z)
        {
            var markerObj = new GameObject($"YardageMarker_{yards}");
            markerObj.transform.SetParent(parent);
            markerObj.transform.localPosition = new Vector3(-fairwayWidth / 2f - 1.5f, MarkerY, z);
            // Face positive X (perpendicular to fairway, readable from tee)
            markerObj.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var tmp = markerObj.AddComponent<TextMeshPro>();
            tmp.text = yards.ToString();
            tmp.fontSize = FontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = MarkerColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            // TMP glow effect
            tmp.fontMaterial.EnableKeyword("GLOW_ON");
            tmp.fontMaterial.SetColor(ShaderUtilities.ID_GlowColor, MarkerColor);
            tmp.fontMaterial.SetFloat(ShaderUtilities.ID_GlowPower, 0.3f);

            markerObj.isStatic = true;
        }

        private static void CreateYardLine(Transform parent, float fairwayWidth, float z, Material material)
        {
            var lineObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lineObj.name = $"YardLine_{(int)z}";
            lineObj.transform.SetParent(parent);
            lineObj.transform.localPosition = new Vector3(0f, LineY, z);
            lineObj.transform.localScale = new Vector3(fairwayWidth, LineHeight, LineHeight);
            lineObj.isStatic = true;

            var renderer = lineObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            // Remove collider — yard lines are visual only
            var coll = lineObj.GetComponent<Collider>();
            if (coll != null)
            {
                Object.Destroy(coll);
            }
        }

        private static Material CreateLineMaterial()
        {
            var shader = ShaderUtils.FindWithFallback("Universal Render Pipeline/Unlit", "Unlit/Color");

            var mat = new Material(shader);
            mat.color = LineColor;

            // Enable transparency
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f);   // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;

            return mat;
        }
    }
}

using System.Collections;
using UnityEngine;
using TMPro;

namespace GolfGame.Environment
{
    /// <summary>
    /// World-space floating text showing distance after each shot.
    /// Floats upward and fades out over 2 seconds, then self-destructs.
    /// </summary>
    public class ShotPopup : MonoBehaviour
    {
        private const float Duration = 2f;
        private const float FloatDistance = 1f;
        private const float WorldFontSize = 6f;

        private Camera targetCamera;
        private TextMeshPro tmp;

        /// <summary>
        /// Factory method: creates a popup at the given position showing the distance.
        /// </summary>
        public static ShotPopup Create(Vector3 position, float distance, Camera cam)
        {
            var obj = new GameObject("ShotPopup");
            obj.transform.position = position;

            var popup = obj.AddComponent<ShotPopup>();
            popup.targetCamera = cam;
            popup.Initialize(distance);

            return popup;
        }

        private void Initialize(float distance)
        {
            tmp = gameObject.AddComponent<TextMeshPro>();
            tmp.text = $"{distance:F1} yds";
            tmp.fontSize = WorldFontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;

            // Drop shadow via TMP underlay
            tmp.fontMaterial.EnableKeyword("UNDERLAY_ON");
            tmp.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, Color.black);
            tmp.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.5f);
            tmp.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.5f);
            tmp.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.3f);

            StartCoroutine(FloatAndFade());
        }

        private void LateUpdate()
        {
            // Billboard: always face camera
            if (targetCamera != null)
            {
                transform.forward = targetCamera.transform.forward;
            }
        }

        private IEnumerator FloatAndFade()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + Vector3.up * FloatDistance;
            Color startColor = tmp.color;

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / Duration;

                transform.position = Vector3.Lerp(startPos, endPos, t);

                Color c = startColor;
                c.a = 1f - t;
                tmp.color = c;

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

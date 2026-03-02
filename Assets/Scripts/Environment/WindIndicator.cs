using UnityEngine;
using GolfGame.Golf;

namespace GolfGame.Environment
{
    /// <summary>
    /// Displays wind direction and speed as a 3D arrow near the tee.
    /// Subscribes to WindSystem.OnWindChanged.
    /// </summary>
    public class WindIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WindSystem windSystem;

        [Header("Settings")]
        [SerializeField] private float baseLength = 1f;
        [SerializeField] private float speedScale = 0.5f;
        [SerializeField] private float minScale = 0.2f;

        private GameObject arrowBody;
        private GameObject arrowHead;
        private Material arrowMaterial;

        private void Start()
        {
            CreateArrow();

            if (windSystem != null)
            {
                windSystem.OnWindChanged += HandleWindChanged;
                HandleWindChanged(windSystem.CurrentWind);
            }
        }

        private void OnDestroy()
        {
            if (windSystem != null)
            {
                windSystem.OnWindChanged -= HandleWindChanged;
            }

            if (arrowMaterial != null)
            {
                Destroy(arrowMaterial);
            }
        }

        private void CreateArrow()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            arrowMaterial = new Material(shader);
            arrowMaterial.color = new Color(1f, 1f, 1f, 0.7f);

            // Arrow body (elongated cube)
            arrowBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrowBody.name = "WindArrowBody";
            arrowBody.transform.SetParent(transform);
            arrowBody.transform.localPosition = new Vector3(0f, 0f, baseLength / 2f);
            arrowBody.transform.localScale = new Vector3(0.15f, 0.15f, baseLength);

            var bodyRenderer = arrowBody.GetComponent<Renderer>();
            if (bodyRenderer != null) bodyRenderer.sharedMaterial = arrowMaterial;

            var bodyColl = arrowBody.GetComponent<Collider>();
            if (bodyColl != null) Destroy(bodyColl);

            // Arrow head (scaled cube as triangle approximation)
            arrowHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrowHead.name = "WindArrowHead";
            arrowHead.transform.SetParent(transform);
            arrowHead.transform.localPosition = new Vector3(0f, 0f, baseLength);
            arrowHead.transform.localScale = new Vector3(0.4f, 0.15f, 0.3f);

            var headRenderer = arrowHead.GetComponent<Renderer>();
            if (headRenderer != null) headRenderer.sharedMaterial = arrowMaterial;

            var headColl = arrowHead.GetComponent<Collider>();
            if (headColl != null) Destroy(headColl);
        }

        private void HandleWindChanged(Vector3 wind)
        {
            if (wind.sqrMagnitude < 0.001f)
            {
                if (arrowBody != null) arrowBody.SetActive(false);
                if (arrowHead != null) arrowHead.SetActive(false);
                return;
            }

            if (arrowBody != null) arrowBody.SetActive(true);
            if (arrowHead != null) arrowHead.SetActive(true);

            // Rotate to face wind direction
            float angle = Mathf.Atan2(wind.x, wind.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Scale by wind speed
            float scale = Mathf.Max(minScale, wind.magnitude * speedScale);
            float bodyLength = baseLength * scale;

            if (arrowBody != null)
            {
                arrowBody.transform.localPosition = new Vector3(0f, 0f, bodyLength / 2f);
                arrowBody.transform.localScale = new Vector3(0.15f, 0.15f, bodyLength);
            }

            if (arrowHead != null)
            {
                arrowHead.transform.localPosition = new Vector3(0f, 0f, bodyLength);
            }
        }
    }
}

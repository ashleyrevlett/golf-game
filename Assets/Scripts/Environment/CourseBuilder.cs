using UnityEngine;

namespace GolfGame.Environment
{
    /// <summary>
    /// Creates course geometry from primitives at runtime.
    /// All objects use shared materials for minimal draw calls.
    /// Call StaticBatchingUtility.Combine() after creation for batching.
    /// </summary>
    public class CourseBuilder : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private CourseConfig config;

        [Header("Output")]
        [SerializeField] private PinController pinController;

        private Material matTee;
        private Material matFairway;
        private Material matRough;
        private Material matGreen;
        private Material matPin;
        private Material matFlag;
        private Material matOBMarker;
        private Material matGround;

        private void Awake()
        {
            CreateMaterials();
            BuildCourse();
        }

        private void CreateMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            matTee = CreateMaterial(shader, new Color(0.298f, 0.686f, 0.314f));      // #4CAF50
            matFairway = CreateMaterial(shader, new Color(0.400f, 0.733f, 0.416f));   // #66BB6A
            matRough = CreateMaterial(shader, new Color(0.180f, 0.490f, 0.196f));     // #2E7D32
            matGreen = CreateMaterial(shader, new Color(0.506f, 0.780f, 0.518f));     // #81C784
            matPin = CreateMaterial(shader, Color.white);
            matFlag = CreateMaterial(shader, new Color(0.957f, 0.263f, 0.212f));      // #F44336
            matOBMarker = CreateMaterial(shader, Color.white);
            matGround = CreateMaterial(shader, new Color(0.553f, 0.431f, 0.388f));    // #8D6E63
        }

        private static Material CreateMaterial(Shader shader, Color color)
        {
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        private void BuildCourse()
        {
            float length = config != null ? config.CourseLength : 114f;
            float fwWidth = config != null ? config.FairwayWidth : 18f;
            float rWidth = config != null ? config.RoughWidth : 9f;
            float gRadius = config != null ? config.GreenRadius : 14f;
            float teeSize = config != null ? config.TeeBoxSize : 4f;
            float pinH = config != null ? config.PinHeight : 2.5f;
            float pinD = config != null ? config.PinDiameter : 0.05f;
            float flagW = config != null ? config.FlagWidth : 0.3f;
            float flagH = config != null ? config.FlagHeight : 0.2f;
            float obBeyond = config != null ? config.ObDistanceBeyondRough : 5f;
            float obHeight = config != null ? config.ObMarkerHeight : 1.5f;
            float obSpacing = config != null ? config.ObMarkerSpacing : 15f;

            var root = new GameObject("Course");
            root.transform.SetParent(transform);

            // Ground plane (large, beneath everything)
            CreatePlane("Ground", root.transform, Vector3.down * 0.05f,
                new Vector3(length * 2f, 1f, (fwWidth + rWidth * 2 + obBeyond * 2) * 2f), matGround);

            // Tee box
            CreatePlane("TeeBox", root.transform, Vector3.zero,
                new Vector3(teeSize, 0.1f, teeSize), matTee);

            // Fairway
            float fwCenterZ = length / 2f;
            CreatePlane("Fairway", root.transform, new Vector3(0f, 0.01f, fwCenterZ),
                new Vector3(fwWidth, 0.1f, length), matFairway);

            // Rough left
            float roughOffsetX = (fwWidth / 2f) + (rWidth / 2f);
            CreatePlane("Rough_L", root.transform,
                new Vector3(-roughOffsetX, 0.01f, fwCenterZ),
                new Vector3(rWidth, 0.1f, length), matRough);

            // Rough right
            CreatePlane("Rough_R", root.transform,
                new Vector3(roughOffsetX, 0.01f, fwCenterZ),
                new Vector3(rWidth, 0.1f, length), matRough);

            // Green
            CreatePlane("Green", root.transform,
                new Vector3(0f, 0.02f, length),
                new Vector3(gRadius * 2f, 0.1f, gRadius * 2f), matGreen);

            // Pin
            var pinObj = CreatePin(root.transform, new Vector3(0f, 0f, length),
                pinH, pinD, flagW, flagH);

            // Assign pin controller
            if (pinController == null)
            {
                pinController = pinObj.AddComponent<PinController>();
            }
            else
            {
                pinController.transform.position = new Vector3(0f, 0f, length);
            }

            // OB markers along both sides
            float obX = (fwWidth / 2f) + rWidth + obBeyond;
            for (float z = 0f; z <= length + gRadius; z += obSpacing)
            {
                CreateOBMarker(root.transform, new Vector3(-obX, 0f, z), obHeight);
                CreateOBMarker(root.transform, new Vector3(obX, 0f, z), obHeight);
            }

            // Static batching
            StaticBatchingUtility.Combine(root);
        }

        private GameObject CreatePlane(string name, Transform parent, Vector3 position,
            Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.isStatic = true;

            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return obj;
        }

        private GameObject CreatePin(Transform parent, Vector3 basePosition,
            float height, float diameter, float flagWidth, float flagHeight)
        {
            var pinRoot = new GameObject("Pin");
            pinRoot.transform.SetParent(parent);
            pinRoot.transform.localPosition = basePosition;

            // Pole
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "PinPole";
            pole.transform.SetParent(pinRoot.transform);
            pole.transform.localPosition = new Vector3(0f, height / 2f, 0f);
            pole.transform.localScale = new Vector3(diameter, height / 2f, diameter);
            pole.isStatic = true;
            var poleRenderer = pole.GetComponent<Renderer>();
            if (poleRenderer != null) poleRenderer.sharedMaterial = matPin;

            // Flag
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "PinFlag";
            flag.transform.SetParent(pinRoot.transform);
            flag.transform.localPosition = new Vector3(flagWidth / 2f, height - flagHeight / 2f, 0f);
            flag.transform.localScale = new Vector3(flagWidth, flagHeight, 0.02f);
            flag.isStatic = true;
            var flagRenderer = flag.GetComponent<Renderer>();
            if (flagRenderer != null) flagRenderer.sharedMaterial = matFlag;

            // Remove colliders from pin visuals (pin shouldn't block ball)
            var poleColl = pole.GetComponent<Collider>();
            if (poleColl != null) Destroy(poleColl);
            var flagColl = flag.GetComponent<Collider>();
            if (flagColl != null) Destroy(flagColl);

            return pinRoot;
        }

        private void CreateOBMarker(Transform parent, Vector3 position, float height)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "OBMarker";
            marker.transform.SetParent(parent);
            marker.transform.localPosition = new Vector3(position.x, height / 2f, position.z);
            marker.transform.localScale = new Vector3(0.1f, height / 2f, 0.1f);
            marker.isStatic = true;

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = matOBMarker;

            // Remove collider — OB markers are visual only
            var coll = marker.GetComponent<Collider>();
            if (coll != null) Destroy(coll);
        }

        private void OnDestroy()
        {
            // Clean up runtime materials
            if (matTee != null) Destroy(matTee);
            if (matFairway != null) Destroy(matFairway);
            if (matRough != null) Destroy(matRough);
            if (matGreen != null) Destroy(matGreen);
            if (matPin != null) Destroy(matPin);
            if (matFlag != null) Destroy(matFlag);
            if (matOBMarker != null) Destroy(matOBMarker);
            if (matGround != null) Destroy(matGround);
        }
    }
}

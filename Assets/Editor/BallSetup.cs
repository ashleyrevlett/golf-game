using UnityEditor;
using UnityEngine;

namespace GolfGame.Editor
{
    public static class BallSetup
    {
        [MenuItem("GolfGame/Setup Ball Mesh")]
        public static void SetupBallMesh()
        {
            var ball = GameObject.Find("BallController");
            if (ball == null) { Debug.LogError("BallController GO not found"); return; }

            // Mesh
            var mf = ball.GetComponent<MeshFilter>();
            if (mf == null) mf = ball.AddComponent<MeshFilter>();
            var mr = ball.GetComponent<MeshRenderer>();
            if (mr == null) mr = ball.AddComponent<MeshRenderer>();

            var tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mf.sharedMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            mr.sharedMaterial = tempSphere.GetComponent<MeshRenderer>().sharedMaterial;
            Object.DestroyImmediate(tempSphere);

            ball.transform.localScale = Vector3.one * 0.04f;

            // Physics
            var rb = ball.GetComponent<Rigidbody>();
            if (rb == null) rb = ball.AddComponent<Rigidbody>();

            var col = ball.GetComponent<SphereCollider>();
            if (col == null) col = ball.AddComponent<SphereCollider>();
            col.radius = 0.5f;

            EditorUtility.SetDirty(ball);
            Debug.Log("[BallSetup] Done");
        }
    }
}

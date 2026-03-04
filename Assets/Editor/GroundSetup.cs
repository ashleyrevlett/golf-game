using UnityEditor;
using UnityEngine;

namespace GolfGame.Editor
{
    public static class GroundSetup
    {
        [MenuItem("GolfGame/Setup Ground Friction")]
        public static void SetupGroundFriction()
        {
            // Create physics material
            var mat = new PhysicsMaterial("CourseSurface");
            mat.dynamicFriction = 0.6f;
            mat.staticFriction = 0.7f;
            mat.bounciness = 0.1f;
            mat.frictionCombine = PhysicsMaterialCombine.Multiply;
            mat.bounceCombine = PhysicsMaterialCombine.Minimum;

            AssetDatabase.CreateAsset(mat, "Assets/Config/CourseSurface.asset");
            AssetDatabase.SaveAssets();

            // Apply to Ground collider
            var ground = GameObject.Find("Ground");
            if (ground == null) { Debug.LogError("Ground GO not found"); return; }

            var col = ground.GetComponent<Collider>();
            if (col == null) { Debug.LogError("No collider on Ground"); return; }

            col.material = mat;
            EditorUtility.SetDirty(ground);

            // Also add angular drag to ball rigidbody so it stops spinning
            var ball = GameObject.Find("BallController");
            if (ball != null)
            {
                var rb = ball.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearDamping = 0.3f;
                    rb.angularDamping = 2f;
                    EditorUtility.SetDirty(ball);
                }
            }

            Debug.Log("[GroundSetup] CourseSurface physics material applied, ball damping set");
        }
    }
}

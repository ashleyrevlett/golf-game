using UnityEngine;
using GolfGame.Core;

namespace GolfGame.Golf
{
    /// <summary>
    /// Debug script for testing shots via keyboard.
    /// Keys 1-4 fire preset shots, R resets the ball to tee.
    /// Only active in the Editor.
    /// </summary>
    public class ShotTester : MonoBehaviour
    {
#if UNITY_EDITOR
        private BallController ballController;
        private GameManager gameManager;

        private void Start()
        {
            ballController = FindFirstObjectByType<BallController>();
            gameManager = FindFirstObjectByType<GameManager>();
        }

        private void Update()
        {
            if (ballController == null || gameManager == null) return;

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                FireShot(0.5f, 0f); // Medium straight
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                FireShot(1.0f, 0f); // Full power straight
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                FireShot(0.7f, -10f); // Medium left
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                FireShot(0.7f, 10f); // Medium right
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                ballController.ResetToTee();
            }
        }

        private void FireShot(float power, float aimAngle)
        {
            if (gameManager.CurrentShotState != ShotState.Ready) return;

            var shot = new ShotParameters
            {
                PowerNormalized = power,
                AimAngleDegrees = aimAngle
            };

            gameManager.LaunchShot();
            ballController.Launch(shot);

            Debug.Log($"[ShotTester] Fired: power={power:F1}, aim={aimAngle:F0}");
        }
#endif
    }
}

using NUnit.Framework;
using UnityEngine;
using GolfGame.Golf;

namespace GolfGame.Tests.EditMode
{
    public class BallPhysicsConfigTests
    {
        [Test]
        public void CreateInstance_HasReasonableDefaults()
        {
            var config = ScriptableObject.CreateInstance<BallPhysicsConfig>();

            Assert.Greater(config.DefaultLoftAngle, 0f, "Loft angle must be positive");
            Assert.Greater(config.MaxPowerMph, 0f, "Max power must be positive");
            Assert.Greater(config.MphToForceMultiplier, 0f, "Force multiplier must be positive");
            Assert.Greater(config.BallMass, 0f, "Ball mass must be positive");
            Assert.Greater(config.BallRadius, 0f, "Ball radius must be positive");

            Object.DestroyImmediate(config);
        }

        [Test]
        public void CreateInstance_WindSpeedRange_IsValid()
        {
            var config = ScriptableObject.CreateInstance<BallPhysicsConfig>();

            Assert.GreaterOrEqual(config.WindMaxSpeed, config.WindMinSpeed,
                "Wind max speed must be >= min speed");
            Assert.Greater(config.WindSensitivity, 0f, "Wind sensitivity must be positive");

            Object.DestroyImmediate(config);
        }

        [Test]
        public void ProjectAsset_ExistsInResources()
        {
            var config = Resources.Load<BallPhysicsConfig>("BallPhysicsConfig");
            Assert.IsNotNull(config,
                "BallPhysicsConfig asset must exist at Resources/BallPhysicsConfig");
        }
    }
}

using NUnit.Framework;
using GolfGame.Environment;

namespace GolfGame.Tests.EditMode
{
    public class ShotResultTests
    {
        [Test]
        public void DefaultValues_AreZero()
        {
            var result = new ShotResult();
            Assert.AreEqual(0, result.ShotNumber);
            Assert.AreEqual(0f, result.DistanceToPin);
            Assert.AreEqual(0f, result.CarryDistance);
            Assert.AreEqual(0f, result.TotalDistance);
            Assert.AreEqual(0f, result.LateralDeviation);
            Assert.AreEqual(0f, result.BallSpeed);
        }

        [Test]
        public void FieldAssignment_RoundTrips()
        {
            var result = new ShotResult
            {
                ShotNumber = 3,
                DistanceToPin = 5.2f,
                CarryDistance = 100.5f,
                TotalDistance = 110.3f,
                LateralDeviation = -2.1f,
                BallSpeed = 45.0f
            };

            Assert.AreEqual(3, result.ShotNumber);
            Assert.AreEqual(5.2f, result.DistanceToPin, 0.01f);
            Assert.AreEqual(100.5f, result.CarryDistance, 0.01f);
            Assert.AreEqual(110.3f, result.TotalDistance, 0.01f);
            Assert.AreEqual(-2.1f, result.LateralDeviation, 0.01f);
            Assert.AreEqual(45.0f, result.BallSpeed, 0.01f);
        }
    }
}

using NUnit.Framework;
using GolfGame.Golf;

namespace GolfGame.Tests.EditMode
{
    public class ShotParametersTests
    {
        [Test]
        public void PowerMph_FullPower_ReturnsMaxMph()
        {
            var shot = new ShotParameters { PowerNormalized = 1f };
            Assert.AreEqual(150f, shot.PowerMph(150f), 0.01f);
        }

        [Test]
        public void PowerMph_HalfPower_ReturnsHalfMaxMph()
        {
            var shot = new ShotParameters { PowerNormalized = 0.5f };
            Assert.AreEqual(75f, shot.PowerMph(150f), 0.01f);
        }

        [Test]
        public void PowerMph_ZeroPower_ReturnsZero()
        {
            var shot = new ShotParameters { PowerNormalized = 0f };
            Assert.AreEqual(0f, shot.PowerMph(150f), 0.01f);
        }

        [Test]
        public void DefaultProperties_AreZero()
        {
            var shot = new ShotParameters();
            Assert.AreEqual(0f, shot.PowerNormalized);
            Assert.AreEqual(0f, shot.AimAngleDegrees);
            Assert.AreEqual(0f, shot.BackspinRpm);
            Assert.AreEqual(0f, shot.SidespinRpm);
        }
    }
}

using NUnit.Framework;
using GolfGame.UI;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for LeaderboardController formatting methods.
    /// </summary>
    public class LeaderboardControllerTests
    {
        [Test]
        public void FormatRank_1_Returns_1st()
        {
            Assert.AreEqual("1st", LeaderboardController.FormatRank(1));
        }

        [Test]
        public void FormatRank_2_Returns_2nd()
        {
            Assert.AreEqual("2nd", LeaderboardController.FormatRank(2));
        }

        [Test]
        public void FormatRank_3_Returns_3rd()
        {
            Assert.AreEqual("3rd", LeaderboardController.FormatRank(3));
        }

        [Test]
        public void FormatRank_4_Returns_PlainNumber()
        {
            Assert.AreEqual("4", LeaderboardController.FormatRank(4));
        }

        [Test]
        public void FormatRank_10_Returns_PlainNumber()
        {
            Assert.AreEqual("10", LeaderboardController.FormatRank(10));
        }

        [Test]
        public void FormatRank_11_Returns_PlainNumber()
        {
            Assert.AreEqual("11", LeaderboardController.FormatRank(11));
        }

        [Test]
        public void FormatRank_21_Returns_PlainNumber()
        {
            Assert.AreEqual("21", LeaderboardController.FormatRank(21));
        }

        [Test]
        public void FormatDistance_RoundsToOneDecimalWithYdsSuffix()
        {
            Assert.AreEqual("38.5 yds", LeaderboardController.FormatDistance(38.456f));
        }

        [Test]
        public void FormatDistance_Zero_Returns_0Point0Yds()
        {
            Assert.AreEqual("0.0 yds", LeaderboardController.FormatDistance(0f));
        }

        [Test]
        public void FormatDistance_LargeValue_FormatsCorrectly()
        {
            Assert.AreEqual("999.9 yds", LeaderboardController.FormatDistance(999.9f));
        }
    }
}

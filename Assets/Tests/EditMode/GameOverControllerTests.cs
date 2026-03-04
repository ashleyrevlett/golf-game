using NUnit.Framework;
using GolfGame.UI;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for GameOverController score display and best-score comparison logic.
    /// </summary>
    public class GameOverControllerTests
    {
        [Test]
        public void FormatFinalScore_OneDecimalWithYardsSuffix()
        {
            string result = GameOverController.FormatFinalScore(38.4f);
            Assert.AreEqual("38.4 yds", result);
        }

        [Test]
        public void IsNewBest_WhenCurrentLessThanPrevious()
        {
            bool result = GameOverController.IsNewBestScore(30.0f, 50.0f);
            Assert.IsTrue(result);
        }

        [Test]
        public void IsNotNewBest_WhenCurrentGreaterOrEqual()
        {
            bool result = GameOverController.IsNewBestScore(60.0f, 50.0f);
            Assert.IsFalse(result);

            bool resultEqual = GameOverController.IsNewBestScore(50.0f, 50.0f);
            Assert.IsFalse(resultEqual);
        }

        [Test]
        public void NoBestExists_WhenBestIsMaxValue()
        {
            bool hasBest = GameOverController.HasBestScore(float.MaxValue);
            Assert.IsFalse(hasBest);
        }

        [Test]
        public void BestLabel_ShowsNewPrefix_WhenNewBest()
        {
            string label = GameOverController.FormatBestScoreLabel(30.0f, true);
            Assert.IsTrue(label.StartsWith("NEW! BEST:"));
            Assert.IsTrue(label.Contains("30.0 yds"));
        }

        [Test]
        public void BestLabel_NoNewPrefix_WhenNotNewBest()
        {
            string label = GameOverController.FormatBestScoreLabel(50.0f, false);
            Assert.IsTrue(label.StartsWith("BEST:"));
            Assert.IsFalse(label.Contains("NEW!"));
        }
    }
}

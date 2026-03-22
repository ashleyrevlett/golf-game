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

        // --- Shot grade tests ---

        [Test]
        public void GetShotGrade_ReturnsA_WhenDistanceUnder5()
        {
            Assert.AreEqual("A", GameOverController.GetShotGrade(2.5f));
        }

        [Test]
        public void GetShotGrade_ReturnsB_WhenDistanceBetween5And15()
        {
            Assert.AreEqual("B", GameOverController.GetShotGrade(10.0f));
        }

        [Test]
        public void GetShotGrade_ReturnsC_WhenDistanceOver15()
        {
            Assert.AreEqual("C", GameOverController.GetShotGrade(25.0f));
        }

        [Test]
        public void GetShotGrade_ReturnsB_WhenDistanceExactly5()
        {
            Assert.AreEqual("B", GameOverController.GetShotGrade(5.0f));
        }

        [Test]
        public void GetShotGrade_ReturnsC_WhenDistanceExactly15()
        {
            Assert.AreEqual("C", GameOverController.GetShotGrade(15.0f));
        }

        [Test]
        public void GetShotGrade_ReturnsA_WhenDistanceIsZero()
        {
            Assert.AreEqual("A", GameOverController.GetShotGrade(0f));
        }

        [Test]
        public void GetGradeClass_ReturnsShotGradeA_ForGradeA()
        {
            Assert.AreEqual("shot-grade-a", GameOverController.GetGradeClass("A"));
        }

        [Test]
        public void GetGradeClass_ReturnsShotGradeB_ForGradeB()
        {
            Assert.AreEqual("shot-grade-b", GameOverController.GetGradeClass("B"));
        }

        [Test]
        public void GetGradeClass_ReturnsShotGradeC_ForGradeC()
        {
            Assert.AreEqual("shot-grade-c", GameOverController.GetGradeClass("C"));
        }

        // --- Share text format tests ---

        [Test]
        public void FormatShareText_ContainsScoreWithOneDecimal()
        {
            string result = GameOverController.FormatShareText(45.3f);
            Assert.IsTrue(result.Contains("45.3 yds"));
        }

        [Test]
        public void FormatShareText_ContainsBeatMePhrase()
        {
            string result = GameOverController.FormatShareText(10.0f);
            Assert.IsTrue(result.Contains("beat me!"));
        }

        [Test]
        public void FormatShareText_FormatsZeroScore()
        {
            string result = GameOverController.FormatShareText(0f);
            Assert.AreEqual("I scored 0.0 yds in Golf Game \u2014 beat me!", result);
        }
    }
}

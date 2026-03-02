using System;
using NUnit.Framework;
using GolfGame.Core;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// Tests for GameState enums.
    /// </summary>
    public class GameStateTests
    {
        [Test]
        public void AppState_HasAllExpectedValues()
        {
            var values = Enum.GetValues(typeof(AppState));
            Assert.AreEqual(6, values.Length);
            Assert.IsTrue(Enum.IsDefined(typeof(AppState), AppState.Title));
            Assert.IsTrue(Enum.IsDefined(typeof(AppState), AppState.Instructions));
            Assert.IsTrue(Enum.IsDefined(typeof(AppState), AppState.TransitionToGame));
            Assert.IsTrue(Enum.IsDefined(typeof(AppState), AppState.Playing));
            Assert.IsTrue(Enum.IsDefined(typeof(AppState), AppState.GameOver));
            Assert.IsTrue(Enum.IsDefined(typeof(AppState), AppState.Leaderboard));
        }

        [Test]
        public void ShotState_HasAllExpectedValues()
        {
            var values = Enum.GetValues(typeof(ShotState));
            Assert.AreEqual(3, values.Length);
            Assert.IsTrue(Enum.IsDefined(typeof(ShotState), ShotState.Ready));
            Assert.IsTrue(Enum.IsDefined(typeof(ShotState), ShotState.Flying));
            Assert.IsTrue(Enum.IsDefined(typeof(ShotState), ShotState.Landed));
        }
    }
}

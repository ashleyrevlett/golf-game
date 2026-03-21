using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Multiplayer;

namespace GolfGame.Tests.EditMode
{
    internal class StubCloudCodeProvider : IUgsCloudCodeProvider
    {
        public object Result { get; set; }
        public Exception ExceptionToThrow { get; set; }
        public string LastFunction { get; private set; }
        public Dictionary<string, object> LastArgs { get; private set; }

        public Task<T> CallEndpointAsync<T>(string function, Dictionary<string, object> args)
        {
            LastFunction = function;
            LastArgs = args;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult((T)Result);
        }
    }

    internal class StubLeaderboardsProvider : IUgsLeaderboardsProvider
    {
        public List<RawLeaderboardScore> Scores { get; set; } = new List<RawLeaderboardScore>();
        public RawLeaderboardScore PlayerScore { get; set; }
        public Exception ExceptionToThrow { get; set; }

        public Task<List<RawLeaderboardScore>> GetScoresAsync(string leaderboardId, int limit)
        {
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(Scores);
        }

        public Task<RawLeaderboardScore> GetPlayerScoreAsync(string leaderboardId)
        {
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(PlayerScore);
        }
    }

    public class UgsLeaderboardServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void PostScoreAsync_Success_NoThrow()
        {
            var cloudCode = new StubCloudCodeProvider
            {
                Result = new ScorePostResult { success = true }
            };
            var leaderboards = new StubLeaderboardsProvider();
            var service = new UgsLeaderboardService(cloudCode, leaderboards);

            Assert.DoesNotThrow(() =>
                service.PostScoreAsync("p", 5.0f).GetAwaiter().GetResult());

            Assert.AreEqual(5.0f, cloudCode.LastArgs["distance"]);
        }

        [Test]
        public void PostScoreAsync_Rejected_ThrowsWithReason()
        {
            var cloudCode = new StubCloudCodeProvider
            {
                Result = new ScorePostResult { success = false, reason = "too far" }
            };
            var leaderboards = new StubLeaderboardsProvider();
            var service = new UgsLeaderboardService(cloudCode, leaderboards);

            var ex = Assert.Throws<InvalidOperationException>(() =>
                service.PostScoreAsync("p", 5.0f).GetAwaiter().GetResult());
            Assert.That(ex.Message, Does.Contain("too far"));
        }

        [Test]
        public void GetLeaderboardAsync_MapsRankToOneBased()
        {
            var leaderboards = new StubLeaderboardsProvider
            {
                Scores = new List<RawLeaderboardScore>
                {
                    new RawLeaderboardScore { Rank = 0, PlayerId = "aaaaaa000", PlayerName = "A", Score = 1.0 },
                    new RawLeaderboardScore { Rank = 1, PlayerId = "bbbbbb111", PlayerName = "B", Score = 2.0 },
                    new RawLeaderboardScore { Rank = 2, PlayerId = "cccccc222", PlayerName = "C", Score = 3.0 }
                }
            };
            var service = new UgsLeaderboardService(new StubCloudCodeProvider(), leaderboards);

            var entries = service.GetLeaderboardAsync(3).GetAwaiter().GetResult();

            Assert.AreEqual(1, entries[0].Rank);
            Assert.AreEqual(2, entries[1].Rank);
            Assert.AreEqual(3, entries[2].Rank);
        }

        [Test]
        public void GetLeaderboardAsync_NullPlayerName_UsesFallback()
        {
            var leaderboards = new StubLeaderboardsProvider
            {
                Scores = new List<RawLeaderboardScore>
                {
                    new RawLeaderboardScore { Rank = 0, PlayerId = "abcdef789", PlayerName = null, Score = 1.0 }
                }
            };
            var service = new UgsLeaderboardService(new StubCloudCodeProvider(), leaderboards);

            var entries = service.GetLeaderboardAsync(1).GetAwaiter().GetResult();

            Assert.AreEqual("Player_abcdef", entries[0].DisplayName);
        }

        [Test]
        public void GetLeaderboardAsync_NonNullPlayerName_UsesIt()
        {
            var leaderboards = new StubLeaderboardsProvider
            {
                Scores = new List<RawLeaderboardScore>
                {
                    new RawLeaderboardScore { Rank = 0, PlayerId = "aaaaaa000", PlayerName = "Eagle_Pro", Score = 1.0 }
                }
            };
            var service = new UgsLeaderboardService(new StubCloudCodeProvider(), leaderboards);

            var entries = service.GetLeaderboardAsync(1).GetAwaiter().GetResult();

            Assert.AreEqual("Eagle_Pro", entries[0].DisplayName);
        }

        [Test]
        public void GetLeaderboardAsync_MapsScoreToDistance()
        {
            var leaderboards = new StubLeaderboardsProvider
            {
                Scores = new List<RawLeaderboardScore>
                {
                    new RawLeaderboardScore { Rank = 0, PlayerId = "aaaaaa000", PlayerName = "A", Score = 3.14 }
                }
            };
            var service = new UgsLeaderboardService(new StubCloudCodeProvider(), leaderboards);

            var entries = service.GetLeaderboardAsync(1).GetAwaiter().GetResult();

            Assert.AreEqual(3.14f, entries[0].Distance, 0.001f);
        }

        [Test]
        public void GetPlayerRankAsync_Success_ReturnsOneBased()
        {
            var leaderboards = new StubLeaderboardsProvider
            {
                PlayerScore = new RawLeaderboardScore { Rank = 4 }
            };
            var service = new UgsLeaderboardService(new StubCloudCodeProvider(), leaderboards);

            var rank = service.GetPlayerRankAsync("p").GetAwaiter().GetResult();

            Assert.AreEqual(5, rank);
        }

        [Test]
        public void GetPlayerRankAsync_Exception_ReturnsNegativeOne()
        {
            var leaderboards = new StubLeaderboardsProvider
            {
                ExceptionToThrow = new Exception("network error")
            };
            var service = new UgsLeaderboardService(new StubCloudCodeProvider(), leaderboards);

            var rank = service.GetPlayerRankAsync("p").GetAwaiter().GetResult();

            Assert.AreEqual(-1, rank);
        }
    }
}

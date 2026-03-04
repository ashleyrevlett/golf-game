using NUnit.Framework;
using GolfGame.Core;
using GolfGame.Multiplayer;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for multiplayer services: ServiceLocator, MockAuth, MockLeaderboard.
    /// </summary>
    public class MultiplayerTests
    {
        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
        }

        // ServiceLocator Tests

        [Test]
        public void ServiceLocator_RegisterAndGet_ReturnsSameInstance()
        {
            var auth = new MockAuthService();
            ServiceLocator.Register<IAuthService>(auth);
            Assert.AreSame(auth, ServiceLocator.Get<IAuthService>());
        }

        [Test]
        public void ServiceLocator_GetUnregistered_ReturnsNull()
        {
            Assert.IsNull(ServiceLocator.Get<IAuthService>());
        }

        [Test]
        public void ServiceLocator_ReRegister_Overwrites()
        {
            var first = new MockAuthService("a", "A");
            var second = new MockAuthService("b", "B");
            ServiceLocator.Register<IAuthService>(first);
            ServiceLocator.Register<IAuthService>(second);
            Assert.AreSame(second, ServiceLocator.Get<IAuthService>());
        }

        [Test]
        public void ServiceLocator_Clear_RemovesAllServices()
        {
            ServiceLocator.Register<IAuthService>(new MockAuthService());
            ServiceLocator.Clear();
            Assert.IsNull(ServiceLocator.Get<IAuthService>());
        }

        [Test]
        public void ServiceLocator_RegisterNull_StoresNull()
        {
            ServiceLocator.Register<IAuthService>(null);
            Assert.IsNull(ServiceLocator.Get<IAuthService>());
        }

        // MockAuthService Tests (async migrated)

        [Test]
        public void MockAuth_GetPlayerTokenAsync_ReturnsNonEmpty()
        {
            var auth = new MockAuthService();
            var token = auth.GetPlayerTokenAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(token);
            Assert.IsNotEmpty(token);
        }

        [Test]
        public void MockAuth_GetPlayerInfoAsync_ReturnsValidPlayer()
        {
            var auth = new MockAuthService();
            var info = auth.GetPlayerInfoAsync().GetAwaiter().GetResult();
            Assert.IsNotNull(info.PlayerId);
            Assert.IsNotEmpty(info.PlayerId);
            Assert.IsNotNull(info.DisplayName);
            Assert.IsNotEmpty(info.DisplayName);
        }

        [Test]
        public void MockAuth_IsSignedIn_ReturnsTrue()
        {
            var auth = new MockAuthService();
            Assert.IsTrue(auth.IsSignedIn);
        }

        [Test]
        public void MockAuth_PlayerId_MatchesPlayerInfo()
        {
            var auth = new MockAuthService();
            var info = auth.GetPlayerInfoAsync().GetAwaiter().GetResult();
            Assert.AreEqual(info.PlayerId, auth.PlayerId);
        }

        [Test]
        public void MockAuth_CustomPlayer_ReturnsCorrectInfo()
        {
            var auth = new MockAuthService("test_id", "TestPlayer");
            var info = auth.GetPlayerInfoAsync().GetAwaiter().GetResult();
            Assert.AreEqual("test_id", info.PlayerId);
            Assert.AreEqual("TestPlayer", info.DisplayName);
        }

        // MockLeaderboardService Tests (async migrated)

        [Test]
        public void MockLeaderboard_PrePopulated_HasEntries()
        {
            var lb = new MockLeaderboardService();
            Assert.Greater(lb.EntryCount, 0);
        }

        [Test]
        public void MockLeaderboard_PostScoreAsync_AddsEntry()
        {
            var lb = new MockLeaderboardService();
            int before = lb.EntryCount;
            lb.PostScoreAsync("new_player", 5.0f).GetAwaiter().GetResult();
            Assert.AreEqual(before + 1, lb.EntryCount);
        }

        [Test]
        public void MockLeaderboard_GetLeaderboardAsync_SortedAscending()
        {
            var lb = new MockLeaderboardService();
            var entries = lb.GetLeaderboardAsync(10).GetAwaiter().GetResult();
            for (int i = 1; i < entries.Length; i++)
            {
                Assert.LessOrEqual(entries[i - 1].Distance, entries[i].Distance,
                    $"Entry {i-1} ({entries[i-1].Distance}) should be <= entry {i} ({entries[i].Distance})");
            }
        }

        [Test]
        public void MockLeaderboard_GetLeaderboardAsync_RespectsCount()
        {
            var lb = new MockLeaderboardService();
            var entries = lb.GetLeaderboardAsync(3).GetAwaiter().GetResult();
            Assert.AreEqual(3, entries.Length);
        }

        [Test]
        public void MockLeaderboard_GetPlayerRankAsync_Correct()
        {
            var lb = new MockLeaderboardService();
            lb.PostScoreAsync("test_player", 0.1f).GetAwaiter().GetResult();
            int rank = lb.GetPlayerRankAsync("test_player").GetAwaiter().GetResult();
            Assert.AreEqual(1, rank);
        }

        [Test]
        public void MockLeaderboard_PostScoreAsync_UpdatesExisting()
        {
            var lb = new MockLeaderboardService();
            lb.PostScoreAsync("test_player", 10.0f).GetAwaiter().GetResult();
            int countAfterFirst = lb.EntryCount;
            lb.PostScoreAsync("test_player", 5.0f).GetAwaiter().GetResult();
            Assert.AreEqual(countAfterFirst, lb.EntryCount); // no duplicate
        }

        [Test]
        public void MockLeaderboard_PostScoreAsync_OnlyUpdatesIfBetter()
        {
            var lb = new MockLeaderboardService();
            lb.PostScoreAsync("test_player", 2.0f).GetAwaiter().GetResult();
            lb.PostScoreAsync("test_player", 10.0f).GetAwaiter().GetResult();
            var entries = lb.GetLeaderboardAsync(20).GetAwaiter().GetResult();
            foreach (var entry in entries)
            {
                if (entry.PlayerId == "test_player")
                {
                    Assert.AreEqual(2.0f, entry.Distance, 0.001f);
                    return;
                }
            }
            Assert.Fail("Player not found on leaderboard");
        }

        [Test]
        public void MockLeaderboard_GetPlayerRankAsync_UnknownPlayer_ReturnsNegative()
        {
            var lb = new MockLeaderboardService();
            int rank = lb.GetPlayerRankAsync("nonexistent").GetAwaiter().GetResult();
            Assert.AreEqual(-1, rank);
        }

        [Test]
        public void MockLeaderboard_Ranks_AreOneBased()
        {
            var lb = new MockLeaderboardService();
            var entries = lb.GetLeaderboardAsync(5).GetAwaiter().GetResult();
            Assert.AreEqual(1, entries[0].Rank);
            Assert.AreEqual(2, entries[1].Rank);
        }
    }
}

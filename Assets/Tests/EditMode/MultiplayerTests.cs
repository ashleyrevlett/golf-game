using NUnit.Framework;
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

        // MockAuthService Tests

        [Test]
        public void MockAuth_GetPlayerToken_ReturnsNonEmpty()
        {
            var auth = new MockAuthService();
            var token = auth.GetPlayerToken();
            Assert.IsNotNull(token);
            Assert.IsNotEmpty(token);
        }

        [Test]
        public void MockAuth_GetPlayerInfo_ReturnsValidPlayer()
        {
            var auth = new MockAuthService();
            var info = auth.GetPlayerInfo();
            Assert.IsNotNull(info.PlayerId);
            Assert.IsNotEmpty(info.PlayerId);
            Assert.IsNotNull(info.DisplayName);
            Assert.IsNotEmpty(info.DisplayName);
        }

        [Test]
        public void MockAuth_CustomPlayer_ReturnsCorrectInfo()
        {
            var auth = new MockAuthService("test_id", "TestPlayer");
            var info = auth.GetPlayerInfo();
            Assert.AreEqual("test_id", info.PlayerId);
            Assert.AreEqual("TestPlayer", info.DisplayName);
        }

        // MockLeaderboardService Tests

        [Test]
        public void MockLeaderboard_PrePopulated_HasEntries()
        {
            var lb = new MockLeaderboardService();
            Assert.Greater(lb.EntryCount, 0);
        }

        [Test]
        public void MockLeaderboard_PostScore_AddsEntry()
        {
            var lb = new MockLeaderboardService();
            int before = lb.EntryCount;
            lb.PostScore("new_player", 5.0f);
            Assert.AreEqual(before + 1, lb.EntryCount);
        }

        [Test]
        public void MockLeaderboard_GetLeaderboard_SortedAscending()
        {
            var lb = new MockLeaderboardService();
            var entries = lb.GetLeaderboard(10);
            for (int i = 1; i < entries.Length; i++)
            {
                Assert.LessOrEqual(entries[i - 1].Distance, entries[i].Distance,
                    $"Entry {i-1} ({entries[i-1].Distance}) should be <= entry {i} ({entries[i].Distance})");
            }
        }

        [Test]
        public void MockLeaderboard_GetLeaderboard_RespectsCount()
        {
            var lb = new MockLeaderboardService();
            var entries = lb.GetLeaderboard(3);
            Assert.AreEqual(3, entries.Length);
        }

        [Test]
        public void MockLeaderboard_GetPlayerRank_Correct()
        {
            var lb = new MockLeaderboardService();
            lb.PostScore("test_player", 0.1f); // very close — should be rank 1
            int rank = lb.GetPlayerRank("test_player");
            Assert.AreEqual(1, rank);
        }

        [Test]
        public void MockLeaderboard_PostScore_UpdatesExisting()
        {
            var lb = new MockLeaderboardService();
            lb.PostScore("test_player", 10.0f);
            int countAfterFirst = lb.EntryCount;
            lb.PostScore("test_player", 5.0f); // better score
            Assert.AreEqual(countAfterFirst, lb.EntryCount); // no duplicate
        }

        [Test]
        public void MockLeaderboard_PostScore_OnlyUpdatesIfBetter()
        {
            var lb = new MockLeaderboardService();
            lb.PostScore("test_player", 2.0f);
            lb.PostScore("test_player", 10.0f); // worse score
            var entries = lb.GetLeaderboard(20);
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
        public void MockLeaderboard_GetPlayerRank_UnknownPlayer_ReturnsNegative()
        {
            var lb = new MockLeaderboardService();
            int rank = lb.GetPlayerRank("nonexistent");
            Assert.AreEqual(-1, rank);
        }

        [Test]
        public void MockLeaderboard_Ranks_AreOneBased()
        {
            var lb = new MockLeaderboardService();
            var entries = lb.GetLeaderboard(5);
            Assert.AreEqual(1, entries[0].Rank);
            Assert.AreEqual(2, entries[1].Rank);
        }
    }
}

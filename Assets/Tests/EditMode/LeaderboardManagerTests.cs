using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Core;
using GolfGame.Multiplayer;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// Tests for LeaderboardManager orchestration: retry queue, polling,
    /// event firing, null guards, and partial failure handling.
    ///
    /// Private event handlers (HandleBestDistanceUpdated, HandleGameOver,
    /// HandleShotStateChanged) are wired in Start() which doesn't run in
    /// EditMode. The underlying async methods they delegate to
    /// (PostScoreWithRetryAsync, PollLeaderboardAsync) are tested directly.
    /// Event wiring coverage belongs in PlayMode tests.
    /// </summary>
    public class LeaderboardManagerTests
    {
        private GameObject managerObj;
        private LeaderboardManager manager;

        [SetUp]
        public void SetUp()
        {
            // Suppress coroutine / warning messages in edit mode
            LogAssert.ignoreFailingMessages = true;
            ServiceLocator.Clear();
            ServiceLocator.Register<IAuthService>(new MockAuthService());
        }

        [TearDown]
        public void TearDown()
        {
            if (managerObj != null)
            {
                UnityEngine.Object.DestroyImmediate(managerObj);
            }
            ServiceLocator.Clear();
            LogAssert.ignoreFailingMessages = false;
        }

        private LeaderboardManager CreateManager(ILeaderboardService leaderboardService)
        {
            ServiceLocator.Register<ILeaderboardService>(leaderboardService);
            managerObj = new GameObject("LeaderboardManager");
            var mgr = managerObj.AddComponent<LeaderboardManager>();
            // Start doesn't fire in edit mode — set internal fields via reflection
            SetPrivateField(mgr, "leaderboardService", leaderboardService);
            SetPrivateField(mgr, "authService", ServiceLocator.Get<IAuthService>());
            SetPrivateField(mgr, "playerId", ServiceLocator.Get<IAuthService>()?.PlayerId);
            return mgr;
        }

        [Test]
        public void PostScoreAsync_WhenServiceThrows_EnqueuesRetry()
        {
            var failing = new FailingLeaderboardService(postThrows: true);
            var mgr = CreateManager(failing);

            // Directly call the internal method
            mgr.PostScoreWithRetryAsync("player1", 5.0f).GetAwaiter().GetResult();

            Assert.AreEqual(1, mgr.RetryQueueCount);
        }

        [Test]
        public void PostScoreAsync_WhenServiceThrows_StartsRetryLoop()
        {
            var failing = new FailingLeaderboardService(postThrows: true);
            var mgr = CreateManager(failing);

            mgr.PostScoreWithRetryAsync("player1", 5.0f).GetAwaiter().GetResult();

            Assert.IsTrue(mgr.IsRetrying, "RetryLoop should be started after failed post");
        }

        [Test]
        public void PostScoreAsync_WhenServiceSucceeds_DoesNotEnqueueRetry()
        {
            var configurable = new ConfigurableLeaderboardService();
            var mgr = CreateManager(configurable);

            mgr.PostScoreWithRetryAsync("player1", 5.0f).GetAwaiter().GetResult();

            Assert.AreEqual(0, mgr.RetryQueueCount);
        }

        [Test]
        public void ProcessRetryQueue_WhenServiceRecovers_DequeuesSuccessfully()
        {
            var failing = new FailingLeaderboardService(postThrows: true);
            var mgr = CreateManager(failing);

            // Enqueue a failed score
            mgr.PostScoreWithRetryAsync("player1", 5.0f).GetAwaiter().GetResult();
            Assert.AreEqual(1, mgr.RetryQueueCount);

            // Recover — stop throwing
            failing.PostThrows = false;
            mgr.ProcessRetryQueueAsync().GetAwaiter().GetResult();

            Assert.AreEqual(0, mgr.RetryQueueCount);
        }

        [Test]
        public void ProcessRetryQueue_WhenServiceStillFailing_KeepsItemInQueue()
        {
            var failing = new FailingLeaderboardService(postThrows: true);
            var mgr = CreateManager(failing);

            mgr.PostScoreWithRetryAsync("player1", 5.0f).GetAwaiter().GetResult();
            Assert.AreEqual(1, mgr.RetryQueueCount);

            // Process retry -- still failing
            mgr.ProcessRetryQueueAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, mgr.RetryQueueCount);
        }

        [Test]
        public void ProcessRetryQueue_WhenEmpty_DoesNothing()
        {
            var configurable = new ConfigurableLeaderboardService();
            var mgr = CreateManager(configurable);

            // Should not throw and queue stays at 0
            mgr.ProcessRetryQueueAsync().GetAwaiter().GetResult();

            Assert.AreEqual(0, mgr.RetryQueueCount);
        }

        [Test]
        public void PollLeaderboard_WhenGetFails_RetainsStaleCache()
        {
            var configurable = new ConfigurableLeaderboardService();
            var mgr = CreateManager(configurable);

            // Initial poll to populate cache (Start doesn't run in edit mode)
            mgr.PollLeaderboardAsync().GetAwaiter().GetResult();

            Assert.Greater(mgr.CurrentEntries.Length, 0);
            var cachedEntries = mgr.CurrentEntries;
            int cachedRank = mgr.PlayerRank;

            // Now make it fail
            configurable.ShouldThrow = true;
            mgr.PollLeaderboardAsync().GetAwaiter().GetResult();

            // Stale cache preserved
            Assert.AreSame(cachedEntries, mgr.CurrentEntries);
            Assert.AreEqual(cachedRank, mgr.PlayerRank);
        }

        [Test]
        public void PollLeaderboard_WhenGetFails_StillFiresEvent()
        {
            var configurable = new ConfigurableLeaderboardService();
            var mgr = CreateManager(configurable);

            // Initial poll to populate cache
            mgr.PollLeaderboardAsync().GetAwaiter().GetResult();

            LeaderboardEntry[] receivedEntries = null;
            int receivedRank = -99;
            mgr.OnLeaderboardUpdated += (entries, rank) =>
            {
                receivedEntries = entries;
                receivedRank = rank;
            };

            // Make it fail
            configurable.ShouldThrow = true;
            mgr.PollLeaderboardAsync().GetAwaiter().GetResult();

            // Event should still fire with stale (non-null) data
            Assert.IsNotNull(receivedEntries, "Event should fire even on failure");
        }

        [Test]
        public void PollLeaderboard_Success_UpdatesCurrentEntries()
        {
            var configurable = new ConfigurableLeaderboardService();
            var mgr = CreateManager(configurable);

            mgr.PollLeaderboardAsync().GetAwaiter().GetResult();

            Assert.Greater(mgr.CurrentEntries.Length, 0);
        }

        [Test]
        public void PollLeaderboard_Success_UpdatesPlayerRank()
        {
            var configurable = new ConfigurableLeaderboardService();
            var mgr = CreateManager(configurable);

            // Post a score for the mock player so they appear in the leaderboard
            configurable.PostScoreAsync("player_local", 3.0f).GetAwaiter().GetResult();

            mgr.PollLeaderboardAsync().GetAwaiter().GetResult();

            Assert.Greater(mgr.PlayerRank, 0, "Player rank should be positive after posting a score");
        }

        [Test]
        public void PollLeaderboard_Success_EventDataMatchesProperties()
        {
            var configurable = new ConfigurableLeaderboardService();
            var mgr = CreateManager(configurable);

            LeaderboardEntry[] receivedEntries = null;
            int receivedRank = -99;
            mgr.OnLeaderboardUpdated += (entries, rank) =>
            {
                receivedEntries = entries;
                receivedRank = rank;
            };

            mgr.PollLeaderboardAsync().GetAwaiter().GetResult();

            Assert.AreSame(mgr.CurrentEntries, receivedEntries,
                "Event entries should be the same reference as CurrentEntries");
            Assert.AreEqual(mgr.PlayerRank, receivedRank,
                "Event rank should match PlayerRank");
        }

        [Test]
        public void PollLeaderboard_WhenServiceNull_ReturnsSilently()
        {
            var configurable = new ConfigurableLeaderboardService();
            var mgr = CreateManager(configurable);

            // Set leaderboardService to null via reflection
            SetPrivateField(mgr, "leaderboardService", null);

            bool eventFired = false;
            mgr.OnLeaderboardUpdated += (entries, rank) => { eventFired = true; };

            mgr.PollLeaderboardAsync().GetAwaiter().GetResult();

            Assert.AreEqual(0, mgr.CurrentEntries.Length,
                "CurrentEntries should remain empty when service is null");
            Assert.IsFalse(eventFired,
                "OnLeaderboardUpdated should not fire when service is null");
        }

        [Test]
        public void PollLeaderboard_WhenGetRankFails_EntriesStillUpdate()
        {
            var failing = new FailingLeaderboardService(rankThrows: true);
            failing.DefaultEntries = new LeaderboardEntry[]
            {
                new LeaderboardEntry { PlayerId = "p1", DisplayName = "Player1", Distance = 2.0f, Rank = 1 },
                new LeaderboardEntry { PlayerId = "p2", DisplayName = "Player2", Distance = 4.0f, Rank = 2 }
            };
            var mgr = CreateManager(failing);

            mgr.PollLeaderboardAsync().GetAwaiter().GetResult();

            // Entries should be updated (assigned before rank throw)
            Assert.AreEqual(2, mgr.CurrentEntries.Length);
            // Rank should remain at default -1 (never overwritten due to throw)
            Assert.AreEqual(-1, mgr.PlayerRank);
        }

        [Test]
        public void RetryQueue_MultipleFailures_ProcessesInFIFOOrder()
        {
            var failing = new FailingLeaderboardService(postThrows: true);
            var mgr = CreateManager(failing);

            // Enqueue two failed scores
            mgr.PostScoreWithRetryAsync("player1", 5.0f).GetAwaiter().GetResult();
            mgr.PostScoreWithRetryAsync("player2", 10.0f).GetAwaiter().GetResult();
            Assert.AreEqual(2, mgr.RetryQueueCount);

            // Make service work and process one
            failing.PostThrows = false;
            failing.TrackLastPostedDistance = true;
            mgr.ProcessRetryQueueAsync().GetAwaiter().GetResult();

            // First enqueued (5.0) should be processed
            Assert.AreEqual(1, mgr.RetryQueueCount);
            Assert.AreEqual(5.0f, failing.LastPostedDistance, 0.001f);
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }

    /// <summary>
    /// Test double that throws on configurable methods.
    /// </summary>
    internal class FailingLeaderboardService : ILeaderboardService
    {
        public bool PostThrows { get; set; }
        public bool GetThrows { get; set; }
        public bool RankThrows { get; set; }
        public bool TrackLastPostedDistance { get; set; }
        public float LastPostedDistance { get; private set; }
        public LeaderboardEntry[] DefaultEntries { get; set; } = Array.Empty<LeaderboardEntry>();

        public FailingLeaderboardService(
            bool postThrows = false,
            bool getThrows = false,
            bool rankThrows = false)
        {
            PostThrows = postThrows;
            GetThrows = getThrows;
            RankThrows = rankThrows;
        }

        public Task PostScoreAsync(string playerId, float distance)
        {
            if (PostThrows)
                throw new Exception("Simulated post failure");
            if (TrackLastPostedDistance)
                LastPostedDistance = distance;
            return Task.CompletedTask;
        }

        public Task<LeaderboardEntry[]> GetLeaderboardAsync(int count)
        {
            if (GetThrows)
                throw new Exception("Simulated get failure");
            return Task.FromResult(DefaultEntries);
        }

        public Task<int> GetPlayerRankAsync(string playerId)
        {
            if (RankThrows)
                throw new Exception("Simulated rank failure");
            return Task.FromResult(-1);
        }
    }

    /// <summary>
    /// Leaderboard service that starts working and can be toggled to fail.
    /// Delegates to MockLeaderboardService when not throwing.
    /// </summary>
    internal class ConfigurableLeaderboardService : ILeaderboardService
    {
        private readonly MockLeaderboardService mock = new MockLeaderboardService();
        public bool ShouldThrow { get; set; }

        public Task PostScoreAsync(string playerId, float distance)
        {
            if (ShouldThrow)
                throw new Exception("Simulated failure");
            return mock.PostScoreAsync(playerId, distance);
        }

        public Task<LeaderboardEntry[]> GetLeaderboardAsync(int count)
        {
            if (ShouldThrow)
                throw new Exception("Simulated failure");
            return mock.GetLeaderboardAsync(count);
        }

        public Task<int> GetPlayerRankAsync(string playerId)
        {
            if (ShouldThrow)
                throw new Exception("Simulated failure");
            return mock.GetPlayerRankAsync(playerId);
        }
    }
}

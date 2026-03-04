using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using GolfGame.Core;
using GolfGame.Multiplayer;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// Tests for LeaderboardManager retry queue and stale cache logic.
    /// Uses FailingLeaderboardService to simulate network failures.
    /// </summary>
    public class LeaderboardManagerRetryTests
    {
        private GameObject managerObj;
        private LeaderboardManager manager;

        [SetUp]
        public void SetUp()
        {
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
        }

        private LeaderboardManager CreateManager(ILeaderboardService leaderboardService)
        {
            ServiceLocator.Register<ILeaderboardService>(leaderboardService);
            managerObj = new GameObject("LeaderboardManager");
            return managerObj.AddComponent<LeaderboardManager>();
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
        public void ProcessRetryQueue_WhenServiceRecovers_DequeuesSuccessfully()
        {
            var failing = new FailingLeaderboardService(postThrows: true);
            var mgr = CreateManager(failing);

            // Enqueue a failed score
            mgr.PostScoreWithRetryAsync("player1", 5.0f).GetAwaiter().GetResult();
            Assert.AreEqual(1, mgr.RetryQueueCount);

            // Swap to working service
            var working = new MockLeaderboardService();
            ServiceLocator.Register<ILeaderboardService>(working);
            // Re-create manager to pick up new service -- or use internal method directly
            // Since PostScoreWithRetryAsync uses the field, we need to test ProcessRetryQueueAsync
            // But the field is set in Start(). For unit test, call process directly with working service.
            // The retry queue is on the manager instance, but the leaderboardService field
            // is set during Start. We'll test the queue behavior via the internal method.

            // For this test, since the manager's leaderboardService was set to failing in Start,
            // and we can't change it, we test that when failing stops failing, it dequeues.
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
        public void PollLeaderboard_WhenGetFails_RetainsStaleCache()
        {
            var mock = new MockLeaderboardService();
            var mgr = CreateManager(mock);

            // Let Start run and populate initial data
            // Wait a frame equivalent -- in EditMode, Start has run synchronously
            var initialEntries = mgr.CurrentEntries;

            // Now swap to failing service and poll
            var failing = new FailingLeaderboardService(getThrows: true);
            ServiceLocator.Register<ILeaderboardService>(failing);

            // Poll will fail but should retain stale cache
            // Since leaderboardService is set during Start (to mock), and PollLeaderboardAsync
            // uses the field, we need to ensure the stale cache behavior.
            // The internal field is set once in Start. For this test, create a manager
            // with a service that works first, then fails.
            UnityEngine.Object.DestroyImmediate(managerObj);

            var configurable = new ConfigurableLeaderboardService();
            mgr = CreateManager(configurable);

            // First poll succeeds (from Start)
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
            return Task.FromResult(Array.Empty<LeaderboardEntry>());
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

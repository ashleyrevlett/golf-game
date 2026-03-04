using NUnit.Framework;
using UnityEngine;
using GolfGame.Multiplayer;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for Bootstrap service registration.
    /// </summary>
    public class BootstrapTests
    {
        [SetUp]
        public void SetUp()
        {
            Bootstrap.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            Bootstrap.ResetForTesting();
        }

        [Test]
        public void Bootstrap_RegistersAuthService()
        {
            var obj = new GameObject("Bootstrap");
            obj.AddComponent<Bootstrap>();

            var auth = ServiceLocator.Get<IAuthService>();
            Assert.IsNotNull(auth);

            Object.DestroyImmediate(obj);
        }

        [Test]
        public void Bootstrap_RegistersLeaderboardService()
        {
            var obj = new GameObject("Bootstrap");
            obj.AddComponent<Bootstrap>();

            var lb = ServiceLocator.Get<ILeaderboardService>();
            Assert.IsNotNull(lb);

            Object.DestroyImmediate(obj);
        }

        [Test]
        public void Bootstrap_AuthIsMockType()
        {
            var obj = new GameObject("Bootstrap");
            obj.AddComponent<Bootstrap>();

            var auth = ServiceLocator.Get<IAuthService>();
            Assert.IsInstanceOf<MockAuthService>(auth);

            Object.DestroyImmediate(obj);
        }

        [Test]
        public void Bootstrap_OnlyRunsOnce()
        {
            var obj1 = new GameObject("Bootstrap1");
            obj1.AddComponent<Bootstrap>();

            // Register a custom service to detect overwrite
            var customAuth = new MockAuthService("custom", "Custom");
            ServiceLocator.Register<IAuthService>(customAuth);

            var obj2 = new GameObject("Bootstrap2");
            obj2.AddComponent<Bootstrap>();

            // Should still be the custom service (bootstrap skipped)
            Assert.AreSame(customAuth, ServiceLocator.Get<IAuthService>());

            Object.DestroyImmediate(obj1);
            Object.DestroyImmediate(obj2);
        }

        [Test]
        public void Bootstrap_Awake_MocksAvailableSynchronously()
        {
            // Mocks must be registered in the synchronous portion of Awake
            // before any await, so they are available immediately.
            var obj = new GameObject("Bootstrap");
            obj.AddComponent<Bootstrap>();

            // Both services should be non-null immediately
            var auth = ServiceLocator.Get<IAuthService>();
            var lb = ServiceLocator.Get<ILeaderboardService>();
            Assert.IsNotNull(auth, "Auth service should be available immediately after Awake");
            Assert.IsNotNull(lb, "Leaderboard service should be available immediately after Awake");

            Object.DestroyImmediate(obj);
        }

        [Test]
        public void Bootstrap_RegistersBestScoreService()
        {
            var obj = new GameObject("Bootstrap");
            obj.AddComponent<Bootstrap>();

            var bestScore = ServiceLocator.Get<IBestScoreService>();
            Assert.IsNotNull(bestScore);
            Assert.IsInstanceOf<PlayerPrefsBestScoreService>(bestScore);

            Object.DestroyImmediate(obj);
        }

        [Test]
        public void Bootstrap_AfterReset_CanReinitialize()
        {
            var obj1 = new GameObject("Bootstrap1");
            obj1.AddComponent<Bootstrap>();

            Assert.IsNotNull(ServiceLocator.Get<IAuthService>());
            Object.DestroyImmediate(obj1);

            // Reset and reinitialize
            Bootstrap.ResetForTesting();
            Assert.IsNull(ServiceLocator.Get<IAuthService>());

            var obj2 = new GameObject("Bootstrap2");
            obj2.AddComponent<Bootstrap>();

            Assert.IsNotNull(ServiceLocator.Get<IAuthService>());
            Object.DestroyImmediate(obj2);
        }
    }
}

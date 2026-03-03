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
    }
}

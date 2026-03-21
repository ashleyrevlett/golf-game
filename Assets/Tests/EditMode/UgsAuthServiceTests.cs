using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Multiplayer;

namespace GolfGame.Tests.EditMode
{
    internal class StubUgsAuthProvider : IUgsAuthProvider
    {
        public bool IsSignedIn { get; set; }
        public string PlayerId { get; set; } = "test-player-id-123456";
        public string AccessToken { get; set; } = "test-access-token";
        public int SignInCallCount { get; private set; }
        public Exception ExceptionToThrow { get; set; }

        public Task SignInAnonymouslyAsync()
        {
            SignInCallCount++;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            IsSignedIn = true;
            return Task.CompletedTask;
        }
    }

    public class UgsAuthServiceTests
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
        public void SignInAsync_AlreadySignedIn_SkipsSignIn()
        {
            var stub = new StubUgsAuthProvider { IsSignedIn = true };
            var service = new UgsAuthService(stub);

            service.SignInAsync().GetAwaiter().GetResult();

            Assert.AreEqual(0, stub.SignInCallCount);
        }

        [Test]
        public void SignInAsync_NotSignedIn_CallsProvider()
        {
            var stub = new StubUgsAuthProvider { IsSignedIn = false };
            var service = new UgsAuthService(stub);

            service.SignInAsync().GetAwaiter().GetResult();

            Assert.AreEqual(1, stub.SignInCallCount);
        }

        [Test]
        public void GetPlayerTokenAsync_NotSignedIn_SignsInThenReturnsToken()
        {
            var stub = new StubUgsAuthProvider
            {
                IsSignedIn = false,
                AccessToken = "tok"
            };
            var service = new UgsAuthService(stub);

            var token = service.GetPlayerTokenAsync().GetAwaiter().GetResult();

            Assert.AreEqual("tok", token);
            Assert.AreEqual(1, stub.SignInCallCount);
        }

        [Test]
        public void GetPlayerTokenAsync_AlreadySignedIn_ReturnsTokenWithoutSignIn()
        {
            var stub = new StubUgsAuthProvider
            {
                IsSignedIn = true,
                AccessToken = "tok"
            };
            var service = new UgsAuthService(stub);

            var token = service.GetPlayerTokenAsync().GetAwaiter().GetResult();

            Assert.AreEqual("tok", token);
            Assert.AreEqual(0, stub.SignInCallCount);
        }

        [Test]
        public void GetPlayerInfoAsync_MapsFields()
        {
            var stub = new StubUgsAuthProvider
            {
                IsSignedIn = true,
                PlayerId = "abcdef123",
                AccessToken = "tok"
            };
            var service = new UgsAuthService(stub);

            var info = service.GetPlayerInfoAsync().GetAwaiter().GetResult();

            Assert.AreEqual("abcdef123", info.PlayerId);
            Assert.AreEqual("Player_abcdef", info.DisplayName);
            Assert.AreEqual("tok", info.Token);
        }

        [Test]
        public void GetPlayerTokenAsync_ProviderThrows_RethrowsAndLogs()
        {
            var stub = new StubUgsAuthProvider
            {
                IsSignedIn = false,
                ExceptionToThrow = new Exception("boom")
            };
            var service = new UgsAuthService(stub);

            LogAssert.ignoreFailingMessages = false;
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("boom"));

            var ex = Assert.Throws<Exception>(() =>
                service.GetPlayerTokenAsync().GetAwaiter().GetResult());
            Assert.That(ex.Message, Does.Contain("boom"));
        }
    }
}

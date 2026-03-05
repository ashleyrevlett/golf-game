using NUnit.Framework;
using GolfGame.Core;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for ServiceLocator register/get/clear lifecycle.
    /// </summary>
    public class ServiceLocatorTests
    {
        // Test interface and implementations
        private interface ITestService { string Name { get; } }

        private class TestServiceA : ITestService
        {
            public string Name => "A";
        }

        private class TestServiceB : ITestService
        {
            public string Name => "B";
        }

        private interface IOtherService { }

        private class OtherServiceImpl : IOtherService { }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
        }

        [Test]
        public void Get_ReturnsNull_WhenNothingRegistered()
        {
            var result = ServiceLocator.Get<ITestService>();
            Assert.IsNull(result);
        }

        [Test]
        public void Register_ThenGet_ReturnsSameInstance()
        {
            var service = new TestServiceA();
            ServiceLocator.Register<ITestService>(service);

            var result = ServiceLocator.Get<ITestService>();
            Assert.AreSame(service, result);
        }

        [Test]
        public void Register_Overwrites_PreviousRegistration()
        {
            var first = new TestServiceA();
            var second = new TestServiceB();

            ServiceLocator.Register<ITestService>(first);
            ServiceLocator.Register<ITestService>(second);

            var result = ServiceLocator.Get<ITestService>();
            Assert.AreSame(second, result);
            Assert.AreEqual("B", result.Name);
        }

        [Test]
        public void Clear_RemovesAllRegistrations()
        {
            ServiceLocator.Register<ITestService>(new TestServiceA());
            ServiceLocator.Register<IOtherService>(new OtherServiceImpl());

            ServiceLocator.Clear();

            Assert.IsNull(ServiceLocator.Get<ITestService>());
            Assert.IsNull(ServiceLocator.Get<IOtherService>());
        }

        [Test]
        public void MultipleTypes_RegisteredIndependently()
        {
            var testService = new TestServiceA();
            var otherService = new OtherServiceImpl();

            ServiceLocator.Register<ITestService>(testService);
            ServiceLocator.Register<IOtherService>(otherService);

            Assert.AreSame(testService, ServiceLocator.Get<ITestService>());
            Assert.AreSame(otherService, ServiceLocator.Get<IOtherService>());
        }

        [Test]
        public void Get_UnregisteredType_ReturnsNull_WhenOthersExist()
        {
            ServiceLocator.Register<ITestService>(new TestServiceA());

            Assert.IsNull(ServiceLocator.Get<IOtherService>());
        }
    }
}

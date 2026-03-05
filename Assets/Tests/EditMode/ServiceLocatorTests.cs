using NUnit.Framework;
using GolfGame.Core;

namespace GolfGame.Tests.EditMode
{
    public class ServiceLocatorTests
    {
        private interface ITestService { }
        private class TestService : ITestService { }
        private interface IOtherService { }
        private class OtherService : IOtherService { }

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

        [Test]
        public void Register_ThenGet_ReturnsSameInstance()
        {
            var service = new TestService();
            ServiceLocator.Register<ITestService>(service);
            Assert.AreSame(service, ServiceLocator.Get<ITestService>());
        }

        [Test]
        public void Get_Unregistered_ReturnsNull()
        {
            Assert.IsNull(ServiceLocator.Get<ITestService>());
        }

        [Test]
        public void Register_Duplicate_OverwritesPrevious()
        {
            var first = new TestService();
            var second = new TestService();
            ServiceLocator.Register<ITestService>(first);
            ServiceLocator.Register<ITestService>(second);
            Assert.AreSame(second, ServiceLocator.Get<ITestService>());
        }

        [Test]
        public void Clear_RemovesAllServices()
        {
            ServiceLocator.Register<ITestService>(new TestService());
            ServiceLocator.Register<IOtherService>(new OtherService());
            ServiceLocator.Clear();
            Assert.IsNull(ServiceLocator.Get<ITestService>());
            Assert.IsNull(ServiceLocator.Get<IOtherService>());
        }

        [Test]
        public void Register_MultipleTypes_ReturnsCorrectInstances()
        {
            var testService = new TestService();
            var otherService = new OtherService();
            ServiceLocator.Register<ITestService>(testService);
            ServiceLocator.Register<IOtherService>(otherService);
            Assert.AreSame(testService, ServiceLocator.Get<ITestService>());
            Assert.AreSame(otherService, ServiceLocator.Get<IOtherService>());
        }
    }
}

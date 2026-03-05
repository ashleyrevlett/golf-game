using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using GolfGame.Environment;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for CourseBuilder geometry creation.
    /// Verifies child hierarchy, pin setup, and default config fallback.
    /// Requires shader support — skips automatically in CI batch mode.
    /// </summary>
    public class CourseBuilderTests
    {
        private GameObject builderObj;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;

            // CourseBuilder.Awake calls Shader.Find which returns null in batch mode.
            // Skip tests when no shader is available rather than failing.
            if (Shader.Find("Standard") == null && Shader.Find("Universal Render Pipeline/Lit") == null)
            {
                Assert.Ignore("Skipping CourseBuilderTests: no shaders available (batch mode)");
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (builderObj != null) Object.DestroyImmediate(builderObj);
            LogAssert.ignoreFailingMessages = false;
        }

        private void CreateBuilder()
        {
            builderObj = new GameObject("CourseBuilder");
            builderObj.AddComponent<CourseBuilder>();
        }

        [Test]
        public void Awake_CreatesCourseChildObject()
        {
            CreateBuilder();

            var course = builderObj.transform.Find("Course");
            Assert.IsNotNull(course, "Course child should exist after Awake");
        }

        [Test]
        public void Awake_CreatesExpectedGeometry()
        {
            CreateBuilder();

            var course = builderObj.transform.Find("Course");
            Assert.IsNotNull(course);

            Assert.IsNotNull(course.Find("Ground"), "Ground should exist");
            Assert.IsNotNull(course.Find("TeeBox"), "TeeBox should exist");
            Assert.IsNotNull(course.Find("Fairway"), "Fairway should exist");
            Assert.IsNotNull(course.Find("Rough_L"), "Rough_L should exist");
            Assert.IsNotNull(course.Find("Rough_R"), "Rough_R should exist");
            Assert.IsNotNull(course.Find("Green"), "Green should exist");
            Assert.IsNotNull(course.Find("Pin"), "Pin should exist");
        }

        [Test]
        public void Awake_PinHasPinControllerComponent()
        {
            CreateBuilder();

            var course = builderObj.transform.Find("Course");
            var pin = course.Find("Pin");
            Assert.IsNotNull(pin);

            var pinController = pin.GetComponent<PinController>();
            Assert.IsNotNull(pinController, "Pin should have PinController component");
        }

        [Test]
        public void Awake_PinHasPoleAndFlagChildren()
        {
            CreateBuilder();

            var pin = builderObj.transform.Find("Course/Pin");
            Assert.IsNotNull(pin);

            Assert.IsNotNull(pin.Find("PinPole"), "Pin should have PinPole child");
            Assert.IsNotNull(pin.Find("PinFlag"), "Pin should have PinFlag child");
        }

        [Test]
        public void Awake_NullConfig_UsesDefaultCourseLength()
        {
            CreateBuilder();

            var pin = builderObj.transform.Find("Course/Pin");
            Assert.IsNotNull(pin);

            Assert.AreEqual(114f, pin.localPosition.z, 0.01f,
                "Pin Z should equal default course length");
        }

        [Test]
        public void Awake_CreatesOBMarkers()
        {
            CreateBuilder();

            var course = builderObj.transform.Find("Course");
            int obCount = 0;
            foreach (Transform child in course)
            {
                if (child.name == "OBMarker") obCount++;
            }

            Assert.Greater(obCount, 0, "Should have OB markers");
            Assert.AreEqual(0, obCount % 2, "OB markers should come in pairs (left/right)");
        }

        [Test]
        public void Awake_GeometryObjectsAreStatic()
        {
            CreateBuilder();

            var course = builderObj.transform.Find("Course");
            var teeBox = course.Find("TeeBox");
            Assert.IsNotNull(teeBox);
            Assert.IsTrue(teeBox.gameObject.isStatic, "TeeBox should be marked static");

            var fairway = course.Find("Fairway");
            Assert.IsNotNull(fairway);
            Assert.IsTrue(fairway.gameObject.isStatic, "Fairway should be marked static");
        }

        [Test]
        public void Awake_GeometryHasRenderers()
        {
            CreateBuilder();

            var course = builderObj.transform.Find("Course");
            var fairway = course.Find("Fairway");
            Assert.IsNotNull(fairway);

            var renderer = fairway.GetComponent<Renderer>();
            Assert.IsNotNull(renderer, "Fairway should have a Renderer");
            Assert.IsNotNull(renderer.sharedMaterial, "Fairway should have a material assigned");
        }
    }
}

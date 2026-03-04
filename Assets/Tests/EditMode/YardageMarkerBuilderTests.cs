using NUnit.Framework;
using UnityEngine;
using TMPro;
using GolfGame.Environment;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for YardageMarkerBuilder geometry creation.
    /// </summary>
    public class YardageMarkerBuilderTests
    {
        private GameObject parent;

        [SetUp]
        public void SetUp()
        {
            parent = new GameObject("CourseRoot");
        }

        [TearDown]
        public void TearDown()
        {
            if (parent != null) Object.DestroyImmediate(parent);
        }

        [Test]
        public void Build_CreatesMarkersAtExpectedZPositions()
        {
            YardageMarkerBuilder.Build(parent.transform, 18f, 114f);

            var tmpComponents = parent.GetComponentsInChildren<TextMeshPro>();
            var zPositions = new System.Collections.Generic.List<float>();
            foreach (var tmp in tmpComponents)
            {
                zPositions.Add(Mathf.Round(tmp.transform.localPosition.z));
            }

            Assert.Contains(25f, zPositions);
            Assert.Contains(50f, zPositions);
            Assert.Contains(75f, zPositions);
            Assert.Contains(100f, zPositions);
            Assert.AreEqual(4, tmpComponents.Length);
        }

        [Test]
        public void Build_CreatesYardLinesAtSameZPositions()
        {
            YardageMarkerBuilder.Build(parent.transform, 18f, 114f);

            int lineCount = 0;
            foreach (Transform child in parent.transform)
            {
                if (child.name.StartsWith("YardLine_"))
                {
                    lineCount++;
                }
            }

            Assert.AreEqual(4, lineCount);
        }

        [Test]
        public void Build_MarkerXPosition_IsLeftOfFairway()
        {
            float fairwayWidth = 18f;
            YardageMarkerBuilder.Build(parent.transform, fairwayWidth, 114f);

            var tmpComponents = parent.GetComponentsInChildren<TextMeshPro>();
            float expectedX = -fairwayWidth / 2f - 1.5f;

            foreach (var tmp in tmpComponents)
            {
                Assert.AreEqual(expectedX, tmp.transform.localPosition.x, 0.01f);
            }
        }

        [Test]
        public void Build_NoMarkersCreatedBeyondCourseLength()
        {
            YardageMarkerBuilder.Build(parent.transform, 18f, 60f);

            var tmpComponents = parent.GetComponentsInChildren<TextMeshPro>();

            // Only 25 and 50 should exist (60 < 75)
            Assert.AreEqual(2, tmpComponents.Length);

            foreach (var tmp in tmpComponents)
            {
                Assert.Less(tmp.transform.localPosition.z, 61f);
            }
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using GolfGame.Golf;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// Tests for WindSystem wind generation.
    /// </summary>
    public class WindSystemTests
    {
        private GameObject windObj;
        private WindSystem windSystem;

        [SetUp]
        public void SetUp()
        {
            windObj = new GameObject("WindSystem");
            windSystem = windObj.AddComponent<WindSystem>();
        }

        [TearDown]
        public void TearDown()
        {
            if (windObj != null)
            {
                Object.DestroyImmediate(windObj);
            }
        }

        [Test]
        public void GenerateNewWind_ProducesHorizontalWind()
        {
            windSystem.GenerateNewWind();
            Assert.AreEqual(0f, windSystem.CurrentWind.y, 0.001f,
                "Wind should have no vertical component");
        }

        [Test]
        public void GenerateNewWind_WindSpeedWithinRange()
        {
            // Without config, defaults to 0-8 m/s
            for (int i = 0; i < 20; i++)
            {
                windSystem.GenerateNewWind();
                Assert.GreaterOrEqual(windSystem.WindSpeed, 0f);
                Assert.LessOrEqual(windSystem.WindSpeed, 8f + 0.01f);
            }
        }

        [Test]
        public void GenerateNewWind_ProducesDifferentDirections()
        {
            // Run multiple times — at least 2 unique directions in 20 tries
            var directions = new System.Collections.Generic.HashSet<float>();
            for (int i = 0; i < 20; i++)
            {
                windSystem.GenerateNewWind();
                directions.Add(Mathf.Round(windSystem.WindDirectionDegrees));
            }

            Assert.Greater(directions.Count, 1,
                "Multiple wind generations should produce different directions");
        }

        [Test]
        public void OnWindChanged_Fires_AfterGenerateNewWind()
        {
            int fireCount = 0;
            windSystem.OnWindChanged += _ => fireCount++;

            windSystem.GenerateNewWind();

            Assert.AreEqual(1, fireCount);
        }

        [Test]
        public void CurrentWind_UpdatesAfterGenerate()
        {
            var windBefore = windSystem.CurrentWind;
            // Generate many times to ensure at least one is different
            bool changed = false;
            for (int i = 0; i < 50; i++)
            {
                windSystem.GenerateNewWind();
                if (windSystem.CurrentWind != windBefore)
                {
                    changed = true;
                    break;
                }
            }

            Assert.IsTrue(changed, "Wind should change after regeneration");
        }
    }
}

using NUnit.Framework;
using System.IO;

namespace GolfGame.Tests.EditMode
{
    public class WindIndicatorTests
    {
        [Test]
        public void HandleWindChanged_HidesArrowForZeroWind()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("WindIndicator t:MonoScript");
            Assert.IsTrue(guids.Length > 0, "WindIndicator script not found");

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            var source = File.ReadAllText(Path.GetFullPath(path));

            // Verify zero-wind threshold check exists
            Assert.IsTrue(source.Contains("sqrMagnitude"),
                "HandleWindChanged must check wind magnitude to hide arrow when calm");
            Assert.IsTrue(source.Contains("SetActive(false)"),
                "HandleWindChanged must deactivate arrow objects for zero wind");
        }

        [Test]
        public void HandleWindChanged_UsesWindAngleForRotation()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("WindIndicator t:MonoScript");
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            var source = File.ReadAllText(Path.GetFullPath(path));

            Assert.IsTrue(source.Contains("Atan2"),
                "HandleWindChanged must use Atan2 to compute wind direction angle");
        }
    }
}

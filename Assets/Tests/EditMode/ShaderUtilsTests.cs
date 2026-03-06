using NUnit.Framework;
using System.IO;

namespace GolfGame.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for ShaderUtils architectural invariants.
    /// </summary>
    public class ShaderUtilsTests
    {
        [Test]
        public void FindWithFallback_HasFallbackPath()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("ShaderUtils t:MonoScript");
            Assert.IsTrue(guids.Length > 0, "ShaderUtils script not found");

            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            var source = File.ReadAllText(Path.GetFullPath(path));

            // Verify the fallback pattern exists — the method must try the fallback shader
            Assert.IsTrue(source.Contains("Shader.Find(fallback)"),
                "FindWithFallback must attempt fallback shader when preferred is null");
        }
    }
}

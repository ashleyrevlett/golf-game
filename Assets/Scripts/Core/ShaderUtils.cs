using UnityEngine;

namespace GolfGame.Core
{
    /// <summary>
    /// Shader lookup with render-pipeline fallback.
    /// </summary>
    public static class ShaderUtils
    {
        public static Shader FindWithFallback(string preferred, string fallback)
        {
            var shader = Shader.Find(preferred);
            if (shader == null)
            {
                shader = Shader.Find(fallback);
            }
            return shader;
        }
    }
}

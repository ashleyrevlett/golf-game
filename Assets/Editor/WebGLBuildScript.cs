using UnityEditor;
using UnityEngine;
using System.Linq;

/// <summary>
/// CI build entry point called by GameCI unity-builder via buildMethod.
/// Sets nameFilesAsHashes before building to bust CDN caches on Cloudflare Pages.
/// </summary>
public static class WebGLBuildScript
{
    public static void Build()
    {
        // Enable content-hashed filenames — prevents stale cache on Cloudflare Pages
        PlayerSettings.WebGL.nameFilesAsHashes = true;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        Debug.Log("[WebGLBuildScript] nameFilesAsHashes=true, compression=Gzip, decompressionFallback=true");

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        Debug.Log($"[WebGLBuildScript] Scenes: {string.Join(", ", scenes)}");

        if (scenes.Length == 0)
        {
            Debug.LogError("[WebGLBuildScript] No enabled scenes in Build Settings!");
            EditorApplication.Exit(1);
            return;
        }

        // Read output path from GameCI env vars.
        // GameCI v4 sets BUILD_PATH (folder) and BUILD_FILE (name) separately;
        // CUSTOM_BUILD_PATH is the full path if set by older GameCI versions.
        string buildPath = System.Environment.GetEnvironmentVariable("CUSTOM_BUILD_PATH");
        if (string.IsNullOrEmpty(buildPath))
        {
            string basePath = System.Environment.GetEnvironmentVariable("BUILD_PATH") ?? "build/WebGL";
            string buildFile = System.Environment.GetEnvironmentVariable("BUILD_FILE") ?? "golf-game";
            buildPath = $"{basePath}/{buildFile}";
        }

        Debug.Log($"[WebGLBuildScript] Output: {buildPath}");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        var result = report.summary.result;
        Debug.Log($"[WebGLBuildScript] Result: {result}");

        EditorApplication.Exit(result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}

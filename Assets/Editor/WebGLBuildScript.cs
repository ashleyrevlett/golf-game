using UnityEditor;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// CI build script for WebGL. Called by GameCI unity-builder via buildMethod.
/// Enables hashed filenames to bust CDN/browser caches on every deploy.
/// </summary>
public static class WebGLBuildScript
{
    public static void Build()
    {
        string outputPath = Environment.GetEnvironmentVariable("BUILD_PATH") ?? "build/WebGL/golf-game";

        Debug.Log($"[WebGLBuildScript] Building to: {outputPath}");

        // Enable content-hashed filenames — prevents stale cache issues on Cloudflare Pages
        PlayerSettings.WebGL.nameFilesAsHashes = true;

        // Keep Brotli compression (set in Project Settings, just confirming here)
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;

        // WebGL memory
        PlayerSettings.WebGL.memorySize = 256;

        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[WebGLBuildScript] No scenes in Build Settings!");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[WebGLBuildScript] Scenes: {string.Join(", ", scenes)}");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        var result = report.summary.result;
        if (result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[WebGLBuildScript] Build succeeded ({report.summary.totalSize / 1024 / 1024}MB)");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[WebGLBuildScript] Build failed: {result}");
            EditorApplication.Exit(1);
        }
    }
}

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Editor.Build
{
    public static class BuildUWP
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/PersistentScene.unity",
            "Assets/Scenes/MenuScene.unity",
            "Assets/Scenes/Gameplay.unity",
            "Assets/Scenes/CalibrationScene.unity",
            "Assets/Scenes/ScoreScene.unity",
        };

        /// <summary>
        /// Called from CI via -executeMethod Editor.Build.BuildUWP.Build
        /// </summary>
        public static void Build()
        {
            // Use scenes from EditorBuildSettings if available, fall back to hardcoded list
            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                scenes = Scenes;
            }

            Debug.Log($"[BuildUWP] Building {scenes.Length} scenes for WSAPlayer");
            foreach (var scene in scenes)
            {
                Debug.Log($"  - {scene}");
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "build/WSAPlayer",
                target = BuildTarget.WSAPlayer,
                targetGroup = BuildTargetGroup.WSA,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);

            Debug.Log($"[BuildUWP] Build result: {report.summary.result}");
            Debug.Log($"[BuildUWP] Total time: {report.summary.totalTime}");
            Debug.Log($"[BuildUWP] Total size: {report.summary.totalSize} bytes");
            Debug.Log($"[BuildUWP] Total warnings: {report.summary.totalWarnings}");
            Debug.Log($"[BuildUWP] Total errors: {report.summary.totalErrors}");

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[BuildUWP] Build FAILED with result: {report.summary.result}");
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log("[BuildUWP] Build SUCCEEDED");
                EditorApplication.Exit(0);
            }
        }
    }
}

using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Linq;

namespace ClaudeBridge
{
    public static class ClaudeBuildManager
    {
        [UnityEditor.MenuItem("Tools/Claude/Build Linux64")]
        public static void BuildLinux64()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/Linux64");
            Directory.CreateDirectory(outDir);
            
            string productName = string.IsNullOrEmpty(PlayerSettings.productName) 
                ? "UnityGame" 
                : PlayerSettings.productName;
            
            string outPath = Path.Combine(outDir, productName);

            var scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
            if (scenes.Length == 0)
            {
                Debug.LogWarning("[CLAUDE-BUILD] Aucune scène active dans Build Settings");
                ClaudeCodeBridge.WriteJson("BuildLinux64", "error", new { 
                    message = "Aucune scène active. Ajoutez des scènes dans File > Build Settings"
                });
                return;
            }

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = scenes,
                target = BuildTarget.StandaloneLinux64,
                locationPathName = outPath,
                options = BuildOptions.StrictMode
            };

            Debug.Log($"[CLAUDE-BUILD] Démarrage build Linux64: {outPath}");
            var report = BuildPipeline.BuildPlayer(opts);
            
            var status = report.summary.result == BuildResult.Succeeded ? "success" : "error";
            
            ClaudeCodeBridge.WriteJson("BuildLinux64", status, new { 
                result = report.summary.result.ToString(), 
                sizeBytes = report.summary.totalSize,
                sizeMB = report.summary.totalSize / (1024f * 1024f),
                warnings = report.summary.totalWarnings,
                errors = report.summary.totalErrors,
                outputPath = outPath,
                scenes = scenes,
                duration = report.summary.totalTime.TotalSeconds
            });
        }

        [UnityEditor.MenuItem("Tools/Claude/Build Windows64")]
        public static void BuildWindows64()
        {
            string outDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds/Windows64");
            Directory.CreateDirectory(outDir);
            
            string productName = string.IsNullOrEmpty(PlayerSettings.productName) 
                ? "UnityGame" 
                : PlayerSettings.productName;
            
            string outPath = Path.Combine(outDir, $"{productName}.exe");

            BuildPlayerOptions opts = new BuildPlayerOptions
            {
                scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes),
                target = BuildTarget.StandaloneWindows64,
                locationPathName = outPath,
                options = BuildOptions.StrictMode
            };

            var report = BuildPipeline.BuildPlayer(opts);
            var status = report.summary.result == BuildResult.Succeeded ? "success" : "error";
            
            ClaudeCodeBridge.WriteJson("BuildWindows64", status, new { 
                result = report.summary.result.ToString(), 
                sizeBytes = report.summary.totalSize,
                outputPath = outPath
            });
        }
    }
}
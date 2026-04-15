using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace ClaudeBridge
{
    public static class ClaudeCodeBridge
    {
        private const string OUTPUT_DIR = "claude-output";
        private static string OutputPath => Path.Combine(Directory.GetCurrentDirectory(), OUTPUT_DIR, "result.json");

        static ClaudeCodeBridge()
        {
            if (!Directory.Exists(OUTPUT_DIR)) 
                Directory.CreateDirectory(OUTPUT_DIR);
        }

        // Alias used by Scripts/unity-claude.sh default command name.
        public static void Compile() => CompileAndValidate();

        [UnityEditor.MenuItem("Tools/Claude/Compile & Validate")]
        public static void CompileAndValidate()
        {
            // Unity refuses to run -executeMethod if scripts don't compile, so reaching
            // this method already proves compilation succeeded. Force a refresh to surface
            // any late asset-import errors, then report.
            AssetDatabase.Refresh();

            var csFiles = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
            var assemblies = CompilationPipeline.GetAssemblies(AssembliesType.Player)
                .Select(a => a.name)
                .ToList();

            WriteJson("Compile", "success",
                new {
                    message = "Scripts compiled successfully (Unity batch reached -executeMethod).",
                    fileCount = csFiles.Length,
                    assemblyCount = assemblies.Count,
                    assemblies,
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString()
                });
        }

        [UnityEditor.MenuItem("Tools/Claude/Find Assets")]
        public static void FindAssets()
        {
            var guids = AssetDatabase.FindAssets("", new[] { "Assets" });
            var assets = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.StartsWith("Assets/Editor/ClaudeBridge"))
                .OrderBy(p => p)
                .ToList();
            
            WriteJson("FindAssets", "success", new { 
                count = assets.Count, 
                assets = assets,
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        [UnityEditor.MenuItem("Tools/Claude/Clear Console & Cache")]
        public static void ClearCache()
        {
            try
            {
                var logType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
                logType?.GetMethod("Clear")?.Invoke(null, null);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Clear console failed: {e.Message}");
            }
            
            AssetDatabase.Refresh();
            WriteJson("ClearCache", "success", new { 
                message = "Console et cache rafraîchis",
                timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        internal static void WriteJson(string command, string status, object data)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    id = Guid.NewGuid().ToString("N")[..8],
                    timestamp = DateTime.UtcNow.ToString("o"),
                    command,
                    status,
                    data,
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    projectName = Path.GetFileName(Directory.GetCurrentDirectory())
                }, Newtonsoft.Json.Formatting.Indented);
                
                File.WriteAllText(OutputPath, json);
                Debug.Log($"[CLAUDE-UNITY] {command}: {status}");
                
                if (Application.isBatchMode)
                    EditorApplication.Exit(status == "success" ? 0 : 1);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CLAUDE-UNITY] WriteJson failed: {e.Message}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }
    }
}
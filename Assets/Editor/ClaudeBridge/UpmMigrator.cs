using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClaudeBridge
{
    public static class UpmMigrator
    {
        private static readonly Dictionary<string, string> LegacyToUpm = new()
        {
            { "Newtonsoft.Json", "com.unity.nuget.newtonsoft-json" },
            { "DOTween", "com.unity.demoteam.dotween" },
            { "TextMeshPro", "com.unity.textmeshpro" },
            { "InputSystem", "com.unity.inputsystem" },
            { "Addressables", "com.unity.addressables" },
            { "Cinemachine", "com.unity.cinemachine" },
            { "PostProcessing", "com.unity.postprocessing" },
            { "UniversalRenderPipeline", "com.unity.render-pipelines.universal" },
            { "Timeline", "com.unity.timeline" },
            { "VisualEffectGraph", "com.unity.visualeffectgraph" },
            { "ShaderGraph", "com.unity.shadergraph" }
        };

        [UnityEditor.MenuItem("Tools/Claude/Scan Legacy Packages")]
        public static void ScanLegacyPackages()
        {
            var report = new Dictionary<string, object>();
            var detected = new List<string>();
            var details = new Dictionary<string, object>();

            foreach (var kvp in LegacyToUpm)
            {
                var info = IsPackageDetected(kvp.Key);
                if (info.detected)
                {
                    detected.Add(kvp.Key);
                    details[kvp.Key] = new { 
                        upmPackage = kvp.Value,
                        locations = info.locations,
                        recommendation = $"Remplacer par {kvp.Value}"
                    };
                }
            }

            report["legacy_detected"] = detected;
            report["upm_available"] = detected.Select(k => LegacyToUpm[k]).ToList();
            report["details"] = details;
            report["timestamp"] = DateTime.UtcNow.ToString("o");
            report["unity_version"] = Application.unityVersion;

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented);
            var outPath = Path.Combine(Directory.GetCurrentDirectory(), "claude-output", "upm_scan.json");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, json);

            Debug.Log($"[CLAUDE-UPM] Scan terminé. {detected.Count} packages legacy détectés.");
            Debug.Log($"[CLAUDE-UPM] Rapport: {outPath}");
            
            if (Application.isBatchMode) 
                EditorApplication.Exit(0);
        }

        [UnityEditor.MenuItem("Tools/Claude/Auto Migrate to UPM")]
        public static void AutoMigrate()
        {
            var scanPath = Path.Combine(Directory.GetCurrentDirectory(), "claude-output", "upm_scan.json");
            if (!File.Exists(scanPath))
            {
                Debug.LogError("[CLAUDE-UPM] Lancez d'abord 'Scan Legacy Packages'");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            var report = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(scanPath));
            var legacy = ((Newtonsoft.Json.Linq.JArray)report["legacy_detected"]).ToObject<List<string>>();

            if (legacy.Count == 0)
            {
                Debug.Log("[CLAUDE-UPM] Aucun package à migrer.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
                return;
            }

            var addRequests = new List<AddRequest>();
            var packagesToAdd = new List<string>();
            
            foreach (var pkg in legacy)
            {
                var upmId = LegacyToUpm[pkg];
                packagesToAdd.Add(upmId);
                Debug.Log($"[CLAUDE-UPM] Installation : {upmId} (remplace {pkg})");
                addRequests.Add(Client.Add(upmId));
            }

            EditorApplication.update += () =>
            {
                if (addRequests.All(r => r.IsCompleted))
                {
                    var results = addRequests.Select((r, i) => new { 
                        package = packagesToAdd[i],
                        status = r.Status.ToString(),
                        error = r.Error?.message,
                        packageId = r.Result?.packageId
                    }).ToList();
                    
                    var finalReport = new { 
                        action = "AutoMigrate",
                        timestamp = DateTime.UtcNow.ToString("o"),
                        totalPackages = packagesToAdd.Count,
                        successful = results.Count(r => r.status == "Succeeded"),
                        failed = results.Count(r => r.status == "Failed"),
                        results
                    };
                    
                    var outPath = Path.Combine(Directory.GetCurrentDirectory(), "claude-output", "upm_migration.json");
                    File.WriteAllText(outPath, Newtonsoft.Json.JsonConvert.SerializeObject(finalReport, Newtonsoft.Json.Formatting.Indented));
                    
                    Debug.Log($"[CLAUDE-UPM] Migration terminée. {finalReport.successful}/{finalReport.totalPackages} réussis.");
                    Debug.Log($"[CLAUDE-UPM] Rapport: {outPath}");
                    
                    EditorApplication.update -= null;
                    if (Application.isBatchMode) EditorApplication.Exit(0);
                }
            };
        }

        private static (bool detected, string[] locations) IsPackageDetected(string legacyName)
        {
            var locations = new List<string>();
            
            // Chercher dans Assets/
            var assetFiles = Directory.GetFiles("Assets", "*", SearchOption.AllDirectories)
                .Where(f => f.Contains(legacyName, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToArray();
            locations.AddRange(assetFiles);

            // Chercher dans les scripts C#
            var csFiles = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
            var scriptRefs = csFiles
                .Where(f => {
                    try {
                        return File.ReadAllText(f).Contains($"using {legacyName}") || 
                               File.ReadAllText(f).Contains($"using {legacyName}.");
                    } catch { return false; }
                })
                .Take(5)
                .ToArray();
            locations.AddRange(scriptRefs);

            return (locations.Count > 0, locations.ToArray());
        }
    }
}
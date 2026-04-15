using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MirrorChronicles.Core;
using MirrorChronicles.Clan;
using MirrorChronicles.Characters;
using MirrorChronicles.Mirror;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;

namespace ClaudeBridge
{
    public static class ClaudeSceneBootstrapper
    {
        private const string ScenePath = "Assets/_Project/Scenes/ClanDomain.unity";
        private const string SceneDir = "Assets/_Project/Scenes";

        // Alias for Scripts/unity-claude.sh default command pattern.
        public static void BootstrapClanDomain() => BootstrapClanDomainScene();

        [MenuItem("Tools/Claude/Bootstrap ClanDomain Scene")]
        public static void BootstrapClanDomainScene()
        {
            if (!Directory.Exists(SceneDir))
                Directory.CreateDirectory(SceneDir);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var systems = new GameObject("[Systems]");

            // Core
            systems.AddComponent<GameManager>();
            systems.AddComponent<TimeManager>();
            systems.AddComponent<SaveSystem>();

            // Clan
            systems.AddComponent<ClanManager>();
            systems.AddComponent<BloodRegistry>();
            systems.AddComponent<LegacySystem>();

            // Characters (all)
            systems.AddComponent<CultivationSystem>();
            systems.AddComponent<BreakthroughSystem>();
            systems.AddComponent<AgingAndDeathSystem>();
            systems.AddComponent<WoundSystem>();
            systems.AddComponent<AscensionSystem>();
            systems.AddComponent<MentalStabilitySystem>();

            // Mirror
            systems.AddComponent<MirrorSystem>();
            systems.AddComponent<DeductionEngine>();

            // Diplomacy
            systems.AddComponent<FactionManager>();
            systems.AddComponent<MarriageSystem>();
            systems.AddComponent<AllianceSystem>();

            // Economy
            systems.AddComponent<ResourceManager>();
            systems.AddComponent<TaskAssignmentSystem>();

            // Events
            systems.AddComponent<EventManager>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);

            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(s => s.path == ScenePath))
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var componentCount = systems.GetComponents<Component>().Length - 1; // minus Transform

            ClaudeCodeBridge.WriteJson("BootstrapClanDomain", saved ? "success" : "error", new
            {
                scenePath = ScenePath,
                rootObject = "[Systems]",
                systemCount = componentCount,
                sceneSaved = saved,
                addedToBuildSettings = true
            });
        }
    }
}

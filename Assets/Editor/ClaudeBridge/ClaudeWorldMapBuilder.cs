using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using MirrorChronicles.UI;

namespace ClaudeBridge
{
    public static class ClaudeWorldMapBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/WorldMap.unity";
        private const string SceneDir = "Assets/_Project/Scenes";

        public static void BuildWorldMap() => BuildWorldMapScene();

        [MenuItem("Tools/Claude/Build WorldMap Scene")]
        public static void BuildWorldMapScene()
        {
            if (!Directory.Exists(SceneDir))
                Directory.CreateDirectory(SceneDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var canvasGO = new GameObject("[WorldMapUI]",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
                typeof(WorldMapUI));

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            BuildTitle(canvasGO.transform);
            BuildMapArea(canvasGO.transform);
            BuildDiplomacyPanel(canvasGO.transform);
            BuildBackButton(canvasGO.transform);

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(EventSystem), typeof(InputSystemUIInputModule));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);

            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (!buildScenes.Any(s => s.path == ScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ClaudeCodeBridge.WriteJson("BuildWorldMap", saved ? "success" : "error", new
            {
                scenePath = ScenePath,
                sceneSaved = saved
            });
        }

        private static void BuildTitle(Transform parent)
        {
            var title = new GameObject("Title", typeof(RectTransform));
            title.transform.SetParent(parent, false);
            var rt = (RectTransform)title.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 80);

            var bg = title.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.08f, 0.9f);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(title.transform, false);
            var lrt = (RectTransform)labelGO.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "World Map — The Celestial Domains";
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.9f, 0.8f, 0.5f);
        }

        private static void BuildMapArea(Transform parent)
        {
            var mapArea = new GameObject("MapArea", typeof(RectTransform), typeof(Image));
            mapArea.transform.SetParent(parent, false);
            var rt = (RectTransform)mapArea.transform;
            rt.anchorMin = new Vector2(0.05f, 0.1f);
            rt.anchorMax = new Vector2(0.6f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            mapArea.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.08f, 0.9f);

            // Faction container centered in map area
            var container = new GameObject("FactionContainer", typeof(RectTransform));
            container.transform.SetParent(mapArea.transform, false);
            var crt = (RectTransform)container.transform;
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(600, 600);

            // Faction marker template
            var marker = new GameObject("FactionMarkerTemplate",
                typeof(RectTransform), typeof(Image), typeof(Button));
            marker.transform.SetParent(container.transform, false);
            var mrt = (RectTransform)marker.transform;
            mrt.sizeDelta = new Vector2(120, 50);
            marker.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.3f, 1f);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(marker.transform, false);
            var lrt = (RectTransform)labelGO.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(5, 0);
            lrt.offsetMax = new Vector2(-5, 0);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Faction";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;

            marker.SetActive(false);
        }

        private static void BuildDiplomacyPanel(Transform parent)
        {
            var panel = new GameObject("DiplomacyPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rt = (RectTransform)panel.transform;
            rt.anchorMin = new Vector2(0.62f, 0.1f);
            rt.anchorMax = new Vector2(0.95f, 0.9f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.95f);

            float yOffset = -20f;

            // Faction name
            CreateLabel(panel.transform, "FactionName", "Faction Name", 32,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, yOffset), new Vector2(-20, yOffset - 50));
            yOffset -= 60f;

            // Faction info
            CreateLabel(panel.transform, "FactionInfo", "Type: ???\nPower: 0\nWealth: 0", 20,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, yOffset), new Vector2(-20, yOffset - 80));
            yOffset -= 90f;

            // Relation text
            CreateLabel(panel.transform, "RelationText", "Relation: 0 (Neutral)", 24,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, yOffset), new Vector2(-20, yOffset - 40));
            yOffset -= 60f;

            // Action buttons
            CreateButton(panel.transform, "TributeButton", "Offer Tribute (100 stones)", yOffset);
            yOffset -= 55f;
            CreateButton(panel.transform, "PactButton", "Non-Aggression Pact", yOffset);
            yOffset -= 55f;
            CreateButton(panel.transform, "SpyButton", "Send Spy", yOffset);
            yOffset -= 55f;
            CreateButton(panel.transform, "WarButton", "Declare War", yOffset, new Color(0.8f, 0.2f, 0.2f));
            yOffset -= 70f;
            CreateButton(panel.transform, "CloseButton", "Close", yOffset, new Color(0.4f, 0.4f, 0.4f));

            panel.SetActive(false);
        }

        private static void BuildBackButton(Transform parent)
        {
            var btnGO = new GameObject("BackButton",
                typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGO.transform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(200, 50);
            rt.anchoredPosition = new Vector2(20, 20);
            btnGO.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(btnGO.transform, false);
            var lrt = (RectTransform)labelGO.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "Back to Domain";
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private static void CreateLabel(Transform parent, string name, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0, 1);
            rt.offsetMin = new Vector2(offsetMin.x, offsetMax.y);
            rt.offsetMax = new Vector2(offsetMax.x, offsetMin.y);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.color = Color.white;
        }

        private static void CreateButton(Transform parent, string name, string text,
            float yOffset, Color? color = null)
        {
            var btnGO = new GameObject(name,
                typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(parent, false);
            var rt = (RectTransform)btnGO.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(-40, 45);
            rt.anchoredPosition = new Vector2(0, yOffset);
            btnGO.GetComponent<Image>().color = color ?? new Color(0.6f, 0.4f, 0.15f, 1f);

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(btnGO.transform, false);
            var lrt = (RectTransform)labelGO.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }
    }
}

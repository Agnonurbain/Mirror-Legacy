using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using MirrorChronicles.UI;

namespace ClaudeBridge
{
    public static class ClaudeUIBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/ClanDomain.unity";
        private const string TmpEssentialsMarker = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string TmpEssentialsPackage =
            "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        // Aliases for Scripts/unity-claude.sh routing.
        public static void BuildUIPlaceholder() => BuildClanDomainUI();
        public static void ImportTMPEssentials() => ImportTMPEssentialsAsync();

        [MenuItem("Tools/Claude/Import TMP Essentials")]
        public static void ImportTMPEssentialsAsync()
        {
            if (File.Exists(TmpEssentialsMarker))
            {
                ClaudeCodeBridge.WriteJson("ImportTMPEssentials", "success", new
                {
                    alreadyImported = true,
                    marker = TmpEssentialsMarker
                });
                return;
            }

            if (!File.Exists(TmpEssentialsPackage))
            {
                ClaudeCodeBridge.WriteJson("ImportTMPEssentials", "error", new
                {
                    reason = "Package not found",
                    expected = TmpEssentialsPackage
                });
                return;
            }

            AssetDatabase.importPackageCompleted += OnTmpImportCompleted;
            AssetDatabase.importPackageFailed += OnTmpImportFailed;
            AssetDatabase.importPackageCancelled += OnTmpImportCancelled;

            AssetDatabase.ImportPackage(TmpEssentialsPackage, false);
            Debug.Log("[CLAUDE-UI] Importing TMP Essentials asynchronously…");
        }

        private static void OnTmpImportCompleted(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImportCompleted;
            AssetDatabase.importPackageFailed -= OnTmpImportFailed;
            AssetDatabase.importPackageCancelled -= OnTmpImportCancelled;

            AssetDatabase.Refresh();
            ClaudeCodeBridge.WriteJson("ImportTMPEssentials", "success", new
            {
                packageName,
                imported = File.Exists(TmpEssentialsMarker)
            });
        }

        private static void OnTmpImportFailed(string packageName, string errorMessage)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImportCompleted;
            AssetDatabase.importPackageFailed -= OnTmpImportFailed;
            AssetDatabase.importPackageCancelled -= OnTmpImportCancelled;

            ClaudeCodeBridge.WriteJson("ImportTMPEssentials", "error", new
            {
                packageName,
                errorMessage
            });
        }

        private static void OnTmpImportCancelled(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImportCompleted;
            AssetDatabase.importPackageFailed -= OnTmpImportFailed;
            AssetDatabase.importPackageCancelled -= OnTmpImportCancelled;

            ClaudeCodeBridge.WriteJson("ImportTMPEssentials", "error", new
            {
                packageName,
                cancelled = true
            });
        }

        [MenuItem("Tools/Claude/Build UI Placeholder")]
        public static void BuildClanDomainUI()
        {
            if (!File.Exists(TmpEssentialsMarker))
            {
                ClaudeCodeBridge.WriteJson("BuildUIPlaceholder", "error", new
                {
                    reason = "TMP Essentials not imported. Run ImportTMPEssentials first.",
                    marker = TmpEssentialsMarker
                });
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Clean any previous UI so the command is idempotent.
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "[UI]" || root.name == "EventSystem")
                    Object.DestroyImmediate(root);
            }

            var canvasGO = new GameObject("[UI]",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ClanDomainUI));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            BuildHeader(canvasGO.transform);
            BuildFooter(canvasGO.transform);
            BuildMembersScrollView(canvasGO.transform);

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem",
                    typeof(EventSystem), typeof(StandaloneInputModule));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ClaudeCodeBridge.WriteJson("BuildUIPlaceholder", saved ? "success" : "error", new
            {
                scenePath = ScenePath,
                uiRoot = "[UI]",
                controller = "ClanDomainUI",
                tmpEssentialsImported = File.Exists(TmpEssentialsMarker),
                sceneSaved = saved
            });
        }

        private static void BuildHeader(Transform parent)
        {
            var header = new GameObject("Header", typeof(RectTransform));
            header.transform.SetParent(parent, false);
            var rt = (RectTransform)header.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 140);
            rt.anchoredPosition = Vector2.zero;

            var bg = header.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

            CreateTmpLabel(header.transform, "YearText",
                new Vector2(30, -20), new Vector2(520, 50), "Year 1",
                fontSize: 44, alignment: TextAlignmentOptions.Left);

            CreateTmpLabel(header.transform, "PhaseText",
                new Vector2(30, -80), new Vector2(520, 40), "Phase: Management",
                fontSize: 28, alignment: TextAlignmentOptions.Left);

            CreateTmpLabel(header.transform, "SpiritStonesText",
                new Vector2(-30, -20), new Vector2(520, 50), "Spirit Stones: 0",
                fontSize: 32, alignment: TextAlignmentOptions.Right,
                anchorMin: new Vector2(1, 1), anchorMax: new Vector2(1, 1),
                pivot: new Vector2(1, 1));
        }

        private static void BuildFooter(Transform parent)
        {
            var footer = new GameObject("Footer", typeof(RectTransform));
            footer.transform.SetParent(parent, false);
            var rt = (RectTransform)footer.transform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, 120);
            rt.anchoredPosition = Vector2.zero;

            var bg = footer.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);

            var buttonGO = new GameObject("EndTurnButton",
                typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(footer.transform, false);
            var brt = (RectTransform)buttonGO.transform;
            brt.anchorMin = new Vector2(1, 0.5f);
            brt.anchorMax = new Vector2(1, 0.5f);
            brt.pivot = new Vector2(1, 0.5f);
            brt.sizeDelta = new Vector2(260, 70);
            brt.anchoredPosition = new Vector2(-30, 0);

            buttonGO.GetComponent<Image>().color = new Color(0.75f, 0.45f, 0.15f, 1f);

            CreateTmpLabel(buttonGO.transform, "Label",
                Vector2.zero, Vector2.zero, "End Turn",
                fontSize: 32, alignment: TextAlignmentOptions.Center,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                pivot: new Vector2(0.5f, 0.5f),
                stretchToParent: true);
        }

        private static void BuildMembersScrollView(Transform parent)
        {
            var scroll = new GameObject("MembersScrollView",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            scroll.transform.SetParent(parent, false);
            var srt = (RectTransform)scroll.transform;
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 1);
            srt.offsetMin = new Vector2(30, 140);
            srt.offsetMax = new Vector2(-30, -160);

            scroll.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.15f, 0.6f);
            var scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scroll.transform, false);
            var vrt = (RectTransform)viewport.transform;
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = (RectTransform)content.transform;
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(0, 0);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vrt;
            scrollRect.content = crt;

            var rowTemplate = new GameObject("MemberRowTemplate",
                typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            rowTemplate.transform.SetParent(content.transform, false);
            rowTemplate.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            rowTemplate.GetComponent<LayoutElement>().minHeight = 44;

            CreateTmpLabel(rowTemplate.transform, "Label",
                Vector2.zero, Vector2.zero, "Member placeholder",
                fontSize: 22, alignment: TextAlignmentOptions.MidlineLeft,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                pivot: new Vector2(0.5f, 0.5f),
                stretchToParent: true, paddingLeft: 20);
        }

        private static void CreateTmpLabel(
            Transform parent, string name,
            Vector2 anchoredPosition, Vector2 sizeDelta,
            string text, int fontSize,
            TextAlignmentOptions alignment,
            Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null,
            bool stretchToParent = false, float paddingLeft = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;

            rt.anchorMin = anchorMin ?? new Vector2(0, 1);
            rt.anchorMax = anchorMax ?? new Vector2(0, 1);
            rt.pivot = pivot ?? new Vector2(0, 1);

            if (stretchToParent)
            {
                rt.offsetMin = new Vector2(paddingLeft, 0);
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                rt.sizeDelta = sizeDelta;
                rt.anchoredPosition = anchoredPosition;
            }

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
        }
    }
}

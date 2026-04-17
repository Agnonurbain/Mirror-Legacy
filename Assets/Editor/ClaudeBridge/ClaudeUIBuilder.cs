using System.IO;
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
                    typeof(EventSystem), typeof(InputSystemUIInputModule));
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
                typeof(RectTransform), typeof(Image), typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));
            rowTemplate.transform.SetParent(content.transform, false);
            rowTemplate.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.8f);
            rowTemplate.GetComponent<LayoutElement>().minHeight = 48;

            var rowLayout = rowTemplate.GetComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(20, 10, 4, 4);
            rowLayout.spacing = 10;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            // Label (takes remaining space)
            var labelGO = new GameObject("Label",
                typeof(RectTransform), typeof(LayoutElement));
            labelGO.transform.SetParent(rowTemplate.transform, false);
            labelGO.GetComponent<LayoutElement>().flexibleWidth = 1;
            var labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "Member placeholder";
            labelTmp.fontSize = 22;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.color = Color.white;

            // Task dropdown (fixed width)
            BuildTaskDropdown(rowTemplate.transform);
        }

        private static void BuildTaskDropdown(Transform parent)
        {
            // Container
            var ddGO = new GameObject("TaskDropdown",
                typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown), typeof(LayoutElement));
            ddGO.transform.SetParent(parent, false);
            ddGO.GetComponent<LayoutElement>().minWidth = 200;
            ddGO.GetComponent<LayoutElement>().preferredWidth = 200;
            ddGO.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Label inside the dropdown (shows selected value)
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(ddGO.transform, false);
            var lrt = (RectTransform)labelGO.transform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(10, 0);
            lrt.offsetMax = new Vector2(-25, 0);
            var lbl = labelGO.AddComponent<TextMeshProUGUI>();
            lbl.text = "None";
            lbl.fontSize = 20;
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            lbl.color = Color.white;

            // Arrow indicator
            var arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowGO.transform.SetParent(ddGO.transform, false);
            var art = (RectTransform)arrowGO.transform;
            art.anchorMin = new Vector2(1, 0.5f);
            art.anchorMax = new Vector2(1, 0.5f);
            art.pivot = new Vector2(1, 0.5f);
            art.sizeDelta = new Vector2(16, 16);
            art.anchoredPosition = new Vector2(-6, 0);
            arrowGO.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f, 1f);

            // Scrollable dropdown template
            var templateGO = new GameObject("Template",
                typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            templateGO.transform.SetParent(ddGO.transform, false);
            var trt = (RectTransform)templateGO.transform;
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(1, 0);
            trt.pivot = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(0, 200);
            trt.anchoredPosition = Vector2.zero;
            templateGO.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 1f);
            templateGO.SetActive(false);

            var viewportGO = new GameObject("Viewport",
                typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGO.transform.SetParent(templateGO.transform, false);
            var vprt = (RectTransform)viewportGO.transform;
            vprt.anchorMin = Vector2.zero;
            vprt.anchorMax = Vector2.one;
            vprt.offsetMin = Vector2.zero;
            vprt.offsetMax = Vector2.zero;
            viewportGO.GetComponent<Image>().color = Color.white;
            viewportGO.GetComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var crt = (RectTransform)contentGO.transform;
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.sizeDelta = new Vector2(0, 0);

            var scrollRect = templateGO.GetComponent<ScrollRect>();
            scrollRect.viewport = vprt;
            scrollRect.content = crt;
            scrollRect.horizontal = false;

            // Item template inside the dropdown
            var itemGO = new GameObject("Item",
                typeof(RectTransform), typeof(Image), typeof(Toggle));
            itemGO.transform.SetParent(contentGO.transform, false);
            var irt = (RectTransform)itemGO.transform;
            irt.sizeDelta = new Vector2(0, 36);
            itemGO.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);

            var itemLabelGO = new GameObject("Item Label", typeof(RectTransform));
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            var ilrt = (RectTransform)itemLabelGO.transform;
            ilrt.anchorMin = Vector2.zero;
            ilrt.anchorMax = Vector2.one;
            ilrt.offsetMin = new Vector2(10, 0);
            ilrt.offsetMax = Vector2.zero;
            var itemLabel = itemLabelGO.AddComponent<TextMeshProUGUI>();
            itemLabel.text = "Option";
            itemLabel.fontSize = 20;
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.color = Color.white;

            // Wire dropdown references
            var dropdown = ddGO.GetComponent<TMP_Dropdown>();
            dropdown.captionText = lbl;
            dropdown.template = trt;
            dropdown.itemText = itemLabel;

            // Toggle needs a target graphic for proper UI behavior
            var toggle = itemGO.GetComponent<Toggle>();
            toggle.targetGraphic = itemGO.GetComponent<Image>();
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

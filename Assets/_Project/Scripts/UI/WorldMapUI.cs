using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;

namespace MirrorChronicles.UI
{
    /// <summary>
    /// Controls the WorldMap scene UI. Displays factions on a map with
    /// clickable territories. Clicking a faction opens a diplomacy panel.
    /// </summary>
    public class WorldMapUI : MonoBehaviour
    {
        private Transform factionContainer;
        private GameObject factionMarkerTemplate;
        private GameObject diplomacyPanel;

        private TMP_Text factionNameText;
        private TMP_Text factionInfoText;
        private TMP_Text relationText;
        private Button tributeButton;
        private Button pactButton;
        private Button spyButton;
        private Button warButton;
        private Button closeButton;
        private Button backButton;

        private FactionData _selectedFaction;
        private readonly List<GameObject> spawnedMarkers = new List<GameObject>();

        private void Awake()
        {
            factionContainer = transform.Find("MapArea/FactionContainer");
            factionMarkerTemplate = factionContainer?.GetChild(0)?.gameObject;
            if (factionMarkerTemplate != null)
                factionMarkerTemplate.SetActive(false);

            diplomacyPanel = transform.Find("DiplomacyPanel")?.gameObject;
            if (diplomacyPanel != null)
            {
                factionNameText = diplomacyPanel.transform.Find("FactionName")?.GetComponent<TMP_Text>();
                factionInfoText = diplomacyPanel.transform.Find("FactionInfo")?.GetComponent<TMP_Text>();
                relationText = diplomacyPanel.transform.Find("RelationText")?.GetComponent<TMP_Text>();
                tributeButton = diplomacyPanel.transform.Find("TributeButton")?.GetComponent<Button>();
                pactButton = diplomacyPanel.transform.Find("PactButton")?.GetComponent<Button>();
                spyButton = diplomacyPanel.transform.Find("SpyButton")?.GetComponent<Button>();
                warButton = diplomacyPanel.transform.Find("WarButton")?.GetComponent<Button>();
                closeButton = diplomacyPanel.transform.Find("CloseButton")?.GetComponent<Button>();
                diplomacyPanel.SetActive(false);
            }

            backButton = transform.Find("BackButton")?.GetComponent<Button>();
        }

        private void OnEnable()
        {
            tributeButton?.onClick.AddListener(OnTributeClicked);
            pactButton?.onClick.AddListener(OnPactClicked);
            spyButton?.onClick.AddListener(OnSpyClicked);
            warButton?.onClick.AddListener(OnWarClicked);
            closeButton?.onClick.AddListener(CloseDiplomacyPanel);
            backButton?.onClick.AddListener(OnBackClicked);
        }

        private void OnDisable()
        {
            tributeButton?.onClick.RemoveListener(OnTributeClicked);
            pactButton?.onClick.RemoveListener(OnPactClicked);
            spyButton?.onClick.RemoveListener(OnSpyClicked);
            warButton?.onClick.RemoveListener(OnWarClicked);
            closeButton?.onClick.RemoveListener(CloseDiplomacyPanel);
            backButton?.onClick.RemoveListener(OnBackClicked);
        }

        private void Start()
        {
            PopulateFactions();
        }

        private void PopulateFactions()
        {
            foreach (var m in spawnedMarkers) Destroy(m);
            spawnedMarkers.Clear();

            if (FactionManager.Instance == null) return;

            var factions = FactionManager.Instance.Factions;
            for (int i = 0; i < factions.Count; i++)
            {
                var faction = factions[i];
                var marker = Instantiate(factionMarkerTemplate, factionContainer);
                marker.SetActive(true);

                var label = marker.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = faction.Name;

                // Position markers in a circle on the map
                float angle = (2f * Mathf.PI * i) / factions.Count;
                float radius = 280f;
                var rt = (RectTransform)marker.transform;
                rt.anchoredPosition = new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );

                // Color by relation
                var img = marker.GetComponent<Image>();
                if (img != null)
                {
                    img.color = GetRelationColor(faction.RelationWithPlayer);
                }

                var btn = marker.GetComponent<Button>();
                if (btn != null)
                {
                    var capturedFaction = faction;
                    btn.onClick.AddListener(() => OpenDiplomacyPanel(capturedFaction));
                }

                spawnedMarkers.Add(marker);
            }
        }

        private Color GetRelationColor(int relation)
        {
            if (relation >= 50) return new Color(0.2f, 0.8f, 0.3f, 1f);
            if (relation >= 0) return new Color(0.8f, 0.8f, 0.3f, 1f);
            if (relation >= -50) return new Color(0.9f, 0.5f, 0.2f, 1f);
            return new Color(0.9f, 0.2f, 0.2f, 1f);
        }

        private void OpenDiplomacyPanel(FactionData faction)
        {
            _selectedFaction = faction;
            if (diplomacyPanel == null) return;

            diplomacyPanel.SetActive(true);

            if (factionNameText != null) factionNameText.text = faction.Name;
            if (factionInfoText != null)
                factionInfoText.text = $"Personality: {faction.Personality}\nPower: {faction.PowerLevel}\nWealth: {faction.Wealth}";
            if (relationText != null)
            {
                string mood = faction.RelationWithPlayer >= 50 ? "Friendly" :
                              faction.RelationWithPlayer >= 0 ? "Neutral" :
                              faction.RelationWithPlayer >= -50 ? "Hostile" : "At War";
                relationText.text = $"Relation: {faction.RelationWithPlayer} ({mood})";
            }
        }

        private void CloseDiplomacyPanel()
        {
            diplomacyPanel?.SetActive(false);
            _selectedFaction = null;
            PopulateFactions();
        }

        private void OnTributeClicked()
        {
            if (_selectedFaction == null) return;
            AllianceSystem.Instance?.OfferTribute(_selectedFaction.ID, 100);
            RefreshDiplomacyPanel();
        }

        private void OnPactClicked()
        {
            if (_selectedFaction == null) return;
            AllianceSystem.Instance?.ProposeNonAggression(_selectedFaction.ID);
            RefreshDiplomacyPanel();
        }

        private void OnSpyClicked()
        {
            if (_selectedFaction == null || EspionageSystem.Instance == null) return;
            var patriarch = Clan.ClanManager.Instance?.GetPatriarch();
            if (patriarch != null)
                EspionageSystem.Instance.AttemptEspionage(patriarch, _selectedFaction);
            RefreshDiplomacyPanel();
        }

        private void OnWarClicked()
        {
            if (_selectedFaction == null) return;
            AllianceSystem.Instance?.DeclareWar(_selectedFaction.ID);
            RefreshDiplomacyPanel();
        }

        private void RefreshDiplomacyPanel()
        {
            if (_selectedFaction != null)
                OpenDiplomacyPanel(_selectedFaction);
        }

        private void OnBackClicked()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("ClanDomain");
        }
    }
}

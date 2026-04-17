using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MirrorChronicles.Clan;
using MirrorChronicles.Core;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;

namespace MirrorChronicles.UI
{
    /// <summary>
    /// Phase 1 placeholder UI controller. Finds its children by name so the scene
    /// can be generated from the BuildUIPlaceholder bridge command without having
    /// to persist serialized references.
    /// </summary>
    public class ClanDomainUI : MonoBehaviour
    {
        private TMP_Text yearText;
        private TMP_Text phaseText;
        private TMP_Text spiritStonesText;
        private Button endTurnButton;
        private Transform membersContent;
        private GameObject memberRowTemplate;

        private readonly List<GameObject> spawnedRows = new List<GameObject>();

        // Task options filtered per realm level.
        private static readonly TaskType[] EmbryonicTasks = { TaskType.None, TaskType.Cultivation, TaskType.Rest };
        private static readonly TaskType[] FullTasks = (TaskType[])Enum.GetValues(typeof(TaskType));

        private void Awake()
        {
            yearText = transform.Find("Header/YearText").GetComponent<TMP_Text>();
            phaseText = transform.Find("Header/PhaseText").GetComponent<TMP_Text>();
            spiritStonesText = transform.Find("Header/SpiritStonesText").GetComponent<TMP_Text>();
            endTurnButton = transform.Find("Footer/EndTurnButton").GetComponent<Button>();

            membersContent = transform.Find("MembersScrollView/Viewport/Content");
            memberRowTemplate = membersContent.GetChild(0).gameObject;
            memberRowTemplate.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnYearStarted += HandleYearStarted;
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
            GameEvents.OnSpiritStonesChanged += HandleSpiritStonesChanged;
            GameEvents.OnCharacterBorn += HandleRosterChanged;
            GameEvents.OnCharacterDied += HandleCharacterDied;
            endTurnButton.onClick.AddListener(OnEndTurnClicked);
        }

        private void OnDisable()
        {
            GameEvents.OnYearStarted -= HandleYearStarted;
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
            GameEvents.OnSpiritStonesChanged -= HandleSpiritStonesChanged;
            GameEvents.OnCharacterBorn -= HandleRosterChanged;
            GameEvents.OnCharacterDied -= HandleCharacterDied;
            endTurnButton.onClick.RemoveListener(OnEndTurnClicked);
        }

        private void Start()
        {
            RefreshAll();
        }

        private void OnEndTurnClicked()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.AdvancePhase();
        }

        private void HandleYearStarted(int year) => RefreshAll();
        private void HandlePhaseChanged(GamePhase phase) => RefreshAll();
        private void HandleSpiritStonesChanged(int stones) => RefreshHeader();
        private void HandleRosterChanged(CharacterData _) => RefreshMembers();
        private void HandleCharacterDied(CharacterData c, DeathCause _) => RefreshMembers();

        private void RefreshAll()
        {
            RefreshHeader();
            RefreshMembers();
        }

        private void RefreshHeader()
        {
            if (TimeManager.Instance != null)
            {
                yearText.text = $"Year {TimeManager.Instance.CurrentYear}";
                phaseText.text = $"Phase: {TimeManager.Instance.CurrentPhase}";
            }

            if (ResourceManager.Instance != null)
                spiritStonesText.text = $"Spirit Stones: {ResourceManager.Instance.SpiritStones}";
        }

        private void RefreshMembers()
        {
            foreach (var row in spawnedRows)
                Destroy(row);
            spawnedRows.Clear();

            if (ClanManager.Instance == null) return;

            foreach (var member in ClanManager.Instance.LivingMembers)
            {
                var row = Instantiate(memberRowTemplate, membersContent);
                row.SetActive(true);

                // Label (left)
                var label = row.transform.Find("Label")?.GetComponent<TMP_Text>();
                if (label != null)
                    label.text = $"{member.FullName}  ·  {member.Age}y  ·  {member.Realm}  ·  MS {member.MentalStability}";

                // Task dropdown (right)
                var dropdown = row.GetComponentInChildren<TMP_Dropdown>();
                if (dropdown != null)
                    SetupTaskDropdown(dropdown, member);

                spawnedRows.Add(row);
            }
        }

        private void SetupTaskDropdown(TMP_Dropdown dropdown, CharacterData member)
        {
            TaskType[] available = member.Realm == CultivationRealm.Embryonic ? EmbryonicTasks : FullTasks;
            var options = available.Select(t => new TMP_Dropdown.OptionData(t.ToString())).ToList();

            dropdown.ClearOptions();
            dropdown.AddOptions(options);

            // Set current selection
            int idx = Array.IndexOf(available, member.CurrentTask);
            dropdown.SetValueWithoutNotify(idx >= 0 ? idx : 0);

            // Wire callback (capture member + available by value)
            var capturedMember = member;
            var capturedTasks = available;
            dropdown.onValueChanged.AddListener(index =>
            {
                if (TaskAssignmentSystem.Instance != null && index >= 0 && index < capturedTasks.Length)
                    TaskAssignmentSystem.Instance.AssignTask(capturedMember, capturedTasks[index]);
            });
        }
    }
}

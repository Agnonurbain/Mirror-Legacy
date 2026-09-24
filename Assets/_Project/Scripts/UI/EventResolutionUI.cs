using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.UI
{
    /// <summary>
    /// Modal overlay that displays a story event and its choices.
    /// Blocks TimeManager.AdvancePhase until the player resolves the event.
    /// </summary>
    public class EventResolutionUI : MonoBehaviour
    {
        public static EventResolutionUI Instance { get; private set; }

        private GameObject panel;
        private TMP_Text titleText;
        private TMP_Text narrativeText;
        private Transform choicesContainer;
        private GameObject choiceButtonTemplate;

        private readonly List<GameObject> spawnedButtons = new List<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            panel = transform.Find("EventPanel")?.gameObject;
            titleText = transform.Find("EventPanel/TitleText")?.GetComponent<TMP_Text>();
            narrativeText = transform.Find("EventPanel/NarrativeText")?.GetComponent<TMP_Text>();
            choicesContainer = transform.Find("EventPanel/ChoicesContainer");

            if (choicesContainer != null && choicesContainer.childCount > 0)
            {
                choiceButtonTemplate = choicesContainer.GetChild(0).gameObject;
                choiceButtonTemplate.SetActive(false);
            }

            if (panel != null)
                panel.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase != GamePhase.Events) return;

            // Check for pending story event after a short delay so other systems process first
            Invoke(nameof(CheckForPendingEvent), 0.1f);
        }

        private void CheckForPendingEvent()
        {
            if (StoryEventManager.Instance != null && StoryEventManager.Instance.PendingEvent != null)
                ShowEvent(StoryEventManager.Instance.PendingEvent);
        }

        public void ShowEvent(StoryEventData eventData)
        {
            if (panel == null || eventData == null) return;

            panel.SetActive(true);

            if (titleText != null)
                titleText.text = eventData.EventName;

            if (narrativeText != null)
                narrativeText.text = eventData.NarrativeText;

            ClearChoices();

            for (int i = 0; i < eventData.Choices.Count; i++)
            {
                var choice = eventData.Choices[i];
                int choiceIndex = i;

                var btnObj = Instantiate(choiceButtonTemplate, choicesContainer);
                btnObj.SetActive(true);

                var label = btnObj.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = choice.Label;

                var button = btnObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnChoiceSelected(choiceIndex));
                }

                spawnedButtons.Add(btnObj);
            }
        }

        private void OnChoiceSelected(int index)
        {
            if (StoryEventManager.Instance != null)
                StoryEventManager.Instance.ResolveChoice(index);

            Hide();
        }

        public void Hide()
        {
            ClearChoices();
            if (panel != null)
                panel.SetActive(false);
        }

        private void ClearChoices()
        {
            foreach (var btn in spawnedButtons)
                Destroy(btn);
            spawnedButtons.Clear();
        }

        public bool IsShowing => panel != null && panel.activeSelf;
    }
}

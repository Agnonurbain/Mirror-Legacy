using System.Collections.Generic;
using UnityEngine;
using MirrorChronicles.Data;
using MirrorChronicles.Clan;
using MirrorChronicles.Characters;
using MirrorChronicles.Economy;
using MirrorChronicles.Core;
using MirrorChronicles.Mirror;

namespace MirrorChronicles.Events
{
    /// <summary>
    /// Handles the random and scripted events during the Event Phase.
    /// Uses a weighted table of RandomEventData ScriptableObjects.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        [Header("Event Table")]
        public List<RandomEventData> EventTable = new List<RandomEventData>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (EventTable.Count == 0)
                BuildDefaultEventTable();
        }

        private void OnEnable()
        {
            GameEvents.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void HandlePhaseChanged(GamePhase newPhase)
        {
            if (newPhase == GamePhase.Events)
            {
                TriggerYearlyEvent();
            }
        }

        public void TriggerYearlyEvent()
        {
            Debug.Log("[EventManager] --- Rolling Yearly Event ---");

            var eligible = GetEligibleEvents();
            if (eligible.Count == 0)
            {
                Debug.Log("[EventManager] No eligible events. A peaceful year.");
                return;
            }

            var selected = WeightedRandom(eligible);
            ExecuteEvent(selected);
        }

        private List<RandomEventData> GetEligibleEvents()
        {
            var result = new List<RandomEventData>();
            int currentYear = TimeManager.Instance != null ? TimeManager.Instance.CurrentYear : 1;

            foreach (var evt in EventTable)
            {
                if (evt == null) continue;
                if (currentYear < evt.MinYear) continue;

                if (evt.MinPatriarchRealm > CultivationRealm.Embryonic)
                {
                    var members = ClanManager.Instance?.LivingMembers;
                    if (members == null || members.Count == 0) continue;
                    bool hasRealm = false;
                    foreach (var m in members)
                    {
                        if (m.Realm >= evt.MinPatriarchRealm) { hasRealm = true; break; }
                    }
                    if (!hasRealm) continue;
                }

                result.Add(evt);
            }
            return result;
        }

        private RandomEventData WeightedRandom(List<RandomEventData> events)
        {
            int totalWeight = 0;
            foreach (var e in events) totalWeight += e.Weight;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            foreach (var e in events)
            {
                cumulative += e.Weight;
                if (roll < cumulative) return e;
            }
            return events[events.Count - 1];
        }

        private void ExecuteEvent(RandomEventData evt)
        {
            Debug.Log($"[EventManager] EVENT: {evt.EventName} — {evt.Description}");

            switch (evt.EventType)
            {
                case RandomEventType.MonsterAttack:
                    Debug.LogWarning("[EventManager] The domain is under siege!");
                    break;

                case RandomEventType.DiplomaticVisit:
                    if (Diplomacy.FactionManager.Instance != null)
                    {
                        var factions = Diplomacy.FactionManager.Instance.Factions;
                        if (factions.Count > 0)
                        {
                            var visitor = factions[Random.Range(0, factions.Count)];
                            Diplomacy.FactionManager.Instance.ChangeRelation(visitor.ID, +10);
                            Debug.Log($"[EventManager] {visitor.Name} sends a delegation (+10 relation).");
                        }
                    }
                    break;

                case RandomEventType.RuinsDiscovery:
                    if (DeductionEngine.Instance != null)
                    {
                        int quality = Random.Range(1, 4);
                        Element element = (Element)Random.Range(1, 8);
                        DeductionEngine.Instance.AddFragment(element, quality, "Ruin Fragment");
                        Debug.Log($"[EventManager] Found a quality-{quality} {element} fragment in ancient ruins!");
                    }
                    break;

                case RandomEventType.GeniusBirth:
                    Debug.Log("[EventManager] A child with exceptional talent is born nearby. Recruit?");
                    break;

                case RandomEventType.InternalBetrayal:
                    var members = ClanManager.Instance?.LivingMembers;
                    if (members != null)
                    {
                        CharacterData traitor = null;
                        int lowestMs = 101;
                        foreach (var m in members)
                        {
                            if (m.MentalStability < lowestMs)
                            {
                                lowestMs = m.MentalStability;
                                traitor = m;
                            }
                        }
                        if (traitor != null && traitor.MentalStability < 30)
                        {
                            MentalStabilitySystem.Instance?.ApplyModifier(traitor, -15);
                            Debug.LogWarning($"[EventManager] {traitor.FullName} is plotting betrayal! (-15 MS)");
                        }
                        else
                        {
                            Debug.Log("[EventManager] Betrayal plot uncovered but contained.");
                        }
                    }
                    break;

                case RandomEventType.NaturalDisaster:
                    if (ResourceManager.Instance != null)
                    {
                        int loss = Random.Range(50, 200);
                        ResourceManager.Instance.ConsumeSpiritStones(loss);
                        Debug.LogWarning($"[EventManager] Natural disaster! Lost {loss} Spirit Stones.");
                    }
                    break;

                case RandomEventType.WanderingMerchant:
                    Debug.Log("[EventManager] A wandering merchant offers rare goods.");
                    break;

                case RandomEventType.RivalChallenge:
                    Debug.LogWarning("[EventManager] A rival cultivator challenges the clan!");
                    break;

                case RandomEventType.Epidemic:
                    var clanMembers = ClanManager.Instance?.LivingMembers;
                    if (clanMembers != null)
                    {
                        foreach (var m in clanMembers)
                        {
                            if (m.Realm <= CultivationRealm.QiRefinement)
                                MentalStabilitySystem.Instance?.ApplyModifier(m, -5);
                        }
                        Debug.LogWarning("[EventManager] Epidemic hits! Low-realm members lose -5 MS.");
                    }
                    break;

                case RandomEventType.MarriageOpportunity:
                    Debug.Log("[EventManager] A neighboring clan proposes a marriage alliance.");
                    break;

                case RandomEventType.PeacefulYear:
                    Debug.Log("[EventManager] A peaceful year passes.");
                    break;
            }
        }

        private void BuildDefaultEventTable()
        {
            EventTable = new List<RandomEventData>();
            AddDefaultEvent("Monster Attack", RandomEventType.MonsterAttack, 20, "Wild beasts assault the domain.");
            AddDefaultEvent("Diplomatic Visit", RandomEventType.DiplomaticVisit, 12, "A faction sends envoys.");
            AddDefaultEvent("Ruins Discovery", RandomEventType.RuinsDiscovery, 10, "Ancient ruins found nearby.");
            AddDefaultEvent("Genius Birth", RandomEventType.GeniusBirth, 5, "A prodigy appears in the region.", minYear: 5);
            AddDefaultEvent("Internal Betrayal", RandomEventType.InternalBetrayal, 8, "Low-stability member plots.");
            AddDefaultEvent("Natural Disaster", RandomEventType.NaturalDisaster, 10, "Flood, earthquake, or storm.");
            AddDefaultEvent("Wandering Merchant", RandomEventType.WanderingMerchant, 15, "A merchant with rare wares.");
            AddDefaultEvent("Rival Challenge", RandomEventType.RivalChallenge, 8, "A rival issues a formal challenge.", minRealm: CultivationRealm.Foundation);
            AddDefaultEvent("Epidemic", RandomEventType.Epidemic, 7, "Disease sweeps the domain.");
            AddDefaultEvent("Marriage Opportunity", RandomEventType.MarriageOpportunity, 10, "A neighboring clan proposes an alliance.");
            AddDefaultEvent("Peaceful Year", RandomEventType.PeacefulYear, 30, "Nothing of note happens.");
        }

        private void AddDefaultEvent(string name, RandomEventType type, int weight, string desc,
            CultivationRealm minRealm = CultivationRealm.Embryonic, int minYear = 0)
        {
            var evt = ScriptableObject.CreateInstance<RandomEventData>();
            evt.EventName = name;
            evt.EventType = type;
            evt.Weight = weight;
            evt.Description = desc;
            evt.MinPatriarchRealm = minRealm;
            evt.MinYear = minYear;
            EventTable.Add(evt);
        }
    }
}

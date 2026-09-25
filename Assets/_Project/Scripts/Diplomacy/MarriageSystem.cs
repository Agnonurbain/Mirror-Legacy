using System;
using System.Linq;
using UnityEngine;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Characters;
using MirrorChronicles.Events;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// Handles marriages within the clan (Love) or with other factions (Arranged),
    /// and the annual marriages that keep the lineage alive.
    /// </summary>
    public class MarriageSystem : MonoBehaviour
    {
        public static MarriageSystem Instance { get; private set; }

        private readonly System.Random rng = new System.Random();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
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
            // Events phase comes before Inheritance, so this year's newlyweds are
            // always counted by ClanManager.ProcessAnnualBirths, whatever the subscription order.
            if (phase == GamePhase.Events)
                ProcessAnnualMarriages();
        }

        /// <summary>
        /// Called during the Events phase. Eligible members may marry an unrelated
        /// clan member or a wandering cultivator, who then joins the clan so the couple can have children.
        /// </summary>
        public void ProcessAnnualMarriages()
        {
            var clan = ClanManager.Instance;
            if (clan == null) return;

            var plans = MarriageMatchmaker.PlanAnnualMarriages(
                clan.LivingMembers.ToList(), FindCharacterById(), MarriageMatchmaker.AnnualMarriageChance, rng);

            foreach (var plan in plans)
            {
                if (!HandleLoveMarriage(plan.Member, plan.Spouse))
                    Debug.LogWarning($"[MarriageSystem] Planned marriage between {plan.Member.FullName} and {plan.Spouse.FullName} failed validation.");
            }
        }

        /// <summary>
        /// Validates if two characters can marry (Age >= 18, not already married,
        /// no common ancestor within <see cref="KinshipRules.MarriageForbiddenGenerations"/> generations).
        /// </summary>
        public bool CanMarry(CharacterData personA, CharacterData personB)
        {
            if (personA == null || personB == null) return false;
            if (!personA.IsAlive || !personB.IsAlive) return false;
            if (personA.Age < 18 || personB.Age < 18) return false;
            if (!string.IsNullOrEmpty(personA.SpouseID) || !string.IsNullOrEmpty(personB.SpouseID)) return false;

            if (KinshipRules.AreCloseKin(personA, personB, KinshipRules.MarriageForbiddenGenerations, FindCharacterById()))
            {
                Debug.LogWarning($"[MarriageSystem] Cannot marry relatives within {KinshipRules.MarriageForbiddenGenerations} generations.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// A marriage for love within the clan or with a wandering rogue cultivator,
        /// who joins the clan. Boosts mental stability but gives no political power.
        /// </summary>
        public bool HandleLoveMarriage(CharacterData member, CharacterData spouse)
        {
            if (!CanMarry(member, spouse)) return false;

            member.SpouseID = spouse.ID;
            spouse.SpouseID = member.ID;
            JoinClanIfOutsider(spouse);

            // Big mental stability boost
            ApplyStability(member, +10);
            ApplyStability(spouse, +10);

            Debug.Log($"[MarriageSystem] LOVE MARRIAGE: {member.FullName} and {spouse.FullName} are now married. (+10 Stability)");
            return true;
        }

        /// <summary>
        /// A political marriage with a rival faction. The spouse joins the clan.
        /// Boosts relations significantly, but may harm the member's mental stability if forced.
        /// </summary>
        public bool HandleArrangedMarriage(CharacterData member, string targetFactionId, bool isForced)
        {
            if (member == null || !member.IsAlive || member.Age < 18 || !string.IsNullOrEmpty(member.SpouseID))
                return false;

            var factions = FactionManager.Instance;
            if (factions == null) return false;

            var faction = factions.GetFactionByID(targetFactionId);
            if (faction == null) return false;

            // Opposite-sex adult from the faction's family (e.g. "Wang"), trained by the faction
            CharacterData politicalSpouse = MarriageMatchmaker.CreateOutsiderSpouse(member, faction.Name.Split(' ')[0], rng);
            politicalSpouse.Realm = CultivationRealm.QiRefinement;
            politicalSpouse.RealmStage = 1;
            politicalSpouse.HasSpiritualOrifice = true; // a faction only trains those it has examined
            politicalSpouse.OrificeKnown = true;
            politicalSpouse.MaxLifespan = PowerLadder.MaxLifespan(politicalSpouse.Realm, politicalSpouse.RealmStage);

            member.SpouseID = politicalSpouse.ID;
            politicalSpouse.SpouseID = member.ID;
            JoinClanIfOutsider(politicalSpouse);

            // Political consequences
            int relationBoost = 25;
            factions.ChangeRelation(targetFactionId, relationBoost);

            // Personal consequences
            if (isForced)
            {
                ApplyStability(member, -15);
                Debug.Log($"[MarriageSystem] ARRANGED MARRIAGE (Forced): {member.FullName} married into {faction.Name}. Relations +{relationBoost}, Stability -15.");
            }
            else
            {
                ApplyStability(member, +5);
                Debug.Log($"[MarriageSystem] ARRANGED MARRIAGE (Willing): {member.FullName} married into {faction.Name}. Relations +{relationBoost}, Stability +5.");
            }

            return true;
        }

        /// <summary>
        /// Spouses from outside must be clan members, otherwise ClanManager never counts
        /// the couple for births. Call after SpouseID is set so listeners see a complete record.
        /// </summary>
        private static void JoinClanIfOutsider(CharacterData spouse)
        {
            var clan = ClanManager.Instance;
            if (clan != null && !clan.LivingMembers.Contains(spouse))
                clan.AddMember(spouse);
        }

        /// <summary>
        /// Null-safe: this runs every year from a GameEvents handler, where an exception
        /// would stop the remaining subscribers for that phase.
        /// </summary>
        private static void ApplyStability(CharacterData character, int amount)
        {
            var stability = MentalStabilitySystem.Instance;
            if (stability == null)
            {
                Debug.LogWarning("[MarriageSystem] MentalStabilitySystem missing: stability change skipped.");
                return;
            }
            stability.ApplyModifier(character, amount);
        }

        private static Func<string, CharacterData> FindCharacterById()
        {
            var registry = BloodRegistry.Instance;
            return registry != null ? registry.GetCharacterByID : null;
        }
    }
}

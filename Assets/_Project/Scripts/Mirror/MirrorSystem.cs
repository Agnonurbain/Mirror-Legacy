using System.Linq;
using UnityEngine;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Events;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The core player interface system. Represents the ancestral bronze mirror.
    /// Manages Mirror Power (0-100) and Divine Interventions.
    /// </summary>
    public class MirrorSystem : MonoBehaviour
    {
        public static MirrorSystem Instance { get; private set; }

        public int MirrorPower { get; private set; } = 50;
        public const int MaxMirrorPower = 100;
        public const int TalismanSeedCost = 40;

        /// <summary>Restored shards of the mirror; the restoration axis itself arrives with phase L6.</summary>
        public int RestoredFragments { get; private set; }
        public int TalismanSeedCapacity => SpiritualOrificeRules.TalismanSeedCapacity(RestoredFragments);

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
            GameEvents.OnYearStarted += HandleYearStarted;
            GameEvents.OnBreakthroughSuccess += HandleBreakthroughSuccess;
        }

        private void OnDisable()
        {
            GameEvents.OnYearStarted -= HandleYearStarted;
            GameEvents.OnBreakthroughSuccess -= HandleBreakthroughSuccess;
        }

        private void HandleYearStarted(int year)
        {
            // Passive recharge: +1 per year
            AddPower(1);
        }

        private void HandleBreakthroughSuccess(CharacterData character, CultivationRealm newRealm)
        {
            // Active recharge: +5 per successful breakthrough
            AddPower(5);
        }

        public void AddPower(int amount)
        {
            MirrorPower = Mathf.Clamp(MirrorPower + amount, 0, MaxMirrorPower);
            Debug.Log($"[MirrorSystem] Mirror Power updated: {MirrorPower}/{MaxMirrorPower}");
        }

        public bool ConsumePower(int amount)
        {
            if (MirrorPower >= amount)
            {
                MirrorPower -= amount;
                Debug.Log($"[MirrorSystem] Consumed {amount} Power. Remaining: {MirrorPower}/{MaxMirrorPower}");
                return true;
            }
            
            Debug.LogWarning($"[MirrorSystem] Not enough Mirror Power! Needed: {amount}, Have: {MirrorPower}");
            return false;
        }

        // --- Divine Interventions ---

        /// <summary>
        /// Cost: 10. Gives a temporary buff in combat.
        /// </summary>
        public bool UseQiPulse(CombatUnit target = null)
        {
            if (!ConsumePower(10)) return false;

            if (target != null)
            {
                int qiBoost = Mathf.RoundToInt(target.MaxQi * 0.3f);
                target.RestoreQi(qiBoost);
                int vitalityBoost = Mathf.RoundToInt(target.MaxVitality * 0.15f);
                target.Heal(vitalityBoost);
                Debug.Log($"[MirrorSystem] Qi Pulse on {target.BaseData.FullName}: +{qiBoost} Qi, +{vitalityBoost} HP!");
            }
            else
            {
                Debug.Log("[MirrorSystem] DIVINE INTERVENTION: Qi Pulse activated! (Combat buff)");
            }
            return true;
        }

        /// <summary>
        /// Cost: 25. Reduces breakthrough failure risk by 30%.
        /// </summary>
        public bool UseAncestralShield()
        {
            if (ConsumePower(25))
            {
                Debug.Log("[MirrorSystem] DIVINE INTERVENTION: Ancestral Shield activated! (Breakthrough protected)");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cost: 40. Plants a Talisman Seed in a mortal's dantian so they can cultivate without an
        /// orifice (LORE.md §4, §11.5). The mirror sustains only a limited number of active seeds.
        /// </summary>
        public bool GrantTalismanSeed(CharacterData target)
        {
            if (target == null || ClanManager.Instance == null) return false;

            int activeSeeds = ClanManager.Instance.LivingMembers.Count(m => m.HasTalismanSeed);
            if (!SpiritualOrificeRules.CanReceiveTalismanSeed(target, activeSeeds, TalismanSeedCapacity))
            {
                Debug.LogWarning($"[MirrorSystem] {target.FullName} cannot receive a Talisman Seed ({activeSeeds}/{TalismanSeedCapacity} active).");
                return false;
            }
            if (!ConsumePower(TalismanSeedCost)) return false;

            target.HasTalismanSeed = true;
            target.OrificeKnown = true; // the mirror knows what it planted
            Debug.Log($"[MirrorSystem] DIVINE INTERVENTION: a Talisman Seed takes root in {target.FullName}.");
            return true;
        }

        /// <summary>
        /// Cost: 50. Punishes a traitor or enemy with Qi Deviation.
        /// </summary>
        public bool UseMirrorJudgment(CharacterData target)
        {
            if (ConsumePower(50))
            {
                Debug.Log($"[MirrorSystem] DIVINE INTERVENTION: Mirror Judgment strikes {target.FullName}!");
                GameEvents.TriggerCharacterDied(target, DeathCause.QiDeviation);
                return true;
            }
            return false;
        }
    }
}

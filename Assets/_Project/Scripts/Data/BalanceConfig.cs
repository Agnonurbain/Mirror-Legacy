using UnityEngine;

namespace MirrorChronicles.Data
{
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "MirrorChronicles/Balance Config")]
    public class BalanceConfig : ScriptableObject
    {
        public static BalanceConfig Instance { get; private set; }

        [Header("Cultivation")]
        public int baseXPPerYear = 10;
        public float spiritualRootXPFactor = 0.5f;
        public float lowStabilityMultiplier = 0.8f;
        public int lowStabilityThreshold = 50;

        [Header("XP Thresholds per Realm")]
        public int xpEmbryonic = 100;
        public int xpQiRefinement = 500;
        public int xpFoundation = 2000;
        public int xpPurpleMansion = 10000;
        public int xpGoldenCore = 50000;

        [Header("Breakthrough")]
        public int baseRiskEmbryonic = 5;
        public int baseRiskQiRefinement = 15;
        public int baseRiskFoundation = 30;
        public int baseRiskPurpleMansion = 50;
        public int baseRiskGoldenCore = 70;
        public int ancestralShieldBonus = 30;

        [Header("Failure Severity (%)")]
        public int minorFailureChance = 70;
        public int majorFailureChance = 25;
        public int catastrophicFailureChance = 5;

        [Header("Lifespan by Realm")]
        public int lifespanEmbryonic = 80;
        public int lifespanQiRefinement = 120;
        public int lifespanFoundation = 250;
        public int lifespanPurpleMansion = 500;
        public int lifespanGoldenCore = 1000;
        public int lifespanDaoEmbryo = 3000;

        [Header("Economy")]
        public int startingSpiritStones = 1000;
        public int mineBaseYield = 50;
        public int mineRealmBonus = 25;
        public float birthChance = 0.25f;

        [Header("Combat")]
        public float realmDamageMultiplier = 0.2f;
        public float affinityDamageBonus = 0.25f;
        public float terrainWaterBonus = 0.2f;
        public float defendDamageReduction = 0.5f;
        public float qiRegenOnConcentratedQi = 0.05f;

        [Header("Espionage")]
        public float espionageRootFactor = 0.005f;
        public float espionagePowerDivisor = 100f;
        public float espionageMinChance = 0.05f;
        public float espionageMaxChance = 0.60f;
        public int espionageFailRelationPenalty = -20;
        public int espionageFailStabilityPenalty = -10;

        [Header("Buildings")]
        public int buildingMaxLevel = 5;
        public int trainingHallXPPerLevel = 2;
        public int minePassivePerLevel = 20;

        [Header("Karma")]
        public float karmaCultivationBonusPerGen = 0.02f;
        public int karmaXPBonusAt5Gen = 5;
        public int karmaXPBonusAt10Gen = 10;

        private void OnEnable()
        {
            Instance = this;
        }
    }
}

using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>Trials that gate a step on the power ladder (LORE.md §5).</summary>
    public enum TrialKind { None, InnerLakeChakra, MeridianWheelChakra, SummitEyeChakra, FoundationWall, PurpleMansionAscension, GoldenCoreRoute }

    /// <summary>Groups of Qi Cultivation levels: 1-3, 4-6, 7-9.</summary>
    public enum QiPhase { None, Early, Middle, Late }

    /// <summary>The next step from a realm and stage.</summary>
    public readonly struct AdvancementStep
    {
        public CultivationRealm TargetRealm { get; }
        public int TargetStage { get; }
        public TrialKind Trial { get; }
        /// <summary>False when the step needs a later system (Purple Mansion ascension, Golden Core routes).</summary>
        public bool IsAvailable { get; }

        public AdvancementStep(CultivationRealm targetRealm, int targetStage, TrialKind trial, bool isAvailable)
        {
            TargetRealm = targetRealm;
            TargetStage = targetStage;
            Trial = trial;
            IsAvailable = isAvailable;
        }
    }

    /// <summary>
    /// Pure rules of the power ladder shared by every cultivation path (LORE.md §5):
    /// sub-levels, trials, experience, lifespans. Balance values are defaults to move into BalanceConfig.
    /// </summary>
    public static class PowerLadder
    {
        public const int Unbounded = int.MaxValue;
        public const int MortalMaxLifespan = 80;

        private const int FoundationAdvisedAge = 60;
        private const int FoundationWallBaseChance = 35;
        private const int FoundationWallChanceLossPerYear = 2;
        private const int MinimumTrialChance = 5;
        private const int DissolutionBaseChance = 20;
        private const int DissolutionChancePerYear = 3;
        private const int MaximumDissolutionChance = 90;

        /// <summary>Number of sub-levels of a realm: 6 chakras, 9 Qi levels, 4 stages.</summary>
        public static int StageCount(CultivationRealm realm)
        {
            switch (realm)
            {
                case CultivationRealm.Embryonic: return 6;
                case CultivationRealm.QiRefinement: return 9;
                case CultivationRealm.Foundation:
                case CultivationRealm.PurpleMansion:
                case CultivationRealm.GoldenCore: return 4;
                default: return 1; // Dao Embryo and Golden Immortal: progression unknown in the lore
            }
        }

        /// <summary>Qi Cultivation levels 1-3 early, 4-6 middle, 7-9 late.</summary>
        public static QiPhase QiPhaseOf(int level)
        {
            if (level < 1) return QiPhase.None;
            if (level <= 3) return QiPhase.Early;
            if (level <= 6) return QiPhase.Middle;
            return QiPhase.Late;
        }

        /// <summary>Purple Mansion stage from divine abilities: 1-2 early, 3 middle, 4 late, 5 Grand Perfection.</summary>
        public static int PurpleMansionStageFromAbilities(int abilities)
        {
            if (abilities <= 0) return 0;
            if (abilities <= 2) return 1;
            return Math.Min(abilities, 5) - 1;
        }

        /// <summary>The next step from a realm and stage, with the trial that gates it.</summary>
        public static AdvancementStep Next(CultivationRealm realm, int stage)
        {
            switch (realm)
            {
                case CultivationRealm.Embryonic:
                    if (stage >= 6) return new AdvancementStep(CultivationRealm.QiRefinement, 1, TrialKind.None, true);
                    return new AdvancementStep(realm, stage + 1, ChakraTrial(stage + 1), true);
                case CultivationRealm.QiRefinement:
                    if (stage >= 9) return new AdvancementStep(CultivationRealm.Foundation, 1, TrialKind.FoundationWall, true);
                    return new AdvancementStep(realm, stage + 1, TrialKind.None, true);
                case CultivationRealm.Foundation:
                    if (stage >= 4) return new AdvancementStep(CultivationRealm.PurpleMansion, 1, TrialKind.PurpleMansionAscension, false);
                    return new AdvancementStep(realm, stage + 1, TrialKind.None, true);
                case CultivationRealm.PurpleMansion:
                    // Stages follow divine abilities; the Golden Core needs a route (LORE.md §5.9).
                    return new AdvancementStep(CultivationRealm.GoldenCore, 1, TrialKind.GoldenCoreRoute, false);
                case CultivationRealm.GoldenCore:
                    return new AdvancementStep(realm, stage, TrialKind.GoldenCoreRoute, false);
                default:
                    return new AdvancementStep(realm, stage, TrialKind.None, false);
            }
        }

        /// <summary>The three blocking chakras: Inner Lake (1), Meridian Wheel (3), Summit Eye (5).</summary>
        private static TrialKind ChakraTrial(int chakra)
        {
            switch (chakra)
            {
                case 1: return TrialKind.InnerLakeChakra;
                case 3: return TrialKind.MeridianWheelChakra;
                case 5: return TrialKind.SummitEyeChakra;
                default: return TrialKind.None;
            }
        }

        /// <summary>Base success chance of a trial before personal modifiers.</summary>
        public static int BaseTrialChance(TrialKind trial, int age)
        {
            switch (trial)
            {
                case TrialKind.InnerLakeChakra: return 70;
                case TrialKind.MeridianWheelChakra: return 80;
                case TrialKind.SummitEyeChakra: return 75;
                case TrialKind.FoundationWall:
                    int lateYears = Math.Max(0, age - FoundationAdvisedAge);
                    return Math.Max(MinimumTrialChance, FoundationWallBaseChance - lateYears * FoundationWallChanceLossPerYear);
                default: return 0;
            }
        }

        /// <summary>Chance that a failed Foundation breakthrough ends in spiritual dissolution (death).</summary>
        public static int DissolutionChanceOnFailure(int age)
        {
            int lateYears = Math.Max(0, age - FoundationAdvisedAge);
            return Math.Min(MaximumDissolutionChance, DissolutionBaseChance + lateYears * DissolutionChancePerYear);
        }

        /// <summary>Lifespan from the lore; stage 0 of Embryonic Breathing is still a mortal.</summary>
        public static int MaxLifespan(CultivationRealm realm, int stage)
        {
            switch (realm)
            {
                case CultivationRealm.Embryonic: return stage <= 0 ? MortalMaxLifespan : 120;
                case CultivationRealm.QiRefinement: return 200;
                case CultivationRealm.Foundation: return 300;
                case CultivationRealm.PurpleMansion: return 500;
                case CultivationRealm.GoldenCore: return 1000;
                default: return Unbounded; // Dao Embryo lives until the end of the world
            }
        }

        /// <summary>Experience needed per sub-level (the former realm thresholds split across stages).</summary>
        public static int XpForNextStage(CultivationRealm realm)
        {
            switch (realm)
            {
                case CultivationRealm.Embryonic: return 17;      // 100 / 6
                case CultivationRealm.QiRefinement: return 56;   // 500 / 9
                case CultivationRealm.Foundation: return 500;    // 2000 / 4
                case CultivationRealm.PurpleMansion: return 2000; // 10000 / 5 divine abilities
                case CultivationRealm.GoldenCore: return 12500;  // 50000 / 4
                default: return 0;
            }
        }

        /// <summary>Clamps the stage into its realm; saves older than stages get the first sub-level.</summary>
        public static void Normalize(CharacterData character)
        {
            int max = StageCount(character.Realm);
            int min = character.Realm == CultivationRealm.Embryonic ? 0 : 1;
            character.RealmStage = Math.Max(min, Math.Min(max, character.RealmStage));
        }

        /// <summary>
        /// The age at which this character dies of old age: their own lifespan (a mortal's roll,
        /// shortened by Dao wounds) when set, otherwise the reach of their realm.
        /// </summary>
        public static int LifespanLimit(CharacterData character)
        {
            return character.MaxLifespan > 0
                ? character.MaxLifespan
                : MaxLifespan(character.Realm, character.RealmStage);
        }

        /// <summary>Keeps lifespan reductions from a save but never lets it exceed the realm's reach.</summary>
        public static void NormalizeLifespan(CharacterData character)
        {
            int reach = MaxLifespan(character.Realm, character.RealmStage);
            if (character.MaxLifespan <= 0 || character.MaxLifespan > reach)
                character.MaxLifespan = reach;
        }
    }
}

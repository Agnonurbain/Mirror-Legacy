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
    /// Pure rules of the power ladder. TDD stub.
    /// </summary>
    public static class PowerLadder
    {
        public static int StageCount(CultivationRealm realm) => 0;
        public static QiPhase QiPhaseOf(int level) => QiPhase.None;
        public static int PurpleMansionStageFromAbilities(int abilities) => -1;
        public static AdvancementStep Next(CultivationRealm realm, int stage) => default;
        public static int BaseTrialChance(TrialKind trial, int age) => 0;
        public static int DissolutionChanceOnFailure(int age) => 0;
        public static int MaxLifespan(CultivationRealm realm, int stage) => 0;
        public static int XpForNextStage(CultivationRealm realm) => 0;
        public static void Normalize(CharacterData character) { }
    }
}

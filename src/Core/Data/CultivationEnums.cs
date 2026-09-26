namespace MirrorChronicles.Data
{
    /// <summary>
    /// The six cultivation Daos (LORE.md §3). The path decides how a character cultivates;
    /// the Dao lineage (Fruition) decides what they cultivate toward.
    /// </summary>
    public enum CultivationPath
    {
        Immortal,
        Devil,
        Buddhist,
        Demonic,
        Shamanic,
        Divine
    }

    /// <summary>
    /// Sub-paths of each cultivation Dao (LORE.md §3.1-3.7).
    /// </summary>
    public enum CultivationSubPath
    {
        PurpleMansionGoldenCore,   // Immortal — the clan's default path
        SpiritualNature,           // Immortal — orthodox, 1 in 1000
        HeavenlyEmbryoDemon,       // Devil — Diverse Mansion / Unified Furnace
        PurpleGoldDemon,           // Devil — discards Qi at the 9th level
        AncientBuddhism,           // Buddhist — Five Dharmas
        ModernBuddhism,            // Buddhist — Seven Dharma Aspects
        MyriadRaces,               // Demonic — beasts, plants, awakened creatures
        TrueSerpentDragon,         // Demonic — dragon lineage
        ShamanicTalisman,          // Shamanic — Transformation / Concealment / Sacrifice
        BorrowedProfundity,        // Divine — borrowing an external Profundity
        UnitedProfundity,          // Divine — uniting with an external Profundity
        OwnedProfundity            // Divine — owning a Profundity
    }

    /// <summary>
    /// What a character is; non-human members open the Demonic Dao (LORE.md §5.9, R16).
    /// </summary>
    public enum Species
    {
        Human,
        SpiritBeast,
        Spirit,
        Dragon
    }

    /// <summary>
    /// Golden Core standing, kept apart from the power tier because the lore allows many
    /// Golden-Core-level states (LORE.md §5.9 F).
    /// </summary>
    public enum GoldenCoreState
    {
        None,
        MetallicEssenceOnly,   // essence forged, no position claimed (R7)
        Realization,           // R1
        Surplus,               // R2, R3
        Intercalary,           // R4, R5, R6
        TrueLeftHand,          // R18 — autonomous, no position
        FalseLeftHand,         // R19 — False Divine Core, depends on a patron
        PseudoGoldenCore,      // R10 — Spiritual Nature
        DivineCore,            // R15
        DharmaMaster,          // R13, R14
        DevilEquivalent,       // R11, R12
        DemonicEquivalent,     // R16
        ShamanicEquivalent     // R17
    }

    /// <summary>
    /// The position five divine abilities lead to in a lineage (LORE.md §5.5.1): its five orthodox abilities
    /// (Realization), a substitute among them (Surplus), or a complete change of lineage (Intercalary: four of
    /// another lineage and one of this one, or three and two with a specialised gold-seeking method).
    /// </summary>
    public enum PositionRoute { None, Realization, Surplus, IntercalaryFourOne, IntercalaryThreeTwo }
}

using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>The Immortal Arts of the game (the user's decision, 2026-10-03): of the 101, the four the lore shows at work.</summary>
    public enum ImmortalArt { Forge, Alchemy, Talismans, Formations }

    /// <summary>One's gift for an art: none (only the Purple Mansion may then practise it), ordinary, or a genius of it.</summary>
    public enum ArtGift { None, Ordinary, Genius }

    /// <summary>
    /// An Immortal Art (balance.json « arts »): its name, what practising it costs an ordinary talent's cultivation, and the
    /// foundations, abilities and talisman Qi that give its gift (📚 Forge Souterraine forges; « Vent de vallée guidant le
    /// feu » refines elixirs; « Garder profit et prospérité » aids the hundred arts).
    /// </summary>
    public sealed record ArtDefinition
    {
        public ImmortalArt Art { get; init; }
        public string Name { get; init; }
        public double OrdinaryCultivation { get; init; } = 0.5;
        public IReadOnlyList<string> GeniusAbilities { get; init; } = new List<string>();
        public IReadOnlyList<string> GiftTalismans { get; init; } = new List<string>();
    }

    /// <summary>
    /// The Immortal Arts (balance.json « arts », AUDIT_LORE.md §2, the user's decisions 2026-10-03/04 — interpretations): below
    /// the Purple Mansion only the gifted practise an art (≈ <see cref="OrdinaryChance"/> ordinary, <see cref="GeniusChance"/>
    /// geniuses per art, at birth), and only with its legacy; a master in the clan teaches at full pace, the manual alone at
    /// <see cref="WithoutMasterFactor"/>. An ordinary talent's cultivation slows while practising; a genius's even gains
    /// (<see cref="GeniusCultivation"/>). Mastery: adept from <see cref="AdeptAt"/>, master from <see cref="MasterAt"/>.
    /// </summary>
    public sealed record ArtSettings
    {
        public double OrdinaryChance { get; init; } = 0.08;
        public double GeniusChance { get; init; } = 0.01;
        public int YearlyMastery { get; init; } = 10;
        public double GeniusMasteryFactor { get; init; } = 2.0;
        public double UngiftedMasteryFactor { get; init; } = 0.5;
        public double WithoutMasterFactor { get; init; } = 0.5;
        public double GeniusCultivation { get; init; } = 1.1;
        public int AdeptAt { get; init; } = 40;
        public int MasterAt { get; init; } = 80;
        public IReadOnlyList<ArtDefinition> Arts { get; init; } = new List<ArtDefinition>();
        public EssencePillSettings EssencePill { get; init; } = new EssencePillSettings();
        public FormationSettings Formation { get; init; } = new FormationSettings();
        public TalismanDrawingSettings Talisman { get; init; } = new TalismanDrawingSettings();
        public LegacySettings Legacy { get; init; } = new LegacySettings();
        public IReadOnlyList<PillDefinition> Pills { get; init; } = new List<PillDefinition>();
        public int WithoutPowderPenalty { get; init; } = 20;   // the fifth chakra without the Bright Spirit Powder (🔎)
        public int PillBuyRelation { get; init; } = 20;        // a power that knows alchemy sells its pills from this relation (🔎)
        public ConvergenceSettings Convergence { get; init; } = new ConvergenceSettings();
    }

    /// <summary>
    /// The Autumn Convergence Pill in battle (balance.json « arts.convergence », AUDIT_LORE.md §2.9, 📚 wiki: replenishes mana,
    /// enhances spell arts, costs about three years of lifespan, three at most; beyond, the Immortal Foundation may collapse —
    /// 🔎 the amounts): a rival swallows its own below <see cref="AiUseBelow"/> of its vitality.
    /// </summary>
    public sealed record ConvergenceSettings
    {
        public double SpellBoost { get; init; } = 0.5;
        public int LifespanCost { get; init; } = 3;
        public int SafeDoses { get; init; } = 3;
        public double CollapseChance { get; init; } = 0.5;
        public int CollapseLifespan { get; init; } = 20;
        public double AiUseBelow { get; init; } = 0.35;
    }

    /// <summary>The pills the clan's arts spend (AUDIT_LORE.md §2.2, §2.8): beside the Essence Gathering Pill, by element.</summary>
    public enum PillKind { BrightSpirit, Purification, Condensation, AutumnConvergence }

    /// <summary>
    /// A pill (balance.json « arts.pills », 🔎 the amounts): its name, what an alchemist of <see cref="Mastery"/> spends to refine
    /// it, what a power asks (<see cref="Worth"/> stones, or an accord in kind when <see cref="Precious"/>).
    /// </summary>
    public sealed record PillDefinition
    {
        public PillKind Kind { get; init; }
        public string Name { get; init; }
        public int Herbs { get; init; }
        public int Stones { get; init; }
        public int Mastery { get; init; } = 1;
        public int Worth { get; init; }
        public bool Precious { get; init; }
    }

    /// <summary>
    /// Where an art's legacy comes from (balance.json « arts.legacy », AUDIT_LORE.md §2.5, the user's decision 2026-10-04 — 🔎
    /// interpretations): a power of the Foundation or above knows an art by its kind's chance (stable for a power), and teaches
    /// it at <see cref="TeachRelation"/> for an accord in kind worth <see cref="Worth"/> (precious: stones weigh nothing); ruins
    /// hold a manual at <see cref="RuinsChance"/>, a Purple Mansion's tomb at <see cref="TombChance"/>; the mirror deduces one
    /// from <see cref="DeduceFragments"/> fragments and <see cref="DeduceMoonlight"/> of its Moonlight.
    /// </summary>
    public sealed record LegacySettings
    {
        public Dictionary<FactionKind, double> PowerArtChance { get; init; } = new Dictionary<FactionKind, double>();
        public int TeachRelation { get; init; } = 40;
        public int Worth { get; init; } = 1500;
        public double RuinsChance { get; init; } = 0.1;
        public double TombChance { get; init; } = 0.5;
        public int DeduceFragments { get; init; } = 4;
        public int DeduceMoonlight { get; init; } = 80;
    }

    /// <summary>
    /// Drawing talismans (balance.json « arts.talisman », AUDIT_LORE.md §2.4, 📚 wiki Li_Xuanxuan: « one of the few ways to earn
    /// stones » — 🔎 the amounts): a year's practice sells its talismans for <see cref="StonesBase"/> and
    /// <see cref="StonesPerMastery"/> a point of mastery, a genius's dearer by <see cref="GeniusFactor"/>.
    /// </summary>
    public sealed record TalismanDrawingSettings
    {
        public int StonesBase { get; init; } = 10;
        public double StonesPerMastery { get; init; } = 0.5;
        public double GeniusFactor { get; init; } = 1.5;
    }

    /// <summary>
    /// The Protective Formation as an Immortal Art (balance.json « arts.formation », AUDIT_LORE.md §2.3 — 🔎 interpretations): an
    /// apprentice of the formations raises its first levels, an adept from <see cref="AdeptFromLevel"/>, a master from
    /// <see cref="MasterFromLevel"/>; or a power at <see cref="HireRelation"/> or more lends its own for a fee (a share of the
    /// stones on top), a Purple Mansion power for the formation's height.
    /// </summary>
    public sealed record FormationSettings
    {
        public int AdeptFromLevel { get; init; } = 3;
        public int MasterFromLevel { get; init; } = 5;
        public int HireRelation { get; init; } = 30;
        public double HireFeeShare { get; init; } = 0.5;

        // World parity: a power's own formation, as high as its realm knows (the highest key at or below its realm), one level
        // below at the world's start; its masters raise it by one level a year at PowerRiseChance.
        public Dictionary<CultivationRealm, int> PowerCapByRealm { get; init; } = new Dictionary<CultivationRealm, int>();
        public double PowerRiseChance { get; init; } = 0.05;
    }

    /// <summary>
    /// The Essence Gathering Pill (balance.json « arts.essencePill », AUDIT_LORE.md §2.6-2.7, the user's decision 2026-10-04 —
    /// 🔎 interpretations): what an alchemist spends to refine one, the mastery it asks, and the odds the Foundation wall loses
    /// when it is tried without a pill of one's own Qi element (📚 a gamble, wiki Li_Chenghui).
    /// </summary>
    public sealed record EssencePillSettings
    {
        public int Herbs { get; init; } = 30;
        public int Stones { get; init; } = 20;
        public int Mastery { get; init; } = 1;
        public int WithoutPillPenalty { get; init; } = 25;

        // Poison (audit §2.7, 📚 wiki Li_Chengliao: an opposed essence fails the wall, an incompatible Qi poisons): a power at or
        // below TaintRelation may slip a poisoned pill into the clan's store each year; an adept finds it, and whose it is.
        public double TaintChance { get; init; } = 0.05;
        public int TaintRelation { get; init; } = -40;
        public int CaughtDistrust { get; init; } = 25;

        // World parity: a power's alchemists refine pills by its kind, up to a cap; its elders take one at the wall, else
        // gamble (their odds times ElderWithoutPillFactor), and a failed wall may kill; a rival may poison its store.
        public Dictionary<FactionKind, double> PowerPillChance { get; init; } = new Dictionary<FactionKind, double>();
        public int PowerPillCap { get; init; } = 4;
        public int PowerStartPills { get; init; } = 1;
        public double ElderWithoutPillFactor { get; init; } = 0.4;
        public double ElderWallDeathChance { get; init; } = 0.2;
        public double WorldTaintChance { get; init; } = 0.03;
    }
}

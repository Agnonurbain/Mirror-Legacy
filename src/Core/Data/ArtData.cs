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

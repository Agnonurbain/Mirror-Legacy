using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Events;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>A building: its level, what it gives now (null at level 0) and at the next level (null at its height), the cost, and why not.</summary>
    public sealed record BuildingLine(BuildingType Type, string Name, int Level, int MaxLevel, string Effect, string NextEffect, int? Cost, string Refusal);

    /// <summary>The clan's buildings (G6): eight of them, five levels each; the refusal is the upgrade's own.</summary>
    public static class BuildingsView
    {
        public static IReadOnlyList<BuildingLine> Buildings(GameSession session)
        {
            int veins = session.Context.Content.Balance.Upkeep.VeinMinersPerMineLevel;
            return session.Buildings.Buildings
                .Select(b =>
                {
                    bool atHeight = b.Level >= BuildingSystem.MaxLevel;
                    return new BuildingLine(b.Type, Name(b.Type), b.Level, BuildingSystem.MaxLevel,
                        b.Level == 0 ? null : Effect(b.Type, b.Level, veins),
                        atHeight ? null : Effect(b.Type, b.Level + 1, veins),
                        atHeight ? null : b.UpgradeCost,
                        session.Buildings.UpgradeRefusal(b.Type));
                })
                .ToList();
        }

        public static string Name(BuildingType type) => type switch
        {
            BuildingType.TrainingHall => "Salle d'entraînement",
            BuildingType.Forge => "Forge",
            BuildingType.Library => "Bibliothèque",
            BuildingType.HerbGarden => "Jardin d'herbes",
            BuildingType.Mine => "Mine de pierres spirituelles",
            BuildingType.CouncilRoom => "Salle du conseil",
            BuildingType.MeditationPagoda => "Pagode de méditation",
            BuildingType.ProtectiveFormation => "Formation protectrice",
            _ => type.ToString()
        };

        /// <summary>What the building gives at this level, from the same constants its systems apply.</summary>
        /// <param name="veinsPerLevel">The veins each level of the Mine opens (balance.json « upkeep »).</param>
        public static string Effect(BuildingType type, int level, int veinsPerLevel) => type switch
        {
            BuildingType.TrainingHall => $"+{level * BuildingSystem.TrainingHallXpPerLevel} d'expérience par an aux membres en cultivation",
            BuildingType.Forge => $"+{Percent(level * BuildingSystem.ForgeYieldBonusPerLevel)} % au rendement des membres affectés à la mine",
            BuildingType.Library => $"+{Percent(level * BuildingSystem.LibraryDiscoveryBonusPerLevel)} % de chances de trouver un fragment à l'étude",
            BuildingType.HerbGarden => $"+{level} de stabilité mentale par an aux membres au repos",
            BuildingType.Mine => $"+{level * BuildingSystem.MineStonesPerLevel} pierres par an et {level * veinsPerLevel} filon(s) de plus pour les mineurs",
            BuildingType.CouncilRoom => $"+{level * BuildingSystem.CouncilRelationBonusPerLevel} de relation par mission diplomatique",
            BuildingType.MeditationPagoda => $"+{level} de stabilité mentale par an à tous les membres",
            BuildingType.ProtectiveFormation => $"−{Percent(Math.Min(1.0, level * EventManager.FormationReliefPerLevel))} % des pertes d'une calamité naturelle",
            _ => null
        };

        private static int Percent(double share) => (int)Math.Round(share * 100);
    }
}

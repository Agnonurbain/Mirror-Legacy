using System;

namespace MirrorChronicles.Data
{
    public enum BuildingType
    {
        TrainingHall,
        Forge,
        Library,
        HerbGarden,
        Mine,
        CouncilRoom,
        MeditationPagoda,
        ProtectiveFormation
    }

    [Serializable]
    public class BuildingData
    {
        public BuildingType Type { get; set; }
        public int Level { get; set; }

        public BuildingData(BuildingType type)
        {
            Type = type;
            Level = 0;
        }

        public int UpgradeCost => GetUpgradeCost(Level + 1);

        public static int GetUpgradeCost(int targetLevel)
        {
            return targetLevel switch
            {
                1 => 200,
                2 => 500,
                3 => 1200,
                4 => 3000,
                5 => 8000,
                _ => int.MaxValue
            };
        }

        public static CultivationRealm GetRequiredRealm(BuildingType type)
        {
            return type switch
            {
                BuildingType.MeditationPagoda => CultivationRealm.PurpleMansion,
                BuildingType.ProtectiveFormation => CultivationRealm.Foundation,
                _ => CultivationRealm.Embryonic
            };
        }
    }
}

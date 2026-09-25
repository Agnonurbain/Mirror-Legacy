using System;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>Builders for the combat tests: plain grids and units of a given realm.</summary>
    internal static class CombatFixtures
    {
        public static BattleField Field(Random rng = null, int width = 10, int height = 10) =>
            new BattleField(new CombatGrid(width, height), rng ?? new Random(1), new RecordingGameLog());

        public static CombatUnit Unit(CultivationRealm realm = CultivationRealm.Embryonic, bool isAlly = true,
            int root = 40, Element affinity = Element.None, AIStrategyType strategy = AIStrategyType.Aggressive)
        {
            var data = new CharacterData
            {
                FirstName = isAlly ? "Ally" : "Foe",
                LastName = "Test",
                Realm = realm,
                RealmStage = realm == CultivationRealm.Embryonic ? 1 : 1,
                SpiritualRoot = root,
                Affinity = affinity
            };
            return new CombatUnit(data, isAlly, strategy);
        }

        /// <summary>Places a new unit on the field and returns it.</summary>
        public static CombatUnit Place(BattleField field, int x, int y, CultivationRealm realm = CultivationRealm.Embryonic,
            bool isAlly = true, int root = 40, Element affinity = Element.None, AIStrategyType strategy = AIStrategyType.Aggressive)
        {
            var unit = Unit(realm, isAlly, root, affinity, strategy);
            field.Place(unit, x, y);
            return unit;
        }
    }
}

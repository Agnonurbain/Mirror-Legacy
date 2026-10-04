using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Immortal Arts a power knows (AUDIT_LORE.md §2.5, 🔎): from the Foundation, each art by its kind's chance — stable for
    /// a power, from its name (no draw: a power's masters do not come and go with the dice).
    /// </summary>
    public static class PowerArts
    {
        public static bool Knows(FactionData power, ImmortalArt art, GameContent content)
        {
            if (power == null || power.HighestRealm < CultivationRealm.Foundation) return false;
            if (!content.Balance.Arts.Legacy.PowerArtChance.TryGetValue(power.Kind, out double chance)) return false;
            uint hash = 2166136261;
            foreach (char c in (power.Name ?? "") + "|art|" + art) hash = (hash ^ c) * 16777619;
            return (hash % 10000) / 10000.0 < chance;
        }

        public static IReadOnlyList<ImmortalArt> Of(FactionData power, GameContent content) =>
            System.Enum.GetValues<ImmortalArt>().Where(a => Knows(power, a, content)).ToList();
    }
}

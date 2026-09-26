using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The pure rules of the talisman Qi (LORE.md §11.5): the rank a sacrifice's realm refines, what the mirror
    /// offers a bearer (one to three to suit talent and temper), and the leap a new talisman gives.
    /// </summary>
    public static class TalismanRules
    {
        /// <summary>Grey from a Qi Cultivation sacrifice, white from the Foundation or beyond; null below.</summary>
        public static TalismanRank? RankOf(CharacterData sacrifice)
        {
            if (sacrifice.Realm >= CultivationRealm.Foundation) return TalismanRank.White;
            if (sacrifice.Realm == CultivationRealm.QiRefinement) return TalismanRank.Grey;
            return null;
        }

        /// <summary>
        /// The talismans offered: of the rank, those suiting the bearer's temper first, then the others, each group
        /// in a random order; as many as the bearer's talent earns (one, two, three).
        /// </summary>
        public static IReadOnlyList<string> Offer(CharacterData bearer, TalismanRank rank, IReadOnlyList<TalismanDefinition> catalog,
            TalismanSettings settings, Random rng)
        {
            int count = 1 + settings.OfferRootThresholds.Count(threshold => bearer.SpiritualRoot >= threshold);
            var ofRank = catalog.Where(t => t.Rank == rank).ToList();
            var suiting = ofRank.Where(t => t.Temperaments.Contains(bearer.Temperament)).OrderBy(_ => rng.Next()).ToList();
            var others = ofRank.Except(suiting).OrderBy(_ => rng.Next());
            return suiting.Concat(others).Take(count).Select(t => t.Id).ToList();
        }

        /// <summary>Lifts the bearer up to this many sub-levels, never past their realm's last.</summary>
        public static void Leap(CharacterData bearer, int stages)
        {
            bearer.RealmStage = Math.Min(PowerLadder.StageCount(bearer.Realm), bearer.RealmStage + Math.Max(0, stages));
        }

        /// <summary>The talisman a member bears, or null.</summary>
        public static TalismanDefinition Of(CharacterData member, IReadOnlyList<TalismanDefinition> catalog) =>
            member?.TalismanQiId == null ? null : catalog.FirstOrDefault(t => t.Id == member.TalismanQiId);
    }
}

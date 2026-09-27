using System;
using MirrorChronicles.Data;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>The pure rules of the treaties (D7: profit decides, profit betrays).</summary>
    public static class TreatyRules
    {
        /// <summary>
        /// How much a power wants this treaty: the kind's base, its temper's bias, its relation, the balance of strength
        /// (an ally wants a strong clan; a suzerain a weak vassal; a vassal a strong suzerain), less what it suspects.
        /// </summary>
        public static int Willingness(FactionData power, TreatyKind kind, bool clanAsSuzerain, CultivationRealm clanStrongest, int suspicion,
            TreatySettings s)
        {
            int bias = s.PersonalityBias.TryGetValue(power.Personality, out var biases) && biases.TryGetValue(kind, out var b) ? b : 0;
            int gap = (int)clanStrongest - (int)power.HighestRealm;
            int strength = kind switch
            {
                TreatyKind.Defence => gap * s.StrengthWeight,
                TreatyKind.Vassalage => (clanAsSuzerain ? gap : -gap) * s.StrengthWeight,
                _ => 0
            };
            int baseValue = s.AcceptBase.TryGetValue(kind, out var v) ? v : 0;
            return baseValue + bias + power.RelationWithPlayer + strength - (int)(suspicion * s.SuspicionWeight);
        }

        /// <summary>A power's yearly chance of betraying: its temper, its suspicion, its strength over the clan, its oath.</summary>
        public static double BetrayalChance(FactionData power, Treaty treaty, int suspicion, CultivationRealm clanStrongest, TreatySettings s)
        {
            double temper = s.BetrayalTemper.TryGetValue(power.Personality, out var t) ? t : 1.0;
            double stronger = (int)power.HighestRealm >= (int)clanStrongest + s.SuzerainRealmMargin ? s.StrongerBetrayalFactor : 1.0;
            double sworn = treaty.Sealed ? s.SealedFactor : 1.0;
            return Math.Clamp(s.BetrayalBase * temper * (1 + suspicion / 100.0 * s.SuspicionBetrayalWeight) * stronger * sworn, 0, 1);
        }
    }
}

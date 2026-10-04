using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>The Immortal Arts' pure rules (AUDIT_LORE.md §2, 2026-10-04): one's gift, whether one may practise, at what pace.</summary>
    public static class ImmortalArtRules
    {
        /// <summary>
        /// One's gift for an art: a genius by an aligned foundation or ability, at least ordinary by a talisman Qi that aids
        /// it, else what one was born with — stable for a person, rare (no draw: a person's nature does not change).
        /// </summary>
        public static ArtGift Gift(CharacterData member, ImmortalArt art, ArtSettings s)
        {
            var def = s.Arts.FirstOrDefault(a => a.Art == art);
            if (def != null && (def.GeniusAbilities.Contains(member.FoundationId)
                || (member.DivineAbilities ?? new System.Collections.Generic.List<string>()).Any(def.GeniusAbilities.Contains))) return ArtGift.Genius;
            var born = Born(member, art, s);
            if (born == ArtGift.None && def != null && member.TalismanQiId != null && def.GiftTalismans.Contains(member.TalismanQiId)) return ArtGift.Ordinary;
            return born;
        }

        /// <summary>The gift one was born with: from one's identity, stable.</summary>
        public static ArtGift Born(CharacterData member, ImmortalArt art, ArtSettings s)
        {
            uint hash = 2166136261;
            foreach (char c in (member.ID ?? "") + "|" + art) hash = (hash ^ c) * 16777619;
            double u = (hash % 10000) / 10000.0;
            return u < s.GeniusChance ? ArtGift.Genius : u < s.GeniusChance + s.OrdinaryChance ? ArtGift.Ordinary : ArtGift.None;
        }

        /// <summary>Below the Purple Mansion only the gifted practise; from it, anyone (📚 « without their natural constraints »).</summary>
        public static bool MayPractise(CharacterData member, ImmortalArt art, ArtSettings s) =>
            member.Realm >= CultivationRealm.PurpleMansion || Gift(member, art, s) != ArtGift.None;

        /// <summary>The cultivation kept while practising: an ordinary talent loses some (formations the most), a genius gains.</summary>
        public static double CultivationFactor(CharacterData member, ImmortalArt art, ArtSettings s) =>
            Gift(member, art, s) == ArtGift.Genius ? s.GeniusCultivation : s.Arts.FirstOrDefault(a => a.Art == art)?.OrdinaryCultivation ?? 0.5;

        /// <summary>The mastery gained in a year: by gift, and halved without a master to teach.</summary>
        public static int YearlyMastery(CharacterData member, ImmortalArt art, bool taught, ArtSettings s)
        {
            var gift = Gift(member, art, s);
            double factor = gift == ArtGift.Genius ? s.GeniusMasteryFactor : gift == ArtGift.Ordinary ? 1.0 : s.UngiftedMasteryFactor;
            return (int)System.Math.Round(s.YearlyMastery * factor * (taught ? 1.0 : s.WithoutMasterFactor));
        }

        /// <summary>The stones a year's talismans sell for (audit §2.4, 🔎): by mastery, a genius's dearer.</summary>
        public static int TalismanStones(CharacterData member, ArtSettings s)
        {
            int mastery = member.ArtMastery != null && member.ArtMastery.TryGetValue(ImmortalArt.Talismans, out int m) ? m : 0;
            double stones = s.Talisman.StonesBase + mastery * s.Talisman.StonesPerMastery;
            return (int)System.Math.Round(stones * (Gift(member, ImmortalArt.Talismans, s) == ArtGift.Genius ? s.Talisman.GeniusFactor : 1.0));
        }

        public static string Rank(int mastery, ArtSettings s) =>
            mastery >= s.MasterAt ? "maître" : mastery >= s.AdeptAt ? "adepte" : mastery > 0 ? "apprenti" : "novice";

        public static string GiftLabel(ArtGift gift) => gift switch { ArtGift.Genius => "génie", ArtGift.Ordinary => "doué", _ => "sans don" };
    }
}

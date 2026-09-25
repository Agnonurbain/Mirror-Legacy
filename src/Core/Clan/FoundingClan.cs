using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// The five founders of a new game: the patriarch and matriarch at Qi Refinement, their son and
    /// daughter and the patriarch's brother in Embryonic Breathing. All cultivate, so their orifices are
    /// known. (First names move to data in phase G2.)
    /// </summary>
    public static class FoundingClan
    {
        public static void Found(ClanManager clan)
        {
            var patriarch = Founder(clan, "Wei", isMale: true, age: 45, root: 65, Element.Fire, CultivationRealm.QiRefinement, 3, stability: 80);
            var matriarch = Founder(clan, "Xue", isMale: false, age: 42, root: 55, Element.Water, CultivationRealm.QiRefinement, 2, stability: 85);
            patriarch.SpouseID = matriarch.ID;
            matriarch.SpouseID = patriarch.ID;

            var son = Founder(clan, "Jian", isMale: true, age: 20, root: 75, Element.Lightning, CultivationRealm.Embryonic, 2);
            var daughter = Founder(clan, "Mei", isMale: false, age: 16, root: 45, Element.Wood, CultivationRealm.Embryonic, 1);
            foreach (var child in new[] { son, daughter })
            {
                child.FatherID = patriarch.ID;
                child.MotherID = matriarch.ID;
            }

            var uncle = Founder(clan, "Shan", isMale: true, age: 40, root: 25, Element.Earth, CultivationRealm.Embryonic, 1, stability: 60);

            foreach (var member in new[] { patriarch, matriarch, son, daughter, uncle })
                clan.AddMember(member);
            clan.AppointPatriarch(patriarch);
        }

        private static CharacterData Founder(ClanManager clan, string firstName, bool isMale, int age, int root,
            Element affinity, CultivationRealm realm, int stage, int stability = 70)
        {
            return new CharacterData
            {
                FirstName = firstName,
                LastName = clan.ClanName,
                IsMale = isMale,
                Age = age,
                SpiritualRoot = root,
                Affinity = affinity,
                Realm = realm,
                RealmStage = stage,
                MaxLifespan = PowerLadder.MaxLifespan(realm, stage),
                MentalStability = stability,
                HasSpiritualOrifice = true,
                OrificeKnown = true
            };
        }
    }
}

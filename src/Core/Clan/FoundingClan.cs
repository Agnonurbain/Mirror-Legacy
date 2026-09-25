using System;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// Founds the clan of a new game from clan.json: the patriarch and the matriarch are married, the
    /// children are theirs, kin have no recorded parents. Every founder cultivates, so their orifices
    /// are known; each practises the method clan.json names, and a Qi cultivator holds its Qi.
    /// </summary>
    public static class FoundingClan
    {
        /// <param name="techniques">The clan's library, which already knows the starting techniques.</param>
        /// <param name="rng">The session's random source, for reproducible IDs.</param>
        public static void Found(ClanManager clan, ClanDefinition definition, TechniqueLibrary techniques, Random rng)
        {
            var founders = definition.Founders.Select(f => (definition: f, member: Create(clan, f, rng))).ToList();
            foreach (var (f, member) in founders.Where(f => f.definition.CultivationMethod != null))
            {
                techniques.AssignMethod(member, f.CultivationMethod);
                if (member.Realm >= CultivationRealm.QiRefinement)
                    member.QiId = techniques.MethodOf(member)?.RequiredQiId; // absorbed long before the story
            }

            var patriarch = founders.Single(f => f.definition.Role == FounderRole.Patriarch).member;
            var matriarch = founders.SingleOrDefault(f => f.definition.Role == FounderRole.Matriarch).member;
            if (matriarch != null)
            {
                patriarch.SpouseID = matriarch.ID;
                matriarch.SpouseID = patriarch.ID;
            }

            var father = patriarch.IsMale ? patriarch : matriarch;
            var mother = patriarch.IsMale ? matriarch : patriarch;
            foreach (var (_, child) in founders.Where(f => f.definition.Role == FounderRole.Child))
            {
                child.FatherID = father?.ID;
                child.MotherID = mother?.ID;
            }

            foreach (var (_, member) in founders)
                clan.AddMember(member);
            clan.AppointPatriarch(patriarch);
        }

        private static CharacterData Create(ClanManager clan, FounderDefinition f, Random rng)
        {
            return new CharacterData
            {
                ID = rng.NextId(),
                FirstName = f.FirstName,
                LastName = clan.ClanName,
                IsMale = f.IsMale,
                Age = f.Age,
                SpiritualRoot = f.SpiritualRoot,
                Affinity = f.Affinity,
                Realm = f.Realm,
                RealmStage = f.RealmStage,
                MaxLifespan = PowerLadder.MaxLifespan(f.Realm, f.RealmStage),
                MentalStability = f.MentalStability,
                HasSpiritualOrifice = true,
                OrificeKnown = true,
                Temperament = f.Temperament != Temperament.None ? f.Temperament : FoundationRules.RandomTemperament(rng)
            };
        }
    }
}

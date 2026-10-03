using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.World
{
    /// <summary>
    /// The world's Metal Essence Demons (the user's rule, 2026-10-03: what befalls the clan befalls the world). A demon born
    /// of a power's elder ravages its region — unless the Underworld's emissaries claim it at once —, ranked as the clan's:
    /// it weakens the region's powers and kills their elders, and the clan's members when the region is the clan's; a True
    /// Monarch of a power of the region may subdue it, and so may the clan's when it ravages its home.
    /// </summary>
    [TestFixture]
    public class WorldDemonTests
    {
        private static GameContent With(double ravage, double elderKill = 0, double subdue = 0)
        {
            var b = Fixtures.QuietContent.Balance;
            var d = b.Demons;
            return Fixtures.QuietContent with
            {
                Balance = b with
                {
                    Demons = d with
                    {
                        WorldRavageChance = ravage, WorldSubdueChance = subdue,
                        Tiers = d.Tiers.ToDictionary(t => t.Key, t => t.Value with { ElderKillChance = elderKill, KillChance = 1.0 })
                    }
                }
            };
        }

        private static GameSession Session(GameContent content) => GameSession.NewGame(new GameSetup { Seed = 1, Content = content });

        private static FactionElder Elder(string id, CultivationRealm realm) =>
            new FactionElder { Id = id, Name = id, Realm = realm, Stage = 4, MaxLifespan = 1000 };

        private static void Years(GameSession s, int years)
        {
            for (int i = 0; i < years; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.Demons.ProcessYear();
            }
        }

        private static string Home(GameSession s) => s.Context.Content.Clan.HomeRegion;

        [Test]
        public void APowersDemon_RavagesItsRegion_NotTheClansElsewhere()
        {
            var s = Session(With(ravage: 1.0, elderKill: 1.0));
            var far = s.Factions.Factions.First(f => f.RegionId != Home(s) && f.Elders.Count > 1);
            var neighbours = s.Factions.Factions.Where(f => f.RegionId == far.RegionId).ToList();
            int power = neighbours.Sum(f => f.PowerLevel), elders = neighbours.Sum(f => f.Elders.Count);
            s.Events.TriggerElderDied(far, Elder("fallen", CultivationRealm.PurpleMansion), true);
            var demon = s.Demons.Ravaging.Single();
            Assert.AreEqual(far.RegionId, demon.RegionId);
            int members = s.Clan.LivingMembers.Count;
            Years(s, 1);
            Assert.Less(neighbours.Sum(f => f.PowerLevel), power, "it weakens the region's powers");
            Assert.Less(neighbours.Sum(f => f.Elders.Count), elders, "and kills their elders");
            Assert.AreEqual(members, s.Clan.LivingMembers.Count, "the clan, far away, is untouched");
        }

        [Test]
        public void APowersDemon_AtTheClansHome_StrikesTheClanToo()
        {
            var s = Session(With(ravage: 1.0));
            var near = s.Factions.Factions.First(f => f.RegionId == Home(s));
            s.Clan.AddMember(Fixtures.Cultivator());
            int members = s.Clan.LivingMembers.Count;
            s.Events.TriggerElderDied(near, Elder("fallen", CultivationRealm.PurpleMansion), true);
            Years(s, 1);
            Assert.Less(s.Clan.LivingMembers.Count, members);
            Assert.IsTrue(s.Demons.Ravaging.Any(d => d.RegionId == Home(s)), "the clan may subdue it with its own True Monarch");
        }

        [Test]
        public void TheUnderworld_MayClaimItAtOnce()
        {
            var s = Session(With(ravage: 0.0));
            s.Events.TriggerElderDied(s.Factions.Factions[0], Elder("fallen", CultivationRealm.PurpleMansion), true);
            Assert.IsEmpty(s.Demons.Ravaging);
        }

        [Test]
        public void AHoldersDemon_IsACatastrophe()
        {
            var s = Session(With(ravage: 1.0));
            var elder = Elder("holder", CultivationRealm.GoldenCore);
            elder.FruitionId = "orthodox-water";
            s.Events.TriggerElderDied(s.Factions.Factions[0], elder, true);
            Assert.AreEqual(DemonTier.Realization, s.Demons.Ravaging.Single().Tier);
        }

        [Test]
        public void ATrueMonarchOfTheRegion_SubduesIt()
        {
            var s = Session(With(ravage: 1.0, subdue: 1.0));
            var far = s.Factions.Factions.First(f => f.RegionId != Home(s));
            far.Elders.Add(Elder("monarch", CultivationRealm.GoldenCore));
            s.Events.TriggerElderDied(far, Elder("fallen", CultivationRealm.PurpleMansion), true);
            Years(s, 1);
            Assert.IsEmpty(s.Demons.Ravaging);
        }

        [Test]
        public void TheClan_CannotSubdueADemonFarFromItsHome()
        {
            var s = Session(With(ravage: 1.0));
            var far = s.Factions.Factions.First(f => f.RegionId != Home(s));
            s.Events.TriggerElderDied(far, Elder("fallen", CultivationRealm.PurpleMansion), true);
            var monarch = Fixtures.Cultivator(age: 400, realm: CultivationRealm.GoldenCore);
            s.Clan.AddMember(monarch);
            StringAssert.Contains("région", s.Demons.Subdue(s.Demons.Ravaging.Single().Id));
        }
    }
}

using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// Those who hinder an ascent (LORE.md §11.10; 2026-10-01): a rival of the patron may learn of the accord; it then
    /// sabotages the practitioner's manifestation retreat, denounces the accord to all, tries to buy the practitioner, or pays the
    /// clan to resist its patron.
    /// </summary>
    [TestFixture]
    public class AscentRivalsTests
    {
        private const string Patron = "Famille Bai";
        private const string Rival = "Famille Tao";
        private const string Method = "silent-tide-sutra";

        private static AscentRivalSettings Settings => Fixtures.Content.Balance.AscentRivals;

        private static TestWorld World(System.Random rng, out CharacterData practitioner, bool known = true)
        {
            var w = new TestWorld(rng);
            w.Factions.AddFaction(new FactionData { Name = Patron, Kind = FactionKind.Family, RegionId = "linxi", RelationWithPlayer = 20,
                HighestRealm = CultivationRealm.GoldenCore, PowerLevel = 5000, Techniques = { Method } });
            w.Factions.AddFaction(new FactionData { Name = Rival, Kind = FactionKind.Family, RegionId = "jingshui-lake", HighestRealm = CultivationRealm.PurpleMansion });
            w.Clan.AppointPatriarch(w.Join(Fixtures.Cultivator()));
            practitioner = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 4));
            practitioner.CultivationMethodId = Method;
            practitioner.MentalStability = 50;
            w.Sponsorships.Restore(null, new[] { new Sponsorship("sp1", Patron, Method, "harvest", 1, false) });
            if (known) w.Rivals.Restore(new[] { new AscentRival(Rival, "sp1") });
            return w;
        }

        [Test]
        public void ARival_LearnsOfTheAccord()
        {
            var w = World(new FixedRandom(0.0), out _, known: false);
            w.Rivals.ProcessYear();
            Assert.AreEqual(new AscentRival(Rival, "sp1"), w.Rivals.Known.Single());
        }

        [Test]
        public void TheRival_SabotagesTheManifestationRetreat()
        {
            var w = World(new FixedRandom(0.0), out var p);
            p.Retreat = Retreat.Manifestation;
            w.Rivals.ProcessYear();
            Assert.IsTrue(!p.IsAlive && p.CauseOfDeath == DeathCause.ManifestationCollapse);
        }

        [Test]
        public void AFoiledSabotage_LeavesTheClanWary()
        {
            var w = World(new SequenceRandom(0.0, 0.99), out var p); // it strikes; it fails
            p.Retreat = Retreat.Manifestation;
            w.Rivals.ProcessYear();
            Assert.IsTrue(p.IsAlive && w.Suspicion.ClanDistrust(Rival) > 0);
        }

        [Test]
        public void ADenouncedAccord_IsDropped_AndThePatronExposed()
        {
            var w = World(new SequenceRandom(0.0), out _); // no retreat: the first roll denounces
            w.Rivals.ProcessYear();
            Assert.IsFalse(w.Sponsorships.Active.Any());
            Assert.Less(w.Factions.GetFactionByName(Patron).RelationWithPlayer, 20);
            Assert.Greater(w.Suspicion.Distrust(Rival, Patron), 0);
        }

        [Test]
        public void TheRival_MayBuyThePractitioner()
        {
            var w = World(new SequenceRandom(0.99, 0.0), out var p); // no denunciation; the purchase succeeds
            w.Rivals.ProcessYear();
            Assert.IsTrue(p.Departed);
        }

        [Test]
        public void ResistingThePatron_IsRewarded_ByTheRival()
        {
            var w = World(new FixedRandom(0.0), out _);
            w.Sponsorships.Restore(null, new[] { new Sponsorship("sp1", Patron, Method, "harvest", 1, true) { Awaiting = true } });
            int stones = w.Resources.SpiritStones;
            w.Sponsorships.Resist("sp1");
            Assert.AreEqual(stones + Settings.OutbidStones, w.Resources.SpiritStones);
        }

        [Test]
        public void TheRivals_SurviveASave()
        {
            var s = GameSession.NewGame(Fixtures.Setup());
            s.Rivals.Restore(new[] { new AscentRival(Rival, "sp1") });
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            CollectionAssert.AreEqual(s.Rivals.Known, reloaded.Rivals.Known);
        }
    }
}

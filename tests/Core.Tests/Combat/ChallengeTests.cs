using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Combat
{
    /// <summary>
    /// A rival's challenge (G6; the random event « Défi d'un rival », until now without effect): a power sends rivals of
    /// the rank of the clan's best free fighter; the clan picks who fights — free cultivators it knows — and the battle is
    /// played on the grid. The wager goes to the victor; the fallen die, the survivors carry their wounds; a refusal, or
    /// silence until the next year, costs face with the challenger.
    /// </summary>
    [TestFixture]
    public class ChallengeTests
    {
        private const string Ruan = "Famille Ruan";
        private static ChallengeSettings Settings => Fixtures.Content.Balance.Challenges;

        private static (TestWorld w, CharacterData champion) World(System.Random rng)
        {
            var w = new TestWorld(rng);
            w.Factions.InitializeFactions();
            var champion = w.Join(Fixtures.Cultivator(realm: CultivationRealm.Foundation, stage: 3));
            w.Clan.AppointPatriarch(champion);
            return (w, champion);
        }

        private static FactionData Power(TestWorld w) => w.Factions.GetFactionByName(Ruan);

        [Test]
        public void ARivalsChallenge_SendsRivals_OfTheRankOfTheClansBest()
        {
            var (w, champion) = World(new FixedRandom(0.0));
            var challenge = w.Challenges.Issue(Power(w));

            Assert.AreEqual(Ruan, challenge.Faction);
            Assert.AreEqual((champion.Realm, champion.RealmStage), (challenge.Realm, challenge.Stage));
            var rivals = w.Challenges.Rivals(challenge);
            Assert.That(rivals.Count, Is.InRange(1, Settings.MaxRivals));
            Assert.IsTrue(rivals.All(r => r.Realm == champion.Realm));
            CollectionAssert.AreEqual(rivals.Select(r => r.FirstName), w.Challenges.Rivals(challenge).Select(r => r.FirstName), "the same rivals each time");
        }

        [Test]
        public void TheRandomEvent_IssuesTheChallenge()
        {
            var (w, _) = World(new FixedRandom(0.0));
            w.Ctx.Events.TriggerRandomEventOccurred(new RandomEventData { EventType = RandomEventType.RivalChallenge, Name = "Défi d'un rival" });
            Assert.IsNotNull(w.Challenges.Pending);
        }

        [Test]
        public void AFighter_MustBeAFreeCultivator_TheClanKnows()
        {
            var (w, champion) = World(new FixedRandom(0.0));
            w.Challenges.Issue(Power(w));
            var captive = w.Join(Fixtures.Cultivator());
            captive.CaptorFaction = Ruan;
            var mortal = w.Join(Fixtures.Mortal());

            StringAssert.Contains("captif", w.Challenges.FighterRefusal(captive));
            StringAssert.Contains("cultive", w.Challenges.FighterRefusal(mortal));
            Assert.IsNull(w.Challenges.FighterRefusal(champion));
            Assert.IsNotNull(w.Challenges.Accept(new[] { captive.ID }));
            Assert.IsNotNull(w.Challenges.Accept(new string[0]), "somebody must fight");
            Assert.IsNull(w.Challenges.Current);
        }

        [Test]
        public void Accepting_OpensTheBattle_OnTheGrid()
        {
            var (w, champion) = World(new FixedRandom(0.0));
            w.Challenges.Issue(Power(w));
            Assert.IsNull(w.Challenges.Accept(new[] { champion.ID }));
            Assert.IsNull(w.Challenges.Pending);
            Assert.AreSame(champion, w.Challenges.Current.Allies.Single().BaseData);
        }

        [Test]
        public void AVictory_WinsTheWager()
        {
            var (w, champion) = World(new FixedRandom(0.0));
            w.Challenges.Issue(Power(w));
            w.Challenges.Accept(new[] { champion.ID });
            foreach (var rival in w.Challenges.Current.Enemies) rival.TakeDamage(rival.MaxVitality);
            w.Challenges.Current.EndTurn(); // the field sees it
            int stones = w.Resources.SpiritStones;

            Assert.IsNull(w.Challenges.Conclude());

            Assert.AreEqual(stones + Settings.Wager, w.Resources.SpiritStones);
            Assert.IsNull(w.Challenges.Current);
        }

        [Test]
        public void ADefeat_LosesTheWager_AndTheFallenDie()
        {
            var (w, champion) = World(new FixedRandom(0.0));
            w.Challenges.Issue(Power(w));
            w.Challenges.Accept(new[] { champion.ID });
            w.Challenges.Current.Allies.Single().TakeDamage(10000);
            w.Challenges.Current.EndTurn();
            int stones = w.Resources.SpiritStones;

            Assert.IsNull(w.Challenges.Conclude());

            Assert.AreEqual(stones - Settings.Wager, w.Resources.SpiritStones);
            Assert.IsFalse(champion.IsAlive);
        }

        [Test]
        public void AFallenFighter_YieldsInARulesBoundChallenge_UnlessABlowIsNotHeldBack()
        {
            var (w, champion) = World(new FixedRandom(0.999)); // the blow is held back
            w.Challenges.Issue(Power(w));
            w.Challenges.Accept(new[] { champion.ID });
            w.Challenges.Current.Allies.Single().TakeDamage(10000);
            w.Challenges.Current.EndTurn();
            Assert.IsNull(w.Challenges.Conclude());
            Assert.IsTrue(champion.IsAlive, "fallen in a challenge by the rules, the fighter yields");
        }

        [Test]
        public void ABattleUnfinished_CannotBeConcluded()
        {
            var (w, champion) = World(new FixedRandom(0.0));
            w.Challenges.Issue(Power(w));
            w.Challenges.Accept(new[] { champion.ID });
            StringAssert.Contains("pas finie", w.Challenges.Conclude());
        }

        [Test]
        public void ARefusal_OrSilence_CostsFace()
        {
            var (w, _) = World(new FixedRandom(0.0));
            int relation = Power(w).RelationWithPlayer;
            w.Challenges.Issue(Power(w));
            Assert.IsNull(w.Challenges.Decline());
            Assert.AreEqual(relation + Settings.DeclineRelation, Power(w).RelationWithPlayer);

            w.Challenges.Issue(Power(w));
            w.Ctx.Clock.Restore(w.Ctx.Clock.Year + 1, w.Ctx.Clock.Phase);
            w.Challenges.ProcessYear();
            Assert.IsNull(w.Challenges.Pending, "unanswered, it lapses");
            Assert.AreEqual(relation + 2 * Settings.DeclineRelation, Power(w).RelationWithPlayer);
        }

        [Test]
        public void RoundTrip_KeepsThePendingChallenge()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            var challenge = s.Challenges.Issue(s.Factions.GetFactionByName(Ruan));
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), Fixtures.Setup());
            Assert.AreEqual(challenge, reloaded.Challenges.Pending);
        }

        [Test]
        public void TheYear_WaitsForTheBattleUnderWay()
        {
            var s = GameSession.NewGame(Fixtures.Setup(1));
            s.Challenges.Issue(s.Factions.GetFactionByName(Ruan));
            var fighter = MirrorChronicles.Presentation.BattleView.Pending(s).Candidates.First(c => c.Refusal == null);
            Assert.IsNull(s.Challenges.Accept(new[] { fighter.Id }));
            var (year, phase) = (s.Clock.Year, s.Clock.Phase);

            s.AdvancePhase();

            Assert.AreEqual((year, phase), (s.Clock.Year, s.Clock.Phase), "a battle is fought to its end first");
            Assert.IsNotNull(s.Challenges.Current);
        }
    }
}

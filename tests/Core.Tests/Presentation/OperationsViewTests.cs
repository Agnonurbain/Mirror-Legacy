using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>
    /// The secret operations screen (L2c.5): the mirror's ritual, the hunt's planning with a preview of its odds, and
    /// the secret — what the mirror perceives of the powers, never their hidden numbers (LORE.md D7).
    /// </summary>
    [TestFixture]
    public class OperationsViewTests
    {
        private static GameSession NewGame() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        // ---- The ritual ----

        [Test]
        public void Ritual_TellsTheYearThePrayersAndTheBeasts()
        {
            var s = NewGame();
            s.Resources.AddBeast(new CapturedBeast("b1", CultivationRealm.QiRefinement, 4, "Famille Ruan"));

            var ritual = OperationsView.Ritual(s);

            Assert.AreEqual(20, ritual.Year);
            Assert.IsFalse(ritual.HuntOpen);
            Assert.AreEqual(Fixtures.Content.Balance.Talismans.PrayersPerRitual, ritual.PrayersNeeded);
            Assert.AreEqual(new BeastLine("b1", "Culture du Qi, stade 4", "Famille Ruan"), ritual.Beasts.Single());
            StringAssert.Contains("an 20", ritual.Refusal, "no ritual before its year");
        }

        [Test]
        public void Ritual_ShowsTheTalismansOffered()
        {
            var s = NewGame();
            s.Talismans.Restore(new TalismanOffer(s.Clan.GetPatriarch().ID, new List<string> { "prolong-life" }));

            var offer = OperationsView.Ritual(s).Offer;

            Assert.AreEqual(s.Clan.GetPatriarch().FullName, offer.Bearer);
            Assert.AreEqual("Prolonger la vie et accroître la longévité", offer.Choices.Single().Name);
        }

        // ---- The hunt ----

        [Test]
        public void HuntTargets_AreTheScoutedBeasts_WithTheirPlace()
        {
            var s = NewGame();
            var beast = s.Bestiary.In("heshan").First();
            Assert.AreEqual(0, OperationsView.HuntTargets(s).Count);

            s.Knowledge.Reveal(FactKind.Beast, beast.Id, KnowledgeSource.Studied);

            var target = OperationsView.HuntTargets(s).Single();
            Assert.AreEqual(beast.Id, target.Id);
            Assert.AreEqual("Préfecture de Heshan", target.Place);
        }

        [Test]
        public void HuntCandidates_AreTheMembersFitToGo()
        {
            var s = NewGame();
            var candidates = OperationsView.HuntCandidates(s);
            Assert.IsTrue(candidates.All(c => s.Clan.FindById(c.Id).Realm >= CultivationRealm.QiRefinement));
            Assert.IsTrue(candidates.Count > 0);
        }

        [Test]
        public void HuntPreview_GivesTheOddsTheTracesAndTheCosts_OrWhyItCannotBe()
        {
            var s = NewGame();
            var beast = s.Bestiary.In("heshan").First();
            s.Knowledge.Reveal(FactKind.Beast, beast.Id, KnowledgeSource.Studied);
            var striker = OperationsView.HuntCandidates(s).First();
            var plan = new HuntPlan
            {
                TargetBeastId = beast.Id,
                Team = new Dictionary<string, HuntRole> { [striker.Id] = HuntRole.Striker },
                Cover = CoverStory.Trade,
                Aid = MirrorAid.Illusion
            };

            var preview = OperationsView.HuntPreview(s, plan);

            StringAssert.Contains("fenêtre", preview.Refusal, "the first ritual is years away");
            Assert.That(preview.Approach, Is.InRange(1, 99));
            Assert.That(preview.Capture, Is.InRange(1, 99));
            Assert.AreEqual(Fixtures.Content.Balance.Hunt.CoverStones[(int)CoverStory.Trade], preview.Stones);
            Assert.AreEqual(Fixtures.Content.Balance.Hunt.AidMirrorCost[(int)MirrorAid.Illusion], preview.MirrorPower);

            s.Talismans.RestoreCalendar(s.Clock.Year);
            Assert.IsNull(OperationsView.HuntPreview(s, plan).Refusal);
        }

        [Test]
        public void Labels_NameEveryChoiceDifferently()
        {
            foreach (var labels in new[]
            {
                System.Enum.GetValues(typeof(HuntRole)).Cast<HuntRole>().Select(OperationsView.RoleLabel),
                System.Enum.GetValues(typeof(HuntTiming)).Cast<HuntTiming>().Select(OperationsView.TimingLabel),
                System.Enum.GetValues(typeof(CoverStory)).Cast<CoverStory>().Select(OperationsView.CoverLabel),
                System.Enum.GetValues(typeof(MirrorAid)).Cast<MirrorAid>().Select(OperationsView.AidLabel)
            })
            {
                var list = labels.ToList();
                Assert.IsTrue(list.All(l => !string.IsNullOrWhiteSpace(l)) && list.Distinct().Count() == list.Count);
            }
        }

        // ---- The secret: signs, never numbers (D7) ----

        [Test]
        public void Signs_ShowWhatTheMirrorPerceives_NeverTheNumbers()
        {
            var s = NewGame();
            s.Suspicion.AddToClan("Famille Ruan", 35);
            s.Suspicion.AddMirrorClues("Famille Lou", 30);

            var signs = OperationsView.Signs(s);

            Assert.AreEqual("on enquête sur le clan", signs.Single(p => p.Power == "Famille Ruan").Sign);
            Assert.AreEqual("des questions circulent", signs.Single(p => p.Power == "Famille Lou").Sign);
            Assert.AreEqual("calme", signs.Single(p => p.Power == "Famille Fang").Sign);
            Assert.IsFalse(signs.Any(p => p.Sign.Any(char.IsDigit)), "the hidden ledger shows no figure");
        }

        [Test]
        public void Signs_ShowTheInvestigatorWhoCame()
        {
            var s = NewGame();
            s.Secrets.RestoreConfrontation(new Confrontation("Secte du Pic des Nuées", 1));
            Assert.AreEqual("un enquêteur est venu", OperationsView.Signs(s).Single(p => p.Power == "Secte du Pic des Nuées").Sign);
        }

        [Test]
        public void Keepers_AreThoseInTheSecret_AndWhetherTheySwore()
        {
            var s = NewGame();
            var members = s.Clan.LivingMembers.ToList();
            members[1].KnowsMirrorSecret = true;
            members[2].KnowsMirrorSecret = true;
            s.Oaths.Swear(members[1], members[0], new[] { "keep-secret" });

            var keepers = OperationsView.Keepers(s);

            Assert.AreEqual(2, keepers.Count);
            Assert.IsTrue(keepers.Single(k => k.Id == members[1].ID).Sworn);
            Assert.IsFalse(keepers.Single(k => k.Id == members[2].ID).Sworn);
        }
    }
}

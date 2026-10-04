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
        public void Ritual_OffersOnlyTheBeastsOfRank()
        {
            var s = NewGame();
            s.Talismans.RestoreCalendar(s.Clock.Year);
            s.Resources.AddPrayers(Fixtures.Content.Balance.Talismans.PrayersPerRitual);
            s.Resources.AddBeast(new CapturedBeast("weak", CultivationRealm.Embryonic, 3, null));
            var ritual = OperationsView.Ritual(s);
            Assert.IsEmpty(ritual.Beasts, "a beast below the Qi Cultivation gives no talisman");
            StringAssert.Contains("Culture du Qi", ritual.Refusal);
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
            s.Mirror.Restore(s.Mirror.MirrorPower, 3); // grand illusions come with three shards (audit §3.6)
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

        [Test]
        public void HuntCandidates_LeaveOutThoseAlreadyOnAnOperation()
        {
            var s = NewGame();
            var busy = OperationsView.HuntCandidates(s).First();
            s.Clan.FindById(busy.Id).LastOperationYear = s.Clock.Year;
            Assert.IsFalse(OperationsView.HuntCandidates(s).Any(c => c.Id == busy.Id));
        }

        [Test]
        public void Ritual_SaysSoWhenNoMemberCanBearATalisman()
        {
            var s = NewGame();
            s.Talismans.RestoreCalendar(s.Clock.Year);
            s.Resources.AddPrayers(Fixtures.Content.Balance.Talismans.PrayersPerRitual);
            s.Resources.AddBeast(new CapturedBeast("b1", CultivationRealm.QiRefinement, 1, null));
            foreach (var m in s.Clan.LivingMembers) m.TalismanQiId = "prolong-life";

            StringAssert.Contains("porteur", OperationsView.Ritual(s).Refusal);
        }
        [Test]
        public void FalseProofRefusal_MatchesEveryRefusalOfTheMirror()
        {
            var s = NewGame();
            const string peak = "Secte du Pic des Nuées", ruan = "Famille Ruan";

            Assert.IsNull(OperationsView.FalseProofRefusal(s, peak, ruan));
            StringAssert.Contains("différentes", OperationsView.FalseProofRefusal(s, ruan, ruan));
            StringAssert.Contains("inconnue", OperationsView.FalseProofRefusal(s, "Puissance disparue", ruan));
            StringAssert.Contains("inconnue", OperationsView.FalseProofRefusal(s, peak, null));

            s.Mirror.ConsumePower(s.Mirror.MirrorPower);
            StringAssert.Contains("puissance du miroir", OperationsView.FalseProofRefusal(s, peak, ruan));
            Assert.IsFalse(s.Secrets.PlantFalseProof(peak, ruan), "the view says no when the mirror says no");
        }

        // ---- The captives (L6a) ----

        [Test]
        public void Captives_ShowOurCaptives_WithTheirRansom_AndTheAgentsWeHold()
        {
            var s = NewGame();
            var member = s.Clan.LivingMembers.Last();
            member.KnowsMirrorSecret = true;
            s.Captives.Take(member, "Famille Ruan");
            s.Captives.RestorePrisoners(new[] { new Prisoner("agent-1", "Famille Lou", CultivationRealm.Foundation, 1) { Interrogated = true } });
            var schemes = Fixtures.Content.Balance.Schemes;

            var captives = OperationsView.Captives(s);

            var held = captives.Held.Single();
            Assert.AreEqual((member.ID, "Famille Ruan", 0, true), (held.Id, held.Captor, held.Years, held.KnowsSecret));
            Assert.AreEqual(SchemeRules.Ransom(member.Realm, schemes), held.Ransom);
            var agent = captives.Agents.Single();
            Assert.AreEqual(new AgentLine("agent-1", "Famille Lou", MirrorChronicles.Characters.RankCatalog.RealmName(CultivationRealm.Foundation), SchemeRules.Ransom(CultivationRealm.Foundation, schemes), true, false), agent);
        }

        // ---- Spouses from elsewhere (2026-09-27) ----

        [Test]
        public void Spouses_FromThePowers_AreListed_TheirSecretOnlyOnceSounded()
        {
            var s = NewGame();
            var member = s.Clan.LivingMembers.Last();
            member.FromFaction = "Porte du Chrysanthème Noir";
            member.SpyFor = "Porte du Chrysanthème Noir";

            var line = OperationsView.Spouses(s).Single();
            Assert.AreEqual((member.ID, "Porte du Chrysanthème Noir", false, (string)null), (line.Id, line.From, line.Sounded, (string)line.SpyFor));

            s.Mirror.AddPower(100);
            s.Intrigues.Unmask(member.ID);
            Assert.AreEqual("Porte du Chrysanthème Noir", OperationsView.Spouses(s).Single().SpyFor);
        }

        [Test]
        public void ThePowersChosen_AreShown_OnlyToAPurpleMansion_WithTheirOdds()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var power = s.Factions.Factions.First();
            power.HighestRealm = CultivationRealm.PurpleMansion;
            s.Events.TriggerElderDied(power, new FactionElder { Id = "a", Name = "ancien", Realm = CultivationRealm.GoldenCore, Stage = 1, MaxLifespan = 1000 }, false);
            if (s.Rebirths.Pending.Count == 0) Assert.Inconclusive("no rebirth at these odds");
            Assert.IsEmpty(OperationsView.Chosen(s));
            var mansion = Fixtures.Cultivator(age: 200, realm: CultivationRealm.PurpleMansion, stage: 2);
            s.Clan.AddMember(mansion);
            var line = OperationsView.Chosen(s).Single();
            Assert.AreEqual(power.Name, line.Power);
            StringAssert.Contains(mansion.FullName, line.Harvests.Single().Label);
            Assert.Greater(line.Harvests.Single().Percent, 0);
        }

        [Test]
        public void OnlyAReturnedAncestor_IsOfferedToBendAPowersMind()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            Assert.IsEmpty(OperationsView.Bends(s));
            var ancestor = Fixtures.Cultivator(age: 40, realm: CultivationRealm.GoldenCore);
            ancestor.RebornFrom = "Mo l'Ancien";
            s.Clan.AddMember(ancestor);
            var line = OperationsView.Bends(s).First();
            StringAssert.Contains(ancestor.FullName, line.Label);
            Assert.Greater(line.Percent, 0);
        }
    }
}

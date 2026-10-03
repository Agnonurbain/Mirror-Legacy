using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Diplomacy
{
    /// <summary>
    /// The precious things are not bought with stones (the user's rule, 2026-10-03): a minor ability, a holder's leave, a
    /// Purple Mansion's artifact, a high technique are paid in kind — knowledge, secrets, beasts, Qi, artifacts, a debt, a
    /// disciple. Stones weigh nothing there; ordinary goods still sell for stones.
    /// </summary>
    [TestFixture]
    public class PreciousTradeTests
    {
        private const string Qi = "clear-spring-qi";

        private static GameSession Session()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            s.Resources.AddSpiritStones(1_000_000);
            return s;
        }

        private static List<AccordTerm> Stones() => new List<AccordTerm> { new AccordTerm(AccordCurrency.Stones, null, 1_000_000) };

        /// <summary>Qi portions worth at least <paramref name="worth"/>, given to the clan to offer.</summary>
        private static List<AccordTerm> QiWorth(GameSession s, int worth)
        {
            int portions = worth / s.Context.Content.Balance.KnowledgeTrade.QiWorthPerPortion + 1;
            s.Resources.AddQi(Qi, portions);
            return new List<AccordTerm> { new AccordTerm(AccordCurrency.Qi, Qi, portions) };
        }

        private static FactionData HolderOf(GameSession s, string lineage)
        {
            var power = s.Factions.Factions.First(f => f.Elders.Count > 0);
            power.Elders[0].FruitionId = lineage;
            power.RelationWithPlayer = 60;
            return power;
        }

        [Test]
        public void AMinorAbility_IsNeverSoldForStones_ButForQi()
        {
            var s = Session();
            var power = HolderOf(s, "gathered-wood");
            Assert.IsNotNull(s.Minors.Buy("gathered-wood", power.Name, Stones()), "too precious for stones");
            Assert.IsNull(s.Minors.Buy("gathered-wood", power.Name, QiWorth(s, s.Context.Content.Balance.MinorAbilities.Worth)));
        }

        [Test]
        public void AHoldersLeave_IsNotBoughtWithStones()
        {
            var s = Session();
            var lineage = s.Context.Content.Fruitions.Select(f => f.Id).First(id => s.Fruitions.State(id).Status == FruitionStatus.Occupied);
            int stones = s.Resources.SpiritStones;
            Assert.IsFalse(s.GoldenCore.RequestPermission(lineage, Stones()));
            Assert.AreEqual(stones, s.Resources.SpiritStones, "stones are no tribute for a True Monarch");
            var terms = QiWorth(s, s.Context.Content.Balance.GoldenCore.PermissionWorth);
            int qi = s.Resources.QiPortions(Qi);
            s.GoldenCore.RequestPermission(lineage, terms);
            Assert.Less(s.Resources.QiPortions(Qi), qi, "the tribute is given, even if refused");
        }

        [Test]
        public void APurpleMansionsArtifact_IsCommissionedInKind_AndNeverSoldForStones()
        {
            var s = Session();
            var sect = s.Factions.Factions.First(f => f.Kind == FactionKind.Sect);
            sect.RelationWithPlayer = 60;
            string form = s.Context.Content.ArtifactForms[0].Id;
            Assert.IsNotNull(s.ArtifactTrade.Commission(sect.Name, form, CultivationRealm.PurpleMansion));
            Assert.IsNull(s.ArtifactTrade.Commission(sect.Name, form, CultivationRealm.PurpleMansion,
                QiWorth(s, s.ArtifactTrade.CommissionPrice(CultivationRealm.PurpleMansion))));
            var spiritual = s.Artifacts.Armoury.Single();
            Assert.IsNotNull(s.ArtifactTrade.Sell(spiritual.Id, sect.Name), "offered in an accord, never sold");
            var dharma = s.Artifacts.Create(form, CultivationRealm.Foundation, null);
            Assert.IsNull(s.ArtifactTrade.Sell(dharma.Id, sect.Name), "a Dharma Artifact still sells");
        }

        [Test]
        public void AHighTechnique_IsNotBoughtWithStones()
        {
            var s = Session();
            var high = s.Context.Content.Techniques.First(t => MirrorChronicles.Characters.TechniqueRules.HasPurpleMansionSecret(t) && !s.Techniques.Knows(t.ID));
            var power = s.Factions.Factions.First();
            power.Techniques.Add(high.ID);
            power.RelationWithPlayer = 90;
            Assert.IsFalse(s.Exchange.BuyTechnique(power.Name, high.ID));
            Assert.AreEqual(0, s.Accords.WorthOf(power.Name, high.ID, new AccordTerm(AccordCurrency.Stones, null, 1_000_000)));
        }

        [Test]
        public void AnArtifact_IsGivenInAnAccord()
        {
            var s = Session();
            var power = HolderOf(s, "gathered-wood");
            var treasure = s.Artifacts.Create(s.Context.Content.ArtifactForms[0].Id, CultivationRealm.PurpleMansion, null, ArtifactClass.SpiritualTreasure);
            var artifact = s.Artifacts.Create(s.Context.Content.ArtifactForms[0].Id, CultivationRealm.PurpleMansion, null);
            var terms = new List<AccordTerm> { new AccordTerm(AccordCurrency.Artifact, treasure.Id, 1), new AccordTerm(AccordCurrency.Artifact, artifact.Id, 1) };
            Assert.IsNull(s.Minors.Buy("gathered-wood", power.Name, terms));
            Assert.IsEmpty(s.Artifacts.Armoury);
            Assert.IsTrue(power.Artifacts.Any(a => a.Id == treasure.Id));
        }
    }
}

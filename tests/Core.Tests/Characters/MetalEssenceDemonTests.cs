using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Metal Essence Demons (LORE.md §6.9; L4e, user decisions 2026-10-03). A demon born of the clan waits for the
    /// clan's choice: leave it to the Underworld (the custom, safe), take its essence back (a Golden Core force must seal
    /// it; the Underworld, provoked, bears a grudge: no ancestor of the clan is reborn, and any new demon of the clan is
    /// claimed at once — a bribe of essence soothes it), or let it be: it ravages the region for years by its rank in the
    /// hierarchy, then the Underworld claims it; a Golden Core of the clan may subdue it. Unanswered for a year, the
    /// custom prevails.
    /// </summary>
    [TestFixture]
    public class MetalEssenceDemonTests
    {
        private static GameContent With(System.Func<DemonSettings, DemonSettings> tweak = null, double rebirth = 0)
        {
            var b = Fixtures.QuietContent.Balance;
            return Fixtures.QuietContent with
            {
                Balance = b with
                {
                    Demons = tweak == null ? b.Demons : tweak(b.Demons),
                    Ancestors = b.Ancestors with { RebirthChance = rebirth, HarvestChance = 0 },
                }
            };
        }

        private static GameSession Session(GameContent content = null) =>
            GameSession.NewGame(new GameSetup { Seed = 1, Content = content ?? With() });

        private static CharacterData Fail(GameSession s, GoldenCoreState held = GoldenCoreState.None)
        {
            var m = Fixtures.Cultivator(age: 300, realm: CultivationRealm.PurpleMansion, stage: 4);
            m.GoldenCore = held;
            s.Clan.AddMember(m);
            s.Clan.Kill(m, DeathCause.MetalEssenceDemon);
            s.Events.TriggerMetalEssenceDemon(m);
            return m;
        }

        private static CharacterData Monarch(GameSession s)
        {
            var m = Fixtures.Cultivator(age: 400, realm: CultivationRealm.GoldenCore);
            m.GoldenCore = GoldenCoreState.MetallicEssenceOnly;
            s.Clan.AddMember(m);
            return m;
        }

        private static void Years(GameSession s, int years)
        {
            for (int i = 0; i < years; i++)
            {
                s.Clock.Restore(s.Clock.Year + 1, s.Clock.Phase);
                s.Demons.ProcessYear();
            }
        }

        [Test]
        public void ADemonOfTheClan_AwaitsTheClansChoice_RankedByItsOrigin()
        {
            var s = Session();
            var failed = Fail(s);
            var demon = s.Demons.Pending.Single();
            Assert.AreEqual(failed.FullName, demon.Name);
            Assert.AreEqual(DemonTier.Ascent, demon.Tier, "a failed ascent: Heaven had already recognised the essence");
        }

        [Test]
        public void LeftToTheUnderworld_ItIsGone_AndNobodyIsProvoked()
        {
            var s = Session();
            Fail(s);
            Assert.IsNull(s.Demons.LeaveToTheUnderworld(s.Demons.Pending[0].Id));
            Assert.IsEmpty(s.Demons.Pending);
            Assert.IsEmpty(s.Demons.Ravaging);
            Assert.AreEqual(0, s.Demons.Grudge);
        }

        [Test]
        public void TakingTheEssence_NeedsAGoldenCoreForce()
        {
            var s = Session();
            Fail(s);
            StringAssert.Contains("Noyau d'Or", s.Demons.TakeTheEssence(s.Demons.Pending[0].Id));
            Monarch(s);
            Assert.IsNull(s.Demons.TakeTheEssence(s.Demons.Pending[0].Id));
            Assert.AreEqual(1, s.Demons.Essences);
            Assert.Greater(s.Demons.Grudge, 0, "against the custom: the Underworld is provoked");
        }

        [Test]
        public void WhileTheUnderworldBearsAGrudge_NoAncestorIsReborn()
        {
            var s = Session(With(rebirth: 1.0));
            Fail(s);
            Monarch(s);
            s.Demons.TakeTheEssence(s.Demons.Pending[0].Id);
            var ancestor = Monarch(s);
            s.Clan.Kill(ancestor, DeathCause.OldAge);
            var child = Fixtures.Mortal(age: 0);
            s.Clan.AddMember(child);
            Assert.IsNull(child.RebornFrom, "it keeps the registers of the living");
        }

        [Test]
        public void WhileTheUnderworldBearsAGrudge_ItClaimsTheClansNextDemonAtOnce()
        {
            var s = Session();
            Fail(s);
            Monarch(s);
            s.Demons.TakeTheEssence(s.Demons.Pending[0].Id);
            Fail(s);
            Assert.IsEmpty(s.Demons.Pending, "its emissaries were waiting");
        }

        [Test]
        public void ABribeOfEssence_SoothesTheGrudge()
        {
            var s = Session();
            Fail(s);
            Monarch(s);
            s.Demons.TakeTheEssence(s.Demons.Pending[0].Id);
            Assert.IsNull(s.Demons.Bribe());
            Assert.AreEqual(0, s.Demons.Grudge);
            Assert.AreEqual(0, s.Demons.Essences);
            Assert.IsNotNull(s.Demons.Bribe(), "nothing left to give");
        }

        [Test]
        public void TheGrudge_FadesWithTheYears()
        {
            var s = Session();
            Fail(s);
            Monarch(s);
            s.Demons.TakeTheEssence(s.Demons.Pending[0].Id);
            Years(s, s.Context.Content.Balance.Demons.GrudgeYears + 1);
            Assert.AreEqual(0, s.Demons.Grudge);
        }

        [Test]
        public void LetBe_ItRavagesTheRegion_ThenTheUnderworldClaimsIt()
        {
            var s = Session(With(d => d with { Tiers = d.Tiers.ToDictionary(t => t.Key, t => t.Value with { KillChance = 1.0 }) }));
            Fail(s);
            Assert.IsNull(s.Demons.LetItBe(s.Demons.Pending[0].Id));
            int living = s.Clan.LivingMembers.Count;
            Years(s, 1);
            Assert.Less(s.Clan.LivingMembers.Count, living, "it kills");
            Assert.IsTrue(s.Clan.Registry.Records.Any(r => r.CauseOfDeath == DeathCause.DemonRavaged));
            Years(s, s.Context.Content.Balance.Demons.Tiers[DemonTier.Ascent].RavageYears + 1);
            Assert.IsEmpty(s.Demons.Ravaging, "the Underworld claims it in the end");
        }

        [Test]
        public void AGoldenCoreOfTheClan_SubduesARavagingDemon()
        {
            var s = Session();
            Fail(s);
            s.Demons.LetItBe(s.Demons.Pending[0].Id);
            var id = s.Demons.Ravaging[0].Id;
            StringAssert.Contains("Noyau d'Or", s.Demons.Subdue(id));
            Monarch(s);
            Assert.IsNull(s.Demons.Subdue(id));
            Assert.IsEmpty(s.Demons.Ravaging);
        }

        [Test]
        public void Unanswered_TheCustomPrevails()
        {
            var s = Session();
            Fail(s);
            Years(s, 1);
            Assert.IsEmpty(s.Demons.Pending);
            Assert.IsEmpty(s.Demons.Ravaging);
        }

        [Test]
        public void ARealizationHoldersDemon_IsTheGreatest()
        {
            var s = Session();
            Fail(s, GoldenCoreState.Realization);
            Assert.AreEqual(DemonTier.Realization, s.Demons.Pending.Single().Tier);
            var tiers = s.Context.Content.Balance.Demons.Tiers;
            Assert.Greater(tiers[DemonTier.Realization].RavageYears, tiers[DemonTier.Ascent].RavageYears);
        }

        [Test]
        public void TheDemons_AndTheGrudge_SurviveASave_AndTheChronicleTellsThem()
        {
            var content = With();
            var s = Session(content);
            var chronicle = new MirrorChronicles.Presentation.Chronicle(s);
            Fail(s);
            Assert.IsTrue(chronicle.Entries.Any(e => e.Contains("Démon d'Essence Métallique")));
            Fail(s);
            Monarch(s);
            s.Demons.TakeTheEssence(s.Demons.Pending[0].Id);
            var reloaded = GameSession.FromSaveData(SaveSerializer.Deserialize(SaveSerializer.Serialize(s.ToSaveData())), new GameSetup { Content = content });
            Assert.AreEqual(s.Demons.Pending.Count, reloaded.Demons.Pending.Count);
            Assert.AreEqual(s.Demons.Grudge, reloaded.Demons.Grudge);
            Assert.AreEqual(1, reloaded.Demons.Essences);
        }
    }
}

using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Tests.Presentation
{
    /// <summary>A hunt's capture on the battle screen (G6): the beast, its rank and owner, how weakened it is, the strikers.</summary>
    [TestFixture]
    public class HuntBattleViewTests
    {
        private static (GameSession s, CharacterData striker) Hunting()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent, InteractiveBattles = true });
            var beast = new WorldBeast("iron-boar-heshan-1", "iron-boar", "heshan", CultivationRealm.QiRefinement, 2, "Famille Ruan");
            s.Bestiary.Restore(new[] { beast });
            s.Knowledge.Reveal(FactKind.Beast, beast.Id, KnowledgeSource.Studied);
            var striker = s.Clan.LivingMembers.First(m => s.Challenges.FighterRefusal(m) == null);
            var lure = s.Clan.LivingMembers.First(m => m != striker);
            s.HuntBattles.Restore(new PendingHunt(new HuntPlan
            {
                TargetBeastId = beast.Id,
                Team = { [striker.ID] = HuntRole.Striker, [lure.ID] = HuntRole.Lure }
            }, s.Clock.Year));
            return (s, striker);
        }

        [Test]
        public void Hunt_IsNull_WhenNoneAwaits()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.VeteranQuietContent });
            Assert.IsNull(BattleView.Hunt(s));
        }

        [Test]
        public void Hunt_TellsTheBeast_ItsRankAndOwner()
        {
            var (s, _) = Hunting();
            var view = BattleView.Hunt(s);
            var species = s.Context.Content.BeastSpecies.First(b => b.Id == "iron-boar").Name;
            Assert.AreEqual((species, RankCatalog.RealmName(CultivationRealm.QiRefinement), "Famille Ruan"), (view.Beast, view.Rank, view.Owner));
        }

        [Test]
        public void Hunt_TellsHowWeakenedTheBeastIs()
        {
            var (s, _) = Hunting();
            Assert.AreEqual((int)System.Math.Round(100 * s.Context.Content.Balance.Hunt.BattleLureWeaken), BattleView.Hunt(s).WeakenedPercent);
        }

        [Test]
        public void Hunt_ShowsTheStrikers()
        {
            var (s, striker) = Hunting();
            CollectionAssert.AreEqual(new[] { striker.FullName }, BattleView.Hunt(s).Strikers);
        }
    }
}

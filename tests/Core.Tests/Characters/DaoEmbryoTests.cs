using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// A member who reaches the Dao Embryo stays in the world and in the clan (the user's decision, 2026-09-30,
    /// LORE.md §11.9): the Dao Embryos who left for the Outer Sky belong to the world's history, not to the clan.
    /// </summary>
    [TestFixture]
    public class DaoEmbryoTests
    {
        [Test]
        public void ADaoEmbryo_StaysAliveInTheClan()
        {
            var s = GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });
            var c = s.Clan.GetPatriarch();
            c.Realm = CultivationRealm.DaoEmbryo;

            s.Events.TriggerBreakthroughSuccess(c, CultivationRealm.DaoEmbryo);

            Assert.IsTrue(c.IsAlive && s.Clan.LivingMembers.Contains(c) && s.Clan.PatriarchID == c.ID);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Tests.Characters
{
    /// <summary>
    /// The Mandate of Life (LORE.md §5.4.3; L4c, user decision 2026-10-03): an ability's data names the life events that
    /// embody its image; when one befalls a Purple Mansion condensing it, the condensation leaps forward — Kuang Yao,
    /// ambushed and oppressed, cultivated « Besieged Monarch » at an incredible speed. A reign or a seclusion feeds it year
    /// after year, more slowly.
    /// </summary>
    [TestFixture]
    public class MandateOfLifeTests
    {
        private const string Besieged = "bright-yang:besieged-monarch";

        private static GameSession Session() => GameSession.NewGame(new GameSetup { Seed = 1, Content = Fixtures.QuietContent });

        private static string AbilityNamed(GameContent c, string name) =>
            c.Fruitions.SelectMany(f => f.Abilities.Where(a => a.Name == name).Select(a => $"{f.Id}:{a.Id}")).First();

        private static CharacterData Condensing(GameSession s, string ability)
        {
            var m = Fixtures.Cultivator(age: 200, realm: CultivationRealm.PurpleMansion);
            m.PursuedAbility = ability;
            m.CultivationXP = 0;
            s.Clan.AddMember(m);
            return m;
        }

        [Test]
        public void TheBesiegedMonarch_IsEmbodiedByPeril()
        {
            var c = Fixtures.Content;
            var (lineage, id) = FoundationRef.Parse(AbilityNamed(c, "Monarque Assiégé"));
            var ability = c.Fruitions.Single(f => f.Id == lineage).Abilities.Single(a => a.Id == id);
            CollectionAssert.Contains(ability.Mandate, LifeMandate.Peril, "Kuang Yao, ambushed and oppressed (LORE.md §5.4.3)");
        }

        [Test]
        public void APerilThatEmbodiesTheImage_AdvancesTheCondensation()
        {
            var s = Session();
            var m = Condensing(s, AbilityNamed(s.Context.Content, "Monarque Assiégé"));
            s.Events.TriggerMemberImperilled(m);
            Assert.Greater(m.CultivationXP, 0);
        }

        [Test]
        public void AnEventForeignToTheImage_DoesNothing()
        {
            var s = Session();
            var m = Condensing(s, AbilityNamed(s.Context.Content, "Monarque Assiégé"));
            var kin = Fixtures.Cultivator();
            kin.FatherID = m.ID;
            s.Clan.AddMember(kin);
            s.Clan.Kill(kin, DeathCause.OldAge); // a mourning: not the besieged monarch's image
            Assert.AreEqual(0, m.CultivationXP);
        }

        [Test]
        public void AMourning_FeedsAnAbilityOfFarewell()
        {
            var s = Session();
            var m = Condensing(s, AbilityNamed(s.Context.Content, "Adieu au Fleuve"));
            var child = Fixtures.Cultivator();
            child.FatherID = m.ID;
            s.Clan.AddMember(child);
            s.Clan.Kill(child, DeathCause.OldAge);
            Assert.Greater(m.CultivationXP, 0, "the farewell to a child");
        }

        [Test]
        public void AReign_FeedsTheThronesGaze_YearAfterYear_MoreSlowly()
        {
            var s = Session();
            var m = Condensing(s, AbilityNamed(s.Context.Content, "Regard du Trône"));
            s.Clan.AppointPatriarch(m);
            s.Mandate.ProcessYear();
            int yearly = m.CultivationXP;
            Assert.Greater(yearly, 0);
            var other = Condensing(s, AbilityNamed(s.Context.Content, "Monarque Assiégé"));
            s.Events.TriggerMemberImperilled(other);
            Assert.Greater(other.CultivationXP, yearly, "an event leaps; a reign feeds slowly");
        }

        [Test]
        public void ACapture_AndAWar_Embody_TheirImages()
        {
            var s = Session();
            var captive = Condensing(s, AbilityNamed(s.Context.Content, "Brume des Bannis"));
            s.Events.TriggerMemberCaptured(captive, s.Factions.Factions[0].Name);
            Assert.Greater(captive.CultivationXP, 0);
            var warrior = Condensing(s, AbilityNamed(s.Context.Content, "Lame du Juste Tranchant"));
            s.Events.TriggerWarBegun(s.Factions.Factions[0].Name, MirrorChronicles.World.SecretBook.ClanHolder);
            Assert.Greater(warrior.CultivationXP, 0);
        }
    }
}

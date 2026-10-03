using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// The pilot and the artifacts (L4f, 2026-10-03): it borrows from a friendly power, its forge makes one artifact a year
    /// when the treasury allows (a guard while a ripe Dao is coveted, a weapon otherwise), and it arms its members, the
    /// strongest first, with the best artifact each can wield — its own lineage first. It steals none, and binds no
    /// Foundation that may still rise.
    /// </summary>
    public static partial class BalanceRun
    {
        private const int MostArtifactsInStore = 2; // the forge rests while the armoury holds this many unborne

        private static void ArmTheClan(GameSession session)
        {
            BorrowAnArtifact(session);
            ForgeAnArtifact(session);
            HandOutTheArtifacts(session);
        }

        private static void BorrowAnArtifact(GameSession session)
        {
            if (session.Artifacts.Armoury.Count > 0) return; // a favour is not spent while the armoury holds something
            if (!session.Clan.LivingMembers.Any(m => m.CaptorFaction == null && m.Artifact == null && m.Realm >= CultivationRealm.QiRefinement)) return;
            int loanRelation = session.Context.Content.Balance.Artifacts.Trade.LoanRelation;
            foreach (var power in session.Factions.Factions.Where(f => f.RelationWithPlayer >= loanRelation).OrderByDescending(f => f.RelationWithPlayer).ToList())
                if (session.ArtifactTrade.Borrow(power.Name) == null) return; // one a year
        }

        private static void ForgeAnArtifact(GameSession session)
        {
            if (session.Artifacts.Armoury.Count >= MostArtifactsInStore) return;
            var settings = session.Context.Content.Balance.Artifacts;
            var smith = session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Retreat == Retreat.None && m.ID != session.Clan.PatriarchID
                    && m.Realm >= CultivationRealm.QiRefinement && m.LastOperationYear != session.Clock.Year)
                .OrderByDescending(m => m.Realm).FirstOrDefault();
            if (smith == null) return;
            var rank = settings.Forging.Keys.Where(r => r <= smith.Realm && settings.Forging[r].ForgeLevel <= session.Buildings.ForgeLevel)
                .DefaultIfEmpty(CultivationRealm.Embryonic).Max();
            if (rank < CultivationRealm.QiRefinement) return;
            var cost = settings.Forging[rank];
            if (session.Resources.SpiritualOres < cost.Ores || !Affords(session, cost.Stones, BuildReserveYears)) return;
            bool coveted = session.Clan.LivingMembers.Any(m => FoundationRules.IsPrey(m, session.Context.Content) && session.DaoHunts.IsCoveted(m));
            var effect = coveted ? ArtifactEffect.Protection : ArtifactEffect.Combat;
            var form = session.Context.Content.ArtifactForms.First(f => f.Effect == effect).Id;
            session.Forge.Make(form, rank, smith.ID);
        }

        /// <summary>How much a member gains from an artifact: its rank, its lineage, and a guard for a coveted Dao.</summary>
        private static int Worth(GameSession session, CharacterData member, ArtifactInstance a)
        {
            if (a == null || a.Rank > member.Realm) return -1;
            bool kin = a.Lineage != null && FoundationRef.Parse(member.FoundationId).FruitionId == a.Lineage;
            bool guard = a.Effect == ArtifactEffect.Protection && FoundationRules.IsPrey(member, session.Context.Content);
            return (int)a.Rank * 10 + (a.Class == ArtifactClass.SpiritualTreasure ? 5 : 0) + (kin ? 3 : 0) + (guard ? 8 : 0);
        }

        private static void HandOutTheArtifacts(GameSession session)
        {
            foreach (var member in session.Clan.LivingMembers.Where(m => m.CaptorFaction == null && !m.TreasureBound && m.Realm >= CultivationRealm.QiRefinement)
                         .OrderByDescending(m => Mirror.HuntRules.Power(m.Realm, m.RealmStage)).ToList())
            {
                var best = session.Artifacts.Armoury.OrderByDescending(a => Worth(session, member, a)).FirstOrDefault();
                if (best != null && Worth(session, member, best) > Worth(session, member, member.Artifact)) session.Artifacts.Equip(member, best.Id);
            }
        }
    }
}

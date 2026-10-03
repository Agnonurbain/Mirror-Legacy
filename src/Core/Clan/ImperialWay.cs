using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// The Imperial Way (LORE.md §3.6, §5.9 R20; user decisions 2026-10-03). The clan founds its kingdom as the powers do —
    /// the hardest founding: its sect founded first, a True Monarch living, three vassals, half the greatest power's weight.
    /// Then its sovereign (the patriarch) cultivates by governing: on the throne at the Purple Mansion, its odds of the
    /// Golden Core grow each year with its vassals and its people, up to a cap; a new sovereign starts anew. The forge
    /// itself stays the clan's own (the precious Purple Mansion still waits for good odds); forged so, the core is imperial.
    /// </summary>
    public sealed class ImperialWay
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly SectSystem sect;
        private readonly TreatySystem treaties;
        private readonly FactionManager factions;

        public ImperialWay(GameContext ctx, ClanManager clan, SectSystem sect, TreatySystem treaties, FactionManager factions)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.sect = sect;
            this.treaties = treaties;
            this.factions = factions;
        }

        private ImperialWaySettings Settings => ctx.Content.Balance.ImperialWay;

        public int? KingdomYear { get; private set; }
        public bool IsKingdom => KingdomYear != null;
        public string SovereignId { get; private set; }
        public int Merit { get; private set; }

        public void Restore(int? kingdomYear, string sovereignId, int merit)
        {
            KingdomYear = kingdomYear;
            SovereignId = sovereignId;
            Merit = merit;
        }

        private int Vassals => treaties.All.Count(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain);

        /// <summary>Null when the clan may found its kingdom, else why not (French).</summary>
        public string FoundingRefusal()
        {
            var rules = ctx.Content.Balance.PowerLifecycle; // as the powers do (the user's choice)
            if (IsKingdom) return "le clan règne déjà sur son royaume";
            if (!sect.Founded) return "il faut d'abord fonder la secte du clan";
            if (!clan.LivingMembers.Any(m => m.CaptorFaction == null && m.Realm >= CultivationRealm.GoldenCore))
                return "il faut un Vrai Monarque vivant pour porter une couronne";
            if (Vassals < rules.KingdomVassals) return $"il faut {rules.KingdomVassals} vassaux (le clan en compte {Vassals})";
            var wars = ctx.Content.Balance.Wars;
            double greatest = factions.Factions.Select(f => WarRules.Strength(f, wars)).DefaultIfEmpty(0).Max();
            if (WarRules.ClanWarStrength(clan.LivingMembers, wars) < greatest * rules.KingdomPowerShare)
                return "le clan doit peser au moins la moitié de la plus grande puissance";
            return null;
        }

        /// <summary>The clan founds its kingdom; the crowns of the world resent it. Null when done, else why not (French).</summary>
        public string Found()
        {
            string refusal = FoundingRefusal();
            if (refusal != null) return refusal;
            KingdomYear = ctx.Clock.Year;
            foreach (var state in factions.Factions.Where(f => f.Kind == FactionKind.State).ToList())
                factions.ChangeRelation(state.ID, -Settings.CrownRelationLoss);
            ctx.Log.Info($"[Imperial] The {clan.ClanName} clan founds its kingdom.");
            ctx.Events.TriggerClanKingdomFounded();
            return null;
        }

        /// <summary>The powers' bonds, to count a kingdom's vassals (set by the session).</summary>
        public PowerPoliticsSystem Politics { get; set; }

        public int WorldMerit(FactionData power) => power?.ImperialMerit ?? 0;

        /// <summary>What governing adds to a kingdom's sovereign elder's odds of the Golden Core (0 to 1).</summary>
        public double GoverningBonus(FactionData power, FactionElder elder) =>
            power != null && power.Kind == FactionKind.State && elder != null && elder.Id == power.SovereignId ? power.ImperialMerit / 100.0 : 0;

        /// <summary>
        /// The kingdoms of the world govern too (the user's rule, 2026-10-03): a kingdom's highest elder is its sovereign; at
        /// the Purple Mansion its odds grow with the years and its vassals, to the clan's own cap; a new one starts anew.
        /// </summary>
        private void GovernTheWorld()
        {
            var s = Settings;
            foreach (var power in factions.Factions.Where(f => f.Kind == FactionKind.State))
            {
                var sovereign = power.Elders.OrderByDescending(e => e.Realm).ThenByDescending(e => e.Stage).FirstOrDefault();
                if (sovereign?.Id != power.SovereignId)
                {
                    power.SovereignId = sovereign?.Id;
                    power.ImperialMerit = 0;
                }
                if (sovereign == null || sovereign.Realm != CultivationRealm.PurpleMansion) continue;
                int vassals = Politics?.Bonds.Count(b => b.Kind == BondKind.Vassalage && b.A == power.Name) ?? 0;
                power.ImperialMerit = System.Math.Min(s.MaxMerit, power.ImperialMerit + s.MeritPerYear + vassals * s.MeritPerVassal);
            }
        }

        /// <summary>A year on the throne: a Purple Mansion sovereign cultivates by governing — the clan's, and the world's kingdoms'.</summary>
        public void ProcessYear()
        {
            GovernTheWorld();
            if (!IsKingdom) return;
            var sovereign = clan.GetPatriarch();
            if (sovereign?.ID != SovereignId)
            {
                SovereignId = sovereign?.ID; // a new reign starts anew
                Merit = 0;
            }
            if (sovereign == null || sovereign.Realm != CultivationRealm.PurpleMansion || sovereign.CaptorFaction != null) return;
            var s = Settings;
            int gain = s.MeritPerYear + Vassals * s.MeritPerVassal + clan.LivingMembers.Count / System.Math.Max(1, s.MembersPerMerit);
            Merit = System.Math.Min(s.MaxMerit, Merit + gain);
        }

        /// <summary>What governing adds to this member's odds of forging (%): only the reigning sovereign's.</summary>
        public int BonusFor(CharacterData member) =>
            IsKingdom && member != null && member.ID == SovereignId && member.ID == clan.PatriarchID ? Merit : 0;
    }
}

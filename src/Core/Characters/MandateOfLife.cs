using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Mandate of Life (LORE.md §5.4.3; L4c, user decision 2026-10-03): when a life event that embodies the image of the
    /// ability a Purple Mansion condenses befalls it, the condensation leaps forward — Kuang Yao, ambushed and oppressed,
    /// cultivated « Besieged Monarch » at an incredible speed. A capture or a release, a peril survived, a war or a victory
    /// of the clan, the death of a parent, a child or a spouse; and year after year, more slowly, a reign or a seclusion.
    /// </summary>
    public sealed class MandateOfLife
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly Diplomacy.FactionManager factions;

        public MandateOfLife(GameContext ctx, ClanManager clan, Diplomacy.FactionManager factions = null)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            var bus = ctx.Events;
            bus.OnMemberCaptured += (m, _) => Embody(m, LifeMandate.Captivity, Settings.EventXpShare);
            bus.OnMemberFreed += m => Embody(m, LifeMandate.Freedom, Settings.EventXpShare);
            bus.OnMemberImperilled += m => Embody(m, LifeMandate.Peril, Settings.EventXpShare);
            bus.OnWarBegun += (a, d) => { if (a == World.SecretBook.ClanHolder || d == World.SecretBook.ClanHolder) All(LifeMandate.War); };
            bus.OnClanWarWon += _ => All(LifeMandate.Victory);
            bus.OnChallengeSettled += (_, outcome) => { if (outcome == ChallengeOutcome.Won) All(LifeMandate.Victory); };
            bus.OnCharacterDied += (dead, _) => Mourn(dead);
            // the world's elders too (the user's rule, 2026-10-03): a war, a demon's peril — a mourning needs kin, which elders have not
            bus.OnWarBegun += (a, d) => { Leap(factions?.GetFactionByName(a)); Leap(factions?.GetFactionByName(d)); };
            bus.OnWorldDemon += demon => { foreach (var p in factions?.Factions.Where(f => f.RegionId == demon.RegionId).ToList() ?? new System.Collections.Generic.List<FactionData>()) Leap(p); };
        }

        /// <summary>An event of its power may embody an elder's image: a Purple Mansion not yet perfected gathers its five abilities.</summary>
        private void Leap(FactionData power)
        {
            if (power == null) return;
            foreach (var elder in power.Elders.Where(e => e.Realm == CultivationRealm.PurpleMansion && !e.Perfected))
                if (ctx.Rng.Chance(Settings.ElderLeapChance))
                {
                    elder.Perfected = true;
                    ctx.Log.Info($"[Mandate] {elder.Name} of {power.Name} reaches its Grand Perfection at a leap.");
                }
        }

        private MandateSettings Settings => ctx.Content.Balance.Mandate;

        private static int Xp => PowerLadder.XpForNextStage(CultivationRealm.PurpleMansion);

        private void All(LifeMandate kind)
        {
            foreach (var m in clan.LivingMembers.ToList()) Embody(m, kind, Settings.EventXpShare);
        }

        private void Mourn(CharacterData dead)
        {
            foreach (var kin in clan.LivingMembers.Where(m => m.ID == dead.FatherID || m.ID == dead.MotherID || m.ID == dead.SpouseID
                         || m.FatherID == dead.ID || m.MotherID == dead.ID).ToList())
                Embody(kin, LifeMandate.Mourning, Settings.EventXpShare);
        }

        /// <summary>A reign and a seclusion feed their images year after year.</summary>
        public void ProcessYear()
        {
            Embody(clan.GetPatriarch(), LifeMandate.Reign, Settings.YearlyXpShare);
            foreach (var m in clan.LivingMembers.Where(m => m.CurrentTask == TaskType.Seclusion).ToList())
                Embody(m, LifeMandate.Seclusion, Settings.YearlyXpShare);
        }

        private void Embody(CharacterData member, LifeMandate kind, double share)
        {
            if (member == null || !member.IsAlive || member.Realm != CultivationRealm.PurpleMansion || member.PursuedAbility == null
                || member.ProgressionSealed) return;
            var (lineage, id) = FoundationRef.Parse(member.PursuedAbility);
            var ability = ctx.Content.Fruitions.FirstOrDefault(f => f.Id == lineage)?.Abilities.FirstOrDefault(a => a.Id == id);
            if (ability == null || !ability.Mandate.Contains(kind)) return;
            member.CultivationXP += System.Math.Max(1, (int)(Xp * share));
            ctx.Log.Info($"[Mandate] {member.FullName}'s life embodies « {ability.Name} » ({kind}).");
            if (share >= Settings.EventXpShare) ctx.Events.TriggerMandateEmbodied(member, ability.Name);
        }
    }
}

using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The powers scheme against the clan for profit (L6a; LORE.md D7 « everything is a plot »: the only true friend is
    /// profit). Each year a power may ambush a member away from the domain — on an errand, a hunt, a diversion — to hold
    /// them for ransom; greed, hostility and temper drive it, friendship only makes it rarer. A failed ambush may leave
    /// its agent in the clan's hands. A power confronting the clan over the mirror plays that game instead.
    /// </summary>
    public sealed class SchemeSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly CaptiveSystem captives;
        private readonly SecretSystem secrets;

        public SchemeSystem(GameContext ctx, ClanManager clan, FactionManager factions, CaptiveSystem captives, SecretSystem secrets)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.captives = captives;
            this.secrets = secrets;
        }

        private SchemeSettings Settings => ctx.Content.Balance.Schemes;

        public void ProcessYear()
        {
            int stones = captives.ClanStones;
            foreach (var power in factions.Factions.ToList())
            {
                if (secrets.Confrontation?.Faction == power.Name) continue; // its move is the confrontation's
                if (ctx.Rng.Chance(SchemeRules.SchemeChance(power, stones, Settings))) Ambush(power);
            }
        }

        /// <summary>An ambush on a member away from the domain; false when nobody was within reach.</summary>
        public bool Ambush(FactionData power)
        {
            var exposed = clan.LivingMembers.Where(IsAway).ToList();
            if (exposed.Count == 0) return false;

            var target = ctx.Rng.Pick(exposed);
            if (ctx.Rng.Chance(SchemeRules.CaptureChance(power, target, Settings)))
            {
                captives.Take(target, power.Name);
                return true;
            }
            if (ctx.Rng.Chance(Settings.AgentTakenChance))
                captives.Imprison(new Prisoner($"agent-{power.ID}-{ctx.Clock.Year}", power.Name, target.Realm, ctx.Clock.Year));
            ctx.Log.Info($"[Schemes] {target.FullName} escapes an ambush.");
            return true;
        }

        private bool IsAway(CharacterData member) =>
            member.CaptorFaction == null
            && (Settings.AwayTasks.Contains(member.CurrentTask) || member.LastOperationYear == ctx.Clock.Year);
    }
}

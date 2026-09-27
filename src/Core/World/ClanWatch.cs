using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The clan's own distrust of each power (user request 2026-09-27): its memory of what a power did to it — a strike,
    /// a member taken, an agent caught, a treaty betrayed, a probe spotted, blackmail, a thief caught, a spy unmasked, a
    /// coalition — fading slowly with the years; an absorbed power is forgotten. It is the clan's own feeling: the
    /// screens show a sign of it (never a figure), and a power the clan distrusts probes it with more difficulty.
    /// </summary>
    public sealed class ClanWatch
    {
        private readonly GameContext ctx;
        private readonly SuspicionLedger suspicion;

        public ClanWatch(GameContext ctx, SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.suspicion = suspicion;
            var bus = ctx.Events;
            bus.OnClanStruck += power => Remember(power, Settings.Struck);
            bus.OnMemberCaptured += (_, power) => Remember(power, Settings.MemberTaken);
            bus.OnAgentCaught += power => Remember(power, Settings.AgentCaught);
            bus.OnTreatyBetrayed += power => Remember(power, Settings.TreatyBetrayed);
            bus.OnProbeSpotted += power => Remember(power, Settings.ProbeSpotted);
            bus.OnBlackmail += power => Remember(power, Settings.Blackmail);
            bus.OnTheft += (_, thief) => Remember(thief, Settings.ThiefCaught); // an unknown thief: nobody to blame
            bus.OnSpyUnmasked += power => Remember(power, Settings.SpyUnmasked);
            bus.OnCoalitionFormed += members => { foreach (var power in members) Remember(power, Settings.Coalition); };
            bus.OnPowerAbsorbed += (vassal, _) => suspicion.AddClanDistrust(vassal, -suspicion.ClanDistrust(vassal));
        }

        private ClanWatchSettings Settings => ctx.Content.Balance.ClanWatch;

        private void Remember(string power, int amount)
        {
            if (power != null) suspicion.AddClanDistrust(power, amount);
        }

        /// <summary>The memory fades a little each year.</summary>
        public void ProcessYear()
        {
            foreach (var power in new System.Collections.Generic.List<string>(suspicion.AllClanDistrust.Keys))
                suspicion.AddClanDistrust(power, -Settings.FadePerYear);
        }

        /// <summary>The sign of the clan's distrust of a power; null when it trusts it enough.</summary>
        public static string Sign(int distrust, ClanWatchSettings s) =>
            distrust >= s.DeepDistrustFrom ? "le clan s'en méfie profondément"
            : distrust >= s.DistrustFrom ? "le clan s'en méfie"
            : distrust >= s.WaryFrom ? "le clan reste sur ses gardes"
            : null;
    }
}

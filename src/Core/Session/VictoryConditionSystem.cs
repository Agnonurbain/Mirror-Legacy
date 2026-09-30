using MirrorChronicles.Clan;

namespace MirrorChronicles.Session
{
    /// <summary>Why a game was lost (not saved: an older loss is told plainly).</summary>
    public enum DefeatCause { None, Extinct, MirrorSeized, Absorbed }

    /// <summary>
    /// The defeats of LORE.md §11.9: no member of the line remains, a power seizes the mirror (L2c.4c), or a suzerain
    /// absorbs the clan. There is no forced victory (B3, 2026-09-30): the dynastic endings are told, and play goes on.
    /// </summary>
    public sealed class VictoryConditionSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;

        public bool GameLost { get; private set; }
        public bool IsOver => GameLost;
        public DefeatCause Loss { get; private set; }

        public VictoryConditionSystem(GameContext ctx, ClanManager clan)
        {
            this.ctx = ctx;
            this.clan = clan;

            ctx.Events.OnCharacterDied += (c, cause) => CheckExtinction();
            ctx.Events.OnMirrorSeized += MirrorSeized;
            ctx.Events.OnClanAbsorbed += ClanAbsorbed;
        }

        public void Restore(bool lost) => GameLost = lost;

        /// <summary>The second defeat of LORE.md §11.9: the mirror discovered and seized by a stronger power.</summary>
        private void MirrorSeized(string faction) =>
            Lose(DefeatCause.MirrorSeized, $"{faction} seizes the mirror: the clan's secret is lost.");

        /// <summary>A defeat of vassalage (2026-09-27): the suzerain's grip complete, the clan is absorbed into its power.</summary>
        private void ClanAbsorbed(string suzerain) =>
            Lose(DefeatCause.Absorbed, $"{suzerain} absorbs the clan: it is no longer its own.");

        private void CheckExtinction()
        {
            if (clan.LivingMembers.Count == 0) Lose(DefeatCause.Extinct, "The line is extinguished.");
        }

        private void Lose(DefeatCause cause, string why)
        {
            if (IsOver) return;
            GameLost = true;
            Loss = cause;
            ctx.Log.Warning($"[Victory] {why}");
            ctx.Events.TriggerGameOver();
        }
    }
}

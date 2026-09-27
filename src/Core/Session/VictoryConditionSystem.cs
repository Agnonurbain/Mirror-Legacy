using MirrorChronicles.Characters;
using MirrorChronicles.Clan;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// The current endings: victory once ten generations have passed and an ancestor has ascended;
    /// defeat when no member of the line remains, or when a power seizes the mirror (L2c.4c). (The dynastic endings of
    /// LORE.md §11.9 arrive with L6.)
    /// </summary>
    public sealed class VictoryConditionSystem
    {
        public const int GenerationsForVictory = 10;
        public const int AscensionsForVictory = 1;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ClanKarmaSystem karma;
        private readonly AscensionSystem ascension;

        public bool GameWon { get; private set; }
        public bool GameLost { get; private set; }
        public bool IsOver => GameWon || GameLost;

        public VictoryConditionSystem(GameContext ctx, ClanManager clan, ClanKarmaSystem karma, AscensionSystem ascension)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.karma = karma;
            this.ascension = ascension;

            ctx.Events.OnYearStarted += year => CheckVictory();
            ctx.Events.OnCharacterDied += (c, cause) => CheckDefeat();
            ctx.Events.OnAncestorAscended += c => CheckDefeat();
            ctx.Events.OnMirrorSeized += MirrorSeized;
            ctx.Events.OnClanAbsorbed += ClanAbsorbed;
        }

        public void Restore(bool won, bool lost)
        {
            GameWon = won;
            GameLost = lost;
        }

        private void CheckVictory()
        {
            if (IsOver) return;
            if (karma.GenerationCount < GenerationsForVictory || ascension.AscendedAncestorsCount < AscensionsForVictory) return;

            GameWon = true;
            ctx.Log.Info("[Victory] Ten generations endured and an ancestor ascended: the line is eternal!");
            ctx.Events.TriggerGameOver(true);
        }

        /// <summary>The second defeat of LORE.md §11.9: the mirror discovered and seized by a stronger power.</summary>
        private void MirrorSeized(string faction)
        {
            if (IsOver) return;
            GameLost = true;
            ctx.Log.Warning($"[Victory] {faction} seizes the mirror: the clan's secret is lost.");
            ctx.Events.TriggerGameOver(false);
        }

        /// <summary>A defeat of vassalage (2026-09-27): the suzerain's grip complete, the clan is absorbed into its power.</summary>
        private void ClanAbsorbed(string suzerain)
        {
            if (IsOver) return;
            GameLost = true;
            ctx.Log.Warning($"[Victory] {suzerain} absorbs the clan: it is no longer its own.");
            ctx.Events.TriggerGameOver(false);
        }

        private void CheckDefeat()
        {
            if (IsOver || clan.LivingMembers.Count > 0) return;

            GameLost = true;
            ctx.Log.Warning("[Victory] The line is extinguished.");
            ctx.Events.TriggerGameOver(false);
        }
    }
}

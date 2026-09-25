using MirrorChronicles.Characters;
using MirrorChronicles.Clan;

namespace MirrorChronicles.Session
{
    /// <summary>
    /// The current endings: victory once ten generations have passed and an ancestor has ascended;
    /// defeat when no member of the line remains. (The dynastic endings of LORE.md §11.9 arrive with L6.)
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

        private void CheckDefeat()
        {
            if (IsOver || clan.LivingMembers.Count > 0) return;

            GameLost = true;
            ctx.Log.Warning("[Victory] The line is extinguished.");
            ctx.Events.TriggerGameOver(false);
        }
    }
}

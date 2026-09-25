using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// Generational progress: a new generation begins each time a new patriarch leads the clan at the
    /// turn of a year. Later generations cultivate a little faster.
    /// </summary>
    public sealed class ClanKarmaSystem
    {
        public const double SpeedBonusPerGeneration = 0.02;
        private const int FirstKarmaThreshold = 5;
        private const int SecondKarmaThreshold = 10;

        private readonly GameContext ctx;
        private readonly ClanManager clan;

        public int GenerationCount { get; private set; } = 1;
        public int TotalBirths { get; private set; }
        public int TotalDeaths { get; private set; }
        public string LastPatriarchId { get; private set; }

        public ClanKarmaSystem(GameContext ctx, ClanManager clan)
        {
            this.ctx = ctx;
            this.clan = clan;

            ctx.Events.OnCharacterBorn += c => { if (c.Age == 0) TotalBirths++; }; // spouses and founders join older
            ctx.Events.OnCharacterDied += (c, cause) => TotalDeaths++;
            ctx.Events.OnYearStarted += year => RecordSuccession();
        }

        /// <summary>The founding generation has no bonus; each later one adds 2%.</summary>
        public double GetCultivationSpeedBonus() => (GenerationCount - 1) * SpeedBonusPerGeneration;

        public int GetBonusXP()
        {
            if (GenerationCount >= SecondKarmaThreshold) return 10;
            if (GenerationCount >= FirstKarmaThreshold) return 5;
            return 0;
        }

        public bool HasAncestralTechniqueUnlock() => GenerationCount >= SecondKarmaThreshold;

        public void Restore(int generationCount, int totalBirths, int totalDeaths, string lastPatriarchId)
        {
            GenerationCount = generationCount < 1 ? 1 : generationCount;
            TotalBirths = totalBirths;
            TotalDeaths = totalDeaths;
            LastPatriarchId = lastPatriarchId;
        }

        private void RecordSuccession()
        {
            string current = clan.PatriarchID;
            if (current == null) return;

            if (LastPatriarchId == null)
            {
                LastPatriarchId = current;
            }
            else if (current != LastPatriarchId)
            {
                GenerationCount++;
                LastPatriarchId = current;
                ctx.Log.Info($"[ClanKarma] Generation {GenerationCount} begins.");
            }
        }
    }
}

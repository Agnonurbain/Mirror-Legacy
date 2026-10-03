namespace MirrorChronicles.Data
{
    /// <summary>
    /// A new Purple Mansion draws the old powers' eyes (balance.json « ascentWatch », audit §1.9, the user's decision
    /// 2026-10-03; 📚 the newly risen Purple Mansions compelled the older powers to intervene, and Chi Wei bore lasting harm
    /// of it). Interpretations: each power of the Purple Mansion or above able to reach the newcomer grows wary by
    /// <see cref="Wariness"/>; by <see cref="TestChance"/> one comes to test it — the stronger wins by
    /// <see cref="TestBase"/> plus <see cref="TestPerPower"/> a point of power apart; the clan's loser bears a Dao wound, an
    /// elder loses <see cref="ElderLifespanLoss"/> years. The world's new Purple Mansions alike.
    /// </summary>
    public sealed record AscentWatchSettings
    {
        public int Wariness { get; init; } = 10;
        public double TestChance { get; init; } = 0.3;
        public double TestBase { get; init; } = 0.5;
        public double TestPerPower { get; init; } = 0.05;
        public int ElderLifespanLoss { get; init; } = 30;
    }
}

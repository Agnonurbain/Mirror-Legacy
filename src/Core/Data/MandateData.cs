namespace MirrorChronicles.Data
{
    /// <summary>
    /// The life events an ability's image may be embodied by (the Mandate of Life, LORE.md §5.4.3; user decision
    /// 2026-10-03). Read by name from the data: never rename a member.
    /// </summary>
    public enum LifeMandate { Captivity, Freedom, Peril, War, Victory, Mourning, Reign, Seclusion }

    /// <summary>
    /// The Mandate of Life (balance.json « mandate », interpretations): an event that embodies the image of the ability a
    /// Purple Mansion condenses gives it this share of the realm's XP; a reign or a seclusion, a smaller share each year.
    /// </summary>
    public sealed record MandateSettings
    {
        public double EventXpShare { get; init; } = 0.3;
        public double YearlyXpShare { get; init; } = 0.05;
    }
}

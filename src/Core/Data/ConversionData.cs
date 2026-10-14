namespace MirrorChronicles.Data
{
    /// <summary>
    /// The conversions between the cultivation Daos (balance.json « conversions »; LORE.md §3.8; the wiki « Cultivation System »:
    /// switching Daos is « circumstantial and safest only at very high or low cultivation realms »; the user's choice of
    /// 2026-10-14: every conversion the wiki allows). The passage is perilous in the middle realms; a temple converts its
    /// captives under duress, and inducts a beaten monk of another school or Aspect; a Qi cultivator may still turn to the
    /// Heavenly Embryo for more blood; a power's dying elder may turn to a temple as the clan's members do.
    /// </summary>
    public sealed record ConversionSettings
    {
        /// <summary>The chance a passage at the Foundation or the Purple Mansion wounds the Dao.</summary>
        public double MiddleWoundChance { get; init; }

        /// <summary>A temple's yearly chance to convert a member of the clan it holds (who renounces the clan).</summary>
        public double CaptiveConversionChance { get; init; }

        /// <summary>The chance a monk who loses a debate to a temple not its own is inducted into it, rather than falling.</summary>
        public double InductionChance { get; init; }

        /// <summary>A power's elder at its last year, below the Golden Core: its chance to turn to a temple rather than die.</summary>
        public double ElderConversionChance { get; init; }

        /// <summary>The blood essence a Qi cultivator spends to refine its Qi into a Diverse Mansion.</summary>
        public int QiFormBlood { get; init; }
    }
}

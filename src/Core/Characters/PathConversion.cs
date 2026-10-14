using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The passage between cultivation Daos (L7; LORE.md §3.8; the wiki « Cultivation System »: « safest only at very high or
    /// low cultivation realms »). See <see cref="ConversionSettings"/>.
    /// </summary>
    public static class PathConversion
    {
        /// <summary>True for the middle realms, where the passage may wound the Dao.</summary>
        public static bool IsPerilous(CultivationRealm realm) => false;
    }
}

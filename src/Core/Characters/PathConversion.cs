using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The passage between cultivation Daos (L7; LORE.md §3.8; the wiki « Cultivation System »: switching Daos is « circumstantial
    /// and safest only at very high or low cultivation realms »): below the Foundation and from the Golden Core it is safe; at the
    /// Foundation or the Purple Mansion it may wound the Dao. Every conversion of a member passes here before it changes its way.
    /// See <see cref="ConversionSettings"/>.
    /// </summary>
    public static class PathConversion
    {
        /// <summary>True for the middle realms, where the passage may wound the Dao.</summary>
        public static bool IsPerilous(CultivationRealm realm) =>
            realm == CultivationRealm.Foundation || realm == CultivationRealm.PurpleMansion;

        /// <summary>The member passes to another Dao: in the middle realms, a chance of a Dao wound.</summary>
        public static void Pass(GameContext ctx, WoundSystem wounds, CharacterData member)
        {
            if (!IsPerilous(member.Realm) || !ctx.Rng.Chance(ctx.Content.Balance.Conversions.MiddleWoundChance)) return;
            wounds.ApplyDaoWound(member);
            ctx.Log.Warning($"[Conversions] {member.FullName}'s passage between Daos wounds its Dao.");
            ctx.Events.TriggerConversionWounded(member);
        }
    }
}

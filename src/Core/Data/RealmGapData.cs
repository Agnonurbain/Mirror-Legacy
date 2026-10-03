namespace MirrorChronicles.Data
{
    /// <summary>
    /// The gap between realms (balance.json « realmGap », AUDIT_LORE.md §1, user decision 2026-10-03). Lore: a Purple
    /// Mansion is invisible to the realms below and slips away at 5,000 m (LORE.md §5.4); a Golden Core's body is only a
    /// vessel (§5.5). Interpretations: from <see cref="UnreachableFrom"/> up, no lower realm reaches a foe; below it, one may
    /// still reach a foe at most <see cref="MaxRealmsBehind"/> realm(s) above (traps, formations, numbers).
    /// </summary>
    public sealed record RealmGapSettings
    {
        public CultivationRealm UnreachableFrom { get; init; } = CultivationRealm.PurpleMansion;
        public int MaxRealmsBehind { get; init; } = 1;
    }
}

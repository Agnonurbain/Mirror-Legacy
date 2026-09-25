using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// An ancestor who reaches the Dao Embryo leaves the mortal plane; the clan keeps count of its
    /// ascended ancestors.
    /// </summary>
    public sealed class AscensionSystem
    {
        public int AscendedAncestorsCount { get; private set; }

        public AscensionSystem(GameContext ctx, ClanManager clan)
        {
            ctx.Events.OnBreakthroughSuccess += (character, realm) =>
            {
                if (realm == CultivationRealm.DaoEmbryo)
                    clan.Ascend(character);
            };
            ctx.Events.OnAncestorAscended += character =>
            {
                AscendedAncestorsCount++;
                ctx.Log.Info($"[Ascension] {character.FullName} watches over the clan from beyond.");
            };
        }

        public void Restore(int ascendedAncestorsCount) => AscendedAncestorsCount = ascendedAncestorsCount;
    }
}

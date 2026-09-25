using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// What a member of the clan leaves behind: the savings of their spatial ring and, from the
    /// Golden Core on, a Dao fragment of their affinity for the mirror to deduce from.
    /// </summary>
    public sealed class LegacySystem
    {
        public const int InheritedStones = 50;
        public const int DaoFragmentQuality = 5;

        public LegacySystem(GameContext ctx, ClanManager clan, ResourceManager resources, DeductionEngine deduction)
        {
            ctx.Events.OnCharacterDied += (deceased, cause) =>
            {
                if (clan.FindById(deceased.ID) == null) return; // strangers leave nothing to the clan

                resources.AddSpiritStones(InheritedStones);
                if (deceased.Realm < CultivationRealm.GoldenCore) return;

                var element = deceased.Affinity != Element.None ? deceased.Affinity : ctx.Rng.NextElement();
                deduction.AddFragment(element, DaoFragmentQuality, $"Dao legacy of {deceased.FullName}");
                ctx.Log.Info($"[Legacy] {deceased.FullName} leaves a profound {element} Dao fragment.");
            };
        }
    }
}

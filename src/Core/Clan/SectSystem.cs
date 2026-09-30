using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// The Double House (📚 the novel's family: « Peak and Town »; B3d, LORE.md §11.9): the clan founds its sect — its
    /// cultivators up on the peaks, its mortals down in the town — once a member of the Purple Mansion or above can hold a
    /// peak, it counts enough cultivators, and it pays for the peaks (🔎 balance.json « sect »). What the sect changes in
    /// play awaits the user's decision.
    /// </summary>
    public sealed class SectSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;

        public int? FoundedYear { get; private set; }
        public bool Founded => FoundedYear != null;

        public SectSystem(GameContext ctx, ClanManager clan, ResourceManager resources)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
        }

        private SectSettings Settings => ctx.Content.Balance.Sect;

        /// <summary>Why the clan cannot found its sect now (French, for the screens), or null.</summary>
        public string FoundingRefusal()
        {
            var s = Settings;
            var free = clan.LivingMembers.Where(m => m.CaptorFaction == null).ToList();
            if (Founded) return "le clan a déjà fondé sa secte";
            if (!free.Any(m => m.Realm >= s.MinRealm)) return "il faut un membre du Manoir Pourpre pour tenir un pic";
            int cultivators = free.Count(m => m.Realm >= CultivationRealm.QiRefinement);
            if (cultivators < s.MinCultivators) return $"il faut {s.MinCultivators} cultivateurs de la Culture du Qi (le clan en compte {cultivators})";
            if (resources.SpiritStones < s.FoundingStones) return $"bâtir les pics coûte {s.FoundingStones} pierres";
            return null;
        }

        /// <summary>The clan founds its sect. Null when done, else why not (French).</summary>
        public string Found()
        {
            string refusal = FoundingRefusal();
            if (refusal != null) return refusal;
            resources.ConsumeSpiritStones(Settings.FoundingStones);
            FoundedYear = ctx.Clock.Year;
            ctx.Log.Info($"[Sect] The {clan.ClanName} clan founds its sect: the peaks above, the town below.");
            ctx.Events.TriggerSectFounded();
            return null;
        }

        public void Restore(int? foundedYear) => FoundedYear = foundedYear;
    }
}

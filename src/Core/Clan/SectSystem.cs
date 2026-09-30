using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Clan
{
    /// <summary>
    /// The Double House (📚 the novel's family: « Peak and Town »; B3d, LORE.md §11.9): the clan founds its sect — its
    /// cultivators up on the peaks, its mortals down in the town — once a member of the Purple Mansion or above can hold a
    /// peak, it counts enough cultivators, and it pays for the peaks. What the sect changes (the user's decision,
    /// 2026-09-30; 🔎 balance.json « sect »): the peak masters teach better, the town prays more, a succession unsettles the
    /// clan less; but the great sects look on it with a cold eye, the greedy covet it more, and the peaks cost their upkeep.
    /// </summary>
    public sealed class SectSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly MentalStabilitySystem stability;
        private readonly ClanKarmaSystem karma;
        private readonly FactionManager factions;
        private int generationKnown;

        public int? FoundedYear { get; private set; }
        public bool Founded => FoundedYear != null;

        public SectSystem(GameContext ctx, ClanManager clan, ResourceManager resources, MentalStabilitySystem stability,
            ClanKarmaSystem karma, FactionManager factions)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.stability = stability;
            this.karma = karma;
            this.factions = factions;

            ctx.Events.OnYearStarted += year => { Succession(); PayThePeaks(); }; // after the karma, built before: it counts the generation
        }

        private SectSettings Settings => ctx.Content.Balance.Sect;

        /// <summary>The share the peak masters add to a teacher's lesson.</summary>
        public double TeachingFactor => Founded ? 1 + Settings.TeachingBonus : 1;

        /// <summary>The share the town adds to its mortals' prayers.</summary>
        public double PrayerFactor => Founded ? 1 + Settings.PrayerBonus : 1;

        /// <summary>How much more a greedy power covets a clan that shows itself as a sect.</summary>
        public double GreedFactor => Founded ? Settings.GreedFactor : 1;

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

        /// <summary>The clan founds its sect, and the great sects look on. Null when done, else why not (French).</summary>
        public string Found()
        {
            string refusal = FoundingRefusal();
            if (refusal != null) return refusal;
            resources.ConsumeSpiritStones(Settings.FoundingStones);
            FoundedYear = ctx.Clock.Year;
            foreach (var sect in factions.Factions.Where(f => f.Kind == FactionKind.Sect).ToList())
                factions.ChangeRelation(sect.ID, -Settings.GreatSectRelationLoss);
            ctx.Log.Info($"[Sect] The {clan.ClanName} clan founds its sect: the peaks above, the town below.");
            ctx.Events.TriggerSectFounded();
            return null;
        }

        /// <summary>The peaks' yearly upkeep, as far as the stones go.</summary>
        public void PayThePeaks()
        {
            if (Founded) resources.ConsumeSpiritStones(System.Math.Min(resources.SpiritStones, Settings.PeaksUpkeep));
        }

        /// <summary>A new patriarch unsettles every member — less in a sect, which no longer hangs on one leader's talent.</summary>
        private void Succession()
        {
            if (karma.GenerationCount <= generationKnown) return;
            bool first = generationKnown == 0;
            generationKnown = karma.GenerationCount;
            if (first) return;
            int unrest = Founded ? Settings.SectSuccessionUnrest : Settings.SuccessionUnrest;
            foreach (var member in clan.LivingMembers.Where(m => m.ID != clan.PatriarchID).ToList()) stability.ApplyModifier(member, -unrest);
        }

        /// <summary>The sect's founding year (none before 2.21), and the generation the clan has reached.</summary>
        public void Restore(int? foundedYear, int generation = -1)
        {
            FoundedYear = foundedYear;
            if (generation >= 0) generationKnown = generation;
        }
    }
}

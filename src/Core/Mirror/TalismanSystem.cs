using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The mirror's talisman Qi (LORE.md §11.5): the clan gathers prayers year after year; a being of the Qi
    /// Cultivation or beyond sacrificed and ten thousand prayers offered, the mirror refines a talisman Qi of the
    /// sacrifice's rank and offers one to three to the bearer; the player chooses one, which gives its trait and
    /// a leap in cultivation. Only clan members can be offered until captives exist (L6): a dilemma the lore allows.
    /// </summary>
    public sealed class TalismanSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;

        public TalismanSystem(GameContext ctx, ClanManager clan, ResourceManager resources)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            ctx.Events.OnYearStarted += year => GatherPrayers();
        }

        private TalismanSettings Settings => ctx.Content.Balance.Talismans;

        /// <summary>The talismans awaiting the player's choice after a ritual, or null.</summary>
        public TalismanOffer PendingOffer { get; private set; }

        /// <summary>
        /// The ritual: the sacrifice dies, the prayers are spent, and the mirror offers talismans of its rank to
        /// the bearer. False when it cannot be performed (an offer already waits, the bearer already has a talisman
        /// or cannot cultivate, the sacrifice is below the Qi Cultivation, the prayers are lacking).
        /// </summary>
        public bool PerformRitual(CharacterData bearer, CharacterData sacrifice)
        {
            var rank = sacrifice == null ? null : TalismanRules.RankOf(sacrifice);
            bool possible = PendingOffer == null && bearer != null && bearer.IsAlive && bearer.TalismanQiId == null
                && SpiritualOrificeRules.CanCultivate(bearer) && sacrifice != bearer && sacrifice.IsAlive && rank != null
                && resources.Prayers >= Settings.PrayersPerRitual;
            if (!possible)
            {
                ctx.Log.Warning($"[Mirror] The talisman ritual for {bearer?.FullName} cannot be performed.");
                return false;
            }

            resources.ConsumePrayers(Settings.PrayersPerRitual);
            clan.Kill(sacrifice, DeathCause.Sacrificed);
            var choices = TalismanRules.Offer(bearer, rank.Value, ctx.Content.Talismans, Settings, ctx.Rng);
            PendingOffer = new TalismanOffer(bearer.ID, choices.ToList());
            ctx.Log.Info($"[Mirror] {sacrifice.FullName} is offered to the mirror: {choices.Count} talisman(s) await {bearer.FullName}.");
            return true;
        }

        /// <summary>The player's choice among the offered talismans: the bearer takes its trait and leaps.</summary>
        public bool Choose(string talismanId)
        {
            var offer = PendingOffer;
            var bearer = offer == null ? null : clan.FindById(offer.BeneficiaryId);
            var talisman = ctx.Content.Talismans.FirstOrDefault(t => t.Id == talismanId);
            if (bearer == null || !bearer.IsAlive || talisman == null || !offer.Choices.Contains(talismanId)) return false;

            PendingOffer = null;
            bearer.TalismanQiId = talisman.Id;
            bearer.MaxLifespan += talisman.LifespanYears;
            TalismanRules.Leap(bearer, talisman.Rank == TalismanRank.White ? Settings.WhiteStageLeap : Settings.GreyStageLeap);
            ctx.Log.Info($"[Mirror] {bearer.FullName} receives the talisman Qi « {talisman.Name} ».");
            return true;
        }

        /// <summary>Restores the offer of a save (null: none).</summary>
        public void Restore(TalismanOffer offer) => PendingOffer = offer;

        /// <summary>Each year, the clan's mortals and its prestige bring prayers to the mirror.</summary>
        private void GatherPrayers()
        {
            int mortals = clan.LivingMembers.Count(m => !SpiritualOrificeRules.CanCultivate(m));
            resources.AddPrayers(mortals * Settings.PrayersPerMortalPerYear + System.Math.Max(0, resources.Prestige) * Settings.PrayersPerPrestigePerYear);
        }
    }
}

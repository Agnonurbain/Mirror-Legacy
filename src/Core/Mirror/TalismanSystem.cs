using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The mirror's talisman Qi (LORE.md §11.5): the clan gathers prayers year after year; a captured spirit beast of
    /// the Qi Cultivation or beyond sacrificed (user decision, 2026-09-26: a beast, never a clan member) and ten
    /// thousand prayers offered, the mirror refines a talisman Qi of the beast's rank — the stronger the beast, the
    /// higher the quality — and offers one to three to the bearer; the player chooses one, which gives its trait and
    /// a leap in cultivation.
    /// </summary>
    public sealed class TalismanSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;

        public TalismanSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions)
        {
            this.factions = factions;
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            ctx.Events.OnYearStarted += year => GatherPrayers();
            ctx.Events.OnCharacterDied += (dead, cause) => { if (PendingOffer?.BeneficiaryId == dead.ID) PendingOffer = null; }; // the offer dies with its bearer
        }

        private TalismanSettings Settings => ctx.Content.Balance.Talismans;

        /// <summary>The talismans awaiting the player's choice after a ritual, or null.</summary>
        public TalismanOffer PendingOffer { get; private set; }

        /// <summary>
        /// The ritual: the beast is offered, the prayers are spent, and the mirror offers talismans of its rank to
        /// the bearer. False when it cannot be performed (an offer already waits, the bearer already has a talisman
        /// or cannot cultivate, the clan holds no such beast or it is below the Qi Cultivation, the prayers are lacking).
        /// </summary>
        public bool PerformRitual(CharacterData bearer, CapturedBeast beast)
        {
            var rank = beast == null ? null : TalismanRules.RankOf(beast);
            string refusal =
                PendingOffer != null ? "an offer already awaits a choice"
                : bearer == null || !bearer.IsAlive || !SpiritualOrificeRules.CanCultivate(bearer) ? "the bearer cannot receive a talisman"
                : bearer.TalismanQiId != null ? "the bearer already has a talisman"
                : beast == null || !resources.Beasts.Contains(beast) ? "the clan holds no such beast"
                : rank == null ? "the beast is below the Qi Cultivation"
                : resources.Prayers < Settings.PrayersPerRitual ? "the prayers are lacking"
                : null;
            if (refusal != null)
            {
                ctx.Log.Warning($"[Mirror] The talisman ritual for {bearer?.FullName} cannot be performed: {refusal}.");
                return false;
            }

            resources.ConsumePrayers(Settings.PrayersPerRitual);
            resources.ConsumeBeast(beast);
            AnswerToTheOwner(beast);
            var choices = TalismanRules.Offer(bearer, rank.Value, ctx.Content.Talismans, Settings, ctx.Rng);
            PendingOffer = new TalismanOffer(bearer.ID, choices.ToList(), TalismanRules.LeapOf(beast, Settings));
            ctx.Log.Info($"[Mirror] A beast of the {beast.Realm} (stage {beast.Stage}) is offered to the mirror: {choices.Count} talisman(s) await {bearer.FullName}.");
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
            int rankLeap = talisman.Rank == TalismanRank.White ? Settings.WhiteStageLeap : Settings.GreyStageLeap;
            TalismanRules.Leap(bearer, offer.Leap ?? rankLeap); // an older save's offer: the rank's leap
            ctx.Log.Info($"[Mirror] {bearer.FullName} receives the talisman Qi « {talisman.Name} ».");
            return true;
        }

        /// <summary>A power whose beast was killed may find out, and resents it (user decision, 2026-09-26).</summary>
        private void AnswerToTheOwner(CapturedBeast beast)
        {
            var owner = factions.GetFactionByName(beast.OwnerFaction);
            if (owner == null || !ctx.Rng.Chance(Settings.OwnedBeastDiscoveryChance)) return;
            factions.ChangeRelation(owner.ID, Settings.OwnedBeastRelationPenalty);
            ctx.Log.Warning($"[Mirror] {owner.Name} learns the clan killed one of its beasts.");
        }

        /// <summary>Restores the offer of a save (null: none); one whose bearer or talismans no longer resolve is dropped.</summary>
        public void Restore(TalismanOffer offer)
        {
            bool resolves = offer?.Choices != null && clan.FindById(offer.BeneficiaryId)?.IsAlive == true
                && offer.Choices.Count > 0 && offer.Choices.All(id => ctx.Content.Talismans.Any(t => t.Id == id));
            PendingOffer = resolves ? offer : null;
        }

        /// <summary>Each year, the clan's mortals and its prestige bring prayers to the mirror.</summary>
        private void GatherPrayers()
        {
            int mortals = clan.LivingMembers.Count(m => !SpiritualOrificeRules.CanCultivate(m));
            resources.AddPrayers(mortals * Settings.PrayersPerMortalPerYear + System.Math.Max(0, resources.Prestige) * Settings.PrayersPerPrestigePerYear);
        }
    }
}

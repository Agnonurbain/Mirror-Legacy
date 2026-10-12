using System;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Demonic Dao of the clan's bound beasts (R16; LORE.md §3.4; the user's choices of 2026-10-12): a captured beast may be
    /// bound to the clan rather than offered to the mirror. It absorbs the Qi around it every year, whatever its task, at a share
    /// of a human's pace but for a longer life; its lineage awakens its stages and realms on its own — no trial, no method, no
    /// Qi — up to the Purple Mansion's Grand Perfection for now. See <see cref="DemonicDaoSettings"/>.
    /// </summary>
    public sealed class DemonicDaoSystem
    {
        /// <summary>The highest realm a bound beast awakens (no Golden Core equivalent yet).</summary>
        public const CultivationRealm Ceiling = CultivationRealm.PurpleMansion;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly TalismanSystem talismans;

        public DemonicDaoSystem(GameContext ctx, ClanManager clan, ResourceManager resources, TalismanSystem talismans)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.talismans = talismans;
        }

        private DemonicDaoSettings Settings => ctx.Content.Balance.DemonicDao;

        /// <summary>True for a member of the Demonic Dao (a bound beast).</summary>
        public static bool IsDemonic(CharacterData member) => member?.Path == CultivationPath.Demonic;

        /// <summary>Why the beast cannot be bound now, or null when it can.</summary>
        public string BindRefusal(CapturedBeast beast) =>
            beast == null || !resources.Beasts.Contains(beast) ? "the clan holds no such beast"
            : talismans.PendingOffer != null ? "an offer of the mirror awaits a choice"
            : null;

        /// <summary>
        /// Binds a captured beast to the clan: it joins as a spirit beast of the Myriad Races, of its realm and its species'
        /// element, on patrol; its owner may learn the clan took it. Null when it cannot be bound.
        /// </summary>
        public CharacterData Bind(CapturedBeast beast)
        {
            string refusal = BindRefusal(beast);
            if (refusal != null)
            {
                ctx.Log.Warning($"[DemonicDao] The beast {beast?.Id} cannot be bound: {refusal}.");
                return null;
            }

            var species = SpeciesOf(beast.Id);
            var bound = new CharacterData
            {
                ID = ctx.Rng.NextId(), // seeded: the same game binds the same beast
                FirstName = species?.Name ?? beast.Id,
                LastName = "",
                IsMale = ctx.Rng.Chance(0.5),
                Age = ctx.Rng.Next(Settings.MinBoundAge, Settings.MaxBoundAge + 1),
                SpiritualRoot = ctx.Rng.Next(Settings.MinRoot, Settings.MaxRoot + 1),
                Affinity = species?.Element ?? Element.None,
                Species = Species.SpiritBeast,
                Path = CultivationPath.Demonic,
                SubPath = CultivationSubPath.MyriadRaces,
                Realm = beast.Realm,
                RealmStage = beast.Stage,
                HasSpiritualOrifice = true, // its lineage awakens it on its own (LORE.md §3.4)
                OrificeKnown = true,
                CurrentTask = TaskType.Patrol,
            };
            PowerLadder.Normalize(bound);
            bound.MaxLifespan = Math.Max(bound.Age + 1, LifespanOf(bound));

            resources.ConsumeBeast(beast);
            talismans.AnswerToTheOwner(beast); // a power whose beast was taken may find out, as for an offering
            clan.AddMember(bound);
            ctx.Log.Info($"[DemonicDao] {bound.FullName} is bound to the clan ({RankCatalog.DisplayName(bound)}).");
            ctx.Events.TriggerBeastBound(bound);
            return bound;
        }

        /// <summary>
        /// Every free bound beast absorbs the year's Qi, whatever its task, and its lineage awakens what that Qi allows.
        /// </summary>
        public void ProcessYear()
        {
            foreach (var beast in clan.LivingMembers.Where(m => IsDemonic(m) && m.CaptorFaction == null && !m.ProgressionSealed).ToList())
            {
                beast.CultivationXP += (int)Math.Round((CultivationSystem.BaseYearlyXp + beast.SpiritualRoot / 2) * Settings.AbsorptionSpeed);
                Awaken(beast);
            }
        }

        /// <summary>Its lineage awakens what the XP allows, up to the ceiling's Grand Perfection.</summary>
        private void Awaken(CharacterData beast) =>
            TriallessClimb.Climb(beast, Ceiling, PowerLadder.StageCount(Ceiling), awakened =>
            {
                awakened.MaxLifespan = Math.Max(awakened.MaxLifespan, LifespanOf(awakened));
                ctx.Log.Info($"[DemonicDao] {awakened.FullName}'s lineage awakens: {RankCatalog.DisplayName(awakened)}.");
                ctx.Events.TriggerLineageAwakened(awakened);
            });

        /// <summary>A beast's lifespan: a human's of its realm, lengthened (its Dao wounds still weigh).</summary>
        private int LifespanOf(CharacterData beast) =>
            PowerLadder.WoundedLifespan((int)(PowerLadder.MaxLifespan(beast.Realm, beast.RealmStage) * Settings.LifespanFactor), beast.DaoWounds);

        /// <summary>The species of a captured beast: the longest species id its own begins with (« boar-cub » before « boar »).</summary>
        private BeastSpecies SpeciesOf(string beastId) =>
            ctx.Content.BeastSpecies.OrderByDescending(s => s.Id.Length).FirstOrDefault(s => beastId.StartsWith(s.Id + "-", StringComparison.Ordinal));
    }
}

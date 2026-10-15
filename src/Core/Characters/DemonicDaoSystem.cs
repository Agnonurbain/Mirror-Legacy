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
    /// Qi — up to the Purple Mansion's Grand Perfection, where it may awaken a demonic Golden Core: on its own, rarely, or fed by
    /// the clan's offering, whose failure wounds its Dao (L7; the user's choices of 2026-10-15). A serpent of the dragon's line
    /// may carry the True Serpent-Dragon's blood, hidden until an awakening: revealed, it walks the True Serpent-Dragon Dao —
    /// faster, nearer its core — and the Eastern Sea's dragons come to reclaim it, unless a True Monarch of the clan stands in
    /// their way or it holds its own core (LORE.md §3.4, §12.1). See <see cref="DemonicDaoSettings"/>.
    /// </summary>
    public sealed class DemonicDaoSystem
    {
        /// <summary>The highest realm a bound beast's lineage climbs to; only the awakening of its core leads further.</summary>
        public const CultivationRealm Ceiling = CultivationRealm.PurpleMansion;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly TalismanSystem talismans;
        private readonly WoundSystem wounds;

        public DemonicDaoSystem(GameContext ctx, ClanManager clan, ResourceManager resources, TalismanSystem talismans, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.talismans = talismans;
            this.wounds = wounds;
        }

        private DemonicDaoSettings Settings => ctx.Content.Balance.DemonicDao;

        /// <summary>True for a member of the Demonic Dao (a bound beast).</summary>
        public static bool IsDemonic(CharacterData member) => member?.Path == CultivationPath.Demonic;

        /// <summary>True for a bound beast whose dragon blood has awakened (the True Serpent-Dragon Dao).</summary>
        public static bool IsDragon(CharacterData member) => IsDemonic(member) && member.SubPath == CultivationSubPath.TrueSerpentDragon;

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
            if (species != null && Settings.DragonBloodSpecies.Contains(species.Id))
                bound.DragonBlood = ctx.Rng.Chance(Settings.DragonBloodChance); // the dragon's line, hidden until an awakening
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
        /// Every free bound beast absorbs the year's Qi, whatever its task, and its lineage awakens what that Qi allows — at the
        /// apogee, rarely, its core. A revealed dragon may first be reclaimed by its kin.
        /// </summary>
        public void ProcessYear()
        {
            foreach (var beast in clan.LivingMembers.Where(m => IsDemonic(m) && m.CaptorFaction == null).ToList())
            {
                if (IsDragon(beast) && ReclaimedByItsKin(beast)) continue;
                if (beast.ProgressionSealed || beast.Realm >= CultivationRealm.GoldenCore) continue;
                double pace = Settings.AbsorptionSpeed * (IsDragon(beast) ? Settings.DragonSpeed : 1);
                beast.CultivationXP += (int)Math.Round((CultivationSystem.BaseYearlyXp + beast.SpiritualRoot / 2) * pace);
                Awaken(beast);
                if (AtTheApogee(beast) && ctx.Rng.Chance(Settings.NaturalCoreChance + DragonBonus(beast))) AwakenTheCore(beast);
            }
        }

        /// <summary>Its lineage awakens what the XP allows, up to the ceiling's Grand Perfection.</summary>
        private void Awaken(CharacterData beast) =>
            TriallessClimb.Climb(beast, Ceiling, PowerLadder.StageCount(Ceiling), awakened =>
            {
                awakened.MaxLifespan = Math.Max(awakened.MaxLifespan, LifespanOf(awakened));
                ctx.Log.Info($"[DemonicDao] {awakened.FullName}'s lineage awakens: {RankCatalog.DisplayName(awakened)}.");
                ctx.Events.TriggerLineageAwakened(awakened);
                RevealTheDragon(awakened);
            });

        private static bool AtTheApogee(CharacterData beast) =>
            beast.Realm == Ceiling && beast.RealmStage >= PowerLadder.StageCount(Ceiling);

        private double DragonBonus(CharacterData beast) => IsDragon(beast) ? Settings.DragonCoreBonus : 0;

        /// <summary>Why the clan cannot feed this beast's awakening now (French), or null.</summary>
        public string CoreRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive || !IsDemonic(member) || !clan.LivingMembers.Contains(member)) return "seule une bête liée au clan s'éveille ainsi";
            if (!AtTheApogee(member)) return "il faut la Grande Perfection du Manoir Pourpre";
            if (member.CaptorFaction != null) return "captive ailleurs";
            if (member.ProgressionSealed) return "sa progression est scellée";
            if (resources.SpiritStones < Settings.OfferingStones) return $"il faut {Settings.OfferingStones} pierres spirituelles";
            return null;
        }

        /// <summary>The clan's offering feeds the awakening of its core: null when tried (won or lost), else why not (French).</summary>
        public string Offer(CharacterData member)
        {
            string refusal = CoreRefusal(member);
            if (refusal != null) return refusal;
            resources.ConsumeSpiritStones(Settings.OfferingStones);
            if (ctx.Rng.Chance(Settings.OfferingCoreChance + DragonBonus(member)))
            {
                AwakenTheCore(member);
                return null;
            }
            ctx.Log.Warning($"[DemonicDao] The offering fails to awaken {member.FullName}'s core.");
            wounds.ApplyDaoWound(member);
            return null;
        }

        /// <summary>Its lineage awakens to the end: a demonic Golden Core, a Golden Core's peer holding no Fruition.</summary>
        private void AwakenTheCore(CharacterData beast)
        {
            beast.Realm = CultivationRealm.GoldenCore;
            beast.RealmStage = 1;
            beast.CultivationXP = 0;
            beast.GoldenCore = GoldenCoreState.DemonicEquivalent;
            beast.MaxLifespan = Math.Max(beast.MaxLifespan, LifespanOf(beast));
            ctx.Log.Info($"[DemonicDao] {beast.FullName}'s lineage awakens a demonic Golden Core.");
            ctx.Events.TriggerDemonicCore(beast);
            RevealTheDragon(beast);
        }

        /// <summary>A hidden dragon blood awakens with its lineage: the beast walks the True Serpent-Dragon Dao.</summary>
        private void RevealTheDragon(CharacterData beast)
        {
            if (!beast.DragonBlood || IsDragon(beast)) return;
            beast.SubPath = CultivationSubPath.TrueSerpentDragon;
            beast.Species = Species.Dragon;
            ctx.Log.Info($"[DemonicDao] The True Serpent-Dragon's blood awakens in {beast.FullName}.");
            ctx.Events.TriggerDragonBloodRevealed(beast);
        }

        /// <summary>
        /// The Eastern Sea's dragons may come for one of their blood: they take it back, unless it holds its own core (it chooses
        /// its clan) or a True Monarch of the clan stands in their way. True when it left the clan.
        /// </summary>
        private bool ReclaimedByItsKin(CharacterData dragon)
        {
            if (dragon.Realm >= CultivationRealm.GoldenCore || !ctx.Rng.Chance(Settings.DragonReclaimChance)) return false;
            if (clan.LivingMembers.Any(m => m != dragon && m.CaptorFaction == null && PowerLadder.IsTrueMonarch(m)))
            {
                ctx.Log.Info($"[DemonicDao] The Eastern Sea's dragons come for {dragon.FullName}; a True Monarch of the clan keeps them away.");
                ctx.Events.TriggerDragonsRepelled(dragon);
                return false;
            }
            if (!clan.Depart(dragon)) return false;
            ctx.Log.Warning($"[DemonicDao] The Eastern Sea's dragons take back {dragon.FullName}.");
            ctx.Events.TriggerDragonReclaimed(dragon);
            return true;
        }

        /// <summary>A beast's lifespan: a human's of its realm, lengthened (its Dao wounds still weigh).</summary>
        private int LifespanOf(CharacterData beast) =>
            PowerLadder.WoundedLifespan((int)(PowerLadder.MaxLifespan(beast.Realm, beast.RealmStage) * Settings.LifespanFactor), beast.DaoWounds);

        /// <summary>The species of a captured beast: the longest species id its own begins with (« boar-cub » before « boar »).</summary>
        private BeastSpecies SpeciesOf(string beastId) =>
            ctx.Content.BeastSpecies.OrderByDescending(s => s.Id.Length).FirstOrDefault(s => beastId.StartsWith(s.Id + "-", StringComparison.Ordinal));
    }
}

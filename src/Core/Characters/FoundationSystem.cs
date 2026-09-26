using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Life with an immortal foundation (LORE.md §5.3): consuming a Dao Partner, and the Dao Heart that
    /// drifts, year after year, towards the temper its lineage favours (§5.3.2).
    /// </summary>
    public sealed class FoundationSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly TechniqueLibrary techniques;

        public FoundationSystem(GameContext ctx, ClanManager clan, TechniqueLibrary techniques)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.techniques = techniques;
            ctx.Events.OnYearStarted += year => AlignHearts();
        }

        /// <summary>
        /// Each year a ripe Dao may be harvested (LORE.md §5.3.3): a stronger cultivator seizes a Foundation at its
        /// peak whose partners the world knows; the victim dies, their foundation devoured.
        /// </summary>
        public void ProcessRipeDaoHunts()
        {
            bool guarded = clan.LivingMembers.Any(m => m.Realm >= CultivationRealm.PurpleMansion);
            foreach (var member in clan.LivingMembers.ToList())
            {
                double chance = FoundationRules.HuntChance(member, guarded, ctx.Content);
                if (chance <= 0 || !ctx.Rng.Chance(chance)) continue;
                ctx.Log.Info($"[Foundation] {member.FullName}'s ripe Dao is harvested by a stronger cultivator.");
                clan.Kill(member, DeathCause.FoundationDevoured);
            }
        }

        /// <summary>
        /// The consumer absorbs the donor's foundation, a Dao Partner of theirs (same lineage, another
        /// foundation): they rise at once to the next Foundation stage, but can never progress again. The
        /// donor, their foundation devoured, dies (user decision, 2026-09-25).
        /// </summary>
        public bool ConsumeDaoPartner(CharacterData consumer, CharacterData donor)
        {
            if (!CanConsume(consumer, donor))
            {
                ctx.Log.Warning($"[Foundation] {consumer.FullName} cannot consume {donor.FullName}'s foundation.");
                return false;
            }

            consumer.RealmStage++;
            consumer.ProgressionSealed = true;
            ctx.Log.Info($"[Foundation] {consumer.FullName} consumes {donor.FullName}'s foundation, a Dao Partner: one stage higher, and no further for ever.");
            ctx.Events.TriggerHarm(consumer, donor); // an oath between them is broken
            clan.Kill(donor, DeathCause.FoundationDevoured);
            return true;
        }

        private bool CanConsume(CharacterData consumer, CharacterData donor)
        {
            if (consumer == null || donor == null || consumer == donor || !consumer.IsAlive || !donor.IsAlive) return false;
            if (consumer.Realm != CultivationRealm.Foundation || donor.Realm != CultivationRealm.Foundation) return false;
            if (consumer.ProgressionSealed || consumer.RealmStage >= PowerLadder.StageCount(CultivationRealm.Foundation)) return false;

            var (consumerLineage, consumerFoundation) = FoundationRef.Parse(consumer.FoundationId);
            var (donorLineage, donorFoundation) = FoundationRef.Parse(donor.FoundationId);
            return consumerLineage != null && consumerLineage == donorLineage && consumerFoundation != donorFoundation
                && techniques.Knowledge.Knows(FactKind.DaoPartners, consumer.FoundationId); // §5.3.3: know one's partners first
        }

        /// <summary>Each year, a foundation's holder may take on the temper its lineage favours.</summary>
        private void AlignHearts()
        {
            double chance = ctx.Content.Balance.HeartAlignmentYearlyChance;
            foreach (var member in clan.LivingMembers.Where(m => m.FoundationId != null).ToList())
            {
                var lineage = FoundationRules.FruitionOf(member.FoundationId, ctx.Content.Fruitions);
                if (lineage == null || lineage.Temperament == Temperament.None || member.Temperament == lineage.Temperament) continue;
                if (!ctx.Rng.Chance(chance)) continue;

                member.Temperament = lineage.Temperament;
                ctx.Log.Info($"[Foundation] {member.FullName}'s heart aligns with the {lineage.Name}.");
            }
        }
    }
}

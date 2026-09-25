using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

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
        /// The consumer absorbs the donor's foundation, a Dao Partner of theirs (same lineage, another
        /// foundation): they rise at once to the next Foundation stage, but can never progress again. The
        /// donor loses the foundation and falls back to the ninth Qi level (decision of 2026-09-25).
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
            LoseFoundation(donor);
            ctx.Log.Info($"[Foundation] {consumer.FullName} consumes {donor.FullName}'s foundation, a Dao Partner: one stage higher, and no further for ever.");
            return true;
        }

        private static bool CanConsume(CharacterData consumer, CharacterData donor)
        {
            if (consumer == null || donor == null || consumer == donor || !consumer.IsAlive || !donor.IsAlive) return false;
            if (consumer.Realm != CultivationRealm.Foundation || donor.Realm != CultivationRealm.Foundation) return false;
            if (consumer.ProgressionSealed || consumer.RealmStage >= PowerLadder.StageCount(CultivationRealm.Foundation)) return false;

            var (consumerLineage, consumerFoundation) = FoundationRef.Parse(consumer.FoundationId);
            var (donorLineage, donorFoundation) = FoundationRef.Parse(donor.FoundationId);
            return consumerLineage != null && consumerLineage == donorLineage && consumerFoundation != donorFoundation;
        }

        /// <summary>The donor's foundation is gone: back to the ninth Qi level, with that realm's reach.</summary>
        private void LoseFoundation(CharacterData donor)
        {
            donor.Realm = CultivationRealm.QiRefinement;
            donor.RealmStage = PowerLadder.StageCount(CultivationRealm.QiRefinement);
            donor.FoundationId = null;
            donor.CultivationXP = 0;
            int reach = PowerLadder.WoundedLifespan(PowerLadder.MaxLifespan(donor.Realm, donor.RealmStage), donor.DaoWounds);
            donor.MaxLifespan = TechniqueRules.LifespanWithMethod(reach, techniques.MethodOf(donor));
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

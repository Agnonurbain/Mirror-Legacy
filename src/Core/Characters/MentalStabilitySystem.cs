using System;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Mental stability (0-100): grief when close kin die, confidence after a breakthrough,
    /// doubt after a failure.
    /// </summary>
    public sealed class MentalStabilitySystem
    {
        public const int GriefPenalty = 15;
        public const int BreakthroughSuccessBonus = 10;
        public const int BreakthroughFailurePenalty = 10;

        private readonly GameContext ctx;
        private readonly ClanManager clan;

        public MentalStabilitySystem(GameContext ctx, ClanManager clan)
        {
            this.ctx = ctx;
            this.clan = clan;

            ctx.Events.OnCharacterDied += Grieve;
            ctx.Events.OnBreakthroughSuccess += (c, realm) => ApplyModifier(c, BreakthroughSuccessBonus);
            ctx.Events.OnBreakthroughFailed += c => ApplyModifier(c, -BreakthroughFailurePenalty);
        }

        public void ApplyModifier(CharacterData character, int amount)
        {
            if (!character.IsAlive) return;

            character.MentalStability = Math.Clamp(character.MentalStability + amount, 0, 100);
            if (character.MentalStability == 0)
                ctx.Log.Warning($"[MentalStability] {character.FullName} has lost all stability: Qi deviation looms.");
        }

        /// <summary>Parents, children and the spouse of the deceased grieve.</summary>
        private void Grieve(CharacterData deceased, DeathCause cause)
        {
            foreach (var member in clan.LivingMembers.ToList()) // a modifier may one day kill: never iterate the live roster
            {
                bool closeKin = member.FatherID == deceased.ID
                    || member.MotherID == deceased.ID
                    || member.SpouseID == deceased.ID
                    || deceased.FatherID == member.ID
                    || deceased.MotherID == member.ID;
                if (closeKin)
                    ApplyModifier(member, -GriefPenalty);
            }
        }
    }
}

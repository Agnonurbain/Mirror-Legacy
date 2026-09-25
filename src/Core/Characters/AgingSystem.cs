using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>Each new year everyone ages; those whose own lifespan runs out die of old age.</summary>
    public sealed class AgingSystem
    {
        private readonly ClanManager clan;

        public AgingSystem(GameContext ctx, ClanManager clan)
        {
            this.clan = clan;
        }

        /// <summary>Returns how many members died of old age.</summary>
        public int AgeOneYear()
        {
            var members = clan.LivingMembers.ToList();
            foreach (var member in members)
                member.Age++;

            // Individual lifespan: a mortal's roll, a realm's reach, minus any Dao wound
            var expired = members.Where(m => m.Age >= PowerLadder.LifespanLimit(m)).ToList();
            foreach (var member in expired)
                clan.Kill(member, DeathCause.OldAge);

            return expired.Count;
        }
    }
}

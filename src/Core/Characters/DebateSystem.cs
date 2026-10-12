using System.Collections.Generic;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>The Dao debates between the clan and the temples (R13-14b). See <see cref="DebateSettings"/>.</summary>
    public sealed class DebateSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly BuddhismSystem buddhism;
        private readonly WoundSystem wounds;
        private readonly Dictionary<string, int> lastDebates = new Dictionary<string, int>();

        public DebateSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, BuddhismSystem buddhism, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.buddhism = buddhism;
            this.wounds = wounds;
        }

        public Debate Pending { get; private set; }
        public IReadOnlyDictionary<string, int> LastDebates => lastDebates;
        public DebateFate? LastFate { get; private set; }

        public void Restore(Debate pending, IReadOnlyDictionary<string, int> last) { }
        public string DebaterRefusal(CharacterData member) => "todo";
        public int Odds(CharacterData member, string templeName) => 0;
        public string ChallengeRefusal(CharacterData member, string templeName) => "todo";
        public string Challenge(CharacterData member, string templeName) => "todo";
        public string Answer(CharacterData member) => "todo";
        public string Decline() => "todo";
        public void ProcessYear() { }
    }
}

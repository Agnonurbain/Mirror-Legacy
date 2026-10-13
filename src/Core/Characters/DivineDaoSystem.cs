using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>The Divine Dao (R15). See <see cref="DivineDaoSettings"/>.</summary>
    public sealed class DivineDaoSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly PatronSystem patrons;
        private readonly WoundSystem wounds;

        public DivineDaoSystem(GameContext ctx, ClanManager clan, ResourceManager resources, PatronSystem patrons, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.patrons = patrons;
            this.wounds = wounds;
        }

        public string AttendantRefusal(CharacterData member) => "todo";
        public string Borrow(CharacterData member, string patronId) => "todo";
        public string Unite(CharacterData member) => "todo";
        public string Own(CharacterData member) => "todo";
        public string CoreRefusal(CharacterData member) => "todo";
        public string CondenseCore(CharacterData member) => "todo";
        public void ProcessYear() { }
    }
}

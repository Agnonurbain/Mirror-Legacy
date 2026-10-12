using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.Diplomacy;

namespace MirrorChronicles.Characters
{
    /// <summary>The Buddhist conversion and its schools (R13-R14). See <see cref="BuddhismSettings"/>.</summary>
    public sealed class BuddhismSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;

        public BuddhismSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
        }

        public string ConversionRefusal(CharacterData member, string temple) => "todo";
        public bool Convert(CharacterData member, string temple) => false;
        public void ProcessYear() { }
    }
}

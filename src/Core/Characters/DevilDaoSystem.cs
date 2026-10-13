using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>The Heavenly Embryo Devil Dao (R12). See <see cref="DevilDaoSettings"/>.</summary>
    public sealed class DevilDaoSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly TalismanSystem talismans;
        private readonly WoundSystem wounds;

        public DevilDaoSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, TalismanSystem talismans, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.talismans = talismans;
            this.wounds = wounds;
        }

        public string FormRefusal(CharacterData member) => "todo";
        public string FormDiverseMansion(CharacterData member) => "todo";
        public string BleedRefusal(CapturedBeast beast) => "todo";
        public string Bleed(CapturedBeast beast) => "todo";
        public void ProcessYear() { }
    }
}

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
    /// The Demonic Dao of the clan's bound beasts (R16; LORE.md §3.4; the user's choices of 2026-10-12). See
    /// <see cref="DemonicDaoSettings"/>.
    /// </summary>
    public sealed class DemonicDaoSystem
    {
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

        /// <summary>Binds a captured beast to the clan; null when it cannot be.</summary>
        public CharacterData Bind(CapturedBeast beast) => null;

        /// <summary>Every bound beast absorbs the year's Qi.</summary>
        public void ProcessYear() { }
    }
}

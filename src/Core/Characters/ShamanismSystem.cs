using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>The Shamanic Talisman Dao (R17). See <see cref="ShamanismSettings"/>.</summary>
    public sealed class ShamanismSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly WoundSystem wounds;

        public ShamanismSystem(GameContext ctx, ClanManager clan, ResourceManager resources, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.wounds = wounds;
        }

        public static bool IsConcealed(CharacterData member) => false;
        public string ConversionRefusal(CharacterData member) => "todo";
        public string Convert(CharacterData member, ShamanMethod method) => "todo";
        public bool Refuge(CharacterData member) => false;
        public string CoreRefusal(CharacterData member) => "todo";
        public string CondenseCore(CharacterData member) => "todo";
        public void ProcessYear() { }
    }
}

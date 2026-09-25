using System.Linq;
using Godot;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// Temporary entry scene: proves the Godot layer reaches the engine-free core.
    /// Run with `-- --smoke` to print and quit (used by Scripts/dev.sh smoke).
    /// </summary>
    public partial class Boot : Node
    {
        public override void _Ready()
        {
            var founder = new CharacterData
            {
                Realm = CultivationRealm.QiRefinement,
                RealmStage = 3,
                HasSpiritualOrifice = true,
                OrificeKnown = true
            };
            GD.Print($"[Boot] Reflets de Lignée — core ready: {RankCatalog.DisplayName(founder)}");

            if (OS.GetCmdlineUserArgs().Contains("--smoke"))
                GetTree().Quit();
        }
    }
}

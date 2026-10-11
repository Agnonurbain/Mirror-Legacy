using MirrorChronicles.Characters;
using MirrorChronicles.Data;

namespace MirrorChronicles.Combat
{
    /// <summary>Who may fight on the grid for the clan (G6): a living, free cultivator it knows to be one.</summary>
    public static class FighterRules
    {
        /// <summary>Why this member cannot fight, or null when it can.</summary>
        public static string Refusal(CharacterData member)
        {
            if (member == null || !member.IsAlive) return "introuvable";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (!member.OrificeKnown || !SpiritualOrificeRules.CanCultivate(member)) return "ne cultive pas";
            return null;
        }
    }
}

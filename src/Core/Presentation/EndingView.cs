using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>The ending screen (B3e, LORE.md §11.9): a dynastic ending's title, who and when, its story, and the way on.</summary>
    public sealed record EndingScreen(string Title, string Byline, string Story, string Continue);

    /// <summary>The dynastic endings and the defeats as the player reads them.</summary>
    public static class EndingView
    {
        public static EndingScreen Of(EndingDefinition ending, string subject, int year) =>
            new EndingScreen(ending.Name, subject == null ? $"An {year} · le clan" : $"An {year} · {subject}", ending.Narrative, "Continuer la partie");

        /// <summary>Why the game is lost (an older save does not tell: the plain word).</summary>
        public static string Defeat(GameSession s) => s.Victory.Loss switch
        {
            DefeatCause.Extinct => "La lignée s'est éteinte.",
            DefeatCause.MirrorSeized => "Le miroir a été saisi : le secret du clan est perdu.",
            DefeatCause.Absorbed => "Le clan a été absorbé par son suzerain : il n'est plus à lui-même.",
            _ => s.Victory.GameLost ? "La partie est perdue." : ""
        };
    }
}

using MirrorChronicles.Data;

namespace MirrorChronicles.Presentation
{
    /// <summary>The artifacts as the clan sees them (L4f).</summary>
    public static class ArtifactView
    {
        public static string ClassLabel(ArtifactClass cls) => cls switch
        {
            ArtifactClass.DharmaArtifact => "Artefact de Dharma",
            ArtifactClass.SpiritualArtifact => "Artefact Spirituel",
            ArtifactClass.SpiritualTreasure => "Trésor Spirituel",
            _ => "artefact"
        };
    }
}

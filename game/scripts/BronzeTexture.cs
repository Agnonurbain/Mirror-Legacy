using Godot;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// Worn bronze for the buttons (Shuimo), drawn once at startup: the metal's tone, fine brushed streaks, a green
    /// patina gathered at the edges, a darker rim. Applied over the project theme's button styles; text stays in ink.
    /// </summary>
    public static class BronzeTexture
    {
        private const int Width = 96;
        private const int Height = 32;
        private const int Rim = 3;

        private static readonly Color Patina = new Color(0.36f, 0.50f, 0.40f);

        public static ImageTexture Build(Color metal, int seed)
        {
            var noise = new FastNoiseLite { Seed = seed, Frequency = 0.08f, NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex };
            var streaks = new FastNoiseLite { Seed = seed + 1, Frequency = 0.6f };
            var image = Image.CreateEmpty(Width, Height, false, Image.Format.Rgba8);
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    float tone = 0.5f + 0.5f * noise.GetNoise2D(x, y);
                    float streak = 0.5f + 0.5f * streaks.GetNoise2D(x * 0.15f, y * 3f); // brushed along the length
                    var colour = metal.Darkened(0.12f * (1 - tone)).Lightened(0.06f * streak);
                    float edge = Mathf.Min(Mathf.Min(x, Width - 1 - x), Mathf.Min(y, Height - 1 - y));
                    if (edge < Rim + 3) colour = colour.Lerp(Patina, 0.18f * tone * (1 - edge / (Rim + 3f)));
                    if (edge < Rim) colour = colour.Darkened(0.35f - 0.08f * edge);
                    image.SetPixel(x, y, colour);
                }
            return ImageTexture.CreateFromImage(image);
        }

        /// <summary>The texture as a button style: the rim kept, the middle stretched, room for the text.</summary>
        public static StyleBoxTexture Style(ImageTexture texture) => new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = Rim + 3, TextureMarginRight = Rim + 3, TextureMarginTop = Rim + 3, TextureMarginBottom = Rim + 3,
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 5, ContentMarginBottom = 5
        };

        /// <summary>Lays worn bronze over the project theme's buttons and dropdowns (normal, hover, disabled).</summary>
        public static void Apply(Theme theme)
        {
            if (theme == null) return;
            var normal = Style(Build(new Color(0.80f, 0.69f, 0.50f), 11));
            var hover = Style(Build(new Color(0.72f, 0.60f, 0.40f), 12));
            var disabled = Style(Build(new Color(0.84f, 0.80f, 0.71f), 13)); // dulled, the patina all but gone
            foreach (var type in new[] { "Button", "OptionButton" })
            {
                theme.SetStylebox("normal", type, normal);
                theme.SetStylebox("hover", type, hover);
                theme.SetStylebox("disabled", type, disabled); // pressed stays the seal's red (shuimo.tres)
            }
        }
    }
}

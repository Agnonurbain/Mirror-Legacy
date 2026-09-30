using System;
using System.Collections.Generic;
using Godot;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// A landscape in fine ink lines for the title screen (Shuimo, after valaxy-theme-shuimo): three ranges of mountains,
    /// the far ones faint, each ridge drawn with the texture strokes (cun) that fall from it; pines at their feet, a
    /// pavilion, still water, a fisherman in his boat, and a red sun — the upper half left empty (liubai). Drawn from a
    /// fixed seed: the same painting every time.
    /// </summary>
    public partial class LandscapeCanvas : Control
    {
        private const int Seed = 1789;
        private static readonly Color Ink = new Color(0.16f, 0.15f, 0.14f);
        private static readonly Color Sun = new Color(0.72f, 0.14f, 0.11f, 0.85f);
        private static readonly Color Paper = new Color(0.955f, 0.925f, 0.84f, 0.82f); // the page's tone, a mist: a nearer peak veils a farther one

        public override void _Ready()
        {
            MouseFilter = MouseFilterEnum.Ignore;
            Resized += QueueRedraw;
        }

        public override void _Draw()
        {
            var rng = new Random(Seed);
            float w = Size.X, h = Size.Y;
            DrawCircle(new Vector2(w * 0.84f, h * 0.2f), h * 0.045f, Sun); // in the empty sky, away from the words

            float horizon = h * 0.78f;
            Range(rng, horizon - h * 0.12f, h * 0.20f, 7, 0.22f, trees: false);  // far
            Range(rng, horizon - h * 0.05f, h * 0.26f, 5, 0.45f, trees: true);   // middle
            Range(rng, horizon + h * 0.02f, h * 0.22f, 3, 0.75f, trees: true);   // near
            Water(rng, horizon + h * 0.05f);
            Pavilion(new Vector2(w * 0.62f, horizon - h * 0.02f), h * 0.035f);
            Boat(new Vector2(w * 0.3f, horizon + h * 0.12f), h * 0.03f);
        }

        /// <summary>A range of peaks along a base line: ridges, their texture strokes, pines at the feet.</summary>
        private void Range(Random rng, float baseY, float height, int peaks, float strength, bool trees)
        {
            float w = Size.X;
            float step = w / peaks;
            for (int i = 0; i < peaks; i++)
            {
                float cx = step * (i + 0.5f) + (float)(rng.NextDouble() - 0.5) * step * 0.6f;
                float half = step * (0.55f + (float)rng.NextDouble() * 0.35f);
                float top = baseY - height * (0.6f + (float)rng.NextDouble() * 0.4f);
                var ridge = Ridge(rng, cx, half, top, baseY);
                var body = new List<Vector2>(ridge); // closed along the foot: no band beneath
                DrawColoredPolygon(body.ToArray(), Paper); // the mountain's body, hiding what lies behind it
                DrawPolyline(ridge.ToArray(), new Color(Ink, strength), 1.4f, antialiased: true);
                Cun(rng, ridge, baseY, strength);
                if (trees) Pines(rng, cx, half, baseY, height, strength);
            }
        }

        /// <summary>The ridge line: up one flank to the peak and down the other, trembling like a brush.</summary>
        private static List<Vector2> Ridge(Random rng, float cx, float half, float top, float baseY)
        {
            var points = new List<Vector2>();
            const int Steps = 26;
            float peakAt = 0.35f + (float)rng.NextDouble() * 0.3f;          // the summit off-centre
            float shoulderAt = rng.NextDouble() < 0.5 ? 0.2f : 0.78f;          // a lower shoulder on one flank
            float shoulder = 0.35f + (float)rng.NextDouble() * 0.3f;
            for (int s = 0; s <= Steps; s++)
            {
                float t = s / (float)Steps;
                float x = cx - half + 2 * half * t;
                float main = 1 - MathF.Pow(MathF.Abs(t - peakAt) / MathF.Max(peakAt, 1 - peakAt), 1.2f);
                float second = shoulder * MathF.Exp(-MathF.Pow((t - shoulderAt) / 0.12f, 2));
                float rise = MathF.Max(0, MathF.Max(main, second));
                float y = baseY - (baseY - top) * rise + (float)(rng.NextDouble() - 0.5) * 3f;
                points.Add(new Vector2(x, y));
            }
            return points;
        }

        /// <summary>Texture strokes (cun): lines falling from the ridge along the slope, fainter lower down.</summary>
        private void Cun(Random rng, List<Vector2> ridge, float baseY, float strength)
        {
            int summit = 0;
            for (int i = 1; i < ridge.Count; i++) if (ridge[i].Y < ridge[summit].Y) summit = i;
            for (int i = 1; i < ridge.Count - 1; i++)
            {
                if (rng.NextDouble() < 0.35) continue;
                var from = ridge[i] + new Vector2(0, 3);
                float lean = i < summit ? -1 : 1; // down each flank, away from the summit
                float length = (baseY - from.Y) * (0.12f + (float)rng.NextDouble() * 0.22f);
                for (int k = 0; k < 2; k++) // two strokes side by side, like a dry brush
                {
                    var stroke = new List<Vector2>();
                    var start = from + new Vector2(k * 3f * lean, k * 4f);
                    for (int s = 0; s <= 5; s++)
                    {
                        float t = s / 5f;
                        stroke.Add(start + new Vector2(lean * length * 0.5f * t * t, length * t));
                    }
                    DrawPolyline(stroke.ToArray(), new Color(Ink, strength * (0.5f - k * 0.2f)), 1f, antialiased: true);
                }
            }
        }

        /// <summary>Pines at a mountain's feet: a trunk and tiers of needles.</summary>
        private void Pines(Random rng, float cx, float half, float baseY, float height, float strength)
        {
            int count = 3 + rng.Next(5);
            for (int i = 0; i < count; i++)
            {
                float x = cx + (float)(rng.NextDouble() - 0.5) * half * 1.2f;
                float tall = height * (0.22f + (float)rng.NextDouble() * 0.16f);
                var root = new Vector2(x, baseY - (float)rng.NextDouble() * height * 0.1f);
                var colour = new Color(Ink, strength * 0.9f);
                DrawLine(root, root - new Vector2(0, tall), colour, 1.2f, antialiased: true);
                for (int tier = 0; tier < 5; tier++) // clusters of needles: dark, ragged blots
                {
                    float y = root.Y - tall * (0.3f + tier * 0.15f);
                    float spread = tall * (0.32f - tier * 0.05f);
                    var blot = new Vector2[7];
                    for (int k = 0; k < blot.Length; k++)
                    {
                        float angle = Mathf.Tau * k / blot.Length;
                        float r = spread * (0.6f + (float)rng.NextDouble() * 0.5f);
                        blot[k] = new Vector2(x + MathF.Cos(angle) * r, y + MathF.Sin(angle) * r * 0.35f);
                    }
                    DrawColoredPolygon(blot, new Color(Ink, strength * 0.75f));
                }
            }
        }

        private void Water(Random rng, float y)
        {
            for (int i = 0; i < 14; i++)
            {
                float x = (float)rng.NextDouble() * Size.X;
                float length = 20 + (float)rng.NextDouble() * 60;
                float row = y + (float)rng.NextDouble() * Size.Y * 0.15f;
                DrawLine(new Vector2(x, row), new Vector2(x + length, row), new Color(Ink, 0.18f), 1f, antialiased: true);
            }
        }

        /// <summary>A pavilion: a curved roof on two posts.</summary>
        private void Pavilion(Vector2 at, float size)
        {
            var colour = new Color(Ink, 0.7f);
            DrawPolyline(new[] { at + new Vector2(-size * 1.3f, -size * 0.6f), at + new Vector2(0, -size * 1.2f), at + new Vector2(size * 1.3f, -size * 0.6f) },
                colour, 1.6f, antialiased: true);
            DrawLine(at + new Vector2(-size * 0.8f, -size * 0.75f), at + new Vector2(-size * 0.8f, 0), colour, 1.2f);
            DrawLine(at + new Vector2(size * 0.8f, -size * 0.75f), at + new Vector2(size * 0.8f, 0), colour, 1.2f);
            DrawLine(at + new Vector2(-size, 0), at + new Vector2(size, 0), colour, 1.2f);
        }

        /// <summary>A fisherman in his boat: the hull, the conical hat, the rod and its line.</summary>
        private void Boat(Vector2 at, float size)
        {
            var colour = new Color(Ink, 0.75f);
            DrawPolyline(new[] { at + new Vector2(-size * 2, -size * 0.3f), at + new Vector2(-size, size * 0.3f), at + new Vector2(size, size * 0.3f),
                at + new Vector2(size * 2, -size * 0.3f) }, colour, 1.5f, antialiased: true);
            DrawLine(at + new Vector2(0, 0), at + new Vector2(0, -size * 0.9f), colour, 1.5f);
            DrawPolyline(new[] { at + new Vector2(-size * 0.6f, -size * 0.8f), at + new Vector2(0, -size * 1.4f), at + new Vector2(size * 0.6f, -size * 0.8f) },
                colour, 1.5f, antialiased: true);
            DrawLine(at + new Vector2(size * 0.3f, -size * 0.6f), at + new Vector2(size * 3.5f, -size * 3f), colour, 1f, antialiased: true);
            DrawLine(at + new Vector2(size * 3.5f, -size * 3f), at + new Vector2(size * 3.7f, size * 0.6f), new Color(Ink, 0.3f), 0.8f);
        }
    }
}

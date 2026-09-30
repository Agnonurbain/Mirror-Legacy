using Godot;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// An ink portrait in fine lines (Shuimo): the aura of the realm behind the head, the robe washed with the element's
    /// tint, the face in a few strokes, the hair by age and sex — two buns for a child, a topknot or a high bun for the
    /// grown, grey hair and a beard for the old. It only draws a <see cref="Portrait"/>.
    /// </summary>
    public static class InkPortrait
    {
        private static readonly Color Ink = new Color(0.14f, 0.13f, 0.12f);
        private static readonly Color Grey = new Color(0.52f, 0.50f, 0.47f);
        private static readonly Color Skin = new Color(0.97f, 0.94f, 0.88f);

        public static Color Tint(Element element) => element switch
        {
            Element.Fire => new Color(0.72f, 0.25f, 0.15f),
            Element.Water => new Color(0.25f, 0.40f, 0.55f),
            Element.Wood => new Color(0.33f, 0.52f, 0.32f),
            Element.Metal => new Color(0.66f, 0.63f, 0.55f),
            Element.Earth => new Color(0.62f, 0.47f, 0.25f),
            Element.Lightning => new Color(0.50f, 0.42f, 0.68f),
            Element.Darkness => new Color(0.28f, 0.24f, 0.30f),
            Element.Light => new Color(0.85f, 0.72f, 0.35f),
            _ => new Color(0.55f, 0.52f, 0.46f)
        };

        public static void Draw(CanvasItem canvas, Rect2 rect, Portrait p)
        {
            float a = p.Faded ? 0.4f : 1f;
            Color Alpha(Color c, float k = 1f) => new Color(c, c.A * a * k);
            var tint = Tint(p.Element);
            float w = rect.Size.X, h = rect.Size.Y;
            var head = rect.Position + new Vector2(w * 0.5f, h * (p.Age == AgeBracket.Child ? 0.46f : 0.40f));
            float r = w * (p.Age == AgeBracket.Child ? 0.19f : 0.17f);
            int v = p.Variation;

            for (int ring = 1; ring <= Mathf.Min(p.Aura, 4); ring++) // the realm's aura
                canvas.DrawArc(head, r + ring * w * 0.07f, 0, Mathf.Tau, 28, Alpha(tint, 0.25f), 1.2f, true);

            // the robe: shoulders, washed with the element, a crossed collar
            float shoulders = w * (p.Age == AgeBracket.Child ? 0.30f : 0.40f);
            var neck = head + new Vector2(0, r * 1.05f);
            var robe = new[] { neck + new Vector2(-r * 0.5f, 0), neck + new Vector2(r * 0.5f, 0),
                rect.Position + new Vector2(w * 0.5f + shoulders, h), rect.Position + new Vector2(w * 0.5f - shoulders, h) };
            canvas.DrawColoredPolygon(robe, Alpha(tint, 0.45f));
            canvas.DrawPolyline(new[] { robe[3], robe[0], robe[1], robe[2] }, Alpha(Ink, 0.8f), 1.2f, true);
            canvas.DrawLine(neck + new Vector2(-r * 0.5f, 0), neck + new Vector2(r * 0.25f, r * 0.9f), Alpha(Ink, 0.7f), 1f, true);
            canvas.DrawLine(neck + new Vector2(r * 0.5f, 0), neck + new Vector2(-r * 0.1f, r * 0.7f), Alpha(Ink, 0.7f), 1f, true);

            // the face
            canvas.DrawCircle(head, r, Alpha(Skin));
            canvas.DrawArc(head, r, 0, Mathf.Tau, 24, Alpha(Ink, 0.85f), 1.2f, true);
            float slant = (v % 3 - 1) * 0.6f;
            canvas.DrawLine(head + new Vector2(-r * 0.5f, -r * 0.05f + slant), head + new Vector2(-r * 0.2f, -r * 0.05f), Alpha(Ink), 1.1f, true);
            canvas.DrawLine(head + new Vector2(r * 0.2f, -r * 0.05f), head + new Vector2(r * 0.5f, -r * 0.05f + slant), Alpha(Ink), 1.1f, true);
            canvas.DrawLine(head + new Vector2(-r * 0.12f, r * 0.45f), head + new Vector2(r * 0.12f, r * 0.45f), Alpha(Ink, 0.6f), 1f, true);

            var hair = p.Age >= AgeBracket.Elder ? Grey : p.Age == AgeBracket.Mature && v % 2 == 0 ? Ink.Lerp(Grey, 0.4f) : Ink;
            canvas.DrawArc(head, r, Mathf.Pi * 1.05f, Mathf.Pi * 1.95f, 16, Alpha(hair), 2.4f, true); // the hairline
            switch (p.Age)
            {
                case AgeBracket.Child: // two little buns
                    canvas.DrawCircle(head + new Vector2(-r * 0.75f, -r * 0.85f), r * 0.3f, Alpha(hair));
                    canvas.DrawCircle(head + new Vector2(r * 0.75f, -r * 0.85f), r * 0.3f, Alpha(hair));
                    break;
                default:
                    if (p.IsMale) // a topknot, a pin through it
                    {
                        canvas.DrawCircle(head + new Vector2(0, -r * 1.15f), r * 0.32f, Alpha(hair));
                        canvas.DrawLine(head + new Vector2(-r * 0.6f, -r * 1.1f), head + new Vector2(r * 0.6f, -r * 1.2f), Alpha(Ink, 0.7f), 1f, true);
                    }
                    else // a high bun and hair falling at the sides
                    {
                        canvas.DrawCircle(head + new Vector2(r * 0.3f, -r * 1.15f), r * 0.42f, Alpha(hair));
                        canvas.DrawLine(head + new Vector2(-r, 0), head + new Vector2(-r * 1.05f, r * 1.3f), Alpha(hair), 1.6f, true);
                        canvas.DrawLine(head + new Vector2(r, 0), head + new Vector2(r * 1.05f, r * 1.3f), Alpha(hair), 1.6f, true);
                    }
                    break;
            }
            if (p.IsMale && p.Age >= AgeBracket.Mature) // a beard, long for the old
            {
                float length = p.Age == AgeBracket.Elder ? r * 1.2f : r * 0.5f;
                canvas.DrawLine(head + new Vector2(0, r * 0.75f), head + new Vector2(0, r * 0.75f + length), Alpha(hair), 1.5f, true);
                canvas.DrawLine(head + new Vector2(-r * 0.2f, r * 0.7f), head + new Vector2(-r * 0.15f, r * 0.7f + length * 0.8f), Alpha(hair), 1f, true);
                canvas.DrawLine(head + new Vector2(r * 0.2f, r * 0.7f), head + new Vector2(r * 0.15f, r * 0.7f + length * 0.8f), Alpha(hair), 1f, true);
            }
            if (p.Age == AgeBracket.Elder) // the years on the brow
                canvas.DrawArc(head + new Vector2(0, -r * 0.35f), r * 0.35f, Mathf.Pi * 1.2f, Mathf.Pi * 1.8f, 8, Alpha(Ink, 0.45f), 0.8f, true);
        }
    }

    /// <summary>A portrait as a control, for the roster.</summary>
    public partial class PortraitIcon : Control
    {
        private Portrait portrait;

        public PortraitIcon(Portrait portrait)
        {
            this.portrait = portrait;
            CustomMinimumSize = new Vector2(40, 48);
            MouseFilter = MouseFilterEnum.Ignore;
            SizeFlagsVertical = SizeFlags.ShrinkCenter;
        }

        public PortraitIcon() { }

        public override void _Draw()
        {
            if (portrait != null) InkPortrait.Draw(this, new Rect2(Vector2.Zero, Size), portrait);
        }
    }
}

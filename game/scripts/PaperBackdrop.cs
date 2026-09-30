using Godot;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// Behind every screen (Shuimo): rice paper (a shader: grain, fibres, edges darkened by age) framed by a double ink
    /// rule, with auspicious clouds (xiangyun) curling in the corners. Added once by <see cref="GameRoot"/> on a canvas
    /// layer below the scenes; it only draws.
    /// </summary>
    public partial class PaperBackdrop : CanvasLayer
    {
        private const int BehindTheScenes = -10;

        public override void _Ready()
        {
            Layer = BehindTheScenes;
            var paper = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore };
            paper.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            paper.Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://theme/paper.gdshader") };
            AddChild(paper);
            var frame = new CloudFrame { MouseFilter = Control.MouseFilterEnum.Ignore };
            frame.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            AddChild(frame);
        }

        /// <summary>The double ink rule and the corner clouds.</summary>
        private partial class CloudFrame : Control
        {
            private static readonly Color Ink = new Color(0.13f, 0.12f, 0.11f, 0.35f);
            private static readonly Color Faint = new Color(0.13f, 0.12f, 0.11f, 0.18f);
            private const float Outer = 5f;
            private const float Inner = 9f;
            private const float Cloud = 14f;

            public override void _Ready() => Resized += QueueRedraw;

            public override void _Draw()
            {
                var size = Size;
                DrawRect(new Rect2(Outer, Outer, size.X - 2 * Outer, size.Y - 2 * Outer), Ink, filled: false, width: 1.5f);
                DrawRect(new Rect2(Inner, Inner, size.X - 2 * Inner, size.Y - 2 * Inner), Faint, filled: false, width: 1f);
                DrawCloud(new Vector2(Inner + Cloud, Inner + Cloud * 0.7f), 1, 1);
                DrawCloud(new Vector2(size.X - Inner - Cloud, Inner + Cloud * 0.7f), -1, 1);
                DrawCloud(new Vector2(Inner + Cloud, size.Y - Inner - Cloud * 0.7f), 1, -1);
                DrawCloud(new Vector2(size.X - Inner - Cloud, size.Y - Inner - Cloud * 0.7f), -1, -1);
            }

            /// <summary>An auspicious cloud: three curling lobes, a spiral in the largest, a tail sweeping along the rule.</summary>
            private void DrawCloud(Vector2 at, int sx, int sy)
            {
                var flip = new Vector2(sx, sy);
                DrawArc(at, Cloud * 0.55f, Mathf.Pi * 0.2f, Mathf.Pi * 1.9f, 24, Ink, 1.6f);
                DrawArc(at, Cloud * 0.3f, Mathf.Pi * 0.6f, Mathf.Pi * 2.3f, 20, Ink, 1.4f);
                DrawArc(at + new Vector2(Cloud * 0.75f, Cloud * 0.25f) * flip, Cloud * 0.32f, Mathf.Pi, Mathf.Pi * 2.4f, 18, Ink, 1.4f);
                DrawArc(at + new Vector2(Cloud * 0.2f, Cloud * 0.62f) * flip, Cloud * 0.25f, -Mathf.Pi * 0.3f, Mathf.Pi * 1.1f, 16, Ink, 1.3f);
                DrawLine(at + new Vector2(Cloud * 1.05f, Cloud * 0.25f) * flip, at + new Vector2(Cloud * 2.4f, Cloud * 0.1f) * flip, Faint, 1.3f);
            }
        }
    }
}

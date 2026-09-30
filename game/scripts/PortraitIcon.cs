using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
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

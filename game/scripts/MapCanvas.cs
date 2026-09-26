using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// Paints the world map in ink on paper (Shuimo): a grained paper, washes under each place (richer where its Qi is
    /// dense), a tinted haze under an atmosphere (violet-grey for a storm that weighs on all, ochre-red otherwise),
    /// borders as brush lines, mountains as three brushed peaks, waters as washes with ripples, wilds as grass strokes,
    /// deserts as dotted sand, towns as roofs, the clan's home as a red seal. Minor places whose name finds no free
    /// room are named only once zoomed in. The wheel zooms around the cursor, a right or middle drag pans. It only
    /// draws the engine-free <see cref="WorldMapView"/>; every stroke is placed by a stable hash of the place, so the
    /// painting never shimmers between frames.
    /// </summary>
    public partial class MapCanvas : Control
    {
        private const float PlaceRadius = 6f;
        private const float StateRadius = 34f;
        private const float HomeRadius = 9f;
        private const float PickRadius = 14f;
        private const int LabelSize = 11;
        private const int StateLabelSize = 15;

        private static readonly Color Paper = new Color(0.93f, 0.90f, 0.82f);
        private static readonly Color Ink = new Color(0.13f, 0.12f, 0.11f);
        private static readonly Color Wash = new Color(0.13f, 0.12f, 0.11f, 0.08f);
        private static readonly Color Water = new Color(0.25f, 0.36f, 0.42f, 0.14f);
        private static readonly Color Brush = new Color(0.13f, 0.12f, 0.11f, 0.35f);
        private static readonly Color Seal = new Color(0.70f, 0.12f, 0.10f);
        private static readonly Color Selection = new Color(0.70f, 0.12f, 0.10f, 0.6f);
        private static readonly Color StateInk = new Color(0.13f, 0.12f, 0.11f, 0.55f);
        private static readonly Color WaterInk = new Color(0.20f, 0.33f, 0.42f);
        private static readonly Color Grain = new Color(0.45f, 0.38f, 0.28f, 0.06f);
        private static readonly Color StormHaze = new Color(0.36f, 0.30f, 0.45f, 0.10f);
        private static readonly Color BalefulHaze = new Color(0.55f, 0.25f, 0.12f, 0.08f);
        private static readonly Color MountainWash = new Color(0.20f, 0.22f, 0.20f, 0.07f);
        private static readonly Color WildsWash = new Color(0.25f, 0.35f, 0.22f, 0.06f);
        private static readonly Color PlainWash = new Color(0.40f, 0.45f, 0.25f, 0.05f);
        private static readonly Color DesertWash = new Color(0.65f, 0.50f, 0.25f, 0.08f);
        private static readonly Color TownWash = new Color(0.13f, 0.12f, 0.11f, 0.04f);
        private const int GrainDots = 900;
        private const float WashRadius = 26f;
        private const int LabelOutline = 4;
        private const float Margin = 48f;

        private IReadOnlyList<MapPlace> places = Array.Empty<MapPlace>();
        private IReadOnlyList<MapBorder> borders = Array.Empty<MapBorder>();
        private string selected;
        private float zoom = 1f;
        private Vector2 pan = Vector2.Zero;

        private const float MinZoom = 1f;
        private const float MaxZoom = 4f;
        private const float ZoomStep = 1.15f;

        /// <summary>Raised with the id of the place the player clicks.</summary>
        public event Action<string> PlaceSelected;

        public void Show(IReadOnlyList<MapPlace> newPlaces, IReadOnlyList<MapBorder> newBorders, string selectedId)
        {
            places = newPlaces;
            borders = newBorders;
            selected = selectedId;
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawRect(new Rect2(Vector2.Zero, Size), Paper);
            DrawGrain();
            var byId = places.ToDictionary(p => p.Id);

            foreach (var place in places.Where(p => !p.IsState)) DrawWash(place, At(place)); // the land first, under everything

            // states and seas are washes behind the places, named in large faint ink; only the places inside are joined
            foreach (var state in places.Where(p => p.IsState))
            {
                DrawCircle(At(state), StateRadius, state.Kind == RegionKind.Sea ? Water : Wash);
                Label(state.Name, StateLabelRect(state).Position + new Vector2(0, StateLabelSize), StateLabelSize, StateInk, centred: false);
            }

            foreach (var border in borders)
                if (byId.TryGetValue(border.A, out var a) && byId.TryGetValue(border.B, out var b) && !a.IsState && !b.IsState)
                    DrawLine(At(a), At(b), Brush, 1f, true);

            foreach (var place in places.Where(p => !p.IsState))
            {
                var at = At(place);
                if (place.Id == selected) DrawArc(at, PickRadius, 0, Mathf.Tau, 24, Selection, 2f, true);
                DrawGlyph(place, at);
            }

            foreach (var (name, at) in PlaceLabels()) // labels last, over every line and glyph
                Label(name, at, LabelSize, Ink, centred: false);
        }

        /// <summary>The paper's grain: faint fibres fixed to the frame, not to the map.</summary>
        private void DrawGrain()
        {
            for (int i = 0; i < GrainDots; i++)
            {
                var at = new Vector2(Noise("paper", i) * Size.X, Noise("paper", i + GrainDots) * Size.Y);
                DrawLine(at, at + new Vector2(2f + Noise("fibre", i) * 4f, Noise("tilt", i) * 2f - 1f), Grain, 1f, true);
            }
        }

        /// <summary>
        /// Layered washes under a place, wider and deeper where its Qi is dense, and the haze of its atmosphere,
        /// each layer nudged by the place's own hash so neighbouring washes blend like wet ink.
        /// </summary>
        private void DrawWash(MapPlace place, Vector2 at)
        {
            float scale = Mathf.Sqrt(zoom) * (float)Math.Clamp(place.QiDensity, 0.5, 1.5);
            var colour = WashOf(place.Kind);
            for (int layer = 0; layer < 3; layer++)
            {
                var offset = new Vector2(Noise(place.Id, layer) - 0.5f, Noise(place.Id, layer + 7) - 0.5f) * WashRadius * 0.6f * scale;
                DrawCircle(at + offset, WashRadius * scale * (1f - layer * 0.22f), colour);
            }
            if (place.Atmosphere != null)
                for (int layer = 0; layer < 2; layer++)
                    DrawCircle(at, WashRadius * 1.5f * scale * (1f - layer * 0.3f), place.AtmosphereHarsh ? StormHaze : BalefulHaze);
        }

        private static Color WashOf(RegionKind kind) => kind switch
        {
            RegionKind.Mountain => MountainWash,
            RegionKind.Lake or RegionKind.River or RegionKind.Sea or RegionKind.Island => Water,
            RegionKind.Wilds => WildsWash,
            RegionKind.Plain => PlainWash,
            RegionKind.Desert => DesertWash,
            _ => TownWash
        };

        /// <summary>
        /// The home as a red seal; mountains as three brushed peaks, waters as washes with ripples, wilds as grass
        /// strokes, deserts as dotted sand, towns as a roof, other places as ink dots. An inhabited place is drawn larger.
        /// </summary>
        private void DrawGlyph(MapPlace place, Vector2 at)
        {
            if (place.IsHome)
            {
                DrawSeal(at);
                return;
            }
            float size = (place.Factions.Count > 0 ? PlaceRadius : PlaceRadius * 0.6f) * Mathf.Sqrt(zoom);
            switch (place.Kind)
            {
                case RegionKind.Mountain: DrawPeaks(at, size); break;
                case RegionKind.Lake:
                case RegionKind.River:
                case RegionKind.Sea:
                case RegionKind.Island: DrawWater(at, size); break;
                case RegionKind.Wilds: DrawGrass(place.Id, at, size); break;
                case RegionKind.Desert: DrawSand(place.Id, at, size); break;
                case RegionKind.Prefecture: DrawRoof(at, size); break;
                default: DrawCircle(at, size, Ink); break;
            }
        }

        private void DrawSeal(Vector2 at)
        {
            float r = HomeRadius * Mathf.Sqrt(zoom);
            DrawRect(new Rect2(at - Vector2.One * r, Vector2.One * r * 2), Seal);
            DrawRect(new Rect2(at - Vector2.One * r * 0.6f, Vector2.One * r * 1.2f), Paper, false, 1.5f); // the carved frame
            DrawLine(at + new Vector2(-r * 0.35f, 0), at + new Vector2(r * 0.35f, 0), Paper, 1.5f);
            DrawLine(at + new Vector2(0, -r * 0.35f), at + new Vector2(0, r * 0.35f), Paper, 1.5f);
        }

        /// <summary>Three peaks, the highest in the middle: a pale wash, then the ink ridge.</summary>
        private void DrawPeaks(Vector2 at, float size)
        {
            foreach (var (dx, h) in new[] { (-0.9f, 0.9f), (0.9f, 1.0f), (0f, 1.5f) }) // the far peaks first
            {
                var top = at + new Vector2(dx * size, -h * size);
                var left = at + new Vector2(dx * size - size, size * 0.8f);
                var right = at + new Vector2(dx * size + size, size * 0.8f);
                DrawColoredPolygon(new[] { top, right, left }, new Color(Ink, 0.25f));
                DrawPolyline(new[] { left, top, right }, Ink, 1.6f, true);
            }
        }

        private void DrawWater(Vector2 at, float size)
        {
            DrawSetTransform(at, 0, new Vector2(1.4f, 0.8f));
            DrawCircle(Vector2.Zero, size * 1.1f, new Color(WaterInk, 0.35f));
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
            for (int ripple = 0; ripple < 2; ripple++)
                DrawArc(at + new Vector2(0, (ripple - 0.5f) * size * 0.8f), size * (0.9f - ripple * 0.3f), Mathf.Pi * 1.15f, Mathf.Pi * 1.85f, 8, WaterInk, 1.2f, true);
        }

        private void DrawGrass(string id, Vector2 at, float size)
        {
            for (int blade = 0; blade < 5; blade++)
            {
                var root = at + new Vector2((Noise(id, blade) - 0.5f) * size * 2.4f, (Noise(id, blade + 11) - 0.5f) * size);
                DrawLine(root, root + new Vector2((Noise(id, blade + 23) - 0.5f) * size * 0.6f, -size * 1.2f), Ink, 1.2f, true);
            }
        }

        private void DrawSand(string id, Vector2 at, float size)
        {
            for (int grain = 0; grain < 9; grain++)
                DrawCircle(at + new Vector2((Noise(id, grain) - 0.5f) * size * 3f, (Noise(id, grain + 31) - 0.5f) * size * 1.4f), 1.1f, Ink);
        }

        /// <summary>A town: a roof over a dot.</summary>
        private void DrawRoof(Vector2 at, float size)
        {
            DrawPolyline(new[] { at + new Vector2(-size * 1.2f, 0), at + new Vector2(0, -size), at + new Vector2(size * 1.2f, 0) }, Ink, 1.8f, true);
            DrawCircle(at + new Vector2(0, size * 0.4f), size * 0.45f, Ink);
        }

        /// <summary>A stable number in [0, 1) for a place and a salt (FNV-1a; never string.GetHashCode, which changes per run).</summary>
        private static float Noise(string id, int salt)
        {
            uint hash = 2166136261;
            foreach (char c in id) hash = (hash ^ c) * 16777619;
            hash = (hash ^ (uint)salt) * 16777619;
            hash ^= hash >> 13;
            hash *= 0x5bd1e995;
            hash ^= hash >> 15;
            return (hash & 0xFFFFFF) / (float)0x1000000;
        }

        /// <summary>
        /// Greedy label placement: the home and the inhabited places first, each label on the first side of its
        /// place (right, left, above, below; close, then farther) that stays on the map and covers no place and no label already set.
        /// A minor place (no power, not home, not selected) whose name finds no free side is named only once zoomed in;
        /// any other goes to the right.
        /// </summary>
        private IEnumerable<(string Name, Vector2 At)> PlaceLabels()
        {
            var font = ThemeDB.FallbackFont;
            var inside = places.Where(p => !p.IsState).ToList();
            var taken = places.Where(p => p.IsState).Select(StateLabelRect).ToList(); // the states' names are set first
            var bounds = new Rect2(Vector2.Zero, Size);
            foreach (var place in inside.OrderByDescending(p => p.IsHome).ThenByDescending(p => p.Factions.Count))
            {
                var at = At(place);
                var size = font.GetStringSize(place.Name, HorizontalAlignment.Left, -1, LabelSize) + new Vector2(2, 2);
                var sides = new[] { PlaceRadius + 3, PlaceRadius * 3 }.SelectMany(gap => new[]
                {
                    new Vector2(at.X + gap, at.Y - size.Y / 2),
                    new Vector2(at.X - gap - size.X, at.Y - size.Y / 2),
                    new Vector2(at.X - size.X / 2, at.Y - gap - size.Y),
                    new Vector2(at.X - size.X / 2, at.Y + gap)
                }).Select(corner => new Rect2(corner, size)).ToList();

                var free = sides.FirstOrDefault(r => bounds.Encloses(r) && !taken.Any(t => t.Intersects(r))
                    && !inside.Any(p => p != place && r.Grow(PlaceRadius / 2).HasPoint(At(p))));
                bool minor = !place.IsHome && place.Factions.Count == 0 && place.Id != selected;
                if (free.Size == Vector2.Zero && minor) continue; // named when zoomed in, where there is room
                var chosen = free.Size == Vector2.Zero ? sides[0] : free;
                taken.Add(chosen);
                yield return (place.Name, new Vector2(chosen.Position.X, chosen.End.Y - LabelSize / 4f - 2));
            }
        }

        /// <summary>Where a state's or sea's name sits: centred above its wash.</summary>
        private Rect2 StateLabelRect(MapPlace state)
        {
            var size = ThemeDB.FallbackFont.GetStringSize(state.Name, HorizontalAlignment.Left, -1, StateLabelSize);
            var at = At(state);
            return new Rect2(at.X - size.X / 2, at.Y - StateRadius - 6 - StateLabelSize, size.X, size.Y);
        }

        /// <summary>Text ringed with paper, so it stays legible over lines and washes.</summary>
        private void Label(string text, Vector2 at, int size, Color colour, bool centred)
        {
            var font = ThemeDB.FallbackFont;
            if (centred) at.X -= font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X / 2;
            DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, size, LabelOutline, Paper);
            DrawString(font, at, text, HorizontalAlignment.Left, -1, size, colour);
        }

        public override void _GuiInput(InputEvent input)
        {
            if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown } wheel)
            {
                ZoomAround(wheel.Position, wheel.ButtonIndex == MouseButton.WheelUp ? ZoomStep : 1f / ZoomStep);
                return;
            }
            if (input is InputEventMouseMotion motion && (motion.ButtonMask & (MouseButtonMask.Right | MouseButtonMask.Middle)) != 0)
            {
                pan += motion.Relative;
                ClampPan();
                QueueRedraw();
                return;
            }
            if (input is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) return;
            var nearest = places.OrderBy(p => At(p).DistanceTo(click.Position)).FirstOrDefault();
            if (nearest != null && At(nearest).DistanceTo(click.Position) <= (nearest.IsState ? StateRadius : PickRadius))
                PlaceSelected?.Invoke(nearest.Id);
        }

        /// <summary>The place on the canvas, inside a margin so nothing touches the frame, zoomed and panned.</summary>
        private Vector2 At(MapPlace place)
        {
            var inner = Size - Vector2.One * Margin * 2;
            var flat = new Vector2(Margin + (float)place.X * inner.X, Margin + (float)place.Y * inner.Y);
            return flat * zoom + pan;
        }

        /// <summary>Zooms keeping the point under the cursor in place (mouse wheel).</summary>
        private void ZoomAround(Vector2 cursor, float factor)
        {
            float next = Mathf.Clamp(zoom * factor, MinZoom, MaxZoom);
            pan = cursor - (cursor - pan) * (next / zoom);
            zoom = next;
            ClampPan();
            QueueRedraw();
        }

        /// <summary>Never lets the map drift off the frame.</summary>
        private void ClampPan()
        {
            var overflow = Size * (zoom - 1f);
            pan = new Vector2(Mathf.Clamp(pan.X, -overflow.X, 0), Mathf.Clamp(pan.Y, -overflow.Y, 0));
        }
    }
}

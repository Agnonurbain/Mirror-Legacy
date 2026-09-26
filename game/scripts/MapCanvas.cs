using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Data;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// Draws the world map in ink on paper (Shuimo): borders as brush lines, states and seas as washes, places as
    /// ink dots, the clan's home as a red seal. It only draws the engine-free <see cref="WorldMapView"/>.
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
        private const int LabelOutline = 4;
        private const float Margin = 48f;

        private IReadOnlyList<MapPlace> places = Array.Empty<MapPlace>();
        private IReadOnlyList<MapBorder> borders = Array.Empty<MapBorder>();
        private string selected;

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
            var byId = places.ToDictionary(p => p.Id);

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

        /// <summary>The home as a red seal; mountains as peaks, waters as blue dots, other places as ink dots.</summary>
        private void DrawGlyph(MapPlace place, Vector2 at)
        {
            if (place.IsHome)
            {
                DrawRect(new Rect2(at - Vector2.One * HomeRadius, Vector2.One * HomeRadius * 2), Seal);
                return;
            }
            float size = place.Factions.Count > 0 ? PlaceRadius : PlaceRadius * 0.6f;
            switch (place.Kind)
            {
                case RegionKind.Mountain:
                    DrawColoredPolygon(new[] { at + new Vector2(0, -size * 1.3f), at + new Vector2(size, size), at + new Vector2(-size, size) }, Ink);
                    break;
                case RegionKind.Lake:
                case RegionKind.River:
                case RegionKind.Sea:
                case RegionKind.Island:
                    DrawCircle(at, size, WaterInk);
                    break;
                default:
                    DrawCircle(at, size, Ink);
                    break;
            }
        }

        /// <summary>
        /// Greedy label placement: the home and the inhabited places first, each label on the first side of its
        /// place (right, left, above, below; close, then farther) that stays on the map and covers no place and no label already set.
        /// A label with no free side goes to the right.
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
            if (input is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click) return;
            var nearest = places.OrderBy(p => At(p).DistanceTo(click.Position)).FirstOrDefault();
            if (nearest != null && At(nearest).DistanceTo(click.Position) <= (nearest.IsState ? StateRadius : PickRadius))
                PlaceSelected?.Invoke(nearest.Id);
        }

        /// <summary>The place on the canvas, inside a margin so nothing touches the frame.</summary>
        private Vector2 At(MapPlace place)
        {
            var inner = Size - Vector2.One * Margin * 2;
            return new Vector2(Margin + (float)place.X * inner.X, Margin + (float)place.Y * inner.Y);
        }
    }
}

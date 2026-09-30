using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// Draws the clan's family tree in ink on paper: a cartouche per member (faded for the dead, ringed in red for the
    /// patriarch), a double stroke between spouses (dashed across rows when they stand in different generations), elbowed strokes from a couple down to its children. It sizes itself
    /// to the tree, inside a scroll container. It only draws the engine-free <see cref="GenealogyView"/>.
    /// </summary>
    public partial class GenealogyCanvas : Control
    {
        private const float CardWidth = 240f;
        private const float CardHeight = 46f;
        private const float ColumnStep = 256f;
        private const float RowStep = 96f;
        private const float Margin = 24f;
        private const int NameSize = 13;
        private const int StatusSize = 10;

        private static readonly Color Paper = new Color(0.93f, 0.90f, 0.82f);
        private static readonly Color Card = new Color(0.97f, 0.95f, 0.89f);
        private static readonly Color Ink = new Color(0.13f, 0.12f, 0.11f);
        private static readonly Color Faded = new Color(0.13f, 0.12f, 0.11f, 0.45f);
        private static readonly Color Brush = new Color(0.13f, 0.12f, 0.11f, 0.5f);
        private static readonly Color Seal = new Color(0.70f, 0.12f, 0.10f);

        private IReadOnlyList<KinNode> nodes = Array.Empty<KinNode>();

        public void Show(IReadOnlyList<KinNode> tree)
        {
            nodes = tree;
            int columns = nodes.Count == 0 ? 1 : nodes.GroupBy(n => n.Generation).Max(g => g.Count());
            int rows = nodes.Count == 0 ? 1 : nodes.Max(n => n.Generation) + 1;
            CustomMinimumSize = new Vector2(Margin * 2 + (columns - 1) * ColumnStep + CardWidth, Margin * 2 + (rows - 1) * RowStep + CardHeight);
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawRect(new Rect2(Vector2.Zero, Size), Paper);
            var byId = nodes.ToDictionary(n => n.Id);

            foreach (var node in nodes.Where(n => n.SpouseId != null && byId.ContainsKey(n.SpouseId)))
            {
                var spouse = byId[node.SpouseId];
                if (string.CompareOrdinal(node.Id, spouse.Id) > 0) continue; // once per couple
                if (spouse.Generation != node.Generation) // spouses of different rows (branches of unequal depth): a dashed link
                {
                    var (upper, lower) = node.Generation < spouse.Generation ? (node, spouse) : (spouse, node);
                    DrawDashedLine(TopLeft(upper) + new Vector2(CardWidth / 2, CardHeight), TopLeft(lower) + new Vector2(CardWidth / 2, 0), Brush, 1.5f, 6f);
                    continue;
                }
                var (left, right) = node.Column < spouse.Column ? (node, spouse) : (spouse, node);
                float y = TopLeft(left).Y + CardHeight / 2;
                DrawLine(new Vector2(TopLeft(left).X + CardWidth, y - 2), new Vector2(TopLeft(right).X, y - 2), Brush, 1.5f, true);
                DrawLine(new Vector2(TopLeft(left).X + CardWidth, y + 2), new Vector2(TopLeft(right).X, y + 2), Brush, 1.5f, true);
            }

            foreach (var child in nodes)
            {
                var parents = new[] { child.FatherId, child.MotherId }.Where(id => id != null && byId.ContainsKey(id)).Select(id => byId[id]).ToList();
                if (parents.Count == 0) continue;
                float fromX = parents.Average(p => TopLeft(p).X + CardWidth / 2);
                float fromY = parents.Max(p => TopLeft(p).Y) + CardHeight / 2 + (parents.Count == 2 ? 0 : CardHeight / 2);
                var to = TopLeft(child) + new Vector2(CardWidth / 2, 0);
                float elbow = to.Y - RowStep * 0.3f;
                DrawPolyline(new[] { new Vector2(fromX, fromY), new Vector2(fromX, elbow), new Vector2(to.X, elbow), to }, Brush, 1.2f, true);
            }

            foreach (var node in nodes) DrawCard(node);
        }

        /// <summary>The portrait of a member of the tree, by id (set by the screen); null draws none.</summary>
        public System.Func<string, Portrait> Portraits { get; set; }

        private void DrawCard(KinNode node)
        {
            var rect = new Rect2(TopLeft(node), new Vector2(CardWidth, CardHeight));
            var ink = node.Alive && !node.Departed ? Ink : Faded; // the dead and the departed, faded
            DrawRect(rect, Card);
            DrawRect(rect, node.IsPatriarch ? Seal : ink, false, node.IsPatriarch ? 2.5f : 1f);
            var font = ThemeDB.FallbackFont;
            float text = 8;
            if (Portraits != null && Portraits(node.Id) is { } portrait)
            {
                InkPortrait.Draw(this, new Rect2(rect.Position + new Vector2(4, 3), new Vector2(34, CardHeight - 6)), portrait);
                text = 44;
            }
            DrawString(font, rect.Position + new Vector2(text, 18), $"{(node.IsMale ? "♂" : "♀")} {node.Name}", HorizontalAlignment.Left, CardWidth - text - 8, NameSize, ink);
            DrawString(font, rect.Position + new Vector2(text, 36), node.Status, HorizontalAlignment.Left, CardWidth - text - 8, StatusSize, ink);
        }

        private static Vector2 TopLeft(KinNode node) => new Vector2(Margin + node.Column * ColumnStep, Margin + node.Generation * RowStep);
    }
}

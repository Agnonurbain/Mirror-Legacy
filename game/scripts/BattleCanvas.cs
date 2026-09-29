using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MirrorChronicles.Combat;
using MirrorChronicles.Presentation;

namespace MirrorChronicles.Game
{
    /// <summary>
    /// Paints the battle grid in ink on paper: a wash per terrain, the clan's fighters as ink seals, its foes as red seals,
    /// the fighter whose turn it is ringed, and the cells the chosen action may target. A click on a cell raises
    /// <see cref="CellClicked"/>. It only draws the engine-free <see cref="BattleView"/>.
    /// </summary>
    public partial class BattleCanvas : Control
    {
        private const float CellSize = 52f;
        private const float UnitRadius = 18f;

        private static readonly Color Paper = new Color(0.93f, 0.90f, 0.82f);
        private static readonly Color Ink = new Color(0.13f, 0.12f, 0.11f);
        private static readonly Color Line = new Color(0.13f, 0.12f, 0.11f, 0.25f);
        private static readonly Color Seal = new Color(0.70f, 0.12f, 0.10f);
        private static readonly Color Target = new Color(0.70f, 0.12f, 0.10f, 0.22f);

        private IReadOnlyList<BattleCell> cells = Array.Empty<BattleCell>();
        private IReadOnlyList<BattleUnit> units = Array.Empty<BattleUnit>();
        private HashSet<(int X, int Y)> targets = new HashSet<(int X, int Y)>();

        /// <summary>Raised with the coordinates of the cell the player clicks.</summary>
        public event Action<int, int> CellClicked;

        public void Show(IReadOnlyList<BattleCell> newCells, IReadOnlyList<BattleUnit> newUnits, IEnumerable<(int X, int Y)> newTargets)
        {
            cells = newCells;
            units = newUnits;
            targets = new HashSet<(int X, int Y)>(newTargets);
            int width = cells.Count == 0 ? 0 : cells.Max(c => c.X) + 1, height = cells.Count == 0 ? 0 : cells.Max(c => c.Y) + 1;
            CustomMinimumSize = new Vector2(width * CellSize, height * CellSize);
            QueueRedraw();
        }

        public override void _Draw()
        {
            var font = ThemeDB.FallbackFont;
            foreach (var cell in cells)
            {
                var rect = new Rect2(cell.X * CellSize, cell.Y * CellSize, CellSize, CellSize);
                DrawRect(rect, Paper);
                DrawRect(rect, TerrainWash(cell.Terrain));
                if (targets.Contains((cell.X, cell.Y))) DrawRect(rect, Target);
                DrawRect(rect, Line, filled: false);
                if (cell.UnitId == null) continue;

                var unit = units.FirstOrDefault(u => u.Id == cell.UnitId);
                var centre = rect.GetCenter();
                DrawCircle(centre, UnitRadius, cell.Ally ? Ink : Seal);
                if (unit?.IsCurrent == true) DrawArc(centre, UnitRadius + 5, 0, Mathf.Tau, 32, Seal, 2.5f);
                string initial = unit == null || string.IsNullOrEmpty(unit.Name) ? "?" : unit.Name.Substring(0, 1);
                DrawString(font, centre + new Vector2(-6, 6), initial, HorizontalAlignment.Center, -1, 16, Paper);
            }
        }

        public override void _GuiInput(InputEvent @event)
        {
            if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } click)
                CellClicked?.Invoke((int)(click.Position.X / CellSize), (int)(click.Position.Y / CellSize));
        }

        private static Color TerrainWash(TerrainType terrain) => terrain switch
        {
            TerrainType.Forest => new Color(0.25f, 0.35f, 0.22f, 0.25f),
            TerrainType.Mountain => new Color(0.20f, 0.22f, 0.20f, 0.30f),
            TerrainType.Water => new Color(0.25f, 0.36f, 0.42f, 0.30f),
            TerrainType.ConcentratedQi => new Color(0.55f, 0.45f, 0.15f, 0.25f),
            _ => new Color(0, 0, 0, 0)
        };
    }
}

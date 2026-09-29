using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Combat;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>A cell of the grid: its terrain, and the fighter standing there (null when empty).</summary>
    public sealed record BattleCell(int X, int Y, TerrainType Terrain, string UnitId, bool Ally);

    /// <summary>A fighter: vitality and Qi, whether its turn it is, and whether it fell or fled (null while it stands).</summary>
    public sealed record BattleUnit(string Id, string Name, string Rank, bool IsAlly, int Vitality, int MaxVitality, int Qi, int MaxQi,
        bool IsCurrent, string Status);

    /// <summary>An action open to the fighter whose turn it is, and the cells it may target (its own cell for a guard or a flight).</summary>
    public sealed record BattleAction(string Id, string Label, int QiCost, IReadOnlyList<(int X, int Y)> Targets);

    public sealed record ChallengeCandidate(string Id, string Name, string Rank, string Refusal);

    /// <summary>A rival's challenge awaiting its answer: who sends it, the rivals' rank and number, who may answer.</summary>
    public sealed record PendingChallenge(string Faction, string Rank, int Rivals, int MaxFighters, IReadOnlyList<ChallengeCandidate> Candidates);

    /// <summary>The battle screen (G6): the grid, the fighters, the actions of the clan's fighter whose turn it is, the outcome.</summary>
    public static class BattleView
    {
        public const string Move = "move";
        public const string Attack = "attack";
        public const string Defend = "defend";
        public const string Flee = "flee";
        private const string TechniquePrefix = "tech:";

        public static IReadOnlyList<BattleCell> Cells(Battle battle) =>
            battle.Field.Grid.Cells
                .Select(c => new BattleCell(c.X, c.Y, c.Terrain, c.Occupant?.BaseData.ID, c.Occupant?.IsAlly ?? false))
                .ToList();

        public static IReadOnlyList<BattleUnit> Units(Battle battle) =>
            battle.Allies.Concat(battle.Enemies)
                .Select(u => new BattleUnit(u.BaseData.ID, u.BaseData.FullName, RankCatalog.RealmName(u.BaseData.Realm), u.IsAlly,
                    u.CurrentVitality, u.MaxVitality, u.CurrentQi, u.MaxQi, u == battle.CurrentUnit && !battle.IsOver,
                    u.HasFled ? "en fuite" : u.IsDown ? "à terre" : null))
                .ToList();

        /// <summary>The actions of the clan's fighter whose turn it is; none on the enemies' turn or once it is over.</summary>
        public static IReadOnlyList<BattleAction> Actions(Battle battle, Func<string, TechniqueData> findTechnique)
        {
            if (battle.State != CombatState.PlayerTurn) return Array.Empty<BattleAction>();
            var unit = battle.CurrentUnit;
            var actions = new List<BattleAction>
            {
                Line(battle, Move, "Se déplacer", new MoveAction()),
                Line(battle, Attack, "Frapper", new AttackAction()),
                Line(battle, Defend, "Se garder", new DefendAction()),
                Line(battle, Flee, "Fuir", new FleeAction()),
            };
            foreach (var technique in unit.BaseData.KnownTechniqueIDs.Select(findTechnique).Where(t => t != null))
                actions.Add(Line(battle, TechniquePrefix + technique.ID, technique.Name, new TechniqueAction(technique)));
            return actions;
        }

        /// <summary>The fighter whose turn it is performs the action on the cell; false when it cannot.</summary>
        public static bool Perform(Battle battle, string actionId, int x, int y, Func<string, TechniqueData> findTechnique)
        {
            if (battle.State != CombatState.PlayerTurn) return false;
            var action = ActionFor(actionId, findTechnique);
            var cell = SelfTargeted(actionId) ? battle.CurrentUnit.CurrentCell : battle.Field.Grid.GetCellAt(x, y);
            return action != null && cell != null && battle.Perform(action, cell);
        }

        public static string Outcome(Battle battle) => battle.State switch
        {
            CombatState.Victory => "Victoire",
            CombatState.Defeat => "Défaite",
            CombatState.Resolution => "Les deux camps se retirent",
            _ => null
        };

        /// <summary>The challenge awaiting the clan's answer and every living member with why they may not fight; null when none.</summary>
        public static PendingChallenge Pending(GameSession session)
        {
            var challenge = session.Challenges.Pending;
            if (challenge == null) return null;
            var candidates = session.Clan.LivingMembers
                .OrderByDescending(m => session.Challenges.FighterRefusal(m) == null).ThenByDescending(m => (int)m.Realm)
                .ThenByDescending(m => m.RealmStage).ThenBy(m => m.FullName, StringComparer.Ordinal)
                .Select(m => new ChallengeCandidate(m.ID, m.FullName, RankCatalog.DisplayName(m), session.Challenges.FighterRefusal(m)))
                .ToList();
            return new PendingChallenge(challenge.Faction, RankCatalog.RealmName(challenge.Realm), challenge.Rivals,
                session.Context.Content.Balance.Challenges.MaxFighters, candidates);
        }

        private static BattleAction Line(Battle battle, string id, string label, ICombatAction action)
        {
            var unit = battle.CurrentUnit;
            var targets = id == Move
                ? MoveAction.Targets(unit, battle.Field).Select(c => (c.X, c.Y)).ToArray()
                : SelfTargeted(id)
                ? (action.IsValid(unit, unit.CurrentCell, battle.Field) ? new[] { (unit.CurrentCell.X, unit.CurrentCell.Y) } : Array.Empty<(int, int)>())
                : battle.Field.Grid.Cells.Where(c => action.IsValid(unit, c, battle.Field)).Select(c => (c.X, c.Y)).ToArray();
            return new BattleAction(id, label, action.QiCost, targets);
        }

        private static bool SelfTargeted(string id) => id == Defend || id == Flee;

        private static ICombatAction ActionFor(string id, Func<string, TechniqueData> findTechnique) => id switch
        {
            Move => new MoveAction(),
            Attack => new AttackAction(),
            Defend => new DefendAction(),
            Flee => new FleeAction(),
            _ when id != null && id.StartsWith(TechniquePrefix, StringComparison.Ordinal)
                && findTechnique(id.Substring(TechniquePrefix.Length)) is { } technique => new TechniqueAction(technique),
            _ => null
        };
    }
}

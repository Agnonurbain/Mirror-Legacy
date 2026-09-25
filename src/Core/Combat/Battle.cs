using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// A tactical battle: the clan's fighters deploy on the west edge, the enemies on the east edge,
    /// and turns follow initiative. Enemy turns are played by their AI until an ally must act
    /// (<see cref="CombatState.PlayerTurn"/>). A side is beaten when none of its fighters stands on the
    /// field (fallen or fled). After <see cref="MaxRounds"/> rounds without a victor, both sides withdraw
    /// (<see cref="CombatState.Resolution"/>).
    /// </summary>
    public sealed class Battle
    {
        public const int DefaultSize = 10;
        public const int MaxRounds = 100;

        private readonly List<CombatUnit> allies;
        private readonly List<CombatUnit> enemies;

        public BattleField Field { get; }
        public TurnOrder Turns { get; }
        public CombatState State { get; private set; } = CombatState.Initialization;
        public IReadOnlyList<CombatUnit> Allies => allies;
        public IReadOnlyList<CombatUnit> Enemies => enemies;
        public CombatUnit CurrentUnit => Turns.Current;
        public bool IsOver => State == CombatState.Victory || State == CombatState.Defeat || State == CombatState.Resolution;

        private Battle(BattleField field, List<CombatUnit> allies, List<CombatUnit> enemies)
        {
            Field = field;
            this.allies = allies;
            this.enemies = enemies;
            Turns = new TurnOrder(allies.Concat(enemies));
        }

        /// <param name="grid">The battlefield; null grows one from the random source.</param>
        /// <param name="findTechnique">Looks up the techniques the fighters know (e.g. the clan's deduced ones).</param>
        public static Battle Start(IReadOnlyList<CharacterData> allyData, IReadOnlyList<CharacterData> enemyData, Random rng,
            IGameLog log, CombatGrid grid = null, AIStrategyType enemyStrategy = AIStrategyType.Aggressive,
            Func<string, TechniqueData> findTechnique = null)
        {
            var field = new BattleField(grid ?? CombatGrid.Generate(DefaultSize, DefaultSize, rng), rng, log, findTechnique);
            var allies = allyData.Select(d => new CombatUnit(d, isAlly: true)).ToList();
            var enemies = enemyData.Select(d => new CombatUnit(d, isAlly: false, enemyStrategy)).ToList();

            Deploy(field, allies, fromWest: true);
            Deploy(field, enemies, fromWest: false);

            var battle = new Battle(field, allies, enemies) { State = CombatState.Deployment };
            log.Info($"[Combat] Battle begins: {allies.Count} of the clan against {enemies.Count}.");
            battle.Advance();
            return battle;
        }

        /// <summary>The current ally performs an action. Returns false when it is not the player's turn or the action is invalid.</summary>
        public bool Perform(ICombatAction action, GridCell target)
        {
            if (State != CombatState.PlayerTurn || !action.IsValid(CurrentUnit, target, Field)) return false;
            action.Execute(CurrentUnit, target, Field);
            if (!CheckEnd() && !CurrentUnit.IsActive) Advance(); // an ally who fled or fell ends its own turn
            return true;
        }

        /// <summary>The current ally ends its turn; enemies act until an ally must act again.</summary>
        public void EndTurn()
        {
            if (State == CombatState.PlayerTurn) Advance();
        }

        /// <summary>Lets the allies' own strategies fight too, until the battle ends.</summary>
        public void AutoPlay()
        {
            while (State == CombatState.PlayerTurn)
            {
                CombatAI.PlayTurn(CurrentUnit, Field);
                if (!CheckEnd()) Advance();
            }
        }

        /// <summary>The fallen of the clan die in combat; the survivors carry their wounds home.</summary>
        public void ResolveAftermath(ClanManager clan, WoundSystem wounds)
        {
            foreach (var ally in allies)
            {
                if (ally.IsDown) clan.Kill(ally.BaseData, DeathCause.Combat);
                else wounds.EvaluatePostCombatWounds(ally.BaseData, ally.MaxVitality, ally.CurrentVitality);
            }
        }

        private void Advance()
        {
            while (!CheckEnd())
            {
                var next = Turns.Next();
                if (next == null) continue; // nobody stands: CheckEnd settles it
                if (Turns.Round > MaxRounds)
                {
                    State = CombatState.Resolution;
                    Field.Log.Info("[Combat] Neither side prevails; both withdraw.");
                    return;
                }
                if (next.IsAlly)
                {
                    State = CombatState.PlayerTurn;
                    return;
                }

                State = CombatState.EnemyTurn;
                CombatAI.PlayTurn(next, Field);
            }
        }

        /// <summary>Returns true when the battle is over (and sets its outcome).</summary>
        private bool CheckEnd()
        {
            if (IsOver) return true;
            if (!Field.SideStands(allies: true)) State = CombatState.Defeat;
            else if (!Field.SideStands(allies: false)) State = CombatState.Victory;
            else return false;

            Field.Log.Info($"[Combat] {(State == CombatState.Victory ? "Victory" : "Defeat")}.");
            return true;
        }

        /// <summary>Fills the edge column from the top, every other row first, then moves inward.</summary>
        private static void Deploy(BattleField field, IReadOnlyList<CombatUnit> units, bool fromWest)
        {
            var grid = field.Grid;
            var slots = Enumerable.Range(0, grid.Width)
                .Select(i => fromWest ? i : grid.Width - 1 - i)
                .SelectMany(x => Enumerable.Range(0, grid.Height).Where(y => y % 2 == 0)
                    .Concat(Enumerable.Range(0, grid.Height).Where(y => y % 2 == 1))
                    .Select(y => grid.GetCellAt(x, y)));

            var free = slots.Where(c => !c.IsOccupied).Take(units.Count).ToList();
            if (free.Count < units.Count) throw new InvalidOperationException("The battlefield is too small for every fighter.");
            for (int i = 0; i < units.Count; i++)
                field.Place(units[i], free[i].X, free[i].Y);
        }
    }
}

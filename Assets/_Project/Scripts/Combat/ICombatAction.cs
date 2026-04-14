namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Command Pattern interface for all tactical actions (Move, Attack, Technique, Defend).
    /// </summary>
    public interface ICombatAction
    {
        /// <summary>
        /// The type of action being performed.
        /// </summary>
        ActionType Type { get; }

        /// <summary>
        /// How much Qi this action costs to execute.
        /// </summary>
        int GetQiCost();

        /// <summary>
        /// Checks if the action is valid (e.g., target is in range, enough Qi).
        /// </summary>
        bool IsValid(CombatUnit user, GridCell targetCell);

        /// <summary>
        /// Executes the action.
        /// </summary>
        void Execute(CombatUnit user, GridCell targetCell);
    }
}

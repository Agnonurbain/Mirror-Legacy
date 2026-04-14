namespace MirrorChronicles.Combat
{
    /// <summary>
    /// Enumerations specific to the Tactical Combat System.
    /// </summary>
    
    public enum CombatState
    {
        Initialization,
        Deployment,
        PlayerTurn,
        EnemyTurn,
        Resolution,
        Victory,
        Defeat
    }

    public enum TerrainType
    {
        Plain,          // No modifiers
        Forest,         // +20% dodge, -1 range for ranged attacks
        Mountain,       // +15% defense, -1 movement
        Water,          // -30% speed, +20% Water Element bonus
        ConcentratedQi  // +5% Qi regen per turn (exception to no-regen rule)
    }

    public enum AIStrategyType
    {
        Aggressive,     // Targets weakest, uses strong techniques first
        Defensive,      // Stays behind allies, uses Defend frequently
        Strategic,      // Targets healers/supports, uses terrain
        Berserker,      // Attacks nearest, ignores defense, spends all Qi
        Cautious        // Flees if Vitality < 40%, saves Qi
    }

    public enum ActionType
    {
        Move,
        PhysicalAttack,
        Technique,
        Defend,
        UseItem,
        Flee
    }
}

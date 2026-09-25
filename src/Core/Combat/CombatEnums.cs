namespace MirrorChronicles.Combat
{
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
        Plain,          // no modifier
        Forest,         // cover
        Mountain,       // costs two movement points
        Water,          // costs two movement points; Water techniques strike 20% harder there
        ConcentratedQi  // +5% Qi at the start of each turn (the only regeneration in combat)
    }

    public enum AIStrategyType
    {
        Aggressive,     // closes in on the nearest foe and strikes
        Defensive,      // holds its ground and defends
        Strategic,      // hunts the weakest foe from good terrain
        Berserker,      // charges, spending Qi on its strongest technique
        Cautious        // flees when badly hurt, strikes only when safe
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

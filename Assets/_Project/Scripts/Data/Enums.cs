namespace MirrorChronicles.Data
{
    /// <summary>
    /// Core enumerations used across the game systems.
    /// </summary>
    
    public enum GameState 
    { 
        MainMenu, 
        Loading, 
        Playing, 
        GameOver 
    }

    public enum GamePhase 
    { 
        Management, 
        Events, 
        Breakthrough, 
        Inheritance 
    }

    public enum CultivationRealm 
    { 
        Embryonic, 
        QiRefinement, 
        Foundation, 
        PurpleMansion, 
        GoldenCore, 
        DaoEmbryo 
    }

    public enum TaskType 
    { 
        None, 
        Cultivation, 
        Mine, 
        Patrol, 
        Study, 
        Teaching, 
        Diplomacy, 
        Espionage, 
        Rest 
    }

    public enum Element 
    { 
        None, 
        Fire, 
        Water, 
        Wood, 
        Metal, 
        Earth, 
        Lightning, 
        Darkness, 
        Light 
    }

    public enum DeathCause 
    { 
        None, 
        OldAge, 
        Combat, 
        QiDeviation, 
        Assassination, 
        Illness 
    }
}

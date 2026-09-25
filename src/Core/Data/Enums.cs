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
        DaoEmbryo,
        GoldenImmortal // appended last for save compatibility; never rename members (StringEnumConverter saves names)
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
        Rest,
        GatherQi // harvest spiritual Qi in wisps (LORE.md §2.5); appended last, like every new member
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
        Illness,
        SpiritualDissolution // failed Foundation breakthrough (LORE.md §5.3.1)
    }

    /// <summary>
    /// The temper of a cultivator's heart (decision of 2026-09-25, LORE.md §5.3.2): at the Foundation the
    /// Dao Heart aligns with the lineage's own, faster for a heart already alike. None: not yet known.
    /// </summary>
    public enum Temperament
    {
        None,
        Dominant, // a domineering leader: the Bright Yang
        Solitary,
        Patient,
        Fiery,
        Cunning,
        Serene
    }
}

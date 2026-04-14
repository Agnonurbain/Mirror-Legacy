using System;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// POCO (Plain Old C# Object) representing a character's data. 
    /// Designed to be strictly data-driven and easily serialized to JSON.
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        public string ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool IsMale { get; set; }
        public int Age { get; set; }
        public int MaxLifespan { get; set; }
        public bool IsAlive { get; set; }
        public DeathCause CauseOfDeath { get; set; }

        // Genetics & Cultivation
        public int SpiritualRoot { get; set; }
        public Element Affinity { get; set; }
        public CultivationRealm Realm { get; set; }
        public int CultivationXP { get; set; }

        // Stats
        public int MentalStability { get; set; }
        public TaskType CurrentTask { get; set; }

        // Family Links (Stored as IDs for easy serialization without circular references)
        public string FatherID { get; set; }
        public string MotherID { get; set; }
        public string SpouseID { get; set; }

        public CharacterData()
        {
            ID = Guid.NewGuid().ToString();
            IsAlive = true;
            MentalStability = 70; // Default starting stability
            Realm = CultivationRealm.Embryonic;
            CurrentTask = TaskType.None;
            CauseOfDeath = DeathCause.None;
        }

        public string FullName => $"{LastName} {FirstName}";
    }
}

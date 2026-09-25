using System;
using System.Collections.Generic;

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
        public CultivationRealm Realm { get; set; } // shared power tier for every path
        public int RealmStage { get; set; }         // chakras 0-6, Qi levels 1-9, stages 1-4 (LORE.md §5)
        public int CultivationXP { get; set; }      // progress toward the next stage

        // Spiritual orifice (LORE.md §4): without it or a Talisman Seed from the mirror, one stays mortal
        public bool HasSpiritualOrifice { get; set; }
        public bool OrificeKnown { get; set; }      // only a confirmed cultivator (or the mirror) can detect it
        public bool HasTalismanSeed { get; set; }   // Graine de Sceau granted by the mirror

        // Permanent Dao wounds: each takes a fifth of the lifespan, even across breakthroughs
        public int DaoWounds { get; set; }

        // Cultivation path and Golden Core standing (LORE.md §3, §5.9)
        public CultivationPath Path { get; set; }
        public CultivationSubPath SubPath { get; set; }
        public Species Species { get; set; }
        public GoldenCoreState GoldenCore { get; set; }
        public string FruitionId { get; set; }          // Dao lineage held or pursued
        public string PatronId { get; set; }            // superior a False Left Hand / borrower depends on
        public string ReincarnationOfId { get; set; }   // reincarnated True Monarch (R9)
        public string FragmentOfId { get; set; }        // fragment of a split Golden Core (R21)

        // Stats
        public int MentalStability { get; set; }
        public TaskType CurrentTask { get; set; }

        // Known Techniques (IDs referencing TechniqueData)
        public List<string> KnownTechniqueIDs { get; set; }

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
            Path = CultivationPath.Immortal;
            SubPath = CultivationSubPath.PurpleMansionGoldenCore;
            Species = Species.Human;
            GoldenCore = GoldenCoreState.None;
            CurrentTask = TaskType.None;
            CauseOfDeath = DeathCause.None;
            KnownTechniqueIDs = new List<string>();
        }

        public string FullName => $"{LastName} {FirstName}";
    }
}

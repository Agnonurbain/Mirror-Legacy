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
        public bool BorrowedLight { get; set; }         // a « Merciful » Purple Mansion on a Fruition's lent light (§5.4.2)
        public bool KnowsMirrorSecret { get; set; }
        public string CaptorFaction { get; set; }       // the power holding them captive; null when free (L6a)
        public int? CapturedYear { get; set; }
        public string FromFaction { get; set; }         // the power that sent them in an arranged marriage (known)
        public string SpyFor { get; set; }              // the power they secretly spy for; null for none (hidden, 2026-09-27)
        public bool SpyUnmasked { get; set; }           // the mirror sounded them
        public bool DoubleAgent { get; set; }           // turned: they feed their power false reports
        public string DiscipleOf { get; set; }          // serving a power as a disciple (LORE.md §11.10, C2): away, no task
        public int DiscipleUntil { get; set; }          // the year the disciple comes home
        public bool Departed { get; set; }              // gone home (a spouse repudiated): alive, but no longer of the clan
        public int? LastOperationYear { get; set; }     // the year of their last hunt or diversion: one operation a year (L2c.5)     // took part in a hunt or bears a talisman: may leak it (L2c.4b)
        public bool TransformedLineage { get; set; }    // a Realization holder's descendant: reaches at least the Purple Mansion (§5.5.2)
        public string ReincarnationOfId { get; set; }   // reincarnated True Monarch (R9)
        public string RebornFrom { get; set; }          // the clan's True Monarch reborn in this child (R9, 2026-10-01)
        public AncestorEssence RebornEssence { get; set; } // what it regains as it grows; null once it has it all
        public ArtifactInstance Artifact { get; set; }   // the artifact it bears (L4f, 2026-10-03); null: none
        public int? TreasureReadyYear { get; set; }     // the year its Dharma Treasure is condensed (L4e, 2026-10-03)
        public bool HasDharmaTreasure { get; set; }     // a True Monarch's treasure of its foundation, lost with it
        public bool ImperialCore { get; set; }          // its Golden Core forged by governing the clan's kingdom (R20, 2026-10-03)
        public string FragmentOfId { get; set; }        // fragment of a split Golden Core (R21)

        // Stats
        public int MentalStability { get; set; }
        public TaskType CurrentTask { get; set; }

        // Known Techniques (IDs referencing TechniqueData)
        public List<string> KnownTechniqueIDs { get; set; }

        // Techniques (LORE.md §2, §5.2): the method practised, and the spiritual Qi absorbed on entering
        // Qi Cultivation, which binds the cultivator to the methods of that Qi
        public string CultivationMethodId { get; set; }
        public string QiId { get; set; }

        // Immortal foundation (LORE.md §5.3): « fruition-id:ability-id » formed at the Foundation; consuming a
        // Dao Partner seals all further progression; the heart's temper aligns with the lineage (§5.3.2)
        public string FoundationId { get; set; }
        public bool ProgressionSealed { get; set; }
        public Temperament Temperament { get; set; }
        public string BodyTrait { get; set; } // the body the foundation made inhuman, or a parent's (§5.3.2, §11.6)

        // The Purple Mansion (LORE.md §5.4): divine abilities (« fruition-id:ability-id », the foundation first),
        // and the retreat of its breakthrough, which may hold a cultivator in the Great Void for life
        public List<string> DivineAbilities { get; set; }
        public string DiplomacyTarget { get; set; }          // the power a diplomat is sent to (2026-10-01); null: wherever
        public string HarvestQiId { get; set; }              // the Qi a harvester is sent for (2026-10-01); null: their own method's
        public string PursuedAbility { get; set; }           // the next ability they are condensing
        public List<string> ShallowAbilities { get; set; }   // condensed with resources: shallow foundations (§5.4.3)
        public List<string> GraftedAbilities { get; set; }   // condensed from a consumed foundation (Dao Graft)
        public Retreat Retreat { get; set; }
        public int RetreatYearsLeft { get; set; }
        public bool ImprisonedInVoid { get; set; }

        // The talisman Qi the mirror refined for this member (LORE.md §11.5), a talismans.json id
        public string TalismanQiId { get; set; }

        // Oaths of the Dao (L4d): years a Heart Demon still haunts an oath-breaker
        public int HeartDemonYearsLeft { get; set; }

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
            DivineAbilities = new List<string>();
            ShallowAbilities = new List<string>();
            GraftedAbilities = new List<string>();
        }

        public string FullName => $"{LastName} {FirstName}";

        /// <summary>An independent copy (saves and loads never share characters with a live game).</summary>
        public CharacterData Clone()
        {
            var copy = (CharacterData)MemberwiseClone();
            copy.KnownTechniqueIDs = new List<string>(KnownTechniqueIDs ?? new List<string>());
            copy.DivineAbilities = new List<string>(DivineAbilities ?? new List<string>());
            copy.ShallowAbilities = new List<string>(ShallowAbilities ?? new List<string>());
            copy.GraftedAbilities = new List<string>(GraftedAbilities ?? new List<string>());
            return copy;
        }
    }
}

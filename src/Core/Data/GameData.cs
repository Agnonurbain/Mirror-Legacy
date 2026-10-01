using System;
using System.Collections.Generic;

namespace MirrorChronicles.Data
{
    /// <summary>
    /// A saved game (JSON, enums by name). Version 2 holds the whole state; version 1 saves only had
    /// the clan, the year, the phase and the stones, and load with defaults for everything else.
    /// Version 2.1 adds the techniques of LORE.md §2 (knowledge, Qi); older saves receive the clan's
    /// starting knowledge on load. Version 2.2 adds the state of the Dao lineages (§6.8); 2.3 the
    /// clan's knowledge (its known techniques now live there); 2.4 the oaths; 2.5 the Golden Core's
    /// permissions; 2.6 the talisman Qi (prayers, an offer awaiting a choice); 2.7 the captured beasts; 2.8 the world's beasts; 2.9 the powers' hidden suspicion and distrust; 2.10 the agents the clan holds (its captives ride on the members); 2.11 the clan's treaties; 2.12 the powers' bonds, a coalition, a call to arms; 2.13 blackmail (spies ride on the members); 2.14 the secrets, the clues gathered, the targets' vigilance; 2.15 the clan's own distrust; 2.16 the pacts with great partners; 2.17 open wars; 2.18 a rival's challenge awaiting its answer; 2.19 a poor year; 2.20 what the hunters know of the clan's ripe Daos; 2.21 the clan's Annals (B3: the old victory and the ascended ancestors are read no more). Field names never
    /// change: older saves must keep loading.
    /// </summary>
    [Serializable]
    public class GameData
    {
        public const string CurrentVersion = "2.21";

        public string SaveVersion { get; set; } = CurrentVersion;
        public int Seed { get; set; }

        // Time and clan
        public string ClanName { get; set; }
        public int CurrentYear { get; set; } = 1;
        public GamePhase CurrentPhase { get; set; }
        public string PatriarchID { get; set; }
        public List<CharacterData> HistoricalRecords { get; set; } = new List<CharacterData>(); // living and dead

        // Treasury (defaults match a new game, for version 1 saves)
        public int SpiritStones { get; set; } = 1000;
        public int MedicinalHerbs { get; set; } = 50;
        public int SpiritualOres { get; set; } = 30;
        public int Prestige { get; set; } = 10;
        public int TechniqueFragments { get; set; }

        // Mirror
        public int MirrorPower { get; set; } = 50;
        public int RestoredFragments { get; set; }
        public List<string> RecoveredShards { get; set; }   // 2.21: the shards recovered (shards.json ids)
        public List<string> RevealedRuins { get; set; }     // 2.21: the ruins the clan knows to hold a shard
        public int MirrorAsleepUntil { get; set; }          // 2.21: the year the spirit wakes from integrating a shard
        public List<FragmentData> Fragments { get; set; }
        public List<TechniqueData> Techniques { get; set; } // deduced by the mirror

        // Techniques and Qi (2.1; null in older saves)
        public List<string> KnownTechniqueIds { get; set; }                 // read from 2.1-2.2 saves only (2.3: in Knowledge)
        public Dictionary<string, int> SpiritualQi { get; set; }            // portions in store, by Qi
        public Dictionary<string, int> QiHarvestProgress { get; set; }      // years of work towards the next portion

        // The Dao lineages of this world (L4; null in older saves, drawn again from the seed)
        public Dictionary<string, FruitionState> FruitionStates { get; set; }

        // What the clan knows (2.3; null in older saves, rebuilt from their techniques and foundations)
        public List<string> Knowledge { get; set; }

        // Oaths of the Dao (2.4; null in older saves)
        public List<PactData> Pacts { get; set; }
        public List<string> VeiledOathBreakers { get; set; } // the mirror's veil bought for their next breach

        // The Golden Core (2.5; null in older saves): lineage → the holder who granted leave to rise there
        public Dictionary<string, string> GoldenCorePermissions { get; set; }

        // The talisman Qi (2.6; zero and null in older saves): prayers gathered, an offer awaiting the player's choice
        public int Prayers { get; set; }
        public TalismanOffer TalismanOffer { get; set; }
        public List<CapturedBeast> CapturedBeasts { get; set; } // 2.7; null in older saves
        public string HuntingGround { get; set; }               // 2.7; null in older saves: the clan's home
        public int? NextRitualYear { get; set; }                // 2.7; null in older saves: the next multiple of the cycle
        public List<WorldBeast> WorldBeasts { get; set; }       // 2.8; null in older saves: drawn again from the seed
        public Dictionary<string, int> SuspicionOfClan { get; set; } // 2.9, hidden: each power's suspicion of the clan
        public Dictionary<string, int> Distrust { get; set; }        // 2.9, hidden: « holder→toward »
        public Confrontation Confrontation { get; set; }             // 2.9: a power that pierced the secret, awaiting its move
        public Dictionary<string, int> MirrorClues { get; set; }     // 2.9, hidden: what each power pieced together about the mirror
        public Dictionary<string, int> Evidence { get; set; }        // 2.9, hidden: the proof each power holds against the clan
        public List<Prisoner> Prisoners { get; set; }                // 2.10; null in older saves: none held
        public List<Treaty> Treaties { get; set; }                   // 2.11; null in older saves: none concluded
        public List<PowerBond> PowerBonds { get; set; }              // 2.12; null in older saves: none
        public Coalition Coalition { get; set; }                     // 2.12: powers banded against the clan
        public List<CallToArms> CallsToArms { get; set; }            // 2.12: the allies' calls awaiting an answer
        public List<Demand> Demands { get; set; }                    // 2.13: blackmail awaiting an answer
        public Dictionary<string, int> QuietUntil { get; set; }      // 2.13: a paid blackmailer's silence, until that year
        public List<Secret> SecretsHeld { get; set; }                // 2.14; null in older saves: the powers' drawn from the seed
        public Dictionary<string, int> SecretProgress { get; set; }  // 2.14, hidden: « watcher→secret » clues
        public Dictionary<string, int> Alertness { get; set; }       // 2.14, hidden: each target's vigilance
        public Dictionary<string, int> ClanDistrust { get; set; }    // 2.15: the clan's own distrust of each power
        public List<string> SpentSecrets { get; set; }               // 2.15: a blackmail paid, a secret exposed
        public List<PatronPact> PatronPacts { get; set; }            // 2.16: pacts with a great beast or a lone figure
        public List<War> Wars { get; set; }                          // 2.17: wars between powers
        public List<ClanWar> ClanWars { get; set; }                  // 2.17: the clan's own wars
        public Challenge PendingChallenge { get; set; }              // 2.18: a rival's challenge awaiting the clan's answer
        public bool Impoverished { get; set; }                       // 2.19: this year's upkeep fell short
        public List<DaoPrey> DaoPreys { get; set; }                  // 2.20: the hunters that learnt a Dao of the clan is ripe
        public List<AnnalEntry> Annals { get; set; }                 // 2.21: the clan's milestones and the dynastic endings reached (LORE.md §11.9)
        public Dictionary<string, string> PositionMoves { get; set; } // 2.21: « from>to » positions risen to, and by whom (the endings)
        public Dictionary<string, int> EndingStreaks { get; set; }    // 2.21: the years a hegemony has held in a row, by ending
        public int? SectFoundedYear { get; set; }                     // 2.21: the year the clan founded its sect (B3d)
        public List<Diplomacy.AbsorbedPower> AbsorbedPowers { get; set; } // 2.21: the vassals the clan absorbed
        public List<Diplomacy.KnowledgeDebt> KnowledgeDebts { get; set; } // 2.21: what the clan owes for a method (§11.10)
        public Diplomacy.SponsorOffer PendingSponsorOffer { get; set; }   // 2.21: a patron's offer awaiting the clan's answer
        public List<Diplomacy.Sponsorship> Sponsorships { get; set; }     // 2.21: accepted patronages and their designs
        public List<Diplomacy.AscentRival> AscentRivals { get; set; }     // 2.21: the rivals that know of an accord

        // Lineage
        public int GenerationCount { get; set; } = 1;
        public int TotalBirths { get; set; }
        public int TotalDeaths { get; set; }
        public string LastPatriarchId { get; set; }
        public int AscendedAncestors { get; set; }  // before 2.21, read no more: a Dao Embryo now stays in the world

        // World
        public List<BuildingData> Buildings { get; set; }
        public List<FactionData> Factions { get; set; }
        public List<StoryTriggerType> TriggeredStoryEvents { get; set; }
        public List<StoryTriggerType> PendingStoryEvents { get; set; }

        // Outcome
        public bool GameWon { get; set; }           // before 2.21, read no more: there is no forced victory
        public bool GameLost { get; set; }
    }
}

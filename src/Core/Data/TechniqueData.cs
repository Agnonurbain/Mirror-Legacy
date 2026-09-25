using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MirrorChronicles.Data
{
    /// <summary>What a technique is (LORE.md §2.1): a cultivation method, a spell, a movement art, a weapon art, an immortal art.</summary>
    public enum TechniqueKind
    {
        Cultivation,
        Spell,
        Movement,
        Weapon,
        ImmortalArt // alchemy, artifacts, formations: the hundred arts
    }

    /// <summary>What an art does in battle; cultivation methods, movement and immortal arts have none.</summary>
    public enum TechniqueEffect
    {
        None,
        Strike, // a foe in range
        Heal    // an ally in range
    }

    /// <summary>
    /// The three categories of LORE.md §2.3: common (bought at a fixed price, its Qi still harvested),
    /// ancestral (obsolete: its Qi has vanished), secret (an ancestral method reworked, often flawed).
    /// </summary>
    public enum TechniqueCategory
    {
        Common,
        Ancestral,
        Secret
    }

    /// <summary>The family a spiritual Qi belongs to, as §2.4 names it; phase L4 ties each to its Fruitions.</summary>
    public enum QiFamily
    {
        Unknown,
        Fire,
        Water,
        Wood,
        Metal,
        Earth,
        Yin,
        Yang,
        Thunder,
        TwelveEssences, // « Douze Essences »
        Archaic,
        Dawnlight,      // Qi of the Lueur de l'Aube
        Other
    }

    /// <summary>
    /// A spiritual Qi of Heaven and Earth (LORE.md §2.5, qi.json). Never interchangeable: a method only
    /// works with its own Qi. Harvested in wisps, condensed into portions.
    /// </summary>
    public sealed class QiDefinition
    {
        public string Id { get; init; }
        public string Name { get; init; }
        public QiFamily Family { get; init; }

        /// <summary>The element its cultivators wield (affinity bonus in battle).</summary>
        public Element Element { get; init; }

        /// <summary>Years of work by one harvester for one portion (the Seven Terraces' Qi: ten to gather, ten to refine).</summary>
        public int YearsPerPortion { get; init; } = 1;

        /// <summary>Its source is gone (an ancestral method's Qi): it can no longer be harvested.</summary>
        public bool Vanished { get; init; }

        /// <summary>Found everywhere (the Souffle Commun's minor Qi): entering Qi Cultivation needs no portion.</summary>
        public bool Ubiquitous { get; init; }
    }

    /// <summary>The drawbacks of an imperfect technique, usually a secret one (LORE.md §2.3-2.4).</summary>
    [Serializable]
    public sealed class TechniqueFlaws
    {
        /// <summary>Multiplies the grade's speed in a realm (the Veilleur du Sentier: fast breathing, slow Qi).</summary>
        public Dictionary<CultivationRealm, double> SpeedByRealm { get; set; } = new Dictionary<CultivationRealm, double>();

        /// <summary>Share of the lifespan its practitioners keep (1 = no loss).</summary>
        public double LifespanFactor { get; set; } = 1.0;

        /// <summary>Its practitioners are powerless against anyone practising this technique (the original sutra).</summary>
        public string CounteredById { get; set; }

        public TechniqueFlaws Clone()
        {
            var copy = (TechniqueFlaws)MemberwiseClone();
            copy.SpeedByRealm = new Dictionary<CultivationRealm, double>(SpeedByRealm ?? new Dictionary<CultivationRealm, double>());
            return copy;
        }
    }

    /// <summary>
    /// Represents a fragment of knowledge found in ruins or stolen from rivals.
    /// Used in the Deduction Engine to create new techniques.
    /// </summary>
    [Serializable]
    public class FragmentData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public Element Element { get; set; }
        public int Quality { get; set; } // 1 (Common) to 5 (Divine)

        public FragmentData Clone() => (FragmentData)MemberwiseClone();

        public FragmentData()
        {
            ID = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// A technique (LORE.md §2): from the catalog (techniques.json) or deduced by the mirror. Its grade
    /// (1-7, 7 standing for « 7+ ») measures quality and potential, not a realm: it caps the realm a
    /// method leads to and sets how fast one cultivates with it.
    /// </summary>
    [Serializable]
    public class TechniqueData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public TechniqueKind Kind { get; set; }
        public TechniqueEffect Effect { get; set; }
        public int Grade { get; set; }
        public TechniqueCategory Category { get; set; }
        public Element DominantElement { get; set; }

        /// <summary>A method's first realm; an art's minimum realm.</summary>
        public CultivationRealm RequiredRealm { get; set; }

        /// <summary>A method's last realm covered entirely; null follows the grade (see TechniqueRules).</summary>
        public CultivationRealm? SupremeRealm { get; set; }

        /// <summary>The Qi a method needs from Qi Cultivation on; null for breathing methods and arts.</summary>
        public string RequiredQiId { get; set; }

        /// <summary>Holds the secret technique of ascent to the Purple Mansion; null follows the grade (5 and above).</summary>
        public bool? HasPurpleMansionSecret { get; set; }

        public TechniqueFlaws Flaws { get; set; }

        // Stats
        public int PowerModifier { get; set; } // Damage or healing
        public int QiCost { get; set; }
        public int Range { get; set; } // Manhattan distance; 1 = melee, 2+ = ranged
        public int RiskFactor { get; set; } // 0-100% chance of Qi Deviation when practicing

        public TechniqueData Clone()
        {
            var copy = (TechniqueData)MemberwiseClone();
            copy.Flaws = Flaws?.Clone();
            return copy;
        }

        public TechniqueData()
        {
            ID = Guid.NewGuid().ToString();
        }

        /// <summary>The classification of saves made before phase L3.</summary>
        internal enum LegacyType { CultivationMethod, MartialArt, SupportArt }

        /// <summary>Reads the « Type » of older saves; never written.</summary>
        [JsonProperty("Type")]
        private LegacyType? LegacyTypeOnLoad
        {
            set
            {
                switch (value)
                {
                    case LegacyType.CultivationMethod: Kind = TechniqueKind.Cultivation; Effect = TechniqueEffect.None; break;
                    case LegacyType.MartialArt: Kind = TechniqueKind.Weapon; Effect = TechniqueEffect.Strike; break;
                    case LegacyType.SupportArt: Kind = TechniqueKind.Spell; Effect = TechniqueEffect.Heal; break;
                }
            }
        }
    }
}

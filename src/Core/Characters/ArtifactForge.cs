using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The clan's forge makes artifacts (L4f, user decisions 2026-10-03): a free smith of the rank's realm at least, one work
    /// a year, a forge high enough, ores and stones; the artifact takes the smith's lineage. An artifact of the armoury may
    /// be raised by one rank, up to the Purple Mansion (📚 a Foundation's soft armour raised to a Spiritual Artifact). A
    /// Spiritual Treasure is never forged nor raised: only found.
    /// </summary>
    public sealed class ArtifactForge
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly BuildingSystem buildings;
        private readonly ArtifactArmoury armoury;

        public ArtifactForge(GameContext ctx, ClanManager clan, ResourceManager resources, BuildingSystem buildings, ArtifactArmoury armoury)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.buildings = buildings;
            this.armoury = armoury;
        }

        private ArtifactSettings Settings => ctx.Content.Balance.Artifacts;

        /// <summary>The clan's Immortal Arts (set by the session): a smith needs the forge's legacy, its gift, its mastery (audit §2.1).</summary>
        public ArtSystem Arts { get; set; }

        /// <summary>
        /// The mastery of the forge a rank asks (🔎 audit §2.1, 2026-10-04): an apprentice forges a Dharma Artifact, an adept a
        /// Spiritual Artifact, a master the Purple Mansion's.
        /// </summary>
        private int MasteryFor(CultivationRealm rank) =>
            rank >= CultivationRealm.PurpleMansion ? ctx.Content.Balance.Arts.MasterAt
            : rank >= CultivationRealm.Foundation ? ctx.Content.Balance.Arts.AdeptAt : 1;

        /// <summary>Why this smith may not work the forge at this rank as an Immortal Art (French), or null.</summary>
        private string ArtRefusal(CharacterData smith, CultivationRealm rank)
        {
            if (Arts == null) return null;
            if (!Arts.HoldsLegacy(ImmortalArt.Forge)) return "le clan ne tient pas l'héritage du raffinement d'artefacts";
            if (!ImmortalArtRules.MayPractise(smith, ImmortalArt.Forge, ctx.Content.Balance.Arts)) return "ce forgeron n'a pas le don de la forge (sans don, il faut le Manoir Pourpre)";
            int needed = MasteryFor(rank);
            if (ArtSystem.MasteryOf(smith, ImmortalArt.Forge) < needed)
                return $"il faut être {ImmortalArtRules.Rank(needed, ctx.Content.Balance.Arts)} de la forge pour ce rang";
            return null;
        }

        /// <summary>Why this smith cannot work an artifact of this rank at this price (French), or null.</summary>
        private string Refusal(CharacterData smith, CultivationRealm rank, double share)
        {
            if (!Settings.Forging.TryGetValue(rank, out var cost)) return "la forge du clan ne fait pas d'artefact de ce rang";
            if (smith == null || !smith.IsAlive || smith.CaptorFaction != null || smith.Retreat != Retreat.None) return "ce forgeron ne peut travailler";
            if (smith.Realm < rank) return "un forgeron ne forge rien au-dessus de son royaume";
            if (ArtRefusal(smith, rank) is { } art) return art;
            if (smith.LastOperationYear == ctx.Clock.Year) return "ce forgeron a déjà œuvré cette année";
            if (buildings.ForgeLevel < cost.ForgeLevel) return $"il faut une forge de niveau {cost.ForgeLevel}";
            if (resources.SpiritualOres < (int)(cost.Ores * share) || resources.SpiritStones < (int)(cost.Stones * share))
                return $"il faut {(int)(cost.Ores * share)} minerais et {(int)(cost.Stones * share)} pierres";
            return null;
        }

        private void Pay(CharacterData smith, CultivationRealm rank, double share)
        {
            var cost = Settings.Forging[rank];
            resources.ConsumeOres((int)(cost.Ores * share));
            resources.ConsumeSpiritStones((int)(cost.Stones * share));
            smith.LastOperationYear = ctx.Clock.Year;
        }

        /// <summary>Why this member cannot work the forge at this rank, whatever the price (French), or null: free, of the realm, of the art.</summary>
        public string SmithRefusal(string smithId, CultivationRealm rank)
        {
            var smith = clan.FindById(smithId);
            if (smith == null || !smith.IsAlive || smith.CaptorFaction != null || smith.Retreat != Retreat.None) return "ce forgeron ne peut travailler";
            if (smith.Realm < rank) return "un forgeron ne forge rien au-dessus de son royaume";
            return ArtRefusal(smith, rank);
        }

        /// <summary>Why this smith cannot forge an artifact of this rank (French), or null.</summary>
        public string MakeRefusal(CultivationRealm rank, string smithId) => Refusal(clan.FindById(smithId), rank, 1.0);

        /// <summary>Why this smith cannot raise this artifact (French), or null.</summary>
        public string RaiseRefusal(string artifactId, string smithId)
        {
            var artifact = armoury.Armoury.FirstOrDefault(a => a.Id == artifactId);
            if (artifact == null) return "cet artefact n'est pas dans l'armurerie du clan";
            if (artifact.Class == ArtifactClass.SpiritualTreasure) return "un Trésor Spirituel ne se forge ni ne s'élève";
            if (artifact.Rank >= CultivationRealm.PurpleMansion) return "au-delà du Manoir Pourpre, ce sont les Trésors de Dharma du Noyau d'Or";
            return Refusal(clan.FindById(smithId), artifact.Rank + 1, Settings.RaiseShare);
        }

        /// <summary>A smith forges an artifact of this form and rank. Null when done, else why not (French).</summary>
        public string Make(string formId, CultivationRealm rank, string smithId)
        {
            var smith = clan.FindById(smithId);
            if (ctx.Content.ArtifactForms.All(f => f.Id != formId)) return "forme d'artefact inconnue";
            if (Refusal(smith, rank, 1.0) is { } why) return why;
            Pay(smith, rank, 1.0);
            var made = armoury.Create(formId, rank, FoundationRef.Parse(smith.FoundationId).FruitionId);
            ctx.Events.TriggerArtifactForged(made, smith);
            return null;
        }

        /// <summary>A smith raises an artifact of the armoury by one rank. Null when done, else why not (French).</summary>
        public string Raise(string artifactId, string smithId)
        {
            var artifact = armoury.Armoury.FirstOrDefault(a => a.Id == artifactId);
            if (artifact == null) return "cet artefact n'est pas dans l'armurerie du clan";
            if (artifact.Class == ArtifactClass.SpiritualTreasure) return "un Trésor Spirituel ne se forge ni ne s'élève";
            if (artifact.Rank >= CultivationRealm.PurpleMansion) return "au-delà du Manoir Pourpre, ce sont les Trésors de Dharma du Noyau d'Or";
            var rank = artifact.Rank + 1;
            var smith = clan.FindById(smithId);
            if (Refusal(smith, rank, Settings.RaiseShare) is { } why) return why;
            Pay(smith, rank, Settings.RaiseShare);
            var raised = armoury.Raise(artifactId, rank);
            ctx.Events.TriggerArtifactForged(raised, smith);
            return null;
        }
    }
}

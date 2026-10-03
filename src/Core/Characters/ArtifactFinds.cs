using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Artifacts found (L4f, user decisions 2026-10-03). A tomb's guardian leaves its Purple Mansion's artifact, sometimes a
    /// Spiritual Treasure; ruins yield Dharma Artifacts; a yielding enemy is plundered by its rank; a Purple Mansion of the
    /// clan who dies may leave a Spiritual Treasure of its lineage — its body turns to things of its foundation (§5.3.5).
    /// Spiritual Treasures come only so. A Foundation at its peak may bind itself to one (LORE.md §5.4.2, the user's rule):
    /// the power of a Purple Mansion with one ability — two when the treasure is of its own lineage — without holding any,
    /// and never a step further.
    /// </summary>
    public sealed class ArtifactFinds
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly ArtifactArmoury armoury;

        public ArtifactFinds(GameContext ctx, ClanManager clan, FactionManager factions, ArtifactArmoury armoury)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.armoury = armoury;
            ctx.Events.OnTombLooted += TombLoot;
            ctx.Events.OnRandomEventOccurred += e => { if (e.EventType == RandomEventType.RuinsDiscovery) SearchTheRuins(); };
            ctx.Events.OnClanWarWon += Plunder;
            ctx.Events.OnCharacterDied += (dead, cause) => { if (cause != DeathCause.AscentCollapse) Remains(dead); };
        }

        private ArtifactFinding Settings => ctx.Content.Balance.Artifacts.Finding;

        private string AnyForm() => ctx.Content.ArtifactForms[ctx.Rng.Next(ctx.Content.ArtifactForms.Count)].Id;

        private string AnyLineage()
        {
            var lineages = ctx.Content.Fruitions.Where(f => f.Abilities.Count > 0).ToList();
            return lineages.Count == 0 ? null : lineages[ctx.Rng.Next(lineages.Count)].Id;
        }

        private void Found(ArtifactInstance artifact, string where)
        {
            ctx.Log.Info($"[Artifacts] {where}: {artifact.Name}.");
            ctx.Events.TriggerArtifactFound(artifact, where);
        }

        private void TombLoot()
        {
            bool treasure = ctx.Rng.Chance(Settings.TombTreasureChance);
            Found(armoury.Create(AnyForm(), CultivationRealm.PurpleMansion, AnyLineage(), treasure ? ArtifactClass.SpiritualTreasure : null),
                "le gardien du tombeau laisse son artefact");
        }

        /// <summary>Ruins searched: perhaps a Dharma Artifact of the Qi Refinement or the Foundation.</summary>
        public void SearchTheRuins()
        {
            if (!ctx.Rng.Chance(Settings.RuinsChance)) return;
            var rank = ctx.Rng.Chance(0.5) ? CultivationRealm.QiRefinement : CultivationRealm.Foundation;
            Found(armoury.Create(AnyForm(), rank, ctx.Rng.Chance(0.5) ? AnyLineage() : null), "les ruines livrent un artefact");
        }

        private void Plunder(string enemy)
        {
            var power = factions.GetFactionByName(enemy);
            if (power == null || power.HighestRealm < CultivationRealm.QiRefinement || !ctx.Rng.Chance(Settings.WarLootChance)) return;
            var rank = power.HighestRealm > CultivationRealm.PurpleMansion ? CultivationRealm.PurpleMansion : power.HighestRealm;
            bool treasure = rank == CultivationRealm.PurpleMansion && ctx.Rng.Chance(Settings.WarTreasureChance);
            Found(armoury.Create(AnyForm(), rank, AnyLineage(), treasure ? ArtifactClass.SpiritualTreasure : null), $"le pillage de {enemy}");
        }

        private void Remains(CharacterData dead)
        {
            if (dead.Realm != CultivationRealm.PurpleMansion || dead.TreasureBound || !ctx.Rng.Chance(Settings.MansionDeathTreasureChance)) return;
            Found(armoury.Create(AnyForm(), CultivationRealm.PurpleMansion, FoundationRef.Parse(dead.FoundationId).FruitionId, ArtifactClass.SpiritualTreasure),
                $"le corps de {dead.FullName} devient un trésor");
        }

        /// <summary>A Foundation at its peak binds itself to a Spiritual Treasure of the armoury. Null when done, else why not (French).</summary>
        public string Bind(string memberId, string artifactId)
        {
            var member = clan.FindById(memberId);
            var treasure = armoury.Armoury.FirstOrDefault(a => a.Id == artifactId);
            if (treasure == null || treasure.Class != ArtifactClass.SpiritualTreasure) return "on ne se lie qu'à un Trésor Spirituel de l'armurerie";
            if (member == null || !member.IsAlive || member.CaptorFaction != null || member.Realm != CultivationRealm.Foundation
                || member.RealmStage < PowerLadder.StageCount(CultivationRealm.Foundation) || member.ProgressionSealed)
                return "seule une Fondation à son sommet, libre d'avancer, peut s'y lier";
            armoury.Withdraw(artifactId, member);
            bool kin = treasure.Lineage != null && FoundationRef.Parse(member.FoundationId).FruitionId == treasure.Lineage;
            member.TreasureBound = true;
            member.ProgressionSealed = true; // its destiny is the treasure's
            member.Realm = CultivationRealm.PurpleMansion;
            member.RealmStage = kin ? 2 : 1;
            member.CultivationXP = 0;
            member.MaxLifespan = System.Math.Max(member.MaxLifespan, PowerLadder.MaxLifespan(CultivationRealm.PurpleMansion, member.RealmStage));
            ctx.Log.Info($"[Artifacts] {member.FullName} binds itself to {treasure.Name}.");
            ctx.Events.TriggerTreasureBound(member, treasure);
            return null;
        }
    }
}

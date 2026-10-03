using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The clan's artifacts (L4f, user decisions 2026-10-03): each member bears one at most, the others lie in the armoury,
    /// where a dead bearer's returns. An artifact is made of a form, a rank (its class follows: a Dharma Artifact below the
    /// Purple Mansion, a Spiritual Artifact at it, a Spiritual Treasure when rarer) and a lineage; the forge, the tombs, the
    /// powers' trade and thefts all make them through <see cref="Create"/>.
    /// </summary>
    public sealed class ArtifactArmoury
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly List<ArtifactInstance> armoury = new List<ArtifactInstance>();

        public ArtifactArmoury(GameContext ctx, ClanManager clan)
        {
            this.ctx = ctx;
            this.clan = clan;
            ctx.Events.OnCharacterDied += (dead, _) => Unequip(dead);
        }

        private ArtifactSettings Settings => ctx.Content.Balance.Artifacts;

        public IReadOnlyList<ArtifactInstance> Armoury => armoury;

        public void Restore(IEnumerable<ArtifactInstance> saved)
        {
            armoury.Clear();
            if (saved != null) armoury.AddRange(saved);
        }

        public static ArtifactClass ClassOf(CultivationRealm rank) =>
            rank >= CultivationRealm.PurpleMansion ? ArtifactClass.SpiritualArtifact : ArtifactClass.DharmaArtifact;

        /// <summary>Makes an artifact (it goes to the armoury) — the forge's, a tomb's, a trade's.</summary>
        public ArtifactInstance Create(string formId, CultivationRealm rank, string lineage, ArtifactClass? cls = null)
        {
            var form = ctx.Content.ArtifactForms.First(f => f.Id == formId);
            var made = Make(form, rank, lineage, cls ?? ClassOf(rank));
            armoury.Add(made);
            ctx.Log.Info($"[Artifacts] The clan gains {made.Name} ({made.Class}, {made.Rank}).");
            return made;
        }

        private ArtifactInstance Make(ArtifactForm form, CultivationRealm rank, string lineage, ArtifactClass cls)
        {
            var s = Settings;
            var scale = s.Ranks.Where(r => r.Key <= rank).OrderByDescending(r => r.Key).Select(r => r.Value).FirstOrDefault() ?? new ArtifactScale();
            double factor = s.ClassFactor.TryGetValue(cls, out var f) ? f : 1.0;
            string lineageName = lineage == null ? null : ctx.Content.Fruitions.FirstOrDefault(x => x.Id == lineage)?.Name ?? lineage;
            return new ArtifactInstance(ctx.Rng.NextId(), form.Id, lineageName == null ? form.Name : $"{form.Name} · {lineageName}", cls, rank, lineage,
                form.Effect,
                form.Effect == ArtifactEffect.Combat ? (int)System.Math.Round(scale.Strength * factor) : 0,
                form.Effect == ArtifactEffect.Protection ? scale.Protection * factor : 0,
                form.Effect == ArtifactEffect.Cultivation ? scale.Cultivation * factor : 0,
                s.LineageFactor);
        }

        /// <summary>An artifact of the armoury raised to a higher rank: the same artifact, its class and gifts those of its new rank.</summary>
        public ArtifactInstance Raise(string artifactId, CultivationRealm rank)
        {
            var old = armoury.FirstOrDefault(a => a.Id == artifactId);
            if (old == null) return null;
            var form = ctx.Content.ArtifactForms.First(f => f.Id == old.FormId);
            var raised = Make(form, rank, old.Lineage, ClassOf(rank)) with { Id = old.Id };
            armoury[armoury.IndexOf(old)] = raised;
            ctx.Log.Info($"[Artifacts] {old.Name} is raised to {rank}.");
            return raised;
        }

        /// <summary>A member takes an artifact of the armoury; the one it bore goes back. Null when done, else why not (French).</summary>
        public string Equip(CharacterData member, string artifactId)
        {
            var artifact = armoury.FirstOrDefault(a => a.Id == artifactId);
            if (artifact == null) return "cet artefact n'est pas dans l'armurerie du clan";
            if (member == null || !member.IsAlive || member.CaptorFaction != null) return "ce membre ne peut le recevoir";
            if (member.Realm < artifact.Rank) return "nul ne manie un artefact au-dessus de son royaume";
            if (member.TreasureBound) return "lié à son trésor, il n'en portera jamais d'autre";
            Unequip(member);
            armoury.Remove(artifact);
            member.Artifact = artifact;
            return null;
        }

        /// <summary>An artifact taken out of the armoury, borne now by a member who binds itself to it.</summary>
        public bool Withdraw(string artifactId, CharacterData bearer)
        {
            var artifact = armoury.FirstOrDefault(a => a.Id == artifactId);
            if (artifact == null || bearer == null) return false;
            Unequip(bearer);
            armoury.Remove(artifact);
            bearer.Artifact = artifact;
            return true;
        }

        /// <summary>The member's artifact goes back to the armoury.</summary>
        public void Unequip(CharacterData member)
        {
            if (member?.Artifact == null) return;
            armoury.Add(member.Artifact);
            member.Artifact = null;
        }
    }
}

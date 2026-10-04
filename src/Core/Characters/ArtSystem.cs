using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Immortal Arts of the clan (AUDIT_LORE.md §2, the user's decisions 2026-10-03/04): the legacies it holds, who practises
    /// which art, their mastery. Below the Purple Mansion only the gifted practise, and only an art whose legacy the clan holds;
    /// a master of the clan teaches at full pace. Practising costs an ordinary talent part of its cultivation, and feeds a
    /// genius's. A legacy is lost with its last master if no one has taken it up.
    /// </summary>
    public sealed class ArtSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly CultivationSystem cultivation;
        private readonly HashSet<ImmortalArt> legacies = new HashSet<ImmortalArt>();

        public ArtSystem(GameContext ctx, ClanManager clan, CultivationSystem cultivation)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.cultivation = cultivation;
            ctx.Events.OnCharacterDied += (dead, cause) => LoseWithTheLastMaster(dead);
        }

        private ArtSettings Settings => ctx.Content.Balance.Arts;

        public IReadOnlyCollection<ImmortalArt> Legacies => legacies;

        public bool HoldsLegacy(ImmortalArt art) => legacies.Contains(art);

        /// <summary>The clan gains an art's legacy (a power's teaching, a tomb, the mirror).</summary>
        public bool GainLegacy(ImmortalArt art)
        {
            if (!legacies.Add(art)) return false;
            ctx.Log.Info($"[Arts] The clan holds the legacy of the {art}.");
            return true;
        }

        public void Restore(IEnumerable<ImmortalArt> saved)
        {
            legacies.Clear();
            foreach (var art in saved ?? Enumerable.Empty<ImmortalArt>()) legacies.Add(art);
        }

        public static int MasteryOf(CharacterData member, ImmortalArt art) =>
            member?.ArtMastery != null && member.ArtMastery.TryGetValue(art, out var m) ? m : 0;

        /// <summary>The clan's best master of the art, alive and at home; null when none.</summary>
        public CharacterData MasterOf(ImmortalArt art) =>
            clan.LivingMembers.Where(m => m.CaptorFaction == null && MasteryOf(m, art) >= Settings.MasterAt)
                .OrderByDescending(m => MasteryOf(m, art)).FirstOrDefault();

        /// <summary>Why the member cannot practise the art now (French), or null.</summary>
        public string PracticeRefusal(CharacterData member, ImmortalArt art)
        {
            if (member == null || !member.IsAlive) return "membre introuvable";
            if (!legacies.Contains(art)) return "le clan n'en tient pas l'héritage";
            if (!ImmortalArtRules.MayPractise(member, art, Settings)) return "sans don, il faut le Manoir Pourpre pour pratiquer un art";
            if (!TaskRules.IsAllowed(member, TaskType.ArtPractice)) return "ce membre ne peut pas s'y consacrer";
            return null;
        }

        /// <summary>The member devotes the year to the art. Null when done; else why not (French).</summary>
        public string Practise(CharacterData member, ImmortalArt art)
        {
            if (PracticeRefusal(member, art) is { } refusal) return refusal;
            member.CurrentTask = TaskType.ArtPractice;
            member.PracticedArt = art;
            return null;
        }

        /// <summary>The arts' year: each practitioner gains mastery, and cultivates by its gift; the taught progress faster.</summary>
        public void ProcessYear()
        {
            foreach (var member in clan.LivingMembers.Where(m => m.CurrentTask == TaskType.ArtPractice).ToList())
            {
                if (member.PracticedArt is not { } art || PracticeRefusal(member, art) != null)
                {
                    member.CurrentTask = TaskType.None;
                    member.PracticedArt = null;
                    continue;
                }
                var master = MasterOf(art);
                bool taught = master != null; // a master of the clan teaches (or the member is one)
                member.ArtMastery ??= new Dictionary<ImmortalArt, int>();
                member.ArtMastery[art] = System.Math.Min(100, MasteryOf(member, art) + ImmortalArtRules.YearlyMastery(member, art, taught, Settings));
                cultivation.ProcessYearlyCultivation(member, ImmortalArtRules.CultivationFactor(member, art, Settings));
            }
        }

        /// <summary>A legacy is lost with its last master if no one has taken it up (📚 Li Quantao failed to inherit the alchemy).</summary>
        private void LoseWithTheLastMaster(CharacterData dead)
        {
            foreach (var art in legacies.ToList())
            {
                if (MasteryOf(dead, art) < Settings.MasterAt) continue;
                if (clan.LivingMembers.Any(m => m != dead && MasteryOf(m, art) > 0)) continue;
                legacies.Remove(art);
                ctx.Log.Warning($"[Arts] With {dead.FullName}, the clan's legacy of the {art} is lost: no one took it up.");
            }
        }
    }
}

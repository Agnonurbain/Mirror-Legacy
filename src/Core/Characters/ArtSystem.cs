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
        private readonly Economy.ResourceManager resources;
        private readonly HashSet<ImmortalArt> legacies = new HashSet<ImmortalArt>();

        public ArtSystem(GameContext ctx, ClanManager clan, CultivationSystem cultivation, Economy.ResourceManager resources)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.cultivation = cultivation;
            this.resources = resources;
            ctx.Events.OnCharacterDied += (dead, cause) => LoseWithTheLastMaster(dead);
            ctx.Events.OnRandomEventOccurred += e => { if (e.EventType == RandomEventType.RuinsDiscovery) SearchTheRuins(); };
            ctx.Events.OnTombLooted += () => { if (ctx.Rng.Chance(Settings.Legacy.TombChance)) Find("le tombeau d'un Manoir Pourpre livre un manuel"); };
        }

        /// <summary>The powers, the accords in kind, the mirror and its fragments (set by the session): the legacies' sources (audit §2.5).</summary>
        public Diplomacy.FactionManager Factions { get; set; }
        public Diplomacy.KnowledgeAccords Accords { get; set; }
        public Mirror.MirrorSystem Mirror { get; set; }
        public Mirror.DeductionEngine Deduction { get; set; }

        private ArtSettings Settings => ctx.Content.Balance.Arts;

        public IReadOnlyCollection<ImmortalArt> Legacies => legacies;

        public bool HoldsLegacy(ImmortalArt art) => legacies.Contains(art);

        /// <summary>The clan gains an art's legacy (a power's teaching, a tomb, the mirror), told in the chronicle when it says how.</summary>
        public bool GainLegacy(ImmortalArt art, string how = null)
        {
            if (!legacies.Add(art)) return false;
            ctx.Log.Info($"[Arts] The clan holds the legacy of the {art}.");
            if (how != null) ctx.Events.TriggerArtLegacyGained(art, how);
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
                if (art == ImmortalArt.Talismans) SellTheTalismans(member);
                var master = MasterOf(art);
                bool taught = master != null; // a master of the clan teaches (or the member is one)
                member.ArtMastery ??= new Dictionary<ImmortalArt, int>();
                member.ArtMastery[art] = System.Math.Min(100, MasteryOf(member, art) + ImmortalArtRules.YearlyMastery(member, art, taught, Settings));
                cultivation.ProcessYearlyCultivation(member, ImmortalArtRules.CultivationFactor(member, art, Settings));
            }
        }

        // ---- The legacies' sources (audit §2.5, the user's decision 2026-10-04) ----

        private System.Collections.Generic.List<ImmortalArt> Lacking() =>
            System.Enum.GetValues<ImmortalArt>().Where(a => !legacies.Contains(a)).ToList();

        /// <summary>A manual found: an art the clan lacks, drawn. False when it holds them all.</summary>
        private bool Find(string how)
        {
            var lacking = Lacking();
            return lacking.Count > 0 && GainLegacy(lacking[ctx.Rng.Next(lacking.Count)], how);
        }

        /// <summary>Ruins found: they may hold an art's manual.</summary>
        public void SearchTheRuins()
        {
            if (Lacking().Count > 0 && ctx.Rng.Chance(Settings.Legacy.RuinsChance)) Find("les ruines livrent un manuel");
        }

        /// <summary>Why this power will not teach the art for these terms (French), or null.</summary>
        public string LearnRefusal(string powerName, ImmortalArt art, IReadOnlyList<Diplomacy.AccordTerm> terms)
        {
            if (legacies.Contains(art)) return "le clan tient déjà cet héritage";
            var power = Factions?.GetFactionByName(powerName);
            if (power == null) return "puissance inconnue";
            if (!PowerArts.Knows(power, art, ctx.Content)) return $"{power.Name} ne connaît pas cet art";
            if (power.RelationWithPlayer < Settings.Legacy.TeachRelation) return $"il faut une relation de {Settings.Legacy.TeachRelation} au moins";
            if (Accords == null) return "aucun accord possible";
            return Accords.BarterRefusal(power.Name, Settings.Legacy.Worth, precious: true, terms);
        }

        /// <summary>A power's master teaches the art for an accord in kind (precious: never for stones). Null when done, else why not.</summary>
        public string LearnFrom(string powerName, ImmortalArt art, IReadOnlyList<Diplomacy.AccordTerm> terms)
        {
            if (LearnRefusal(powerName, art, terms) is { } why) return why;
            string name = Settings.Arts.FirstOrDefault(a => a.Art == art)?.Name ?? art.ToString();
            Accords.Barter(powerName, terms, $"l'héritage de {name}");
            GainLegacy(art, $"un maître de {powerName} enseigne au clan");
            return null;
        }

        /// <summary>Why the mirror cannot deduce this art from these fragments now (French), or null.</summary>
        public string DeduceRefusal(ImmortalArt art, IReadOnlyList<string> fragmentIds)
        {
            if (legacies.Contains(art)) return "le clan tient déjà cet héritage";
            if (Mirror == null || Deduction == null) return "le miroir ne déduit rien";
            var ids = fragmentIds ?? new List<string>();
            if (ids.Distinct().Count() != Settings.Legacy.DeduceFragments || ids.Any(id => Deduction.Fragments.All(f => f.ID != id)))
                return $"il faut {Settings.Legacy.DeduceFragments} fragments du clan";
            return Mirror.PayRefusal(Settings.Legacy.DeduceMoonlight);
        }

        /// <summary>The mirror deduces the art's legacy from fragments, for its Moonlight. Null when done, else why not (French).</summary>
        public string Deduce(ImmortalArt art, IReadOnlyList<string> fragmentIds)
        {
            if (DeduceRefusal(art, fragmentIds) is { } why) return why;
            if (!Mirror.ConsumePower(Settings.Legacy.DeduceMoonlight)) return "le miroir n'a pu payer";
            Deduction.Consume(fragmentIds);
            GainLegacy(art, "le miroir déduit un art immortel de ses fragments");
            return null;
        }

        /// <summary>The year's talismans are sold (📚 « one of the few ways to earn stones »): by the drawer's mastery, before the year's gain.</summary>
        private void SellTheTalismans(CharacterData drawer)
        {
            int stones = ImmortalArtRules.TalismanStones(drawer, Settings);
            resources.AddSpiritStones(stones);
            ctx.Log.Info($"[Arts] {drawer.FullName}'s talismans sell for {stones} stones.");
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

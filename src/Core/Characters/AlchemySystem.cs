using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.World;

namespace MirrorChronicles.Characters
{
    /// <summary>A poisoned pill in the clan's store: one of its element's, slipped in by this power (saved).</summary>
    public sealed record PoisonedPill(Element Element, string Power);

    /// <summary>What the wall's pill was: none, a true one, or a rival's poison.</summary>
    public enum PillTaken { None, Pure, Poisoned }

    /// <summary>
    /// The clan's alchemy (AUDIT_LORE.md §2.6-2.7, the user's decision 2026-10-04): its store of Essence Gathering Pills, each of
    /// an element, and the alchemists who refine them — the gifted (or the Purple Mansion) of a clan that holds alchemy's
    /// legacy, one work a year, from herbs and stones. The Foundation wall takes a pill of the cultivator's Qi element; without
    /// one it is a gamble (📚 wiki Li_Chenghui; a pill of another essence serves nothing, Li_Chengliao). A hostile power may slip
    /// a poisoned pill — an opposed essence in the guise of the store's own — that fails the wall; an adept of alchemy finds it,
    /// and whose it is (audit §2.7).
    /// </summary>
    public sealed class AlchemySystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly ArtSystem arts;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly Dictionary<Element, int> essencePills = new Dictionary<Element, int>(); // every pill, the poisoned among them
        private readonly List<PoisonedPill> poisoned = new List<PoisonedPill>();
        private readonly Dictionary<PillKind, int> pills = new Dictionary<PillKind, int>();

        /// <summary>The accords in kind (set by the session): a Purple Mansion's pill is bought only so.</summary>
        public KnowledgeAccords Accords { get; set; }

        public AlchemySystem(GameContext ctx, ClanManager clan, ResourceManager resources, ArtSystem arts, FactionManager factions, SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.arts = arts;
            this.factions = factions;
            this.suspicion = suspicion;
        }

        public IReadOnlyList<PoisonedPill> Poisoned => poisoned;

        private EssencePillSettings Pill => ctx.Content.Balance.Arts.EssencePill;

        public IReadOnlyDictionary<Element, int> EssencePills => essencePills;

        public int EssencePillsOf(Element element) => essencePills.TryGetValue(element, out int n) ? n : 0;

        /// <summary>The element of the Qi a cultivator absorbed — the pill's it needs (null: no Qi).</summary>
        public static Element? ElementFor(CharacterData member, GameContent content) =>
            content.Qi.FirstOrDefault(q => q.Id == member.QiId)?.Element;

        public bool HoldsEssencePillFor(CharacterData member) =>
            ElementFor(member, ctx.Content) is { } e && EssencePillsOf(e) > 0;

        /// <summary>The clan gains pills (an alchemist's work, a power's gift, a tomb).</summary>
        public void GainEssencePills(Element element, int count)
        {
            if (count <= 0) return;
            essencePills[element] = EssencePillsOf(element) + count;
        }

        /// <summary>A pill of the member's element is swallowed at the wall — perhaps a poisoned one, which looks the same.</summary>
        public PillTaken TakeEssencePill(CharacterData member)
        {
            if (ElementFor(member, ctx.Content) is not { } e || EssencePillsOf(e) == 0) return PillTaken.None;
            int bad = poisoned.Count(p => p.Element == e);
            bool poison = bad > 0 && ctx.Rng.Next(EssencePillsOf(e)) < bad;
            Remove(e);
            if (poison)
            {
                poisoned.Remove(poisoned.First(p => p.Element == e));
                ctx.Log.Info($"[Alchemy] {member.FullName} swallows a poisoned pill at the Foundation wall: an opposed essence.");
                return PillTaken.Poisoned;
            }
            ctx.Log.Info($"[Alchemy] {member.FullName} swallows an Essence Gathering Pill ({e}) at the Foundation wall.");
            return PillTaken.Pure;
        }

        private void Remove(Element e)
        {
            essencePills[e]--;
            if (essencePills[e] == 0) essencePills.Remove(e);
        }

        /// <summary>A power turns one of the clan's true pills of this element into its poison. False when there is none to turn.</summary>
        public bool Poison(Element element, string power)
        {
            if (EssencePillsOf(element) <= poisoned.Count(p => p.Element == element)) return false;
            poisoned.Add(new PoisonedPill(element, power));
            ctx.Log.Info($"[Alchemy] {power} slips a poisoned pill ({element}) into the clan's store.");
            return true;
        }

        /// <summary>Each year the most hostile power may poison one of the clan's pills (audit §2.7, 🔎).</summary>
        public void ProcessYear()
        {
            var pill = Pill;
            var held = essencePills.Where(p => p.Value > poisoned.Count(x => x.Element == p.Key)).Select(p => p.Key).OrderBy(e => e).ToList();
            if (held.Count == 0) return;
            var foe = factions.Factions.Where(f => f.RelationWithPlayer <= pill.TaintRelation).OrderBy(f => f.RelationWithPlayer).FirstOrDefault();
            if (foe == null || !ctx.Rng.Chance(pill.TaintChance)) return;
            Poison(held[ctx.Rng.Next(held.Count)], foe.Name);
        }

        /// <summary>Why this member cannot examine the store's pills (French), or null: an adept of alchemy tells a poisoned one.</summary>
        public string ExamineRefusal(CharacterData alchemist)
        {
            if (alchemist == null || !alchemist.IsAlive || alchemist.CaptorFaction != null || alchemist.Retreat != Retreat.None) return "cet alchimiste ne peut travailler";
            if (essencePills.Count == 0) return "le clan n'a aucune pilule à examiner";
            if (!arts.HoldsLegacy(ImmortalArt.Alchemy)) return "le clan ne tient pas l'héritage de l'alchimie";
            if (!ImmortalArtRules.MayPractise(alchemist, ImmortalArt.Alchemy, ctx.Content.Balance.Arts)) return "il n'a pas le don de l'alchimie (sans don, il faut le Manoir Pourpre)";
            if (ArtSystem.MasteryOf(alchemist, ImmortalArt.Alchemy) < ctx.Content.Balance.Arts.AdeptAt) return "il faut un adepte de l'alchimie pour reconnaître un poison";
            if (alchemist.LastOperationYear == ctx.Clock.Year) return "cet alchimiste a déjà œuvré cette année";
            return null;
        }

        /// <summary>
        /// An adept examines the store (its year's work): the poisoned pills are thrown away, and the clan knows whose they were.
        /// Null when done, else why not (French).
        /// </summary>
        public string Examine(string alchemistId, out IReadOnlyList<string> culprits)
        {
            culprits = new List<string>();
            var alchemist = clan.FindById(alchemistId);
            if (ExamineRefusal(alchemist) is { } why) return why;
            alchemist.LastOperationYear = ctx.Clock.Year;
            culprits = poisoned.Select(p => p.Power).Distinct().OrderBy(p => p, System.StringComparer.Ordinal).ToList();
            foreach (var p in poisoned) Remove(p.Element);
            poisoned.Clear();
            foreach (var power in culprits) suspicion.AddClanDistrust(power, Pill.CaughtDistrust);
            ctx.Log.Info($"[Alchemy] {alchemist.FullName} examines the pills: {(culprits.Count == 0 ? "none is poisoned" : "poison of " + string.Join(", ", culprits))}.");
            return null;
        }

        /// <summary>Why this member cannot refine an Essence Gathering Pill now (French), or null.</summary>
        public string RefineRefusal(CharacterData alchemist)
        {
            if (alchemist == null || !alchemist.IsAlive || alchemist.CaptorFaction != null || alchemist.Retreat != Retreat.None) return "cet alchimiste ne peut travailler";
            if (!arts.HoldsLegacy(ImmortalArt.Alchemy)) return "le clan ne tient pas l'héritage de l'alchimie";
            if (!ImmortalArtRules.MayPractise(alchemist, ImmortalArt.Alchemy, ctx.Content.Balance.Arts)) return "il n'a pas le don de l'alchimie (sans don, il faut le Manoir Pourpre)";
            if (ArtSystem.MasteryOf(alchemist, ImmortalArt.Alchemy) < Pill.Mastery) return "il n'a pas encore appris l'alchimie";
            if (alchemist.LastOperationYear == ctx.Clock.Year) return "cet alchimiste a déjà œuvré cette année";
            if (resources.MedicinalHerbs < Pill.Herbs || resources.SpiritStones < Pill.Stones) return $"il faut {Pill.Herbs} herbes et {Pill.Stones} pierres";
            return null;
        }

        /// <summary>An alchemist refines an Essence Gathering Pill of this element. Null when done, else why not (French).</summary>
        public string RefineEssencePill(string alchemistId, Element element)
        {
            var alchemist = clan.FindById(alchemistId);
            if (RefineRefusal(alchemist) is { } why) return why;
            resources.ConsumeHerbs(Pill.Herbs);
            resources.ConsumeSpiritStones(Pill.Stones);
            alchemist.LastOperationYear = ctx.Clock.Year;
            GainEssencePills(element, 1);
            ctx.Log.Info($"[Alchemy] {alchemist.FullName} refines an Essence Gathering Pill ({element}).");
            return null;
        }

        // ---- The other pills (audit §2.2, §2.8) ----

        public IReadOnlyDictionary<PillKind, int> Pills => pills;

        public int PillsOf(PillKind kind) => pills.TryGetValue(kind, out int n) ? n : 0;

        public void GainPills(PillKind kind, int count)
        {
            if (count > 0) pills[kind] = PillsOf(kind) + count;
        }

        /// <summary>A pill is swallowed. False when the clan has none.</summary>
        public bool TakePill(PillKind kind)
        {
            if (PillsOf(kind) == 0) return false;
            pills[kind]--;
            if (pills[kind] == 0) pills.Remove(kind);
            return true;
        }

        public PillDefinition Definition(PillKind kind) => ctx.Content.Balance.Arts.Pills.FirstOrDefault(p => p.Kind == kind);

        public string PillName(PillKind kind) => Definition(kind)?.Name ?? kind.ToString();

        /// <summary>Why this member cannot refine this pill now (French), or null: the art's legacy and gift, the pill's mastery, a free year, herbs and stones.</summary>
        public string PillRefusal(CharacterData alchemist, PillKind kind)
        {
            var def = Definition(kind);
            if (def == null) return "pilule inconnue";
            if (alchemist == null || !alchemist.IsAlive || alchemist.CaptorFaction != null || alchemist.Retreat != Retreat.None) return "cet alchimiste ne peut travailler";
            if (!arts.HoldsLegacy(ImmortalArt.Alchemy)) return "le clan ne tient pas l'héritage de l'alchimie";
            if (!ImmortalArtRules.MayPractise(alchemist, ImmortalArt.Alchemy, ctx.Content.Balance.Arts)) return "il n'a pas le don de l'alchimie (sans don, il faut le Manoir Pourpre)";
            if (ArtSystem.MasteryOf(alchemist, ImmortalArt.Alchemy) < def.Mastery)
                return $"il faut un {ImmortalArtRules.Rank(def.Mastery, ctx.Content.Balance.Arts)} de l'alchimie pour cette pilule";
            if (alchemist.LastOperationYear == ctx.Clock.Year) return "cet alchimiste a déjà œuvré cette année";
            if (resources.MedicinalHerbs < def.Herbs || resources.SpiritStones < def.Stones) return $"il faut {def.Herbs} herbes et {def.Stones} pierres";
            return null;
        }

        /// <summary>An alchemist refines this pill (its year's work). Null when done, else why not (French).</summary>
        public string RefinePill(string alchemistId, PillKind kind)
        {
            var alchemist = clan.FindById(alchemistId);
            if (PillRefusal(alchemist, kind) is { } why) return why;
            var def = Definition(kind);
            resources.ConsumeHerbs(def.Herbs);
            resources.ConsumeSpiritStones(def.Stones);
            alchemist.LastOperationYear = ctx.Clock.Year;
            GainPills(kind, 1);
            ctx.Log.Info($"[Alchemy] {alchemist.FullName} refines a {kind} pill.");
            return null;
        }

        /// <summary>Why this power will not sell this pill for these terms (French), or null: it knows alchemy, is friendly enough, is paid.</summary>
        public string BuyRefusal(string powerName, PillKind kind, IReadOnlyList<AccordTerm> terms)
        {
            var def = Definition(kind);
            if (def == null) return "pilule inconnue";
            var power = factions.GetFactionByName(powerName);
            if (power == null) return "puissance inconnue";
            if (!PowerArts.Knows(power, ImmortalArt.Alchemy, ctx.Content)) return $"{power.Name} ne connaît pas l'alchimie";
            if (power.RelationWithPlayer < ctx.Content.Balance.Arts.PillBuyRelation) return $"il faut une relation de {ctx.Content.Balance.Arts.PillBuyRelation} au moins";
            if (!def.Precious) return resources.SpiritStones < def.Worth ? $"il faut {def.Worth} pierres" : null;
            if (Accords == null) return "aucun accord possible";
            return Accords.BarterRefusal(power.Name, def.Worth, precious: true, terms);
        }

        /// <summary>The clan buys a pill of a power (stones, or in kind for a Purple Mansion's). Null when done, else why not (French).</summary>
        public string BuyPill(string powerName, PillKind kind, IReadOnlyList<AccordTerm> terms)
        {
            if (BuyRefusal(powerName, kind, terms) is { } why) return why;
            var def = Definition(kind);
            if (def.Precious) Accords.Barter(powerName, terms, def.Name);
            else
            {
                resources.ConsumeSpiritStones(def.Worth);
                factions.GetFactionByName(powerName).Wealth += def.Worth;
            }
            GainPills(kind, 1);
            ctx.Log.Info($"[Alchemy] The clan buys a {kind} pill of {powerName}.");
            return null;
        }

        public void Restore(IReadOnlyDictionary<Element, int> saved, IEnumerable<PoisonedPill> savedPoison = null, IReadOnlyDictionary<PillKind, int> savedPills = null)
        {
            essencePills.Clear();
            poisoned.Clear();
            pills.Clear();
            foreach (var (k, n) in savedPills ?? new Dictionary<PillKind, int>()) GainPills(k, n);
            foreach (var (e, n) in saved ?? new Dictionary<Element, int>()) GainEssencePills(e, n);
            foreach (var p in savedPoison ?? Enumerable.Empty<PoisonedPill>())
                if (EssencePillsOf(p.Element) > poisoned.Count(x => x.Element == p.Element)) poisoned.Add(p);
        }
    }
}

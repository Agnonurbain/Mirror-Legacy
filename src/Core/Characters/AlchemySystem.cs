using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The clan's alchemy (AUDIT_LORE.md §2.6-2.7, the user's decision 2026-10-04): its store of Essence Gathering Pills, each of
    /// an element, and the alchemists who refine them — the gifted (or the Purple Mansion) of a clan that holds alchemy's
    /// legacy, one work a year, from herbs and stones. The Foundation wall takes a pill of the cultivator's Qi element; without
    /// one it is a gamble (📚 wiki Li_Chenghui; a pill of another essence serves nothing, Li_Chengliao).
    /// </summary>
    public sealed class AlchemySystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly ArtSystem arts;
        private readonly Dictionary<Element, int> essencePills = new Dictionary<Element, int>();

        public AlchemySystem(GameContext ctx, ClanManager clan, ResourceManager resources, ArtSystem arts)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.arts = arts;
        }

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

        /// <summary>A pill of the member's element is swallowed at the wall. False when the clan has none.</summary>
        public bool TakeEssencePill(CharacterData member)
        {
            if (ElementFor(member, ctx.Content) is not { } e || EssencePillsOf(e) == 0) return false;
            essencePills[e]--;
            if (essencePills[e] == 0) essencePills.Remove(e);
            ctx.Log.Info($"[Alchemy] {member.FullName} swallows an Essence Gathering Pill ({e}) at the Foundation wall.");
            return true;
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

        public void Restore(IReadOnlyDictionary<Element, int> saved)
        {
            essencePills.Clear();
            if (saved == null) return;
            foreach (var (e, n) in saved) GainEssencePills(e, n);
        }
    }
}

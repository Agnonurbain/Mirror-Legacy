using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The marks the clan's stolen arts keep of their power (AUDIT_LORE.md §3.9, 2026-10-04; 📚 the mirror rewrites a sect's
    /// arts to remove their personal flairs). An art taken by theft or as the loot of war bears its power's style: the power
    /// may recognize it and gain proof; the mirror cleanses it. An art bought, traded or given bears none.
    /// </summary>
    public sealed class TechniqueMarks
    {
        private readonly GameContext ctx;
        private readonly MirrorSystem mirror;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly Dictionary<string, string> marks = new Dictionary<string, string>(); // art → the power it was taken from

        public TechniqueMarks(GameContext ctx, MirrorSystem mirror, FactionManager factions, SuspicionLedger suspicion)
        {
            this.ctx = ctx;
            this.mirror = mirror;
            this.factions = factions;
            this.suspicion = suspicion;
            ctx.Events.OnManualStolen += (power, art) => { if (art != null && power != null) marks[art] = power; };
        }

        private TechniqueMarkSettings Settings => ctx.Content.Balance.TechniqueMarks;

        public IReadOnlyDictionary<string, string> Marks => marks;

        /// <summary>The power whose mark the art bears, or null.</summary>
        public string MarkOf(string art) => art != null && marks.TryGetValue(art, out var power) ? power : null;

        public void Restore(IDictionary<string, string> saved)
        {
            marks.Clear();
            foreach (var kv in saved ?? new Dictionary<string, string>()) marks[kv.Key] = kv.Value;
        }

        /// <summary>Each year, a power may recognize its style in an art stolen from it.</summary>
        public void ProcessYear()
        {
            foreach (var (art, power) in marks.ToList())
            {
                if (factions.GetFactionByName(power) == null) { marks.Remove(art); continue; } // no one left to recognize it
                if (!ctx.Rng.Chance(Settings.RecognizeChance)) continue;
                suspicion.AddEvidence(power, Settings.Evidence);
                ctx.Log.Info($"[Marks] {power} recognizes its own style in an art of the clan.");
            }
        }

        /// <summary>The mirror scrubs the art of its power's mark. Null when done; else why not (French).</summary>
        public string Cleanse(string art)
        {
            if (MarkOf(art) == null) return "cet art ne porte aucune marque";
            if (mirror.PayRefusal(Settings.CleanseCost) is { } refusal) return refusal;
            mirror.ConsumePower(Settings.CleanseCost);
            marks.Remove(art);
            ctx.Log.Info("[Marks] The mirror scrubs a stolen art of its origin's flair.");
            return null;
        }
    }
}

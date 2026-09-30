using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Mirror
{
    /// <summary>
    /// The mirror's shards (LORE.md §11.5, B3c; the user's decisions of 2026-09-30): seven pieces of jade — one in the lake,
    /// two in ruins, three held by powers, one in the Great Void. Each one recovered restores the mirror, gives back its
    /// memory (a technique, facts, a clue to the mirror's origin), and puts the spirit to sleep while it integrates it.
    /// The lake's shard is dredged up by the clan's searchers (B3c1); a discovery of ruins may reveal ruins holding one,
    /// which an expedition brings back (B3c2).
    /// </summary>
    public sealed class ShardSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly MirrorSystem mirror;
        private readonly TechniqueLibrary techniques;
        private readonly KnowledgeBase knowledge;
        private readonly WoundSystem wounds;
        private readonly List<string> recovered = new List<string>();
        private readonly List<string> revealedRuins = new List<string>();

        public IReadOnlyList<string> Recovered => recovered;

        /// <summary>The ruins the clan knows to hold a shard still.</summary>
        public IReadOnlyList<string> RevealedRuins => revealedRuins;

        public ShardSystem(GameContext ctx, ClanManager clan, MirrorSystem mirror, TechniqueLibrary techniques, KnowledgeBase knowledge,
            WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.mirror = mirror;
            this.techniques = techniques;
            this.knowledge = knowledge;
            this.wounds = wounds;

            ctx.Events.OnRandomEventOccurred += e => { if (e.EventType == RandomEventType.RuinsDiscovery) MaybeRevealRuins(); };
        }

        private ShardSettings Settings => ctx.Content.Balance.Shards;

        public bool IsRecovered(string shardId) => recovered.Contains(shardId);

        /// <summary>The lake is searched until its shard is found.</summary>
        public bool LakeSearchOpen => LakeShard != null && !IsRecovered(LakeShard.Id);

        private ShardDefinition LakeShard => ctx.Content.Shards.FirstOrDefault(s => s.Source == ShardSource.Lake);

        /// <summary>
        /// A shard comes back to the mirror: it grows, sleeps to integrate it, and gives back its memory. False when the
        /// shard is unknown or already recovered.
        /// </summary>
        public bool Recover(string shardId)
        {
            var shard = ctx.Content.Shards.FirstOrDefault(s => s.Id == shardId);
            if (shard == null || IsRecovered(shardId)) return false;

            recovered.Add(shardId);
            revealedRuins.Remove(shardId);
            mirror.RestoreShard(shard.SleepYears);
            if (shard.Memory.TechniqueId != null) techniques.Learn(shard.Memory.TechniqueId);
            foreach (var fact in shard.Memory.Facts ?? System.Array.Empty<string>())
                knowledge.Reveal(Fact.Parse(fact), KnowledgeSource.Mirror);

            ctx.Log.Info($"[Shards] The mirror recovers {shard.Name} ({recovered.Count}/{ctx.Content.Shards.Count}); it sleeps {shard.SleepYears} year(s).");
            ctx.Events.TriggerShardRecovered(shard);
            return true;
        }

        /// <summary>A year of dredging the lake: each searcher may find its shard (🔎 balance.json « shards »).</summary>
        public void SearchLake(int searchers)
        {
            if (searchers <= 0 || !LakeSearchOpen) return;
            double chance = 1 - System.Math.Pow(1 - Settings.LakeSearchChance, searchers);
            if (ctx.Rng.Chance(chance)) Recover(LakeShard.Id);
        }

        // ---- The ruins (B3c2) ----

        /// <summary>A discovery of ruins may lead to the next ruins that hold a shard (🔎 balance.json « shards »).</summary>
        private void MaybeRevealRuins()
        {
            var next = ctx.Content.Shards.FirstOrDefault(s => s.Source == ShardSource.Ruins && !IsRecovered(s.Id) && !revealedRuins.Contains(s.Id));
            if (next == null || !ctx.Rng.Chance(Settings.RuinsRevealChance)) return;
            revealedRuins.Add(next.Id);
            ctx.Log.Info($"[Shards] Ancient ruins are found: {next.Name} lies there.");
            ctx.Events.TriggerRuinsRevealed(next);
        }

        /// <summary>Why an expedition cannot leave (French, for the screens), or null.</summary>
        public string ExpeditionRefusal(string shardId, IReadOnlyList<string> teamIds, out List<CharacterData> team)
        {
            team = (teamIds ?? new List<string>()).Select(clan.FindById).ToList();
            var s = Settings;
            if (!revealedRuins.Contains(shardId)) return "ces ruines ne sont pas connues";
            if (team.Count == 0 || team.Count > s.ExpeditionMaxTeam) return $"une expédition part à 1 à {s.ExpeditionMaxTeam} membres";
            if (team.Any(m => m == null || !m.IsAlive || m.CaptorFaction != null)) return "un membre de l'équipe n'est pas libre";
            if (team.Any(m => m.Realm < CultivationRealm.QiRefinement)) return "l'expédition demande des cultivateurs de la Culture du Qi";
            if (team.Any(m => m.LastOperationYear == ctx.Clock.Year)) return "un membre de l'équipe a déjà mené une opération cette année";
            return null;
        }

        /// <summary>
        /// An expedition to ruins that hold a shard: the team measures itself against the ruins' guardian (its strongest
        /// member, and a share of the others); success brings the shard back; failure may kill the weakest and wound the rest.
        /// </summary>
        public ExpeditionOutcome Expedition(string shardId, IReadOnlyList<string> teamIds)
        {
            string refusal = ExpeditionRefusal(shardId, teamIds, out var team);
            if (refusal != null)
            {
                ctx.Log.Warning($"[Shards] The expedition does not leave: {refusal}.");
                return new ExpeditionOutcome(false, false, refusal);
            }
            foreach (var member in team) member.LastOperationYear = ctx.Clock.Year;

            var shard = ctx.Content.Shards.First(x => x.Id == shardId);
            if (ctx.Rng.Chance(ExpeditionChance(team, shard)))
            {
                Recover(shardId);
                return new ExpeditionOutcome(true, true, null);
            }

            var s = Settings;
            var weakest = team.OrderBy(m => HuntRules.Power(m.Realm, m.RealmStage)).First();
            if (ctx.Rng.Chance(s.ExpeditionDeathChance)) clan.Kill(weakest, DeathCause.Combat);
            foreach (var member in team.Where(m => m.IsAlive))
                if (ctx.Rng.Chance(s.ExpeditionWoundChance)) wounds.ApplyDaoWound(member);
            ctx.Log.Info($"[Shards] The expedition to {shard.Name} fails.");
            return new ExpeditionOutcome(true, false, null);
        }

        /// <summary>The expedition's odds against the ruins' guardian (🔎 balance.json « shards »).</summary>
        public double ExpeditionChance(IReadOnlyList<CharacterData> team, ShardDefinition shard)
        {
            var s = Settings;
            var powers = team.Select(m => (double)HuntRules.Power(m.Realm, m.RealmStage)).OrderByDescending(p => p).ToList();
            double strength = powers[0] + powers.Skip(1).Sum() * s.ExpeditionHelpShare;
            double guardian = HuntRules.Power(shard.GuardRealm, 5);
            return System.Math.Clamp(s.ExpeditionBaseChance + (strength - guardian) * s.ExpeditionChancePerPower, s.ExpeditionMinChance, s.ExpeditionMaxChance);
        }

        public void RestoreRuins(IEnumerable<string> saved)
        {
            revealedRuins.Clear();
            revealedRuins.AddRange((saved ?? Enumerable.Empty<string>())
                .Where(id => ctx.Content.Shards.Any(s => s.Id == id && s.Source == ShardSource.Ruins) && !IsRecovered(id)).Distinct());
        }

        public void Restore(IEnumerable<string> saved)
        {
            recovered.Clear();
            recovered.AddRange((saved ?? Enumerable.Empty<string>()).Where(id => ctx.Content.Shards.Any(s => s.Id == id)).Distinct());
        }
    }

    /// <summary>What an expedition did: whether it left, whether it found the shard, and why it could not leave.</summary>
    public sealed record ExpeditionOutcome(bool Launched, bool Found, string Refusal);
}

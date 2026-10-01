using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>A power the clan absorbed (saved): who, what, where, and when — the endings count it as bowed.</summary>
    public sealed record AbsorbedPower(string Name, FactionKind Kind, string RegionId, int Year);

    /// <summary>
    /// The clan absorbs its vassals (the user's decision, 2026-09-30; P1: a suzerain's rules over the clan, turned the other
    /// way). Once the clan's grip on a vassal has filled often enough (<see cref="TreatySystem"/>), the player may absorb it:
    /// its wealth, its arts, a few cultivators who take the clan's name (📚 the novel's Lu family), what it knew of others'
    /// secrets, and a shard it held. Every other power watches the clan grow, with a silent distrust (🔎 balance.json « treaties »).
    /// </summary>
    public sealed class ClanAbsorption
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly TreatySystem treaties;
        private readonly SuspicionLedger suspicion;
        private readonly TechniqueLibrary techniques;
        private readonly PowerShards powerShards;
        private readonly ShardSystem shards;
        private readonly List<AbsorbedPower> absorbed = new List<AbsorbedPower>();

        public IReadOnlyList<AbsorbedPower> Absorbed => absorbed;

        public ClanAbsorption(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, TreatySystem treaties,
            SuspicionLedger suspicion, TechniqueLibrary techniques, PowerShards powerShards, ShardSystem shards)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.treaties = treaties;
            this.suspicion = suspicion;
            this.techniques = techniques;
            this.powerShards = powerShards;
            this.shards = shards;
        }

        private TreatySettings Settings => ctx.Content.Balance.Treaties;

        /// <summary>Why the clan cannot absorb this power now (French, for the screens), or null.</summary>
        public string Refusal(string power)
        {
            var treaty = treaties.All.FirstOrDefault(t => t.Kind == TreatyKind.Vassalage && t.ClanIsSuzerain && t.Faction == power);
            if (treaty == null || factions.GetFactionByName(power) == null) return $"{power} n'est pas un vassal du clan";
            int steps = ctx.Content.Balance.Politics.ClanAbsorptionSteps;
            return treaty.Absorptions < steps ? $"l'emprise du clan sur {power} n'est pas encore complète ({treaty.Absorptions}/{steps})" : null;
        }

        /// <summary>The clan absorbs a vassal whose grip is complete. Null when done, else why not (French).</summary>
        public string Absorb(string name)
        {
            string refusal = Refusal(name);
            if (refusal != null) return refusal;
            var power = factions.GetFactionByName(name);

            foreach (var shard in ctx.Content.Shards.Where(s => s.Source == ShardSource.Power && powerShards.HolderOf(s.Id) == name).ToList())
                shards.Recover(shard.Id); // its treasure is the clan's now
            resources.AddSpiritStones(System.Math.Max(0, power.Wealth));
            foreach (var art in power.Techniques.Where(id => ctx.Content.Techniques.Any(t => t.ID == id))) techniques.Learn(art);
            for (int i = 0; i < Settings.AbsorbedJoiners; i++) clan.AddMember(Joiner(power, i));
            foreach (var witness in factions.Factions.Where(f => f.Name != name).ToList())
            {
                suspicion.AddToClan(witness.Name, Settings.AbsorbSuspicion);
                suspicion.AddDistrust(witness.Name, SecretBook.ClanHolder, Settings.AbsorbDistrust);
            }
            absorbed.Add(new AbsorbedPower(power.Name, power.Kind, power.RegionId, ctx.Clock.Year));
            factions.RemoveFaction(name);
            ctx.Log.Warning($"[Absorption] The clan absorbs {name}.");
            ctx.Events.TriggerPowerAbsorbed(name, SecretBook.ClanHolder); // its treaties end, its secrets and beasts pass on
            return null;
        }

        /// <summary>A cultivator of the absorbed power who takes the clan's name: the strongest near its best, the others at the Qi Cultivation.</summary>
        private CharacterData Joiner(FactionData power, int index)
        {
            var rng = ctx.Rng;
            bool isMale = rng.Next(2) == 0;
            var names = isMale ? ctx.Content.Names.Male : ctx.Content.Names.Female;
            var realm = index == 0 && power.HighestRealm > CultivationRealm.QiRefinement ? power.HighestRealm - 1 : CultivationRealm.QiRefinement;
            var joiner = new CharacterData
            {
                ID = rng.NextId(),
                FirstName = names.Count == 0 ? "Sans-Nom" : names[rng.Next(names.Count)],
                LastName = clan.ClanName, // they abandon their name (📚)
                IsMale = isMale,
                Age = rng.Next(20, 61),
                HasSpiritualOrifice = true,
                OrificeKnown = true,
                Realm = realm,
                RealmStage = rng.Next(1, 5),
                SpiritualRoot = rng.Next(30, 71),
                Temperament = FoundationRules.RandomTemperament(rng)
            };
            if (joiner.Realm >= CultivationRealm.Foundation) joiner.FoundationId = ctx.Content.Qi.FirstOrDefault(q => q.Foundation != null && !q.Vanished)?.Foundation; // a foundation of its own
            PowerLadder.Normalize(joiner);
            PowerLadder.NormalizeLifespan(joiner);
            joiner.MaxLifespan = System.Math.Max(joiner.MaxLifespan, joiner.Age + 10);
            techniques.NormalizeMember(joiner);
            return joiner;
        }

        public void Restore(IEnumerable<AbsorbedPower> saved)
        {
            absorbed.Clear();
            absorbed.AddRange(saved ?? Enumerable.Empty<AbsorbedPower>());
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The spirit beasts of the world (L2c.2; user decision, 2026-09-26). Each place of the map has its beasts,
    /// drawn from the world's seed at the start of a game: where powers live, most belong to one of them; elsewhere
    /// they are solitary, rarer and stronger. The clan hunts only those it has scouted.
    /// </summary>
    public sealed class BeastRegistry
    {
        private static readonly CultivationRealm[] Realms = { CultivationRealm.QiRefinement, CultivationRealm.Foundation, CultivationRealm.PurpleMansion };

        private readonly GameContext ctx;
        private readonly List<WorldBeast> beasts = new List<WorldBeast>();

        public BeastRegistry(GameContext ctx)
        {
            this.ctx = ctx;
        }

        public IReadOnlyList<WorldBeast> Beasts => beasts;

        public IEnumerable<WorldBeast> In(string regionId) => beasts.Where(b => b.RegionId == regionId);

        /// <summary>The world's own source for the beasts of a game seed (separate from the game's draws).</summary>
        public static Random WorldRandom(int seed) => new Random(unchecked(seed * 6007 + 90001));

        /// <summary>Places the beasts of a new world, in the content's order: the same seed, the same beasts.</summary>
        public void Draw(Random worldRng)
        {
            beasts.Clear();
            var s = ctx.Content.Balance.Bestiary;
            foreach (var region in ctx.Content.Regions.Where(r => r.ParentId != null))
            {
                int count = s.BeastsByKind.TryGetValue(region.Kind, out int n) ? n : 1;
                var species = ctx.Content.BeastSpecies.Where(b => b.Habitats.Contains(region.Kind)).ToList();
                if (species.Count == 0) species = ctx.Content.BeastSpecies.ToList();
                var powers = ctx.Content.Factions.Where(f => f.RegionId == region.Id).ToList();

                for (int i = 0; i < count; i++)
                {
                    var kind = species[worldRng.Next(species.Count)];
                    string owner = powers.Count > 0 && worldRng.NextDouble() < s.OwnedShare ? powers[worldRng.Next(powers.Count)].Name : null;
                    var realm = DrawRealm(worldRng, s);
                    if (owner == null && powers.Count == 0) realm = (CultivationRealm)Math.Max((int)realm, (int)DrawRealm(worldRng, s)); // the wild's solitary beasts are stronger
                    int stage = 1 + worldRng.Next(PowerLadder.StageCount(realm));
                    beasts.Add(new WorldBeast($"{kind.Id}-{region.Id}-{i + 1}", kind.Id, region.Id, realm, stage, owner));
                }
            }
        }

        /// <summary>Restores the beasts of a save.</summary>
        public void Restore(IEnumerable<WorldBeast> saved)
        {
            beasts.Clear();
            beasts.AddRange((saved ?? Enumerable.Empty<WorldBeast>()).Where(b => b != null));
        }

        /// <summary>A beast taken from the world (captured); false when it is not there.</summary>
        public bool Take(WorldBeast beast) => beasts.Remove(beast);

        private static CultivationRealm DrawRealm(Random rng, BestiarySettings s)
        {
            double roll = rng.NextDouble(), sum = 0;
            for (int i = 0; i < Realms.Length; i++)
            {
                sum += s.RealmOdds[i];
                if (roll < sum) return Realms[i];
            }
            return Realms[Realms.Length - 1];
        }
    }
}

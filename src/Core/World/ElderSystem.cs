using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Session;

namespace MirrorChronicles.World
{
    /// <summary>
    /// The powers' elders (the living world, step A, user decision 2026-10-01). Each power counts the named figures of
    /// figures.json and elders the world draws: at least one at its highest realm, and cadets by its kind. Each year they
    /// age and die at their lifespan's end; a cadet rises a realm in time; a power below its ranks raises a new cadet.
    /// A Purple Mansion at its peak is precious to any faction: it tries the Golden Core only from good odds or in its
    /// last years, and a failure births a demon that kills it. A power's highest realm follows its elders — losing its only
    /// Purple Mansion, it falls.
    /// </summary>
    public sealed class ElderSystem
    {
        private readonly GameContext ctx;
        private readonly FactionManager factions;

        public ElderSystem(GameContext ctx, FactionManager factions)
        {
            this.ctx = ctx;
            this.factions = factions;
        }

        private ElderSettings Settings => ctx.Content.Balance.Elders;

        private EssencePillSettings Pill => ctx.Content.Balance.Arts.EssencePill;

        /// <summary>The world's own draw (it never shifts the game's).</summary>
        public static Random WorldRandom(int seed) => new Random(unchecked(seed * 7919 + 17));

        /// <summary>Gives every power without elders its own: its figures, an elder at its highest realm, its cadets.</summary>
        public void Populate(Random worldRng)
        {
            int year = ctx.Clock.Year;
            foreach (var power in factions.Factions.Where(f => f.Elders == null || f.Elders.Count == 0))
            {
                power.Elders ??= new List<FactionElder>();
                foreach (var figure in ctx.Content.Figures.Where(f => f.FactionName == power.Name))
                    power.Elders.Add(Elder(worldRng, figure.Id, figure.Name, figure.Realm, year, figure.BornYear));
                if (!power.Elders.Any(e => e.Realm >= power.HighestRealm))
                    power.Elders.Add(Elder(worldRng, null, DrawName(worldRng, power), power.HighestRealm, year, null));
                int cadets = Settings.CadetsByKind.TryGetValue(power.Kind, out int n) ? n : 2;
                for (int i = 0; i < cadets; i++)
                    power.Elders.Add(Elder(worldRng, null, DrawName(worldRng, power), Below(power.HighestRealm, worldRng), year, null));
                foreach (var elder in power.Elders) elder.Ancient = true; // there when the world began
                power.EssencePills = Pill.PowerStartPills;
                Sync(power);
            }
        }

        /// <summary>A new elder of the power at this realm (a risen family's founder, its cadets): drawn by the game.</summary>
        public FactionElder NewElder(FactionData power, CultivationRealm realm)
        {
            var elder = Elder(ctx.Rng, null, DrawName(ctx.Rng, power), realm, ctx.Clock.Year, null);
            elder.RealmSinceYear = ctx.Clock.Year;
            return elder;
        }

        /// <summary>A cadet is drawn one or two realms below its power's best, never below the Qi Cultivation.</summary>
        private static CultivationRealm Below(CultivationRealm top, Random rng) =>
            (CultivationRealm)Math.Max((int)CultivationRealm.QiRefinement, (int)top - 1 - rng.Next(2));

        private FactionElder Elder(Random rng, string figureId, string name, CultivationRealm realm, int year, int? bornYear)
        {
            int lifespan = PowerLadder.MaxLifespan(realm, 1);
            int age = bornYear.HasValue ? year - bornYear.Value : (int)(lifespan * (0.2 + rng.NextDouble() * 0.6));
            var elder = new FactionElder
            {
                Id = figureId ?? $"elder-{rng.Next():x8}",
                Name = name,
                FigureId = figureId,
                Realm = realm,
                Stage = 1 + rng.Next(4),
                BornYear = year - Math.Min(age, lifespan - 1), // a figure older than its lifespan still lives its last year
                MaxLifespan = lifespan,
                RealmSinceYear = year - rng.Next(Math.Max(1, MinYears(realm) * 2)),
            };
            if (realm == CultivationRealm.PurpleMansion) // one who held better odds would have risen before the world began
            {
                elder.GoldenCoreOdds = Math.Min(DrawOdds(rng), Settings.RiseOdds - 0.05);
                elder.Perfected = rng.NextDouble() < Settings.PerfectionChance;
            }
            return elder;
        }

        private double DrawOdds(Random rng) =>
            Settings.MinGoldenCoreOdds + rng.NextDouble() * (Settings.MaxGoldenCoreOdds - Settings.MinGoldenCoreOdds);

        private int MinYears(CultivationRealm realm) =>
            Settings.MinYearsInRealm[Math.Min((int)realm, Settings.MinYearsInRealm.Length - 1)];

        private string DrawName(Random rng, FactionData power)
        {
            var names = ctx.Content.Names;
            string given = names.Male.Count == 0 ? "Sans-Nom" : names.Male[rng.Next(names.Male.Count)];
            string family = power.FamilyName ?? (names.OutsiderFamilies.Count == 0 ? "" : names.OutsiderFamilies[rng.Next(names.OutsiderFamilies.Count)]);
            return $"{family} {given}".Trim();
        }

        public void ProcessYear()
        {
            int year = ctx.Clock.Year;
            foreach (var power in factions.Factions.ToList())
            {
                foreach (var elder in power.Elders.ToList())
                {
                    if (elder.Age(year) >= elder.MaxLifespan) Die(power, elder, demon: false);
                    else if (elder.Realm < CultivationRealm.PurpleMansion) Rise(power, elder, year);
                    else if (elder.Realm == CultivationRealm.PurpleMansion) TryTheGoldenCore(power, elder, year);
                }
                RefinePills(power);
                RaiseACadet(power, year);
                Sync(power);
            }
        }

        private void Rise(FactionData power, FactionElder elder, int year)
        {
            int realm = (int)elder.Realm;
            if (realm >= Settings.RiseChance.Length || year - elder.RealmSinceYear < MinYears(elder.Realm)) return;
            if (!ctx.Rng.Chance(Settings.RiseChance[realm])) return;
            if (elder.Realm == CultivationRealm.QiRefinement && !CrossesTheWall(power, elder)) return;
            Advance(power, elder, elder.Realm + 1, year);
            if (elder.Realm == CultivationRealm.PurpleMansion)
            {
                elder.GoldenCoreOdds = DrawOdds(ctx.Rng);
                elder.Perfected = ctx.Rng.NextDouble() < Settings.PerfectionChance; // most never gather their five abilities
            }
        }

        /// <summary>
        /// The Foundation wall, as the clan's (audit §2.6-2.7, parity): the elder takes a pill of its power's store — unless a
        /// rival poisoned it — else gambles; a failed wall may kill it.
        /// </summary>
        private bool CrossesTheWall(FactionData power, FactionElder elder)
        {
            if (power.EssencePills > 0)
            {
                power.EssencePills--;
                if (!ctx.Rng.Chance(Pill.WorldTaintChance)) return true;
                ctx.Log.Info($"[Elders] {elder.Name} of {power.Name} swallows a poisoned pill at the Foundation wall.");
            }
            else if (ctx.Rng.Chance(Pill.ElderWithoutPillFactor)) return true;
            if (ctx.Rng.Chance(Pill.ElderWallDeathChance)) Die(power, elder, demon: false, "dies at the Foundation wall");
            return false;
        }

        /// <summary>A power's alchemists refine an Essence Gathering Pill now and then, up to its store's cap.</summary>
        private void RefinePills(FactionData power)
        {
            if (power.EssencePills >= Pill.PowerPillCap || !Pill.PowerPillChance.TryGetValue(power.Kind, out double chance)) return;
            if (ctx.Rng.Chance(chance)) power.EssencePills++;
        }

        /// <summary>
        /// A Grand Perfection dares the Golden Core from a lower bar — a race for a lineage freed (WorldFruitions): its
        /// odds decide; a failure births a demon. False when it was not ready to try.
        /// </summary>
        public bool Dare(FactionData power, FactionElder elder, double bar)
        {
            if (elder.Realm != CultivationRealm.PurpleMansion || !elder.Perfected || elder.GoldenCoreOdds < bar) return false;
            if (ctx.Rng.NextDouble() < elder.GoldenCoreOdds) Advance(power, elder, CultivationRealm.GoldenCore, ctx.Clock.Year);
            else Die(power, elder, demon: true);
            ElderSystem.Sync(power);
            return true;
        }

        /// <summary>What governing a kingdom adds to its sovereign's odds (the Imperial Way, 2026-10-03; set by the session).</summary>
        public Func<FactionData, FactionElder, double> GoverningBonus { get; set; }

        /// <summary>A precious Purple Mansion at its peak dares only from good odds, or with nothing left to lose.</summary>
        private void TryTheGoldenCore(FactionData power, FactionElder elder, int year)
        {
            if (elder.TreasureBound || !elder.Perfected || year - elder.RealmSinceYear < MinYears(CultivationRealm.PurpleMansion)) return; // bound: never further
            elder.GoldenCoreOdds = Math.Min(1.0, elder.GoldenCoreOdds + Settings.OddsGainPerYear); // it prepares
            double governing = GoverningBonus?.Invoke(power, elder) ?? 0;
            double odds = Math.Min(0.99, elder.GoldenCoreOdds + governing);
            bool lastYears = elder.MaxLifespan - elder.Age(year) <= Settings.LastYears;
            if (odds < Settings.RiseOdds && !lastYears) return;
            if (ctx.Rng.NextDouble() < odds)
            {
                elder.ImperialCore = governing > 0; // forged by governing: an imperial core
                Advance(power, elder, CultivationRealm.GoldenCore, year);
            }
            else Die(power, elder, demon: true);
        }

        private void Advance(FactionData power, FactionElder elder, CultivationRealm realm, int year)
        {
            elder.Realm = realm;
            elder.Stage = 1;
            elder.RealmSinceYear = year;
            elder.MaxLifespan = Math.Max(elder.MaxLifespan, PowerLadder.MaxLifespan(realm, 1));
            ctx.Log.Info($"[Elders] {elder.Name} of {power.Name} reaches the {realm}.");
            ctx.Events.TriggerElderRose(power, elder);
        }

        private void Die(FactionData power, FactionElder elder, bool demon, string how = null)
        {
            power.Elders.Remove(elder);
            ctx.Log.Info($"[Elders] {elder.Name} of {power.Name} {how ?? (demon ? "fails the Golden Core: a demon is born" : "dies of age")}.");
            ctx.Events.TriggerElderDied(power, elder, demon);
        }

        /// <summary>A power below its ranks raises a new cadet now and then: a Foundation of its disciples.</summary>
        private void RaiseACadet(FactionData power, int year)
        {
            int ranks = (Settings.CadetsByKind.TryGetValue(power.Kind, out int n) ? n : 2) + 1;
            if (power.Elders.Count >= ranks || !ctx.Rng.Chance(Settings.NewElderChance)) return;
            var cadet = Elder(ctx.Rng, null, DrawName(ctx.Rng, power), CultivationRealm.Foundation, year, null);
            cadet.RealmSinceYear = year;
            power.Elders.Add(cadet);
        }

        /// <summary>A power's highest realm is its strongest elder's; with none left, it falls to the Qi Cultivation.</summary>
        public static void Sync(FactionData power) =>
            power.HighestRealm = power.Elders.Count == 0 ? CultivationRealm.QiRefinement : power.Elders.Max(e => e.Realm);
    }
}

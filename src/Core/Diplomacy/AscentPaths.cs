using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>The other paths' odds (balance.json « ascentPaths »; LORE.md §11.10, B; 🔎).</summary>
    public sealed record AscentPathSettings
    {
        public int DiscipleYears { get; init; }           // years a disciple serves before coming back
        public int DiscipleMinRelation { get; init; }     // the trust a power asks to take a disciple
        public double DiscipleHarvestChance { get; init; } // each year: the power reaps the disciple (📚 the sect that refines its disciples)
        public double TombChance { get; init; }           // a discovery of ruins reveals a Purple Mansion's tomb
        public double DefectionChance { get; init; }      // each year, a cultivator of a power defects with its method
        public double FalseDefectorChance { get; init; }  // …and is a spy
        public int DefectionRelationLoss { get; init; }
    }

    /// <summary>
    /// The other paths to an ascent method (LORE.md §11.10, B; 2026-10-01): a disciple sent to a power that holds one (C2; he may be
    /// reaped), the loot of a yielding enemy (C4), a Purple Mansion's tomb revealed by a discovery of ruins and robbed by an
    /// expedition (C5), a stolen manual (C7), a defector who practises his method — or spies (C8). A disciple of a faction is bound by
    /// a Dao oath (the user's rule, 2026-10-01): he practises its method but cannot pass it on — neither the returning disciple nor
    /// the defector teaches it to the clan.
    /// The patrons (C3, C10) and the mirror (C9) are elsewhere; absorption gives every art of a vassal.
    /// </summary>
    public sealed class AscentPaths
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly TechniqueLibrary techniques;
        private readonly SuspicionLedger suspicion;
        private readonly ShardSystem shards;
        private readonly WoundSystem wounds;

        /// <summary>The method a known Purple Mansion's tomb keeps, or null.</summary>
        public string Tomb { get; private set; }

        public AscentPaths(GameContext ctx, ClanManager clan, FactionManager factions, TechniqueLibrary techniques, SuspicionLedger suspicion,
            ShardSystem shards, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.techniques = techniques;
            this.suspicion = suspicion;
            this.shards = shards;
            this.wounds = wounds;
            ctx.Events.OnClanWarWon += enemy =>
            {
                if (factions.GetFactionByName(enemy) is { } power && AscentMethodOf(power) is { } loot)
                {
                    techniques.Learn(loot.ID); // the loot of war (C4)
                    ctx.Events.TriggerManualStolen(power.Name, loot.ID); // it bears its power's mark (audit §3.9)
                }
            };
            ctx.Events.OnRandomEventOccurred += e => { if (e.EventType == RandomEventType.RuinsDiscovery) MaybeRevealTomb(); };
        }

        private AscentPathSettings Settings => ctx.Content.Balance.AscentPaths;
        private ShardSettings Ops => ctx.Content.Balance.Shards;

        private static bool IsAscent(TechniqueData t) => t != null && t.Kind == TechniqueKind.Cultivation && TechniqueRules.HasPurpleMansionSecret(t) && t.RequiredQiId != null;

        private TechniqueData AscentMethodOf(FactionData power) =>
            power.Techniques.Select(id => ctx.Content.Techniques.FirstOrDefault(t => t.ID == id)).Where(IsAscent)
                .Where(t => !techniques.Knows(t.ID)).OrderByDescending(t => t.Grade).FirstOrDefault();

        public void ProcessYear()
        {
            foreach (var disciple in clan.LivingMembers.Where(m => m.DiscipleOf != null).ToList()) Serve(disciple);
            MaybeDefect();
        }

        // ---- A disciple (C2) ----

        public string SendAsDisciple(string memberId, string powerName)
        {
            var member = clan.FindById(memberId);
            var power = factions.GetFactionByName(powerName);
            if (member == null || !member.IsAlive || member.CaptorFaction != null || member.DiscipleOf != null || member.ID == clan.PatriarchID
                || member.Realm < CultivationRealm.QiRefinement) return "ce membre ne peut partir comme disciple";
            if (power == null || AscentMethodOf(power) == null) return "cette puissance n'enseigne aucune méthode qui mène au Manoir Pourpre";
            if (power.RelationWithPlayer < Settings.DiscipleMinRelation) return $"la relation est trop froide ({Settings.DiscipleMinRelation} requise)";
            member.DiscipleOf = power.Name;
            member.DiscipleUntil = ctx.Clock.Year + Settings.DiscipleYears;
            member.CurrentTask = TaskType.None;
            ctx.Log.Info($"[Paths] {member.FullName} leaves to serve {power.Name} as a disciple.");
            return null;
        }

        private void Serve(CharacterData disciple)
        {
            var power = factions.GetFactionByName(disciple.DiscipleOf);
            if (power == null) { disciple.DiscipleOf = null; return; } // the power is no more: home
            if (ctx.Rng.Chance(Settings.DiscipleHarvestChance))
            {
                ctx.Log.Warning($"[Paths] {power.Name} reaps its disciple {disciple.FullName}.");
                clan.Kill(disciple, DeathCause.FoundationDevoured);
                return;
            }
            if (ctx.Clock.Year < disciple.DiscipleUntil) return;
            disciple.DiscipleOf = null;
            if (AscentMethodOf(power) is { } method) Practise(disciple, method); // sworn: he practises it, the clan does not learn it
            ctx.Log.Info($"[Paths] {disciple.FullName} comes home from {power.Name}.");
        }

        // ---- A stolen manual (C7) ----

        public ShardTaking StealManual(string powerName, string techniqueId, IReadOnlyList<string> teamIds)
        {
            var power = factions.GetFactionByName(powerName);
            var ids = teamIds ?? new List<string>();
            var team = ids.Distinct().Select(clan.FindById).ToList();
            string refusal = power == null || !power.Techniques.Contains(techniqueId) || techniques.Knows(techniqueId) ? "il n'y a rien à voler là"
                : ids.Distinct().Count() != ids.Count || team.Count == 0 || team.Count > Ops.ExpeditionMaxTeam ? $"un vol se mène à 1 à {Ops.ExpeditionMaxTeam} membres"
                : team.Any(m => m == null || !m.IsAlive || m.CaptorFaction != null || m.DiscipleOf != null || m.Realm < CultivationRealm.QiRefinement) ? "un membre de l'équipe ne peut partir"
                : team.Any(m => m.LastOperationYear == ctx.Clock.Year) ? "un membre de l'équipe a déjà mené une opération cette année"
                : null;
            if (refusal != null) return ShardTaking.Refused(refusal);
            foreach (var m in team) m.LastOperationYear = ctx.Clock.Year;
            if (ctx.Rng.Chance(Chance(team, power.HighestRealm, Ops.TheftBaseChance, Ops.TheftChancePerPower, Ops.TheftMinChance, Ops.TheftMaxChance)))
            {
                techniques.Learn(techniqueId);
                return new ShardTaking(true, true, false, null);
            }
            if (!ctx.Rng.Chance(Ops.TheftCaughtChance)) return new ShardTaking(true, false, false, null);
            suspicion.AddEvidence(power.Name, Ops.TheftEvidence);
            suspicion.AddToClan(power.Name, Ops.CaughtClues);
            return new ShardTaking(true, false, true, null);
        }

        /// <summary>The odds of stealing a manual from this power with this team (0 when it holds nothing to steal).</summary>
        public double ManualTheftChance(string powerName, IReadOnlyList<CharacterData> team) =>
            factions.GetFactionByName(powerName) is { } power && team is { Count: > 0 }
                ? Chance(team, power.HighestRealm, Ops.TheftBaseChance, Ops.TheftChancePerPower, Ops.TheftMinChance, Ops.TheftMaxChance) : 0;

        /// <summary>The odds of an expedition to the known tomb, against its Purple Mansion guardian.</summary>
        public double TombExpeditionChance(IReadOnlyList<CharacterData> team) =>
            team is { Count: > 0 } ? Chance(team, CultivationRealm.PurpleMansion, Ops.ExpeditionBaseChance, Ops.ExpeditionChancePerPower, Ops.ExpeditionMinChance, Ops.ExpeditionMaxChance) : 0;

        private static double Chance(IReadOnlyList<CharacterData> team, CultivationRealm guard, double baseChance, double perPower, double min, double max)
        {
            var powers = team.Select(m => (double)HuntRules.Power(m)).OrderByDescending(p => p).ToList();
            double strength = powers[0] + powers.Skip(1).Sum() * 0.3;
            return System.Math.Clamp(baseChance + (strength - HuntRules.Power(guard, 5)) * perPower, min, max);
        }

        // ---- A Purple Mansion's tomb (C5) ----

        private void MaybeRevealTomb()
        {
            if (Tomb != null || !ctx.Rng.Chance(Settings.TombChance)) return;
            var method = ctx.Content.Techniques.Where(IsAscent).Where(t => !techniques.Knows(t.ID))
                .OrderBy(t => factions.Factions.Count(f => f.Techniques.Contains(t.ID))).ThenByDescending(t => t.Grade).FirstOrDefault();
            if (method == null) return;
            Tomb = method.ID;
            ctx.Log.Info($"[Paths] The ruins hide a Purple Mansion's tomb: « {method.Name} » lies there.");
        }

        /// <summary>An expedition to the tomb, against its guardian (a Purple Mansion's): success brings its method back.</summary>
        public ExpeditionOutcome ExploreTomb(IReadOnlyList<string> teamIds)
        {
            if (Tomb == null) return new ExpeditionOutcome(false, false, "aucun tombeau n'est connu");
            var ids = teamIds ?? new List<string>();
            var team = ids.Distinct().Select(clan.FindById).ToList();
            if (ids.Distinct().Count() != ids.Count || team.Count == 0 || team.Count > Ops.ExpeditionMaxTeam
                || team.Any(m => m == null || !m.IsAlive || m.CaptorFaction != null || m.DiscipleOf != null || m.Realm < CultivationRealm.QiRefinement
                    || m.LastOperationYear == ctx.Clock.Year))
                return new ExpeditionOutcome(false, false, $"une expédition part à 1 à {Ops.ExpeditionMaxTeam} cultivateurs libres");
            foreach (var m in team) m.LastOperationYear = ctx.Clock.Year;
            if (ctx.Rng.Chance(Chance(team, CultivationRealm.PurpleMansion, Ops.ExpeditionBaseChance, Ops.ExpeditionChancePerPower, Ops.ExpeditionMinChance, Ops.ExpeditionMaxChance)))
            {
                techniques.Learn(Tomb);
                Tomb = null;
                ctx.Events.TriggerTombLooted(); // its guardian's artifact (L4f)
                return new ExpeditionOutcome(true, true, null);
            }
            var weakest = team.OrderBy(m => HuntRules.Power(m)).First();
            if (ctx.Rng.Chance(Ops.ExpeditionDeathChance)) clan.Kill(weakest, DeathCause.Combat);
            foreach (var m in team.Where(m => m.IsAlive)) if (ctx.Rng.Chance(Ops.ExpeditionWoundChance)) wounds.ApplyDaoWound(m);
            return new ExpeditionOutcome(true, false, null);
        }

        // ---- A defector (C8) ----

        private void MaybeDefect()
        {
            var holders = factions.Factions.Where(f => AscentMethodOf(f) != null).ToList();
            if (holders.Count == 0 || !ctx.Rng.Chance(Settings.DefectionChance)) return;
            var power = holders[ctx.Rng.Next(holders.Count)];
            var method = AscentMethodOf(power);
            bool spy = ctx.Rng.Chance(Settings.FalseDefectorChance);
            var names = ctx.Content.Names.Male;
            var defector = new CharacterData
            {
                ID = ctx.Rng.NextId(),
                FirstName = names.Count == 0 ? "Sans-Nom" : names[ctx.Rng.Next(names.Count)],
                LastName = clan.ClanName,
                IsMale = true,
                Age = ctx.Rng.Next(30, 81),
                HasSpiritualOrifice = true,
                OrificeKnown = true,
                Realm = CultivationRealm.Foundation,
                RealmStage = ctx.Rng.Next(1, 5),
                SpiritualRoot = ctx.Rng.Next(40, 81),
                QiId = method.RequiredQiId,
                SpyFor = spy ? power.Name : null,
                FromFaction = power.Name, // the clan knows whence he came, not whom he serves
                Temperament = FoundationRules.RandomTemperament(ctx.Rng)
            };
            PowerLadder.Normalize(defector);
            PowerLadder.NormalizeLifespan(defector);
            defector.MaxLifespan = System.Math.Max(defector.MaxLifespan, defector.Age + 20);
            defector.FoundationId = techniques.FindQi(method.RequiredQiId)?.Foundation;
            if (defector.FoundationId != null) // the clan knows the foundation he bears, not the method he swore to keep
                techniques.Knowledge.Reveal(World.FactKind.Ability, defector.FoundationId, World.KnowledgeSource.Formed);
            clan.AddMember(defector);
            Practise(defector, method); // sworn to his faction: he cannot teach it
            factions.ChangeRelation(power.ID, -Settings.DefectionRelationLoss);
            ctx.Log.Warning($"[Paths] {defector.FullName} defects from {power.Name} with « {method.Name} ».");
        }

        /// <summary>A disciple of a faction practises its method, bound by a Dao oath never to pass it on (the clan's library stays without it).</summary>
        private static void Practise(CharacterData member, TechniqueData method)
        {
            if (!member.KnownTechniqueIDs.Contains(method.ID)) member.KnownTechniqueIDs.Add(method.ID);
            if (TechniqueRules.CanPractise(member, method)) member.CultivationMethodId = method.ID; // a method works only with its own Qi
        }

        public void Restore(string tomb) => Tomb = tomb;
    }
}

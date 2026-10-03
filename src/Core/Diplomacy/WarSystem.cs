using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Diplomacy
{
    /// <summary>
    /// Open wars (user decision 2026-09-27; D7). Between powers: each side drags its allies — a coalition; a battle a
    /// year, weighed by the sides' war strength; the loser bleeds strength and wealth and the winner loots; a side fallen
    /// under its surrender ratio yields, its leader becoming the victor's vassal when it can; after long years, a
    /// weary peace. An aggressive power at odds with a neighbour it distrusts declares war; a power towering over the rest
    /// sees its wary neighbours band against it. An ally of the clan caught in a war calls it to arms. The clan's own war:
    /// declared by the clan, or by a hostile, stronger power that suspects it, or by a greedy one refused its
    /// « protection »; a battle a year, its defensive allies perhaps joining; loot, or stones lost and a life; it may sue
    /// for peace with a tribute, and an exhausted enemy yields and pays. Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class WarSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly SuspicionLedger suspicion;
        private readonly TreatySystem treaties;
        private readonly PowerPoliticsSystem politics;
        private readonly AllianceSystem alliances;
        private readonly List<War> wars = new List<War>();
        private readonly List<ClanWar> clanWars = new List<ClanWar>();
        private readonly Dictionary<string, bool> alliesComing = new Dictionary<string, bool>(); // this year's answer of each ally
        private int alliesYear = -1;

        public WarSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, SuspicionLedger suspicion,
            TreatySystem treaties, PowerPoliticsSystem politics, AllianceSystem alliances)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.suspicion = suspicion;
            this.treaties = treaties;
            this.politics = politics;
            this.alliances = alliances;
            ctx.Events.OnPowerAbsorbed += (vassal, _) => Forget(vassal);
            ctx.Events.OnExtortionRefused += power =>
            {
                if (factions.GetFactionByName(power) is { } greedy && !AtWar(greedy.Name)) MakeWarOnClan(greedy); // one war at a time
            };
        }

        /// <summary>An absorbed power leaves its wars: its side fights on under its first member left, or the war ends.</summary>
        private void Forget(string power)
        {
            clanWars.RemoveAll(w => w.Enemy == power);
            for (int i = wars.Count - 1; i >= 0; i--)
            {
                var war = wars[i];
                if (!war.SideA.Contains(power) && !war.SideB.Contains(power)) continue;
                var updated = war with { SideA = war.SideA.Where(p => p != power).ToList(), SideB = war.SideB.Where(p => p != power).ToList() };
                if (updated.SideA.Count == 0 || updated.SideB.Count == 0) wars.RemoveAt(i);
                else wars[i] = updated;
            }
        }

        private WarSettings Settings => ctx.Content.Balance.Wars;

        public IReadOnlyList<War> Wars => wars;
        public IReadOnlyList<ClanWar> ClanWars => clanWars;

        public void Restore(IEnumerable<War> savedWars, IEnumerable<ClanWar> savedClanWars)
        {
            wars.Clear();
            if (savedWars != null) wars.AddRange(savedWars.Where(w => w != null));
            clanWars.Clear();
            if (savedClanWars != null) clanWars.AddRange(savedClanWars.Where(w => w != null));
        }

        /// <summary>Whether a power is at war, with another power or with the clan (one war at a time).</summary>
        public bool AtWar(string power) =>
            wars.Any(w => w.SideA.Contains(power) || w.SideB.Contains(power)) || clanWars.Any(w => w.Enemy == power);

        // ---- Wars between powers ----

        /// <summary>A war between two powers: each drags its allies not already at war.</summary>
        public War Start(FactionData attacker, FactionData defender)
        {
            var sideA = Side(attacker.Name, defender.Name);
            var sideB = Side(defender.Name, attacker.Name).Where(p => !sideA.Contains(p)).ToList();
            var war = new War($"war-{attacker.ID}-{defender.ID}-{ctx.Clock.Year}", sideA, sideB, ctx.Clock.Year, Total(sideA), Total(sideB));
            wars.Add(war);
            ctx.Log.Warning($"[Wars] {attacker.Name} makes war on {defender.Name}.");
            ctx.Events.TriggerWarBegun(attacker.Name, defender.Name);
            return war;
        }

        private List<string> Side(string leader, string enemy) =>
            new[] { leader }.Concat(politics.AlliesOf(leader).Where(a => a != enemy && !AtWar(a))).Distinct().ToList();

        private int Total(IEnumerable<string> side) => side.Select(factions.GetFactionByName).Where(p => p != null).Sum(p => p.PowerLevel);

        private CultivationRealm SideTop(IEnumerable<string> side) =>
            side.Select(factions.GetFactionByName).Where(p => p != null).Select(p => p.HighestRealm).DefaultIfEmpty(CultivationRealm.Embryonic).Max();

        private double SideStrength(IEnumerable<string> side) =>
            side.Select(factions.GetFactionByName).Where(p => p != null).Sum(p => WarRules.Strength(p, Settings));

        /// <summary>A battle: the loser side bleeds; the winner's leader loots; the clan's allies in it call it to arms.</summary>
        public void Battle(War war)
        {
            var s = Settings;
            double a = SideStrength(war.SideA), b = SideStrength(war.SideB);
            bool aWins = ctx.Rng.Chance(WarRules.WinChance(a, SideTop(war.SideA), b, SideTop(war.SideB), ctx.Content.Balance.RealmGap));
            var (winners, losers) = aWins ? (war.SideA, war.SideB) : (war.SideB, war.SideA);
            int loot = 0;
            foreach (var loser in losers.Select(factions.GetFactionByName).Where(p => p != null))
            {
                loser.PowerLevel -= (int)(loser.PowerLevel * s.BattleLossShare);
                int lost = (int)(Math.Max(0, loser.Wealth) * s.BattleLossShare);
                loser.Wealth -= lost;
                loot += lost;
            }
            if (factions.GetFactionByName(winners[0]) is { } leader) leader.Wealth += (int)(loot * s.WinnerLootShare);
            bool alliesOnA = war.SideA.Any(IsDefended), alliesOnB = war.SideB.Any(IsDefended);
            if (alliesOnA == alliesOnB) return; // none, or allies of the clan on both sides: it answers neither
            foreach (var member in (alliesOnA ? war.SideA : war.SideB).Where(IsDefended))
                politics.CallClanToArms(member, alliesOnA ? war.SideB[0] : war.SideA[0]);
        }

        private bool IsDefended(string power) => treaties.Has(power, TreatyKind.Defence);

        // ---- The clan's war ----

        public string DeclareOn(string faction)
        {
            var power = factions.GetFactionByName(faction);
            if (power == null) return "puissance inconnue";
            if (clanWars.Any(w => w.Enemy == faction)) return "le clan est déjà en guerre contre elle";
            if (treaties.With(faction).Count > 0) return "un traité vous lie : rompez-le d'abord";
            alliances.DeclareWar(power.ID);
            clanWars.Add(new ClanWar(faction, ctx.Clock.Year, power.PowerLevel));
            ctx.Events.TriggerWarBegun(SecretBook.ClanHolder, faction);
            return null;
        }

        /// <summary>A power makes war on the clan (a patron resisted, 2026-10-01).</summary>
        public void WagedOnClan(string faction)
        {
            var power = factions.GetFactionByName(faction);
            if (power == null || clanWars.Any(w => w.Enemy == faction)) return;
            clanWars.Add(new ClanWar(faction, ctx.Clock.Year, power.PowerLevel));
            ctx.Events.TriggerWarBegun(faction, SecretBook.ClanHolder);
        }

        public string SuePeace(string faction)
        {
            var war = clanWars.FirstOrDefault(w => w.Enemy == faction);
            if (war == null) return "aucune guerre contre elle";
            int tribute = (int)(resources.SpiritStones * Settings.PeaceTributeShare);
            resources.ConsumeSpiritStones(tribute);
            if (factions.GetFactionByName(faction) is { } power) power.Wealth += tribute;
            MakePeace(war);
            return null;
        }

        private void MakePeace(ClanWar war)
        {
            clanWars.Remove(war);
            if (factions.GetFactionByName(war.Enemy) is { } power)
                factions.ChangeRelation(power.ID, Settings.PeaceRelation - power.RelationWithPlayer);
            ctx.Log.Info($"[Wars] Peace between the clan and {war.Enemy}.");
            ctx.Events.TriggerPeace(SecretBook.ClanHolder, war.Enemy);
        }

        /// <summary>The clan's own war strength, without its allies: its strongest free member, and the others' weight.</summary>
        public double ClanWarStrength() => WarRules.ClanWarStrength(clan.LivingMembers, Settings) + (DomainGuard?.Invoke() ?? 0);

        /// <summary>What the clan's Dharma Treasures and Rank Designations add to its war strength (L4e; set by the session).</summary>
        public System.Func<double> DomainGuard { get; set; }

        private double ClanStrength(ClanWar war, out CultivationRealm top)
        {
            double strength = ClanWarStrength();
            top = clan.LivingMembers.Where(m => m.CaptorFaction == null).Select(m => m.Realm).DefaultIfEmpty(CultivationRealm.Embryonic).Max();
            foreach (var ally in treaties.All.Where(t => t.Kind == TreatyKind.Defence && t.Faction != war.Enemy).Select(t => factions.GetFactionByName(t.Faction)))
                if (ally != null && Comes(ally.Name))
                {
                    strength += WarRules.Strength(ally, Settings); // it comes, or lingers
                    if (ally.HighestRealm > top) top = ally.HighestRealm;
                }
            return strength;
        }

        /// <summary>An ally answers once a year, whatever the number of the clan's wars.</summary>
        private bool Comes(string ally)
        {
            if (alliesYear != ctx.Clock.Year) { alliesComing.Clear(); alliesYear = ctx.Clock.Year; }
            if (!alliesComing.TryGetValue(ally, out bool comes)) alliesComing[ally] = comes = ctx.Rng.Chance(Settings.AllyJoinChance);
            return comes;
        }

        private void ClanBattle(ClanWar war, FactionData enemy)
        {
            var s = Settings;
            double ours = ClanStrength(war, out var ourTop), theirs = WarRules.Strength(enemy, s);
            if (ctx.Rng.Chance(WarRules.WinChance(ours, ourTop, theirs, enemy.HighestRealm, ctx.Content.Balance.RealmGap)))
            {
                enemy.PowerLevel -= (int)(enemy.PowerLevel * s.BattleLossShare);
                int loot = (int)(Math.Max(0, enemy.Wealth) * s.ClanLootShare);
                enemy.Wealth -= loot;
                resources.AddSpiritStones(loot);
                return;
            }
            resources.ConsumeSpiritStones((int)(resources.SpiritStones * s.ClanWarStonesLoss));
            if (!ctx.Rng.Chance(s.ClanWarDeathChance)) return;
            var fallen = clan.LivingMembers.Where(m => m.CaptorFaction == null && m.Realm >= CultivationRealm.QiRefinement)
                .OrderBy(m => (int)m.Realm).ThenBy(m => m.RealmStage).FirstOrDefault();
            if (fallen != null) clan.Kill(fallen, DeathCause.Combat);
        }

        // ---- The year ----

        public void ProcessYear()
        {
            foreach (var war in clanWars.ToList()) ClanYear(war);
            foreach (var war in wars.ToList()) WarYear(war);
            DeclareWars();
        }

        private void ClanYear(ClanWar war)
        {
            var enemy = factions.GetFactionByName(war.Enemy);
            if (enemy == null) { clanWars.Remove(war); return; }
            if (enemy.PowerLevel < war.EnemyInitial * Settings.SurrenderRatio)
            {
                int tribute = (int)(Math.Max(0, enemy.Wealth) * Settings.PeaceTributeShare); // exhausted, it yields and pays
                enemy.Wealth -= tribute;
                resources.AddSpiritStones(tribute);
                MakePeace(war);
                ctx.Events.TriggerClanWarWon(enemy.Name);
                return;
            }
            ClanBattle(war, enemy);
        }

        private void WarYear(War war)
        {
            var s = Settings;
            bool aBeaten = Total(war.SideA) < war.InitialA * s.SurrenderRatio;
            bool bBeaten = Total(war.SideB) < war.InitialB * s.SurrenderRatio;
            if (aBeaten || bBeaten || ctx.Clock.Year - war.StartYear >= s.MaxWarYears)
            {
                wars.Remove(war);
                if (aBeaten != bBeaten)
                {
                    var (victor, vanquished) = bBeaten ? (war.SideA[0], war.SideB[0]) : (war.SideB[0], war.SideA[0]);
                    politics.Subjugate(factions.GetFactionByName(victor), factions.GetFactionByName(vanquished));
                    ctx.Events.TriggerPeace(victor, vanquished);
                    return;
                }
                ctx.Log.Info($"[Wars] A weary peace between {war.SideA[0]} and {war.SideB[0]}.");
                ctx.Events.TriggerPeace(war.SideA[0], war.SideB[0]);
                return;
            }
            Battle(war);
        }

        private void DeclareWars()
        {
            var s = Settings;
            var powers = factions.Factions.ToList();
            if (powers.Count == 0) return;
            double median = powers.Select(p => (double)p.PowerLevel).OrderBy(v => v).ElementAt(powers.Count / 2);
            var hegemon = powers.Where(p => p.PowerLevel >= median * s.HegemonRatio && !AtWar(p.Name)).OrderByDescending(p => p.PowerLevel).FirstOrDefault();
            if (hegemon != null && ctx.Rng.Chance(s.HegemonChance))
            {
                var wary = powers.Where(p => p != hegemon && !AtWar(p.Name) && factions.AreNeighbours(p, hegemon)
                    && suspicion.Distrust(p.Name, hegemon.Name) >= s.WarDistrust).ToList();
                if (wary.Count > 0)
                {
                    var war = Start(wary[0], hegemon);
                    var banded = war.SideA.Concat(wary.Skip(1).Select(o => o.Name).Where(n => !war.SideA.Contains(n) && !war.SideB.Contains(n))).ToList();
                    wars[wars.IndexOf(war)] = war with { SideA = banded, InitialA = Total(banded) }; // they band together
                }
            }
            foreach (var power in powers.Where(p => p.Personality == FactionPersonality.Aggressive && !AtWar(p.Name)))
            {
                var foe = powers.FirstOrDefault(f => f != power && !AtWar(f.Name) && factions.AreNeighbours(power, f)
                    && suspicion.Distrust(power.Name, f.Name) >= s.WarDistrust);
                if (foe != null && ctx.Rng.Chance(s.WarChance)) Start(power, foe);
            }
            var clanStrength = WarRules.ClanStrength(clan.LivingMembers);
            foreach (var power in powers.Where(p => !AtWar(p.Name) && suspicion.OfClan(p.Name) >= s.DeclareSuspicion
                && p.RelationWithPlayer <= s.DeclareRelation && WarRules.Strength(p, s) > clanStrength && !treaties.Spares(p.Name)
                && World.TravelRules.PowerReaches(p, ctx.Content.Clan.HomeRegion, ctx.Content))) // it must reach the domain (audit §6.3)
            {
                if (!ctx.Rng.Chance(s.PowerDeclareChance)) continue;
                MakeWarOnClan(power);
            }
        }

        /// <summary>A power makes war on the clan (suspicion, or greed refused); once at a time.</summary>
        private void MakeWarOnClan(FactionData power)
        {
            if (clanWars.Any(w => w.Enemy == power.Name)) return;
            clanWars.Add(new ClanWar(power.Name, ctx.Clock.Year, power.PowerLevel));
            ctx.Log.Warning($"[Wars] {power.Name} makes war on the clan.");
            ctx.Events.TriggerWarBegun(power.Name, SecretBook.ClanHolder);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// A war's yearly battle fought on the grid (G6; the user's choices of 2026-10-12): the clan picks up to WarMaxFighters of
    /// its free cultivators against the enemy's leader, at its strongest realm, and its soldiers a realm below — a soldier per
    /// ArmyPowerPerSoldier of its power, each ally who came keeping one off the field. Won, the enemy is weakened and looted;
    /// lost, the war's toll in stones, and each fallen fighter may die; left, a smaller toll. Several wars' battles wait in
    /// turn. The clan may also let chance settle a battle, as before. Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class WarBattles
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly FactionManager factions;
        private readonly WarSystem wars;
        private readonly TechniqueLibrary techniques;
        private readonly WoundSystem wounds;
        private readonly List<PendingWarBattle> queue = new List<PendingWarBattle>();

        public WarBattles(GameContext ctx, ClanManager clan, FactionManager factions, WarSystem wars, TechniqueLibrary techniques, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.factions = factions;
            this.wars = wars;
            this.techniques = techniques;
            this.wounds = wounds;
        }

        /// <summary>The battles waiting, the first answered first.</summary>
        public IReadOnlyList<PendingWarBattle> Queue => queue;

        /// <summary>The first battle waiting; those whose war ended meanwhile (peace, a power gone) are forgotten.</summary>
        public PendingWarBattle Pending
        {
            get
            {
                if (Current == null) queue.RemoveAll(b => wars.War(b.Enemy) == null); // the battle under way stays first
                return queue.Count > 0 ? queue[0] : null;
            }
        }

        /// <summary>The battle under way, until it is concluded; never saved (it is fought on its screen).</summary>
        public Battle Current { get; private set; }

        /// <summary>The clan's temples (set by the session): a renegade monk leading a temple's army may be taken back (L7).</summary>
        public BuddhismSystem Buddhism { get; set; }

        /// <summary>The clan's alchemy (set by the session): its fighters draw on its Autumn Convergence Pills.</summary>
        public AlchemySystem Alchemy { get; set; }

        public void Restore(IEnumerable<PendingWarBattle> saved)
        {
            queue.Clear();
            if (saved != null) queue.AddRange(saved);
        }

        private const int SoldierRoot = 40;
        private const int LeaderRoot = 70;

        private WarSettings Settings => ctx.Content.Balance.Wars;

        /// <summary>The year's battle waits for the clan; false when it has nobody who can fight (chance settles it then).</summary>
        public bool Spring(ClanWar war, FactionData enemy, int alliesCame)
        {
            if (war == null || enemy == null || !clan.LivingMembers.Any(m => FighterRefusal(m) == null)) return false;
            if (queue.Any(b => b.Enemy == enemy.Name)) return true; // its battle already waits: one a war
            var s = Settings;
            int soldiers = Math.Min(s.ArmyMaxSoldiers, Math.Max(0, enemy.PowerLevel) / s.ArmyPowerPerSoldier);
            var soldierRealm = (CultivationRealm)Math.Max(0, (int)enemy.HighestRealm - 1);
            queue.Add(new PendingWarBattle(enemy.Name, ctx.Clock.Year, enemy.HighestRealm, soldierRealm, soldiers, alliesCame, ctx.Rng.Next()));
            ctx.Log.Warning($"[Wars] {enemy.Name} marches on the clan: its leader and {soldiers} soldier(s), {alliesCame} ally(ies) came.");
            return true;
        }

        /// <summary>Why this member cannot fight, or null when it can.</summary>
        public string FighterRefusal(CharacterData member) => FighterRules.Refusal(member);

        /// <summary>
        /// The enemy on the field: its leader — a temple's renegade monk, once of the clan, when it has one (L7) — then the
        /// soldiers its allies did not keep away (drawn from the seed).
        /// </summary>
        public IReadOnlyList<CharacterData> Army(PendingWarBattle battle)
        {
            var rng = new Random(battle.Seed);
            var names = ctx.Content.Names;
            int onField = Math.Max(0, battle.Soldiers - battle.AlliesCame);
            var renegade = BuddhismSystem.RenegadeOf(factions.GetFactionByName(battle.Enemy));
            return Enumerable.Range(0, 1 + onField).Select(i =>
            {
                if (i == 0 && renegade != null)
                    return new CharacterData
                    {
                        ID = renegade.Id, FirstName = renegade.Name, LastName = "", IsMale = rng.Next(2) == 0, Age = renegade.Age(ctx.Clock.Year),
                        Realm = renegade.Realm, RealmStage = renegade.Stage, SpiritualRoot = LeaderRoot, HasSpiritualOrifice = true, OrificeKnown = true,
                        Path = CultivationPath.Buddhist, FromFaction = battle.Enemy
                    };
                bool leader = i == 0;
                bool isMale = rng.Next(2) == 0;
                var pool = isMale ? names.Male : names.Female;
                return new CharacterData
                {
                    ID = $"soldier-{battle.Seed}-{i}",
                    FirstName = pool.Count == 0 ? (leader ? "Chef" : "Soldat") : pool[rng.Next(pool.Count)],
                    LastName = names.OutsiderFamilies.Count == 0 ? battle.Enemy : names.OutsiderFamilies[rng.Next(names.OutsiderFamilies.Count)],
                    IsMale = isMale,
                    Age = 25 + rng.Next(50),
                    Realm = leader ? battle.LeaderRealm : battle.SoldierRealm,
                    RealmStage = 5,
                    SpiritualRoot = leader ? LeaderRoot : SoldierRoot,
                    HasSpiritualOrifice = true,
                    OrificeKnown = true,
                    FromFaction = battle.Enemy
                };
            }).ToList();
        }

        /// <summary>The clan answers with these fighters: the battle opens on the grid.</summary>
        public string Fight(IReadOnlyList<string> fighterIds)
        {
            if (Current != null) return "la bataille est déjà engagée";
            if (Pending == null) return "aucune bataille de guerre n'attend le clan";
            var ids = (fighterIds ?? Array.Empty<string>()).Distinct().ToList();
            if (ids.Count == 0) return "il faut au moins un combattant";
            if (ids.Count > Settings.WarMaxFighters) return $"au plus {Settings.WarMaxFighters} combattants";
            var fighters = ids.Select(clan.FindById).ToList();
            var unfit = fighters.Select(FighterRefusal).FirstOrDefault(r => r != null);
            if (unfit != null) return unfit;

            Current = Battle.Start(fighters, Army(Pending), ctx.Rng, ctx.Log, findTechnique: techniques.Find, gap: ctx.Content.Balance.RealmGap,
                convergence: ctx.Content.Balance.Arts.Convergence, equip: Equip);
            ctx.Events.TriggerClanFought(fighters.ToList()); // a non-violent vow breaks on the grid (R13-14b)
            return null; // the battle stays first in the queue until it is concluded
        }

        /// <summary>The clan lets chance settle the battle, as before.</summary>
        public string LetBe()
        {
            if (Pending == null) return "aucune bataille de guerre n'attend le clan";
            if (Current != null) return "la bataille est engagée : menez-la à son terme";
            var battle = Pending;
            queue.RemoveAt(0);
            var war = wars.War(battle.Enemy);
            var enemy = factions.GetFactionByName(battle.Enemy);
            if (war != null && enemy != null) wars.ResolveBattle(war, enemy); // a peace made meanwhile: nothing left to fight
            return null;
        }

        /// <summary>Once the battle is over: the enemy weakened and looted, or the war's toll and the fallen; the wounded.</summary>
        public string Conclude()
        {
            if (Current == null) return "aucune bataille en cours";
            if (!Current.IsOver) return "la bataille n'est pas finie";
            ctx.Events.TriggerClanBattleEnded(Current.Allies.Select(u => u.BaseData).ToList(), Current.Enemies.Count(u => u.IsDown)); // a devil's blood (R12)
            var battle = Pending;
            bool warOver = wars.War(battle.Enemy) == null; // peace made during the battle: it costs and wins nothing
            var outcome = Current.State == CombatState.Victory ? WarBattleOutcome.Won
                : Current.State == CombatState.Defeat && !Current.Allies.Any(a => a.HasFled) ? WarBattleOutcome.Lost
                : WarBattleOutcome.Withdrawn; // fled, or neither side prevailed
            foreach (var fighter in Current.Allies)
            {
                if (!warOver && outcome == WarBattleOutcome.Lost && fighter.IsDown && ctx.Rng.Chance(Settings.ClanWarDeathChance))
                    clan.Kill(fighter.BaseData, DeathCause.Combat); // a defeat's fallen may die
                else wounds.EvaluatePostCombatWounds(fighter.BaseData, fighter.MaxVitality, fighter.CurrentVitality);
            }
            var enemy = factions.GetFactionByName(battle.Enemy);
            if (warOver) { }
            else if (outcome == WarBattleOutcome.Won && enemy != null)
            {
                wars.Won(enemy);
                TakeBackTheRenegade(enemy);
            }
            else if (outcome == WarBattleOutcome.Lost) wars.LoseStones(1);
            else if (outcome == WarBattleOutcome.Withdrawn) wars.LoseStones(Settings.WithdrawStonesFactor);
            ctx.Log.Info($"[Wars] The battle against {battle.Enemy} ends: {outcome}.");
            ctx.Events.TriggerWarBattleSettled(battle.Enemy, outcome);
            queue.RemoveAt(0);
            Current = null;
            return null;
        }

        /// <summary>A temple's renegade beaten at the head of its army may be taken back by the clan (L7).</summary>
        private void TakeBackTheRenegade(FactionData enemy)
        {
            var renegade = BuddhismSystem.RenegadeOf(enemy);
            bool beaten = renegade != null && Current.Enemies.Any(u => u.BaseData.ID == renegade.Id && u.IsDown);
            if (beaten && Buddhism != null && ctx.Rng.Chance(ctx.Content.Balance.Conversions.RenegadeTakeBackChance)) Buddhism.TakeBack(enemy, renegade);
        }

        /// <summary>What each fighter carries: the clan's Autumn Convergence Pills; the enemy's leader one, if its power knows alchemy.</summary>
        private void Equip(CombatUnit unit)
        {
            if (unit.IsAlly)
            {
                if (Alchemy == null) return;
                unit.TakeConvergencePill = () => Alchemy.TakePill(PillKind.AutumnConvergence);
                unit.ConvergencePillsLeft = () => Alchemy.PillsOf(PillKind.AutumnConvergence);
                return;
            }
            if (!PowerArts.Knows(factions.GetFactionByName(Pending?.Enemy), ImmortalArt.Alchemy, ctx.Content)) return;
            int left = 1;
            unit.TakeConvergencePill = () => left-- > 0;
            unit.ConvergencePillsLeft = () => left;
        }
    }
}

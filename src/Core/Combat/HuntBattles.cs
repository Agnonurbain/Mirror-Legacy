using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// A hunt's capture fought on the grid (G6; the user's choices of 2026-10-12): once the team got close unseen, its strikers
    /// alone face the beast, weakened by the lures and the timing (by night it is livelier). Won, the beast is taken; lost, it
    /// breaks free, the strikers shaken, and one far weaker than it may die; fled, it breaks free. The clan may also let chance
    /// settle the capture, as before the battles. Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class HuntBattles
    {
        private const int BeastRoot = 50;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly HuntOperations hunts;
        private readonly TechniqueLibrary techniques;
        private readonly WoundSystem wounds;
        private PendingHunt fought; // the hunt whose battle is under way

        public HuntBattles(GameContext ctx, ClanManager clan, HuntOperations hunts, TechniqueLibrary techniques, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.hunts = hunts;
            this.techniques = techniques;
            this.wounds = wounds;
        }

        private HuntSettings Settings => ctx.Content.Balance.Hunt;

        public PendingHunt Pending { get; private set; }

        /// <summary>The battle under way, until it is concluded; never saved (it is fought on its screen).</summary>
        public Battle Current { get; private set; }

        /// <summary>The clan's alchemy (set by the session): its strikers draw on its Autumn Convergence Pills.</summary>
        public AlchemySystem Alchemy { get; set; }

        public void Restore(PendingHunt pending) => Pending = pending;

        /// <summary>Another battle waits or is fought (set by the session): one battle at a time, so chance settles the capture then.</summary>
        public Func<bool> OtherBattle { get; set; }

        /// <summary>The capture waits for the strikers; false when none of them can fight (chance settles it then).</summary>
        public bool Spring(HuntPlan plan, WorldBeast beast)
        {
            if (plan == null || beast == null || Pending != null || Current != null || OtherBattle?.Invoke() == true) return false;
            var hunt = new PendingHunt(plan, ctx.Clock.Year);
            if (Strikers(hunt).Count == 0) return false;
            Pending = hunt;
            return true;
        }

        /// <summary>The plan's strikers who can fight.</summary>
        public IReadOnlyList<CharacterData> Strikers(PendingHunt hunt) =>
            hunt == null ? Array.Empty<CharacterData>() : hunts.Strikers(hunt.Plan).Where(m => FighterRules.Refusal(m) == null).ToList();

        /// <summary>The beast as it stands on the grid: its species' name, realm and stage; null when it is gone.</summary>
        public CharacterData Beast(PendingHunt hunt)
        {
            var beast = hunt == null ? null : hunts.Target(hunt.Plan);
            if (beast == null) return null;
            var species = ctx.Content.BeastSpecies.FirstOrDefault(s => s.Id == beast.SpeciesId);
            return new CharacterData
            {
                ID = beast.Id,
                LastName = species?.Name ?? "Bête",
                FirstName = "",
                Realm = beast.Realm,
                RealmStage = beast.Stage,
                SpiritualRoot = BeastRoot,
                HasSpiritualOrifice = true,
                OrificeKnown = true,
                FromFaction = beast.OwnerFaction
            };
        }

        /// <summary>The share of its vitality the beast has lost to the lures and the timing when the battle opens.</summary>
        public double Weakening(HuntPlan plan)
        {
            int lures = plan.Team.Values.Count(r => r == HuntRole.Lure);
            double share = lures * Settings.BattleLureWeaken + Settings.BattleTimingWeaken[(int)plan.Timing];
            return Math.Clamp(share, 0, Settings.BattleMaxWeaken);
        }

        /// <summary>The strikers fight: the battle opens on the grid, the beast weakened.</summary>
        public string Fight()
        {
            if (Pending == null) return "aucune chasse n'attend ses frappeurs";
            var beast = Beast(Pending);
            if (beast == null) return "la bête n'est plus là : laissez faire le sort";
            var strikers = Strikers(Pending);
            if (strikers.Count == 0) return "aucun frappeur ne peut combattre : laissez faire le sort";

            var battle = Battle.Start(strikers, new[] { beast }, ctx.Rng, ctx.Log, findTechnique: techniques.Find, gap: ctx.Content.Balance.RealmGap,
                convergence: ctx.Content.Balance.Arts.Convergence, equip: Equip);
            ctx.Events.TriggerClanFought(strikers.ToList()); // a non-violent vow breaks on the grid (R13-14b)
            var foe = battle.Enemies.Single();
            foe.TakeDamage((int)Math.Round(foe.MaxVitality * Weakening(Pending.Plan))); // the lures wore it down
            fought = Pending;
            Current = battle;
            Pending = null;
            return null;
        }

        /// <summary>The clan lets chance settle the capture, as before the battles.</summary>
        public string LetBe()
        {
            if (Pending == null) return "aucune chasse n'attend ses frappeurs";
            var plan = Pending.Plan;
            Pending = null;
            if (hunts.Target(plan) is { } beast) hunts.ResolveCapture(plan, beast);
            return null;
        }

        /// <summary>Once the battle is over: the beast taken, or broken free; the fallen and the wounded.</summary>
        public string Conclude()
        {
            if (Current == null) return "aucune bataille en cours";
            if (!Current.IsOver) return "la bataille n'est pas finie";
            ctx.Events.TriggerClanBattleEnded(Current.Allies.Select(u => u.BaseData).ToList(), Current.Enemies.Count(u => u.IsDown)); // a devil's blood (R12)
            var downed = Current.State == CombatState.Defeat ? Current.Allies.Where(u => u.IsDown).Select(u => u.BaseData).ToList() : null; // told once the fallen are settled (audit §7.14)
            var plan = fought.Plan;
            var beast = hunts.Target(plan);
            bool lost = Current.State == CombatState.Defeat;
            if (lost) hunts.Shake(plan);
            var fallen = new List<string>();
            foreach (var striker in Current.Allies)
            {
                if (lost && striker.IsDown && beast != null && hunts.MayKill(striker.BaseData, beast)) // only a beast far stronger, and victorious, kills
                {
                    clan.Kill(striker.BaseData, DeathCause.Combat);
                    fallen.Add(striker.BaseData.ID);
                }
                else wounds.EvaluatePostCombatWounds(striker.BaseData, striker.MaxVitality, striker.CurrentVitality);
            }
            if (beast != null)
            {
                if (Current.State == CombatState.Victory) hunts.Take(plan, beast);
                else
                {
                    ctx.Log.Info($"[Hunt] The beast breaks free of the strikers{(lost ? "" : ", who withdraw")}.");
                    hunts.Escaped(plan, beast, fallen);
                }
            }
            if (downed != null) ctx.Events.TriggerClanBattleLost(downed);
            Current = null;
            fought = null;
            return null;
        }

        /// <summary>What each striker carries: the clan's Autumn Convergence Pills; a beast carries none.</summary>
        private void Equip(CombatUnit unit)
        {
            if (!unit.IsAlly || Alchemy == null) return;
            unit.TakeConvergencePill = () => Alchemy.TakePill(PillKind.AutumnConvergence);
            unit.ConvergencePillsLeft = () => Alchemy.PillsOf(PillKind.AutumnConvergence);
        }
    }
}

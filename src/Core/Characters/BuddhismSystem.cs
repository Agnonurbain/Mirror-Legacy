using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Buddhist conversion and its two schools (R13-R14; LORE.md §3.3, §3.7, §11.2; the user's choices of 2026-10-12): a
    /// dying cultivator of any human way — or one whose way is closed (Tang Shedu; L7 conversions) — may convert at a temple,
    /// paying an offering, to escape death. It gives up its way — a realm
    /// lower, its foundation, abilities and method lost — and nourishes its nature and destiny, cultivating without trial at its
    /// temple's school's pace: the ancient school slow, up to the Dharma Master's apogee; the modern one faster, capped at the
    /// Dharma Master's beginning, and its temple holds its soul — fallen out with the clan, it may call the monk back. A True
    /// Monarch does not give up its position: only those below the Golden Core convert. A temple may convert a captive of the
    /// clan under duress, who renounces it (L7). See <see cref="BuddhismSettings"/>.
    /// </summary>
    public sealed class BuddhismSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly WoundSystem wounds;

        public BuddhismSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, WoundSystem wounds)
        {
            this.wounds = wounds;
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            ctx.Events.OnClanFought += BreakNonViolence;
            ctx.Events.OnCharacterDied += Departed;
            ctx.Events.OnCharacterBorn += Born;
        }

        private BuddhismSettings Settings => ctx.Content.Balance.Buddhism;

        private readonly List<MonkSoul> souls = new List<MonkSoul>(); // awaiting a child of the clan, the first dead first

        /// <summary>The monks' souls awaiting rebirth in the clan (R13-14c).</summary>
        public IReadOnlyList<MonkSoul> PendingSouls => souls;

        /// <summary>Restores the souls of a save (none before 2.50).</summary>
        public void RestoreSouls(IEnumerable<MonkSoul> saved)
        {
            souls.Clear();
            if (saved != null) souls.AddRange(saved.Where(s => s != null));
        }

        /// <summary>True for a cultivator of the clan who may be offered the conversion, whatever the temple (dying, of the immortal way, free).</summary>
        public bool MayConvert(CharacterData member) => MemberRefusal(member) == null;

        /// <summary>Why the member may not convert at this temple now, or null when it may.</summary>
        public string ConversionRefusal(CharacterData member, string templeName)
        {
            string refusal = MemberRefusal(member);
            if (refusal != null) return refusal;
            var temple = factions.GetFactionByName(templeName);
            if (temple == null || temple.Path != CultivationPath.Buddhist || temple.School == null) return "seul un temple bouddhiste convertit";
            if (resources.SpiritStones < Settings.OfferingStones) return $"l'offrande demande {Settings.OfferingStones} pierres spirituelles";
            return null;
        }

        private string MemberRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive || !clan.LivingMembers.Contains(member)) return "introuvable";
            if (member.Species != Species.Human) return "une bête liée ne se convertit pas";
            if (member.Path == CultivationPath.Buddhist) return "déjà moine";
            if (!SpiritualOrificeRules.CanCultivate(member) || (member.Realm == CultivationRealm.Embryonic && member.RealmStage <= 0))
                return "seul un cultivateur se convertit";
            if (member.Realm >= CultivationRealm.GoldenCore) return "un Vrai Monarque ne renonce pas à sa position";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (member.Retreat != Retreat.None) return "en retraite";
            if (!IsDying(member) && !member.ProgressionSealed) return "ni aux portes de la mort, ni sur un chemin fermé";
            return null;
        }

        /// <summary>At death's door: its years left within the conversion's reach.</summary>
        private bool IsDying(CharacterData member) => PowerLadder.LifespanLimit(member) - member.Age <= Settings.DyingYears;

        /// <summary>The member converts at the temple; false (nothing changes) when it may not.</summary>
        public bool Convert(CharacterData member, string templeName)
        {
            string refusal = ConversionRefusal(member, templeName);
            if (refusal != null)
            {
                ctx.Log.Warning($"[Buddhism] {member?.FullName} cannot convert at {templeName}: {refusal}.");
                return false;
            }

            var temple = factions.GetFactionByName(templeName);
            bool dying = IsDying(member);
            resources.ConsumeSpiritStones(Settings.OfferingStones);
            factions.ChangeRelation(temple.ID, Settings.ConversionRelation);
            GiveUpItsWay(member, temple);
            if (dying) member.MaxLifespan = member.Age + Settings.YearsGained; // death escaped; a closed way gains no years

            ctx.Log.Info($"[Buddhism] {member.FullName} converts at {temple.Name}: {RankCatalog.DisplayName(member)}.");
            ctx.Events.TriggerBuddhistConverted(member, temple.Name);
            return true;
        }

        /// <summary>
        /// A debate lost (R13-14b): the temple converts a cultivator of any other way below the Golden Core by force — no
        /// offering, no years gained, it is not dying. False when it cannot be (a True Monarch, a monk already, a bound beast,
        /// no such temple).
        /// </summary>
        public bool ForceConvert(CharacterData member, string templeName)
        {
            var temple = factions.GetFactionByName(templeName);
            if (member == null || !member.IsAlive || member.Species != Species.Human || member.Path == CultivationPath.Buddhist
                || member.Realm >= CultivationRealm.GoldenCore || temple?.Path != CultivationPath.Buddhist || temple.School == null) return false;
            GiveUpItsWay(member, temple);
            ctx.Log.Info($"[Buddhism] {member.FullName} is converted by force at {temple.Name}: {RankCatalog.DisplayName(member)}.");
            ctx.Events.TriggerBuddhistConverted(member, temple.Name);
            return true;
        }

        /// <summary>
        /// The member becomes a monk of the temple's school, a realm lower, its former way given up — the passage perilous in
        /// the middle realms (L7 conversions).
        /// </summary>
        private void GiveUpItsWay(CharacterData member, FactionData temple)
        {
            PathConversion.Pass(ctx, wounds, member);
            BecomeMonk(member, temple.Name, temple.School.Value,
                (CultivationRealm)Math.Max((int)CultivationRealm.Embryonic, (int)member.Realm - 1), 1); // a realm lower
        }

        /// <summary>
        /// A debate lost to a temple not its own (L7; the wiki « Buddhist Dao »: forced conversion between Dharma Aspect paths,
        /// induction from Ancient to Modern Buddhism or the reverse): the monk enters the victor's temple and school, keeping its
        /// rank — a modern school caps it at its Dharma Master's ceiling. False when it cannot be.
        /// </summary>
        public bool Induct(CharacterData monk, string templeName)
        {
            var temple = factions.GetFactionByName(templeName);
            if (monk == null || !monk.IsAlive || monk.Path != CultivationPath.Buddhist || monk.Temple == templeName
                || temple?.Path != CultivationPath.Buddhist || temple.School == null) return false;
            monk.Temple = temple.Name;
            monk.SubPath = temple.School.Value;
            if (monk.SubPath == CultivationSubPath.ModernBuddhism && (monk.Realm > CultivationRealm.GoldenCore
                || (monk.Realm == CultivationRealm.GoldenCore && monk.RealmStage > Settings.ModernCeilingStage)))
            {
                monk.Realm = CultivationRealm.GoldenCore; // never a Revered One, the Dharma Master's beginning at most
                monk.RealmStage = Settings.ModernCeilingStage;
                monk.CultivationXP = 0;
                monk.MaxLifespan = Math.Max(monk.Age + 1, PowerLadder.MaxLifespan(monk.Realm, monk.RealmStage));
            }
            ctx.Log.Info($"[Buddhism] {monk.FullName} is inducted into {temple.Name}: {RankCatalog.DisplayName(monk)}.");
            return true;
        }

        /// <summary>The member becomes a monk of the temple and school at the rank, its immortal way given up.</summary>
        private static void BecomeMonk(CharacterData member, string temple, CultivationSubPath school, CultivationRealm realm, int stage)
        {
            member.Path = CultivationPath.Buddhist;
            member.SubPath = school;
            member.Temple = temple;
            member.Realm = realm;
            member.RealmStage = stage;
            member.CultivationXP = 0;
            // its foundation, abilities, method and Qi, any lineage pursued or light borrowed
            member.FoundationId = null;
            member.DivineAbilities.Clear();
            member.ShallowAbilities?.Clear();
            member.GraftedAbilities?.Clear();
            member.PursuedAbility = null;
            member.CultivationMethodId = null;
            member.QiId = null;
            member.FruitionId = null;
            member.BorrowedLight = false;
            member.PatronId = null;
            member.GoldenCore = GoldenCoreState.None;
            member.ForbiddenTasks = null;
            member.HoldsTrial = false;
            member.Vow = BuddhistVow.None;
            // what another way had bound to it (L7 conversions): a shaman's method, a Profundity, a borrowed leap, a closed way
            member.ShamanMethod = ShamanMethod.None;
            member.ProfundityOf = null;
            member.BorrowedLeap = 0;
            member.ProgressionSealed = false;
        }

        /// <summary>Why the member may not take this vow now (French), or null when it may.</summary>
        public string VowRefusal(CharacterData member, BuddhistVow vow)
        {
            if (member == null || !member.IsAlive || !clan.LivingMembers.Contains(member)) return "introuvable";
            if (member.Path != CultivationPath.Buddhist) return "seul un moine fait vœu";
            if (vow == BuddhistVow.None) return "choisir un vœu";
            if (member.Vow != BuddhistVow.None) return "un vœu est déjà tenu";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (ForbiddenBy(vow).Contains(member.CurrentTask)) return "sa tâche actuelle est interdite par ce vœu : changez-la d'abord";
            return null;
        }

        /// <summary>The monk takes the vow: null when done, else why not (French).</summary>
        public string TakeVow(CharacterData member, BuddhistVow vow)
        {
            string refusal = VowRefusal(member, vow);
            if (refusal != null) return refusal;
            member.Vow = vow;
            ctx.Log.Info($"[Buddhism] {member.FullName} takes a vow: {vow}.");
            ctx.Events.TriggerVowTaken(member, vow);
            return null;
        }

        /// <summary>The tasks a vow forbids.</summary>
        public IReadOnlyList<TaskType> ForbiddenBy(BuddhistVow vow) =>
            Settings.VowTasks.TryGetValue(vow, out var tasks) ? tasks : new List<TaskType>();

        /// <summary>A non-violent monk who fought on the grid has broken its vow.</summary>
        private void BreakNonViolence(IReadOnlyList<CharacterData> fighters)
        {
            foreach (var monk in (fighters ?? new CharacterData[0]).Where(m => m != null && m.IsAlive && m.Vow == BuddhistVow.NonViolence).ToList())
                Break(monk);
        }

        /// <summary>The vow broken: the mandate wounded, the vow lost.</summary>
        private void Break(CharacterData monk)
        {
            var vow = monk.Vow;
            monk.Vow = BuddhistVow.None;
            wounds.ApplyDaoWound(monk);
            ctx.Log.Warning($"[Buddhism] {monk.FullName} breaks its vow ({vow}): its mandate is wounded.");
            ctx.Events.TriggerVowBroken(monk, vow);
        }

        /// <summary>
        /// A year: a modern temple fallen out with the clan may call back the souls it holds; every other monk cultivating
        /// nourishes its nature, and rises as far as its school allows.
        /// </summary>
        public void ProcessYear()
        {
            foreach (var captive in clan.LivingMembers.Where(m => m.CaptorFaction != null).ToList()) ConvertUnderDuress(captive);
            foreach (var monk in clan.LivingMembers.Where(m => m.Path == CultivationPath.Buddhist && m.CaptorFaction == null).ToList())
            {
                bool modern = monk.SubPath == CultivationSubPath.ModernBuddhism;
                var temple = factions.GetFactionByName(monk.Temple);
                if (modern && temple != null && temple.RelationWithPlayer < Settings.RecallBelow && ctx.Rng.Chance(Settings.RecallChance))
                {
                    Recall(monk, temple.Name);
                    continue;
                }
                if (modern && temple == null && monk.Realm > CultivationRealm.QiRefinement) LoseTheLentPower(monk); // the pyramid broken
                double lent = modern && temple != null ? 1 + Settings.LentSpeedPerRealm * (int)temple.HighestRealm : 1; // its temple's strength

                if (monk.Vow != BuddhistVow.None && ForbiddenBy(monk.Vow).Contains(monk.CurrentTask)) Break(monk); // a year at a forbidden task
                double vowed = monk.Vow != BuddhistVow.None ? 1 + Settings.VowSpeedBonus : 1;
                if (monk.CurrentTask == TaskType.Cultivation && !monk.ProgressionSealed)
                    monk.CultivationXP += (int)Math.Round((CultivationSystem.BaseYearlyXp + monk.SpiritualRoot / 2)
                        * (modern ? Settings.ModernSpeed : Settings.AncientSpeed) * vowed * lent);
                if (modern && temple == null) // nothing left to borrow: a Monk at most
                    TriallessClimb.Climb(monk, CultivationRealm.QiRefinement, PowerLadder.StageCount(CultivationRealm.QiRefinement), Rise);
                else TriallessClimb.Climb(monk, CultivationRealm.GoldenCore, modern ? Settings.ModernCeilingStage : Settings.AncientCeilingStage, Rise);
                if (!modern && monk.Realm == CultivationRealm.GoldenCore && monk.RealmStage >= Settings.AncientCeilingStage
                    && ctx.Rng.Chance(Settings.VenerableChance)) BecomeRevered(monk);
            }
            foreach (var child in clan.LivingMembers.Where(m => m.RebornMonk != null && m.Age >= Settings.RegainAge && m.CaptorFaction == null).ToList())
                Regain(child);
        }

        /// <summary>
        /// A temple holding a member of the clan may convert it under duress (L7; the wiki « Li Chengpan »): a realm lower, its
        /// way given up, it renounces the clan and serves the temple. Never a monk already, a bound beast nor a True Monarch.
        /// </summary>
        private void ConvertUnderDuress(CharacterData captive)
        {
            var temple = factions.GetFactionByName(captive.CaptorFaction);
            if (temple?.Path != CultivationPath.Buddhist || temple.School == null || captive.Species != Species.Human
                || captive.Path == CultivationPath.Buddhist || captive.Realm >= CultivationRealm.GoldenCore) return;
            if (!ctx.Rng.Chance(ctx.Content.Balance.Conversions.CaptiveConversionChance)) return;
            GiveUpItsWay(captive, temple);
            captive.CaptorFaction = null;
            captive.CapturedYear = null;
            if (!clan.Depart(captive)) return;
            ctx.Log.Warning($"[Buddhism] {temple.Name} converts its captive {captive.FullName}: it renounces the clan.");
            ctx.Events.TriggerCaptiveConverted(captive, temple.Name);
        }

        /// <summary>A modern monk whose temple is no more: what it borrowed fades, it is a Monk again (R14).</summary>
        private void LoseTheLentPower(CharacterData monk)
        {
            monk.Realm = CultivationRealm.QiRefinement;
            monk.RealmStage = 1;
            monk.CultivationXP = 0;
            monk.GoldenCore = GoldenCoreState.None;
            ctx.Log.Warning($"[Buddhism] {monk.FullName}'s temple is no more: the power it borrowed fades.");
            ctx.Events.TriggerLentPowerLost(monk, monk.Temple);
        }

        /// <summary>An ancient Dharma Master at its apogee becomes a Revered One: it no longer dies of age (R13).</summary>
        private void BecomeRevered(CharacterData monk)
        {
            monk.Realm = CultivationRealm.DaoEmbryo;
            monk.RealmStage = 1;
            monk.MaxLifespan = PowerLadder.Unbounded;
            ctx.Log.Info($"[Buddhism] {monk.FullName} becomes a Revered One.");
            ctx.Events.TriggerMonkRose(monk);
        }

        /// <summary>
        /// The Pure Lands (R13-14c): an ancient monk may choose its rebirth in the clan; a modern one's soul goes to its
        /// temple, which gives it back only to a clan in good standing.
        /// </summary>
        private void Departed(CharacterData dead, DeathCause cause)
        {
            if (dead.Path != CultivationPath.Buddhist || dead.Species != Species.Human || cause == DeathCause.SoulReplaced) return;
            if (dead.SubPath == CultivationSubPath.ModernBuddhism)
            {
                var temple = factions.GetFactionByName(dead.Temple);
                if (temple == null || temple.RelationWithPlayer < Settings.ModernRebirthRelation)
                {
                    ctx.Log.Info($"[Buddhism] {dead.Temple} keeps {dead.FullName}'s soul in its Pure Land.");
                    ctx.Events.TriggerSoulKept(dead, dead.Temple);
                    return;
                }
            }
            else if (!ctx.Rng.Chance(Settings.AncientRebirthChance)) return;
            souls.Add(new MonkSoul(dead.FullName, dead.Temple, dead.SubPath, dead.Realm, dead.RealmStage));
            ctx.Log.Info($"[Buddhism] {dead.FullName}'s soul awaits a child of the clan.");
        }

        /// <summary>The clan's next child — not one marrying in, nor an ancestor reborn — carries the first soul awaiting.</summary>
        private void Born(CharacterData child)
        {
            if (souls.Count == 0 || child.RebornFrom != null || child.RebornMonk != null || !child.IsAlive || child.Species != Species.Human
                || child.Age >= TaskRules.WorkingAge || child.FromFaction != null) return;
            var soul = souls[0];
            souls.RemoveAt(0);
            child.RebornMonk = soul;
            child.HasSpiritualOrifice = true; // the monk's soul opens the way
            ctx.Log.Info($"[Buddhism] {soul.Name} is reborn in {child.FullName}.");
            ctx.Events.TriggerMonkReborn(child, soul.Name);
        }

        /// <summary>The grown child regains the monk's rank, temple and school: its immortal way, if any, given up.</summary>
        private void Regain(CharacterData child)
        {
            var soul = child.RebornMonk;
            child.RebornMonk = null;
            if (child.Path != CultivationPath.Immortal || child.Realm >= CultivationRealm.GoldenCore) return; // another way already, or a True Monarch
            var realm = (CultivationRealm)Math.Max((int)soul.Realm, (int)child.Realm);
            int stage = realm == soul.Realm ? soul.Stage : child.RealmStage;
            BecomeMonk(child, soul.Temple, soul.School, realm, stage);
            child.OrificeKnown = true;
            child.MaxLifespan = Math.Max(child.MaxLifespan, PowerLadder.MaxLifespan(realm, stage));
            if (realm >= CultivationRealm.GoldenCore) child.GoldenCore = GoldenCoreState.DharmaMaster;
            if (realm >= CultivationRealm.DaoEmbryo) child.MaxLifespan = PowerLadder.Unbounded;
            ctx.Log.Info($"[Buddhism] {child.FullName} regains {soul.Name}'s rank: {RankCatalog.DisplayName(child)}.");
            ctx.Events.TriggerMonkRegained(child, soul.Name);
        }

        /// <summary>A monk enters its next rank: it lives at least that rank's span; past the Maha, a Dharma Master.</summary>
        private void Rise(CharacterData monk)
        {
            monk.MaxLifespan = Math.Max(monk.MaxLifespan, PowerLadder.MaxLifespan(monk.Realm, monk.RealmStage));
            if (monk.Realm == CultivationRealm.GoldenCore) monk.GoldenCore = GoldenCoreState.DharmaMaster;
            ctx.Log.Info($"[Buddhism] {monk.FullName} rises: {RankCatalog.DisplayName(monk)}.");
            ctx.Events.TriggerMonkRose(monk);
        }

        /// <summary>The temple holding the monk's soul calls it back: it leaves the clan, alive.</summary>
        private void Recall(CharacterData monk, string temple)
        {
            if (!clan.Depart(monk)) return;
            ctx.Log.Warning($"[Buddhism] {temple} calls back the soul of {monk.FullName}: it leaves the clan.");
            ctx.Events.TriggerSoulRecalled(monk, temple);
        }
    }
}

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
    /// dying cultivator may convert at a temple, paying an offering, to escape death. It gives up its immortal way — a realm
    /// lower, its foundation, abilities and method lost — and nourishes its nature and destiny, cultivating without trial at its
    /// temple's school's pace: the ancient school slow, up to the Dharma Master's apogee; the modern one faster, capped at the
    /// Dharma Master's beginning, and its temple holds its soul — fallen out with the clan, it may call the monk back. A True
    /// Monarch does not give up its position: only those below the Golden Core convert. See <see cref="BuddhismSettings"/>.
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
        }

        private BuddhismSettings Settings => ctx.Content.Balance.Buddhism;

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
            if (member.Path != CultivationPath.Immortal) return "déjà engagé(e) sur une autre voie";
            if (!SpiritualOrificeRules.CanCultivate(member) || (member.Realm == CultivationRealm.Embryonic && member.RealmStage <= 0))
                return "seul un cultivateur se convertit";
            if (member.Realm >= CultivationRealm.GoldenCore) return "un Vrai Monarque ne renonce pas à sa position";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (member.Retreat != Retreat.None) return "en retraite";
            if (PowerLadder.LifespanLimit(member) - member.Age > Settings.DyingYears) return "pas encore aux portes de la mort";
            return null;
        }

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
            resources.ConsumeSpiritStones(Settings.OfferingStones);
            factions.ChangeRelation(temple.ID, Settings.ConversionRelation);
            GiveUpTheImmortalWay(member, temple);
            member.MaxLifespan = member.Age + Settings.YearsGained; // death escaped

            ctx.Log.Info($"[Buddhism] {member.FullName} converts at {temple.Name}: {RankCatalog.DisplayName(member)}.");
            ctx.Events.TriggerBuddhistConverted(member, temple.Name);
            return true;
        }

        /// <summary>
        /// A debate lost (R13-14b): the temple converts an immortal below the Golden Core by force — no offering, no years
        /// gained, it is not dying. False when it cannot be (a True Monarch, a monk already, a bound beast, no such temple).
        /// </summary>
        public bool ForceConvert(CharacterData member, string templeName)
        {
            var temple = factions.GetFactionByName(templeName);
            if (member == null || !member.IsAlive || member.Species != Species.Human || member.Path != CultivationPath.Immortal
                || member.Realm >= CultivationRealm.GoldenCore || temple?.Path != CultivationPath.Buddhist || temple.School == null) return false;
            GiveUpTheImmortalWay(member, temple);
            ctx.Log.Info($"[Buddhism] {member.FullName} is converted by force at {temple.Name}: {RankCatalog.DisplayName(member)}.");
            ctx.Events.TriggerBuddhistConverted(member, temple.Name);
            return true;
        }

        /// <summary>The member becomes a monk of the temple's school, a realm lower, its immortal way given up.</summary>
        private static void GiveUpTheImmortalWay(CharacterData member, FactionData temple)
        {
            member.Path = CultivationPath.Buddhist;
            member.SubPath = temple.School.Value;
            member.Temple = temple.Name;
            member.Realm = (CultivationRealm)Math.Max((int)CultivationRealm.Embryonic, (int)member.Realm - 1); // a realm lower
            member.RealmStage = 1;
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
            foreach (var monk in clan.LivingMembers.Where(m => m.Path == CultivationPath.Buddhist && m.CaptorFaction == null).ToList())
            {
                bool modern = monk.SubPath == CultivationSubPath.ModernBuddhism;
                var temple = factions.GetFactionByName(monk.Temple);
                if (modern && temple != null && temple.RelationWithPlayer < Settings.RecallBelow && ctx.Rng.Chance(Settings.RecallChance))
                {
                    Recall(monk, temple.Name);
                    continue;
                }

                if (monk.Vow != BuddhistVow.None && ForbiddenBy(monk.Vow).Contains(monk.CurrentTask)) Break(monk); // a year at a forbidden task
                double vowed = monk.Vow != BuddhistVow.None ? 1 + Settings.VowSpeedBonus : 1;
                if (monk.CurrentTask == TaskType.Cultivation && !monk.ProgressionSealed)
                    monk.CultivationXP += (int)Math.Round((CultivationSystem.BaseYearlyXp + monk.SpiritualRoot / 2)
                        * (modern ? Settings.ModernSpeed : Settings.AncientSpeed) * vowed);
                TriallessClimb.Climb(monk, CultivationRealm.GoldenCore, modern ? Settings.ModernCeilingStage : Settings.AncientCeilingStage, Rise);
            }
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

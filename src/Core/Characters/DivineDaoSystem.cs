using System;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Divine Dao (R15; LORE.md §3.6; the wiki « Divine Dao »; the user's choices of 2026-10-13): « Profundity precedes
    /// power ». A cultivator of the clan's way from the Qi Cultivation to the Purple Mansion binds itself to a Profundity and
    /// becomes a Divine Attendant, keeping its realm:
    /// <list type="bullet">
    /// <item>borrowing a great partner's might, it weighs a realm higher while the pact holds — no progress of its own, and the
    /// pact ended, its might falls and its Dao is wounded;</item>
    /// <item>uniting with the clan's domain, it no longer dies of age but never leaves the domain, cultivating up to the
    /// Purple Mansion's Grand Perfection;</item>
    /// <item>owning a Profundity, it refines it by rituals that spend the clan's prestige, and at its apogee may condense a
    /// Divine Core — a Golden Core's peer, holding no Fruition.</item>
    /// </list>
    /// See <see cref="DivineDaoSettings"/>.
    /// </summary>
    public sealed class DivineDaoSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly PatronSystem patrons;
        private readonly WoundSystem wounds;

        public DivineDaoSystem(GameContext ctx, ClanManager clan, ResourceManager resources, PatronSystem patrons, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.patrons = patrons;
            this.wounds = wounds;
        }

        private DivineDaoSettings Settings => ctx.Content.Balance.DivineDao;

        /// <summary>A Divine Attendant united with the clan's domain: it never leaves it.</summary>
        public static bool IsUnited(CharacterData member) =>
            member?.Path == CultivationPath.Divine && member.SubPath == CultivationSubPath.UnitedProfundity;

        /// <summary>Why the member cannot become a Divine Attendant now (French), or null.</summary>
        public string AttendantRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive || !clan.LivingMembers.Contains(member)) return "introuvable";
            if (member.Species != Species.Human || member.Path != CultivationPath.Immortal) return "seul un cultivateur de la voie du clan se lie à une Profondeur";
            if (member.Realm < CultivationRealm.QiRefinement || member.Realm > CultivationRealm.PurpleMansion)
                return "de la Culture du Qi au Manoir Pourpre seulement";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (member.Retreat != Retreat.None) return "en retraite";
            return null;
        }

        /// <summary>The member borrows the great partner's might: null when done, else why not (French).</summary>
        public string Borrow(CharacterData member, string patronId)
        {
            string refusal = AttendantRefusal(member)
                ?? (patrons.Pacts.Any(p => p.PatronId == patronId) ? null : "il faut un pacte avec ce grand partenaire");
            if (refusal != null) return refusal;
            Become(member, CultivationSubPath.BorrowedProfundity);
            member.ProfundityOf = patronId;
            member.BorrowedLeap = Settings.BorrowLeap;
            return null;
        }

        /// <summary>The member unites with the clan's domain: null when done, else why not (French).</summary>
        public string Unite(CharacterData member)
        {
            string refusal = AttendantRefusal(member);
            if (refusal != null) return refusal;
            Become(member, CultivationSubPath.UnitedProfundity);
            member.MaxLifespan = PowerLadder.Unbounded; // undying while the domain stands
            if (member.CurrentTask != TaskType.None && !TaskRules.IsAllowed(member, member.CurrentTask, true, true)) member.CurrentTask = TaskType.None;
            return null;
        }

        /// <summary>The member owns a Profundity: null when done, else why not (French).</summary>
        public string Own(CharacterData member)
        {
            string refusal = AttendantRefusal(member);
            if (refusal != null) return refusal;
            Become(member, CultivationSubPath.OwnedProfundity);
            return null;
        }

        private void Become(CharacterData member, CultivationSubPath method)
        {
            member.Path = CultivationPath.Divine;
            member.SubPath = method;
            member.PursuedAbility = null;
            member.HoldsTrial = false;
            ctx.Log.Info($"[DivineDao] {member.FullName} becomes a Divine Attendant ({method}).");
            ctx.Events.TriggerDivineAttendant(member, method);
        }

        /// <summary>Why the owner cannot condense its Divine Core now (French), or null.</summary>
        public string CoreRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive || member.Path != CultivationPath.Divine || member.SubPath != CultivationSubPath.OwnedProfundity)
                return "seul qui possède une Profondeur condense un Noyau Divin";
            if (member.Realm != CultivationRealm.PurpleMansion || member.RealmStage < PowerLadder.StageCount(CultivationRealm.PurpleMansion))
                return "il faut la Grande Perfection du Manoir Pourpre";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (resources.Prestige < Settings.CorePrestige) return $"il faut {Settings.CorePrestige} de prestige";
            return null;
        }

        /// <summary>The owner tries its Divine Core: null when tried (won or lost), else why not (French).</summary>
        public string CondenseCore(CharacterData member)
        {
            string refusal = CoreRefusal(member);
            if (refusal != null) return refusal;
            resources.AddPrestige(-Settings.CorePrestige);
            if (!ctx.Rng.Chance(Settings.CoreChance))
            {
                ctx.Log.Warning($"[DivineDao] {member.FullName} fails to condense a Divine Core.");
                wounds.ApplyDaoWound(member);
                return null;
            }
            member.Realm = CultivationRealm.GoldenCore;
            member.RealmStage = 1;
            member.CultivationXP = 0;
            member.GoldenCore = GoldenCoreState.DivineCore;
            member.MaxLifespan = Math.Max(member.MaxLifespan, PowerLadder.MaxLifespan(CultivationRealm.GoldenCore, 1));
            ctx.Log.Info($"[DivineDao] {member.FullName} condenses a Divine Core.");
            ctx.Events.TriggerDivineCore(member);
            return null;
        }

        /// <summary>A year: a borrower whose pact ended falls; the united and the owners cultivate (the owners at a price).</summary>
        public void ProcessYear()
        {
            foreach (var member in clan.LivingMembers.Where(m => m.Path == CultivationPath.Divine && m.CaptorFaction == null).ToList())
            {
                switch (member.SubPath)
                {
                    case CultivationSubPath.BorrowedProfundity:
                        if (member.ProfundityOf != null && !patrons.Pacts.Any(p => p.PatronId == member.ProfundityOf)) LoseTheBorrowedMight(member);
                        break;
                    case CultivationSubPath.UnitedProfundity:
                        Cultivate(member);
                        break;
                    case CultivationSubPath.OwnedProfundity:
                        if (member.CurrentTask == TaskType.Cultivation && resources.Prestige >= Settings.OwnPrestigePerYear)
                        {
                            resources.AddPrestige(-Settings.OwnPrestigePerYear); // the rituals spend the clan's standing
                            Cultivate(member);
                        }
                        break;
                }
            }
        }

        private void Cultivate(CharacterData member)
        {
            if (member.CurrentTask != TaskType.Cultivation || member.ProgressionSealed || member.Realm >= CultivationRealm.GoldenCore) return;
            member.CultivationXP += (int)Math.Round((CultivationSystem.BaseYearlyXp + member.SpiritualRoot / 2) * Settings.Speed);
            TriallessClimb.Climb(member, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion), risen =>
            {
                risen.MaxLifespan = Math.Max(risen.MaxLifespan, PowerLadder.MaxLifespan(risen.Realm, risen.RealmStage));
                ctx.Events.TriggerBreakthroughSuccess(risen, risen.Realm);
            });
        }

        private void LoseTheBorrowedMight(CharacterData member)
        {
            ctx.Log.Warning($"[DivineDao] The pact with {member.ProfundityOf} has ended: {member.FullName}'s borrowed might falls.");
            ctx.Events.TriggerBorrowedMightLost(member, member.ProfundityOf);
            member.ProfundityOf = null;
            member.BorrowedLeap = 0;
            wounds.ApplyDaoWound(member);
        }
    }
}

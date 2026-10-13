using System;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Shamanic Talisman Dao (R17; LORE.md §3.5; the user's choices of 2026-10-13): a member whose foundation is of the
    /// Grand Shaman's lineage may pass to the shamanic Dao keeping its realm — back to its first stage, its abilities kept as
    /// talismans — at any time, or as a refuge when its Golden Core fails, escaping the Emissaries of Death (then by the Grand
    /// Shaman's own method, the Sacrifice). It walks one method: Transformation weighs a realm higher; Concealment hides it from
    /// the powers' ambushes, captures and Dao hunts; Sacrifice burns blood essence to cultivate far faster. It cultivates
    /// without trial up to the Purple Mansion's Grand Perfection, where a rite of blood may give it a shamanic Golden Core —
    /// a Golden Core's peer, holding no Fruition. See <see cref="ShamanismSettings"/>.
    /// </summary>
    public sealed class ShamanismSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly WoundSystem wounds;

        public ShamanismSystem(GameContext ctx, ClanManager clan, ResourceManager resources, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.wounds = wounds;
        }

        private ShamanismSettings Settings => ctx.Content.Balance.Shamanism;

        /// <summary>A shaman of the Concealment: the powers' hunts do not find it.</summary>
        public static bool IsConcealed(CharacterData member) =>
            member?.Path == CultivationPath.Shamanic && member.ShamanMethod == ShamanMethod.Concealment;

        /// <summary>Why the member cannot pass to the shamanic Dao now (French), or null.</summary>
        public string ConversionRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive || !clan.LivingMembers.Contains(member)) return "introuvable";
            if (member.Species != Species.Human || member.Path != CultivationPath.Immortal) return "seul un cultivateur de la voie du clan y passe";
            if (member.SubPath == CultivationSubPath.SpiritualNature) return "un élu de la Nature Spirituelle suit son destin";
            if (FoundationRef.Parse(member.FoundationId).FruitionId != Settings.GateFruition) return "il faut une fondation de la lignée du Grand Chaman";
            if (member.Realm >= CultivationRealm.GoldenCore) return "un Vrai Monarque ne quitte pas sa position";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (member.Retreat != Retreat.None) return "en retraite";
            return null;
        }

        /// <summary>The member passes to the shamanic Dao by the method: null when done, else why not (French).</summary>
        public string Convert(CharacterData member, ShamanMethod method)
        {
            string refusal = method == ShamanMethod.None ? "choisir une méthode" : ConversionRefusal(member);
            if (refusal != null) return refusal;
            Become(member, method);
            return null;
        }

        /// <summary>The refuge from a failed Golden Core: true when the member passed instead of becoming a demon.</summary>
        public bool Refuge(CharacterData member)
        {
            if (member == null || !member.IsAlive || member.Species != Species.Human || member.Path != CultivationPath.Immortal
                || member.SubPath == CultivationSubPath.SpiritualNature || member.Retreat != Retreat.None
                || FoundationRef.Parse(member.FoundationId).FruitionId != Settings.GateFruition || member.Realm >= CultivationRealm.GoldenCore) return false;
            Become(member, ShamanMethod.Sacrifice, announce: false); // the refuge tells its own tale
            ctx.Events.TriggerShamanRefuge(member);
            return true;
        }

        private void Become(CharacterData member, ShamanMethod method, bool announce = true)
        {
            member.Path = CultivationPath.Shamanic;
            member.SubPath = CultivationSubPath.ShamanicTalisman;
            member.ShamanMethod = method;
            member.RealmStage = 1; // its realm kept, back to its first stage
            member.CultivationXP = 0;
            member.GoldenCore = GoldenCoreState.None;
            member.PursuedAbility = null;
            member.HoldsTrial = false;
            member.BorrowedLeap = method == ShamanMethod.Transformation ? Settings.TransformLeap : 0; // weighs as the leap it takes
            ctx.Log.Info($"[Shamanism] {member.FullName} passes to the shamanic Dao ({method}).");
            if (announce) ctx.Events.TriggerShamanConverted(member, method);
        }

        /// <summary>Why the shaman cannot attempt its rite now (French), or null.</summary>
        public string CoreRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive || member.Path != CultivationPath.Shamanic) return "seul un chaman accomplit ce rite";
            if (member.Realm != CultivationRealm.PurpleMansion || member.RealmStage < PowerLadder.StageCount(CultivationRealm.PurpleMansion))
                return "il faut la Grande Perfection du Manoir Pourpre";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (resources.BloodEssence < Settings.CoreBlood) return $"il faut {Settings.CoreBlood} essences de sang";
            return null;
        }

        /// <summary>The rite of blood: null when tried (won or lost), else why not (French).</summary>
        public string CondenseCore(CharacterData member)
        {
            string refusal = CoreRefusal(member);
            if (refusal != null) return refusal;
            resources.ConsumeBlood(Settings.CoreBlood);
            if (!ctx.Rng.Chance(Settings.CoreChance))
            {
                ctx.Log.Warning($"[Shamanism] {member.FullName}'s rite of blood fails.");
                wounds.ApplyDaoWound(member);
                return null;
            }
            member.Realm = CultivationRealm.GoldenCore;
            member.RealmStage = 1;
            member.CultivationXP = 0;
            member.GoldenCore = GoldenCoreState.ShamanicEquivalent;
            member.MaxLifespan = Math.Max(member.MaxLifespan, PowerLadder.MaxLifespan(CultivationRealm.GoldenCore, 1));
            ctx.Log.Info($"[Shamanism] {member.FullName} forges a shamanic Golden Core.");
            ctx.Events.TriggerShamanicCore(member);
            return null;
        }

        /// <summary>A year: every free shaman cultivating gains its pace — a Sacrifice shaman far more when blood feeds it.</summary>
        public void ProcessYear()
        {
            foreach (var shaman in clan.LivingMembers.Where(m => m.Path == CultivationPath.Shamanic && m.CaptorFaction == null).ToList())
            {
                if (shaman.CurrentTask != TaskType.Cultivation || shaman.ProgressionSealed || shaman.Realm >= CultivationRealm.GoldenCore) continue;
                if (shaman.Realm == CultivationRealm.PurpleMansion && shaman.RealmStage >= PowerLadder.StageCount(CultivationRealm.PurpleMansion))
                    continue; // at its apogee only the rite of blood leads further: no blood burnt for nothing
                double pace = Settings.Speed;
                if (shaman.ShamanMethod == ShamanMethod.Sacrifice && resources.ConsumeBlood(Settings.SacrificeBlood)) pace *= Settings.SacrificeSpeed;
                shaman.CultivationXP += (int)Math.Round((CultivationSystem.BaseYearlyXp + shaman.SpiritualRoot / 2) * pace);
                TriallessClimb.Climb(shaman, CultivationRealm.PurpleMansion, PowerLadder.StageCount(CultivationRealm.PurpleMansion), risen =>
                {
                    risen.MaxLifespan = Math.Max(risen.MaxLifespan, PowerLadder.MaxLifespan(risen.Realm, risen.RealmStage));
                    ctx.Events.TriggerBreakthroughSuccess(risen, risen.Realm);
                });
            }
        }
    }
}

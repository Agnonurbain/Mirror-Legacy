using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Mirror;
using MirrorChronicles.Session;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// The Heavenly Embryo Devil Dao (R12; LORE.md §3.2; the wiki « Cultivation System »; the user's choices of 2026-10-13):
    /// after the six chakras a member may form a Diverse Mansion instead of entering Qi Cultivation — no method nor Qi, but
    /// blood essence; a Qi cultivator may still refine its Qi away into one, for more blood (L7 conversions). It cultivates at its own pace and climbs its stages freely; each realm thirsts for blood: the Unified
    /// Furnace (its foundation of the member's element), then the single ascent to the Purple Mansion, one trial where the
    /// immortal way has four. From the Purple Mansion it condenses its abilities and forges its core as any other. Blood comes
    /// from captured beasts bled and from foes struck down on the grid beside a devil; a devil found out earns the orthodox
    /// powers' distrust. See <see cref="DevilDaoSettings"/>.
    /// </summary>
    public sealed class DevilDaoSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly TalismanSystem talismans;
        private readonly WoundSystem wounds;

        public DevilDaoSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, TalismanSystem talismans, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.talismans = talismans;
            this.wounds = wounds;
            ctx.Events.OnClanBattleEnded += Harvest;
        }

        private DevilDaoSettings Settings => ctx.Content.Balance.DevilDao;

        /// <summary>True for a devil of the Heavenly Embryo.</summary>
        public static bool IsDevil(CharacterData member) =>
            member?.Path == CultivationPath.Devil && member.SubPath == CultivationSubPath.HeavenlyEmbryoDemon;

        /// <summary>Why the member cannot form a Diverse Mansion now (French), or null.</summary>
        public string FormRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive || !clan.LivingMembers.Contains(member)) return "introuvable";
            if (member.Species != Species.Human || member.Path != CultivationPath.Immortal || member.SubPath != CultivationSubPath.PurpleMansionGoldenCore)
                return "seul un cultivateur de la voie du clan peut se tourner vers le Diable";
            bool afterTheChakras = member.Realm == CultivationRealm.Embryonic && member.RealmStage >= PowerLadder.StageCount(CultivationRealm.Embryonic);
            if (!afterTheChakras && member.Realm != CultivationRealm.QiRefinement)
                return "seulement après les six chakras, ou en raffinant son Qi à la Culture du Qi";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (member.Retreat != Retreat.None) return "en retraite";
            int blood = BloodToForm(member);
            if (resources.BloodEssence < blood) return $"il faut {blood} essence(s) de sang";
            return null;
        }

        /// <summary>
        /// The blood to form a Diverse Mansion: after the chakras, the Dao's own price; a Qi cultivator refining its Qi away
        /// pays more (the Golden Feather's disciples turned devils, L7 conversions).
        /// </summary>
        public int BloodToForm(CharacterData member) =>
            member.Realm == CultivationRealm.QiRefinement ? ctx.Content.Balance.Conversions.QiFormBlood : Settings.FormBlood;

        /// <summary>The member forms its Diverse Mansion: null when done, else why not (French).</summary>
        public string FormDiverseMansion(CharacterData member)
        {
            string refusal = FormRefusal(member);
            if (refusal != null) return refusal;
            resources.ConsumeBlood(BloodToForm(member));
            member.Path = CultivationPath.Devil;
            member.SubPath = CultivationSubPath.HeavenlyEmbryoDemon;
            member.Realm = CultivationRealm.QiRefinement; // the Diverse Mansion, as the Qi Cultivation
            member.RealmStage = 1;
            member.CultivationXP = 0;
            member.CultivationMethodId = null; // no method, no Qi: blood
            member.QiId = null;
            member.MaxLifespan = PowerLadder.LifespanAfterAdvance(member);
            ctx.Log.Info($"[DevilDao] {member.FullName} forms a Diverse Mansion: the Heavenly Embryo Devil Dao.");
            ctx.Events.TriggerDiverseMansionFormed(member);
            return null;
        }

        /// <summary>Why the beast cannot be bled now (French), or null.</summary>
        public string BleedRefusal(CapturedBeast beast) =>
            beast == null || !resources.Beasts.Contains(beast) ? "le clan ne détient pas cette bête" : null;

        /// <summary>The beast is bled for its blood essence, by its realm: null when done, else why not (French).</summary>
        public string Bleed(CapturedBeast beast)
        {
            string refusal = BleedRefusal(beast);
            if (refusal != null) return refusal;
            resources.ConsumeBeast(beast);
            talismans.AnswerToTheOwner(beast); // its owner may learn of it, as of an offering
            int blood = Settings.BloodByRealm.TryGetValue(beast.Realm, out int b) ? b : 1;
            resources.AddBlood(blood);
            ctx.Log.Info($"[DevilDao] A beast of the {beast.Realm} is bled: {blood} blood essence.");
            return null;
        }

        /// <summary>Foes struck down beside a devil leave their blood.</summary>
        private void Harvest(IReadOnlyList<CharacterData> fighters, int foesDown)
        {
            if (foesDown <= 0 || fighters == null || !fighters.Any(IsDevil)) return;
            resources.AddBlood(foesDown * Settings.BloodPerKill);
        }

        /// <summary>
        /// A year: every free devil cultivating gains its pace, climbs what its blood allows, and may be found out.
        /// </summary>
        public void ProcessYear()
        {
            foreach (var devil in clan.LivingMembers.Where(m => IsDevil(m) && m.CaptorFaction == null).ToList())
            {
                if (devil.CurrentTask == TaskType.Cultivation && !devil.ProgressionSealed)
                    devil.CultivationXP += (int)Math.Round((CultivationSystem.BaseYearlyXp + devil.SpiritualRoot / 2) * Settings.Speed);
                Climb(devil);
                if (ctx.Rng.Chance(Settings.DiscoveryChance)) FoundOut(devil);
            }
        }

        /// <summary>Stages freely; the Unified Furnace and the Purple Mansion each for their blood.</summary>
        private void Climb(CharacterData devil)
        {
            while (devil.Realm < CultivationRealm.PurpleMansion)
            {
                int required = PowerLadder.XpForNextStage(devil.Realm);
                if (required <= 0 || devil.CultivationXP < required) return;
                if (devil.RealmStage < PowerLadder.StageCount(devil.Realm))
                {
                    devil.CultivationXP -= required;
                    devil.RealmStage++;
                    continue;
                }
                if (devil.Realm == CultivationRealm.QiRefinement)
                {
                    if (!resources.ConsumeBlood(Settings.FurnaceBlood)) return; // it thirsts
                    devil.CultivationXP -= required;
                    devil.FoundationId ??= FoundationOf(devil.Affinity);
                    Enter(devil, CultivationRealm.Foundation);
                    continue;
                }
                // the single ascent to the Purple Mansion
                if (!resources.ConsumeBlood(Settings.MansionBlood)) return;
                devil.CultivationXP -= required;
                if (ctx.Rng.Chance(Settings.AscentChance))
                {
                    Enter(devil, CultivationRealm.PurpleMansion);
                    ctx.Events.TriggerPurpleMansionAscent(devil);
                }
                else
                {
                    ctx.Log.Warning($"[DevilDao] {devil.FullName}'s single ascent fails.");
                    wounds.ApplyDaoWound(devil);
                }
                return;
            }
        }

        private void Enter(CharacterData devil, CultivationRealm realm)
        {
            devil.Realm = realm;
            devil.RealmStage = 1;
            devil.MaxLifespan = PowerLadder.LifespanAfterAdvance(devil);
            ctx.Log.Info($"[DevilDao] {devil.FullName} reaches {RankCatalog.DisplayName(devil)}.");
            ctx.Events.TriggerBreakthroughSuccess(devil, realm);
        }

        /// <summary>The foundation the blood shapes: that of a known Qi of the member's element (any, else).</summary>
        private string FoundationOf(Element element)
        {
            var qi = ctx.Content.Qi.Where(q => q.Foundation != null && !q.Vanished).OrderBy(q => q.Id, StringComparer.Ordinal).ToList();
            return (qi.FirstOrDefault(q => q.Element == element) ?? qi.FirstOrDefault())?.Foundation;
        }

        /// <summary>A devil found out: the orthodox powers distrust the clan.</summary>
        private void FoundOut(CharacterData devil)
        {
            foreach (var power in factions.Factions.Where(f => f.Path == CultivationPath.Immortal || f.Path == CultivationPath.Buddhist).ToList())
                factions.ChangeRelation(power.ID, -Settings.DiscoveryDistrust);
            ctx.Log.Warning($"[DevilDao] The orthodox powers learn that {devil.FullName} walks the Devil Dao.");
            ctx.Events.TriggerDevilFoundOut(devil);
        }
    }
}

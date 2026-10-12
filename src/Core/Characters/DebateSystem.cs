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
    /// The Dao debates between the clan and the temples (R13-14b; LORE.md §3.7; the user's choices of 2026-10-12): a temple
    /// may challenge the clan — it answers with a champion or refuses, losing face — and the clan may challenge a temple, each
    /// once in a while. The odds weigh the debater's rank against the temple's, its mind, a monk's training and a kept vow. A win
    /// brings face and warmth; a loss costs them, and befalls the debater: an immortal below the Golden Core may be converted
    /// by force, or else its mandate is wounded; a monk's Dharma path breaks a realm lower. See <see cref="DebateSettings"/>.
    /// </summary>
    public sealed class DebateSystem
    {
        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly BuddhismSystem buddhism;
        private readonly WoundSystem wounds;
        private readonly Dictionary<string, int> lastDebates = new Dictionary<string, int>();

        public DebateSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions, BuddhismSystem buddhism, WoundSystem wounds)
        {
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.buddhism = buddhism;
            this.wounds = wounds;
        }

        private BuddhismSettings Vows => ctx.Content.Balance.Buddhism;
        private DebateSettings Settings => Vows.Debates;

        /// <summary>A temple's challenge awaiting the clan's champion, or null.</summary>
        public Debate Pending { get; private set; }

        /// <summary>The year each temple last debated the clan.</summary>
        public IReadOnlyDictionary<string, int> LastDebates => lastDebates;

        /// <summary>What the last debate came to (for the screen's report), or null.</summary>
        public DebateFate? LastFate { get; private set; }

        /// <summary>Restores the pending challenge and the temples' last debates of a save.</summary>
        public void Restore(Debate pending, IReadOnlyDictionary<string, int> last)
        {
            Pending = pending != null && Temple(pending.Temple) != null ? pending : null;
            lastDebates.Clear();
            foreach (var kv in last ?? new Dictionary<string, int>()) lastDebates[kv.Key] = kv.Value;
        }

        /// <summary>The temples of the world (a Buddhist power with a school).</summary>
        public IEnumerable<FactionData> Temples => factions.Factions.Where(f => f.Path == CultivationPath.Buddhist && f.School != null);

        private FactionData Temple(string name) => Temples.FirstOrDefault(t => t.Name == name);

        /// <summary>Why the member cannot debate (French), or null: a free cultivator of the clan, human.</summary>
        public string DebaterRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive || !clan.LivingMembers.Contains(member)) return "introuvable";
            if (member.Species != Species.Human) return "une bête liée ne débat pas";
            if (!member.OrificeKnown || !SpiritualOrificeRules.CanCultivate(member) || (member.Realm == CultivationRealm.Embryonic && member.RealmStage <= 0))
                return "seul un cultivateur débat du Dao";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (member.Retreat != Retreat.None) return "en retraite";
            return null;
        }

        /// <summary>The member's chance in percent to win a debate against the temple.</summary>
        public int Odds(CharacterData member, string templeName)
        {
            var templeRealm = Temple(templeName)?.HighestRealm ?? CultivationRealm.Foundation;
            int gap = (int)PowerLadder.EffectiveRealm(member) - (int)templeRealm;
            bool monk = member.Path == CultivationPath.Buddhist;
            int chance = Settings.BaseChance + gap * Settings.RealmStep + (int)Math.Round((member.MentalStability - 70) * Settings.StabilityWeight)
                + (monk ? Settings.MonkBonus : 0) + (monk && member.Vow != BuddhistVow.None ? Vows.VowDebateBonus : 0);
            return Math.Clamp(chance, Settings.MinChance, Settings.MaxChance);
        }

        /// <summary>Why the clan cannot challenge the temple with this member now (French), or null.</summary>
        public string ChallengeRefusal(CharacterData member, string templeName)
        {
            string refusal = DebaterRefusal(member);
            if (refusal != null) return refusal;
            if (Temple(templeName) == null) return "on ne débat du Dao qu'avec un temple";
            if (Pending?.Temple == templeName) return "ce temple attend déjà la réponse du clan";
            if (lastDebates.TryGetValue(templeName, out int year) && ctx.Clock.Year < year + Settings.CooldownYears)
                return $"ce temple a débattu avec le clan en l'an {year}";
            return null;
        }

        /// <summary>The clan challenges the temple with its member: null when the debate was held, else why not (French).</summary>
        public string Challenge(CharacterData member, string templeName)
        {
            string refusal = ChallengeRefusal(member, templeName);
            if (refusal != null) return refusal;
            Settle(member, Temple(templeName));
            return null;
        }

        /// <summary>The clan answers the temple's challenge with its champion: null when held, else why not (French).</summary>
        public string Answer(CharacterData champion)
        {
            if (Pending == null) return "aucun débat en attente";
            string refusal = DebaterRefusal(champion);
            if (refusal != null) return refusal;
            var temple = Temple(Pending.Temple);
            Pending = null;
            if (temple == null) return "le temple n'est plus";
            Settle(champion, temple);
            return null;
        }

        /// <summary>The clan refuses the temple's challenge: it loses face, the temple looks down on it.</summary>
        public string Decline()
        {
            if (Pending == null) return "aucun débat en attente";
            var temple = Temple(Pending.Temple);
            Pending = null;
            if (temple == null)
            {
                LastFate = null;
                return "le temple n'est plus";
            }
            lastDebates[temple.Name] = ctx.Clock.Year;
            resources.AddPrestige(-Settings.RefusePrestige);
            factions.ChangeRelation(temple.ID, -Settings.RefuseRelation);
            LastFate = DebateFate.Shamed;
            ctx.Log.Info($"[Debates] The clan declines {temple.Name}'s debate: it loses face.");
            ctx.Events.TriggerDebateSettled(null, temple.Name, DebateFate.Shamed);
            return null;
        }

        /// <summary>A year: a challenge left unanswered is a refusal; a temple may challenge the clan, if it has a debater.</summary>
        public void ProcessYear()
        {
            if (Pending != null && Pending.Year < ctx.Clock.Year) Decline();
            if (Pending != null || !clan.LivingMembers.Any(m => DebaterRefusal(m) == null)) return;
            foreach (var temple in Temples.OrderBy(t => t.Name, StringComparer.Ordinal).ToList())
            {
                if (lastDebates.TryGetValue(temple.Name, out int year) && ctx.Clock.Year < year + Settings.CooldownYears) continue;
                if (!ctx.Rng.Chance(Settings.ChallengeChance)) continue;
                Pending = new Debate(temple.Name, ctx.Clock.Year);
                ctx.Log.Info($"[Debates] {temple.Name} challenges the clan to a Dao debate.");
                ctx.Events.TriggerDebateChallenge(temple.Name);
                return;
            }
        }

        /// <summary>The debate held: a roll on the odds, and what follows.</summary>
        private void Settle(CharacterData debater, FactionData temple)
        {
            lastDebates[temple.Name] = ctx.Clock.Year;
            int odds = Odds(debater, temple.Name);
            DebateFate fate;
            if (ctx.Rng.Next(1, 101) <= odds)
            {
                resources.AddPrestige(Settings.WinPrestige);
                factions.ChangeRelation(temple.ID, Settings.WinRelation);
                fate = DebateFate.Won;
            }
            else
            {
                resources.AddPrestige(-Settings.LossPrestige);
                factions.ChangeRelation(temple.ID, -Settings.LossRelation);
                fate = Befall(debater, temple);
            }
            LastFate = fate;
            ctx.Log.Info($"[Debates] {debater.FullName} debates {temple.Name} ({odds}%): {fate}.");
            ctx.Events.TriggerDebateSettled(debater, temple.Name, fate);
        }

        /// <summary>What a lost debate does to the debater.</summary>
        private DebateFate Befall(CharacterData debater, FactionData temple)
        {
            if (debater.Path == CultivationPath.Buddhist && debater.Realm > CultivationRealm.Embryonic) // its Dharma path breaks: a realm lower
            {
                debater.Realm = (CultivationRealm)Math.Max((int)CultivationRealm.Embryonic, (int)debater.Realm - 1);
                debater.RealmStage = 1;
                debater.CultivationXP = 0;
                if (debater.Realm < CultivationRealm.GoldenCore) debater.GoldenCore = GoldenCoreState.None;
                return DebateFate.Fell;
            }
            if (debater.Path == CultivationPath.Immortal && debater.Realm < CultivationRealm.GoldenCore && ctx.Rng.Chance(Settings.ForcedConversionChance) && buddhism.ForceConvert(debater, temple.Name))
                return DebateFate.Converted;
            wounds.ApplyDaoWound(debater); // its mandate wounded
            return DebateFate.Wounded;
        }
    }
}

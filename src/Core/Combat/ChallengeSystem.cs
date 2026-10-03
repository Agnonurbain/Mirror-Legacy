using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Clan;
using MirrorChronicles.Data;
using MirrorChronicles.Diplomacy;
using MirrorChronicles.Economy;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Combat
{
    /// <summary>
    /// A rival's challenge (G6; the random event « Défi d'un rival »): a power sends rivals of the rank of the clan's best
    /// free fighter. The clan picks who fights — free cultivators it knows — and the battle is played on the grid; the
    /// victor takes the wager; a challenge by the rules, the clan's fallen yield gravely wounded, unless a blow was not
    /// held back; its survivors carry their wounds. A power that hates the clan may challenge it to the death: its fallen
    /// always die, and fleeing costs far more face. A refusal, or silence until the
    /// next year, costs face with the challenger. Each action answers with its refusal, or null when done.
    /// </summary>
    public sealed class ChallengeSystem
    {
        private const int RivalMinRoot = 30;
        private const int RivalMaxRoot = 80;

        private readonly GameContext ctx;
        private readonly ClanManager clan;
        private readonly ResourceManager resources;
        private readonly FactionManager factions;
        private readonly TechniqueLibrary techniques;
        private readonly WoundSystem wounds;
        private readonly SuspicionLedger suspicion;
        private Challenge fought; // the challenge whose battle is under way

        public ChallengeSystem(GameContext ctx, ClanManager clan, ResourceManager resources, FactionManager factions,
            TechniqueLibrary techniques, WoundSystem wounds, SuspicionLedger suspicion)
        {
            this.suspicion = suspicion;
            this.ctx = ctx;
            this.clan = clan;
            this.resources = resources;
            this.factions = factions;
            this.techniques = techniques;
            this.wounds = wounds;
            ctx.Events.OnRandomEventOccurred += e =>
            {
                if (e.EventType == RandomEventType.RivalChallenge && Pending == null && Current == null && factions.RandomFaction() is { } power)
                    Issue(power);
            };
        }

        private ChallengeSettings Settings => ctx.Content.Balance.Challenges;

        public Challenge Pending { get; private set; }

        /// <summary>The battle under way, until it is concluded; never saved (it is fought on its screen).</summary>
        public Battle Current { get; private set; }

        public void Restore(Challenge pending) => Pending = pending;

        /// <summary>Why this member cannot fight, or null when it can.</summary>
        public string FighterRefusal(CharacterData member)
        {
            if (member == null || !member.IsAlive) return "introuvable";
            if (member.CaptorFaction != null) return "captif ailleurs";
            if (!member.OrificeKnown || !SpiritualOrificeRules.CanCultivate(member)) return "ne cultive pas";
            return null;
        }

        /// <summary>The challenge a power sends, the rivals of the rank of the clan's best free fighter; null when none can fight.</summary>
        public Challenge Issue(FactionData power)
        {
            var best = clan.LivingMembers.Where(m => FighterRefusal(m) == null)
                .OrderByDescending(m => (int)m.Realm).ThenByDescending(m => m.RealmStage).FirstOrDefault();
            if (power == null || best == null) return null;
            var s = Settings;
            bool grudge = power.RelationWithPlayer <= s.DeathGrudgeRelation || suspicion.OfClan(power.Name) >= s.DeathGrudgeSuspicion;
            // hatred wants blood — shown by the deed, never by a figure (D7): like a war declared on suspicion, it may let the
            // player guess a hidden grudge
            bool toTheDeath = grudge && ctx.Rng.Chance(s.DeathChallengeChance);
            Pending = new Challenge(power.Name, ctx.Clock.Year, best.Realm, best.RealmStage, ctx.Rng.Next(1, s.MaxRivals + 1), ctx.Rng.Next(),
                toTheDeath);
            ctx.Log.Info($"[Challenge] {power.Name} challenges the clan: {Pending.Rivals} rival(s).");
            return Pending;
        }

        /// <summary>The rivals of a challenge, the same each time (drawn from its seed).</summary>
        public IReadOnlyList<CharacterData> Rivals(Challenge challenge)
        {
            var rng = new Random(challenge.Seed);
            var names = ctx.Content.Names;
            return Enumerable.Range(0, challenge.Rivals).Select(i =>
            {
                bool isMale = rng.Next(2) == 0;
                var pool = isMale ? names.Male : names.Female;
                return new CharacterData
                {
                    ID = $"rival-{challenge.Seed}-{i}",
                    FirstName = pool.Count == 0 ? "Rival" : pool[rng.Next(pool.Count)],
                    LastName = names.OutsiderFamilies.Count == 0 ? challenge.Faction : names.OutsiderFamilies[rng.Next(names.OutsiderFamilies.Count)],
                    IsMale = isMale,
                    Age = 20 + rng.Next(40),
                    Realm = challenge.Realm,
                    RealmStage = challenge.Stage,
                    SpiritualRoot = rng.Next(RivalMinRoot, RivalMaxRoot + 1),
                    HasSpiritualOrifice = true,
                    OrificeKnown = true,
                    FromFaction = challenge.Faction
                };
            }).ToList();
        }

        /// <summary>The clan answers with these fighters: the battle opens on the grid.</summary>
        public string Accept(IReadOnlyList<string> fighterIds)
        {
            if (Pending == null) return "aucun défi en attente";
            var ids = (fighterIds ?? Array.Empty<string>()).Distinct().ToList();
            if (ids.Count == 0) return "il faut au moins un combattant";
            if (ids.Count > Settings.MaxFighters) return $"au plus {Settings.MaxFighters} combattants";
            var fighters = ids.Select(clan.FindById).ToList();
            var unfit = fighters.Select(FighterRefusal).FirstOrDefault(r => r != null);
            if (unfit != null) return unfit;

            fought = Pending;
            Pending = null;
            Current = Battle.Start(fighters, Rivals(fought), ctx.Rng, ctx.Log, findTechnique: techniques.Find, gap: ctx.Content.Balance.RealmGap);
            return null;
        }

        /// <summary>Once the battle is over: the wager, the fallen and the wounded, and the chronicle.</summary>
        public string Conclude()
        {
            if (Current == null) return "aucune bataille en cours";
            if (!Current.IsOver) return "la bataille n'est pas finie";
            Current.ResolveAftermath(clan, wounds, fought.ToTheDeath ? null : () => ctx.Rng.Chance(Settings.DeathChance)); // by the rules, the fallen yield
            var outcome = Current.State switch
            {
                CombatState.Victory => ChallengeOutcome.Won,
                CombatState.Defeat => ChallengeOutcome.Lost,
                _ => ChallengeOutcome.Withdrawn
            };
            if (outcome == ChallengeOutcome.Won) resources.AddSpiritStones(Settings.Wager);
            else if (outcome == ChallengeOutcome.Lost) resources.ConsumeSpiritStones(Math.Min(Settings.Wager, resources.SpiritStones));
            ctx.Events.TriggerChallengeSettled(fought.Faction, outcome);
            Current = null;
            fought = null;
            return null;
        }

        public string Decline()
        {
            if (Pending == null) return "aucun défi en attente";
            LoseFace(Pending);
            return null;
        }

        /// <summary>A challenge left unanswered past its year lapses: silence is a refusal.</summary>
        public void ProcessYear()
        {
            if (Pending != null && ctx.Clock.Year > Pending.Year) LoseFace(Pending);
        }

        private void LoseFace(Challenge challenge)
        {
            int lost = (int)Math.Round(Settings.DeclineRelation * (challenge.ToTheDeath ? Settings.DeathDeclineFactor : 1)); // fleeing a death duel shames more
            if (factions.GetFactionByName(challenge.Faction) is { } power) factions.ChangeRelation(power.ID, lost);
            ctx.Events.TriggerChallengeSettled(challenge.Faction, ChallengeOutcome.Declined);
            Pending = null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;
using MirrorChronicles.World;

namespace MirrorChronicles.Presentation
{
    /// <summary>
    /// The clan's chronicle as the player reads it: births, deaths, breakthroughs, events and what the clan learns, one French
    /// line each, dated by year. Keeps the most recent entries only. (The full Annals arrive with L6.)
    /// </summary>
    public sealed class Chronicle
    {
        public const int DefaultCapacity = 200;

        private readonly GameSession session;
        private readonly int capacity;
        private readonly List<string> entries = new List<string>();

        public IReadOnlyList<string> Entries => entries;
        public event Action<string> OnEntryAdded;

        private readonly HashSet<string> fell = new HashSet<string>(); // a fall is told once, not again as an absorption

        private static string RegionName(GameSession session, string regionId) =>
            session.Context.Content.Regions.FirstOrDefault(r => r.Id == regionId)?.Name ?? regionId;

        public Chronicle(GameSession session, int capacity = DefaultCapacity)
        {
            this.session = session;
            this.capacity = Math.Max(1, capacity);

            var bus = session.Events;
            bus.OnCharacterBorn += c => Add(c.Age == 0 ? $"naissance de {c.FullName}." : $"{c.FullName} rejoint le clan.");
            bus.OnCharacterDied += (c, cause) => Add($"{c.FullName} meurt {ClanDomainView.DeathLabel(cause)}.");
            bus.OnBreakthroughSuccess += (c, realm) => Add($"{c.FullName} atteint {RankCatalog.DisplayName(c)}.");
            bus.OnBreakthroughFailed += c => Add($"{c.FullName} échoue à sa percée.");
            bus.OnRandomEventOccurred += e => Add($"{e.Name} — {e.Description}");
            bus.OnStoryEventRaised += e => Add($"{e.Name}.");
            bus.OnMirrorSeized += faction => Add($"{faction} s'empare du miroir : le secret du clan est perdu.");
            bus.OnMemberCaptured += (c, faction) => Add($"{c.FullName} est enlevé(e) par {faction}.");
            bus.OnMemberFreed += c => Add($"{c.FullName} est libre.");
            bus.OnAgentCaught += faction => Add($"un agent de {faction} tombe entre les mains du clan.");
            bus.OnClanAbsorbed += suzerain => Add($"{suzerain} absorbe le clan : il n'est plus à lui-même.");
            bus.OnPowerAbsorbed += (vassal, suzerain) => { if (!fell.Remove(vassal)) Add($"{Who(suzerain)} absorbe {vassal}."); };
            bus.OnCoalitionFormed += members => Add($"une coalition se forme contre le clan : {string.Join(", ", members)}.");
            bus.OnCallToArms += (ally, attacker) => Add($"{ally}, attaquée par {attacker}, appelle le clan aux armes.");
            bus.OnTheft += (what, thief) => Add(thief == null ? $"vol au domaine : {what}, sans que l'on sache qui." : $"vol au domaine : {what} ; le voleur, de {thief}, est pris.");
            bus.OnProbeSpotted += prober => Add($"des gens de {prober} sont surpris à sonder le clan.");
            bus.OnPatronWrath += patron => Add($"{patron} tourne sa colère contre le clan.");
            bus.OnWarBegun += (attacker, defender) => Add($"{Who(attacker)} fait la guerre {(defender == World.SecretBook.ClanHolder ? "au clan" : "à " + defender)}.");
            bus.OnPeace += (a, b) => Add($"paix entre {Who(a, object_: true)} et {Who(b, object_: true)}.");
            bus.OnChallengeSettled += (faction, outcome) => Add(outcome switch
            {
                ChallengeOutcome.Won => $"le clan relève le défi de {faction} et l'emporte.",
                ChallengeOutcome.Lost => $"le clan relève le défi de {faction} et le perd.",
                ChallengeOutcome.Withdrawn => $"le défi de {faction} s'achève sans vainqueur.",
                _ => $"le clan se dérobe au défi de {faction}."
            });
            bus.OnExtortion += power => Add($"{power} convoite le trésor du clan et exige le prix de sa protection.");
            bus.OnDaoHuntFoiled += power => Add($"des gens de {power} fondent sur un Dao mûr du clan ; ils sont repoussés.");
            bus.OnHunt += (_, captured) => Add(captured ? "une chasse du clan ramène une bête spirituelle." : "une chasse du clan revient les mains vides.");
            bus.OnAccordDenounced += (rival, patron) => Add($"{rival} dénonce l'accord de {patron} avec le clan : {patron}, exposé, le renie.");
            bus.OnMemberLured += (member, rival) => Add($"{member.FullName} quitte le clan pour {rival}.");
            bus.OnPatronDemand += (s, d) => Add($"{s.Power} réclame son dû : {d.Name}. Le clan doit répondre.");
            bus.OnPatronDesignResisted += (s, d) => Add($"le clan résiste à {s.Power}, qui ne l'oubliera pas.");
            bus.OnPatronOffer += offer => Add($"{offer.Power} offre au clan une méthode qui mène au Manoir Pourpre. Que cache ce don ?");
            bus.OnPatronDesignDue += (s, d) => Add(session.Sponsorships.IsRevealed(s) ? $"le dessein de {s.Power} arrive à son terme : {d.Name}." : $"{s.Power} semble attendre quelque chose du clan.");
            bus.OnSectFounded += () => Add("le clan fonde sa secte : les pics en haut, la ville en bas.");
            bus.OnRuinsRevealed += shard => Add($"des ruines anciennes sont découvertes ; le miroir y sent {shard.Name}.");
            bus.OnPowerFell += (fallen, heir) => { fell.Add(fallen); Add($"{fallen} se disperse ; {Who(heir)} recueille ses restes."); };
            bus.OnPowerRose += (power, parent) => Add(parent == null ? $"la {power.Name} s'élève parmi les puissances."
                : $"une branche quitte {parent} et fonde {power.Name}.");
            bus.OnKingdomFounded += power => Add($"{power.Name} fonde un royaume.");
            bus.OnElderDied += (power, elder, demon) =>
            {
                if (elder.Realm >= CultivationRealm.PurpleMansion)
                    Add(demon ? $"{elder.Name}, de {power.Name}, échoue au Noyau d'Or : un démon d'essence métallique naît."
                              : $"{elder.Name}, de {power.Name}, meurt au terme de sa vie.");
            };
            bus.OnElderRose += (power, elder) =>
            {
                if (elder.Realm >= CultivationRealm.PurpleMansion)
                    Add($"{elder.Name}, de {power.Name}, atteint {MirrorChronicles.Characters.RankCatalog.RealmName(elder.Realm)}.");
            };
            bus.OnShardSensed += (shard, region) => Add($"le miroir sent un éclat vers {RegionName(session, region)} : les porteurs de ses graines l'entendent.");
            bus.OnShardRecovered += shard => Add($"un éclat du miroir revient : {shard.Name}. Le miroir s'endort pour l'intégrer.");
            bus.OnEndingReached += (ending, subject) => Add($"fin dynastique : {ending.Name}. La partie continue.");
            bus.OnGameOver += () =>
            {
                if (session.Clan.LivingMembers.Count == 0) Add("la lignée s'éteint."); // a seized mirror and an absorbed clan are told above
            };
            session.Knowledge.Revealed += OnRevealed;
        }

        /// <summary>What the clan learns, except what an older save's knowledge rebuilds (it was known all along).</summary>
        private void OnRevealed(Fact fact, KnowledgeSource source)
        {
            if (source == KnowledgeSource.OlderSave || source == KnowledgeSource.Start) return;
            Add($"le clan apprend {KnowledgeView.Describe(fact, session.Context.Content)}.");
        }

        private void Add(string text)
        {
            string entry = $"An {session.Clock.Year} : {text}";
            entries.Add(entry);
            if (entries.Count > capacity) entries.RemoveAt(0);
            OnEntryAdded?.Invoke(entry);
        }

        /// <summary>A party as the chronicle names it: the clan by its name, never by the book's code.</summary>
        private static string Who(string party, bool object_ = false) =>
            party == World.SecretBook.ClanHolder ? (object_ ? "le clan" : "Le clan") : party;
    }
}

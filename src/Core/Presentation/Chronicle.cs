using System;
using System.Collections.Generic;
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
            bus.OnPowerAbsorbed += (vassal, suzerain) => Add($"{suzerain} absorbe {vassal}.");
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
            bus.OnRuinsRevealed += shard => Add($"des ruines anciennes sont découvertes ; le miroir y sent {shard.Name}.");
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

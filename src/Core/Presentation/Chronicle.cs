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

        private static string FruitionName(GameSession session, string fruitionId) =>
            session.Context.Content.Fruitions.FirstOrDefault(f => f.Id == fruitionId)?.Name ?? fruitionId;

        private static string AbilityName(GameSession session, string reference)
        {
            var (lineage, id) = FoundationRef.Parse(reference);
            var fruition = session.Context.Content.Fruitions.FirstOrDefault(f => f.Id == lineage);
            var ability = fruition?.Abilities.FirstOrDefault(a => a.Id == id);
            return ability == null ? reference : $"« {ability.Name} » ({fruition.Name})";
        }

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
            bus.OnAmbushSettled += (faction, outcome) => Add(outcome switch
            {
                AmbushOutcome.Repelled => $"les membres du clan repoussent l'embuscade de {faction} et capturent un de ses agents.",
                AmbushOutcome.Taken => $"les agents de {faction} l'emportent dans l'embuscade et emmènent leur proie.",
                _ => $"les membres du clan échappent à l'embuscade de {faction}."
            });
            bus.OnWarBattleSettled += (enemy, outcome) => Add(outcome switch
            {
                WarBattleOutcome.Won => $"le clan remporte une bataille contre {enemy} et pille son trésor.",
                WarBattleOutcome.Lost => $"le clan perd une bataille contre {enemy}.",
                _ => $"le clan se retire d'une bataille contre {enemy}."
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
            bus.OnMandateEmbodied += (m, ability) => Add($"la vie de {m.FullName} épouse l'image de « {ability} » : sa capacité avance d'un bond.");
            bus.OnMinorAbilityLearnt += ability => Add($"le clan apprend {AbilityName(session, ability)}, une capacité mineure d'un ancien Vrai Monarque.");
            bus.OnArtLegacyGained += (art, how) => Add($"{how} : le clan tient désormais l'héritage de {session.Context.Content.Balance.Arts.Arts.FirstOrDefault(a => a.Art == art)?.Name ?? art.ToString()}.");
            bus.OnArtifactFound += (a, where) => Add($"{where} : {a.Name} ({ArtifactView.ClassLabel(a.Class)}).");
            bus.OnTreasureBound += (m, a) => Add($"{m.FullName} lie son destin à {a.Name} : la puissance d'un Manoir Pourpre, et plus jamais un pas au-delà.");
            bus.OnArtifactForged += (a, smith) => Add($"{smith.FullName} forge {a.Name} ({ArtifactView.ClassLabel(a.Class)}).");
            bus.OnDharmaTreasure += m => Add($"{m.FullName} condense son Trésor de Dharma.");
            bus.OnRankDesignation += d => Add($"{d.MasterName} hypothèque son trésor sur sa Fruition : une Désignation de Rang garde désormais le domaine.");
            bus.OnDemonBorn += d => Add(d.Tier == DemonTier.Realization
                ? $"l'essence de {d.Name} devient un Démon d'Essence Métallique, une catastrophe digne d'un Vrai Monarque."
                : $"l'essence de {d.Name} devient un Démon d'Essence Métallique.");
            bus.OnShamanConverted += (m, method) => Add($"{m.FullName} passe au Dao chamanique par {DiplomacyView.ShamanMethodLabel(method)}.");
            bus.OnShamanRefuge += m => Add($"{m.FullName} échoue à forger son Noyau d'Or, mais échappe aux Émissaires de la Mort en passant au Dao chamanique.");
            bus.OnConversionWounded += m => Add($"le passage d'une voie à l'autre blesse le Dao de {m.FullName}.");
            bus.OnCaptiveConverted += (m, temple) => Add($"captif du {temple}, {m.FullName} cède aux chants et se convertit : il renie le clan et sert désormais le temple ({RankCatalog.DisplayName(m)}).");
            bus.OnShamanicCore += m => Add($"{m.FullName} forge un Noyau d'Or chamanique par un rite de sang.");
            bus.OnDivineAttendant += (m, method) => Add(method switch
            {
                CultivationSubPath.BorrowedProfundity => $"{m.FullName} emprunte la puissance d'un grand partenaire : un Serviteur Divin, qui tombera avec le pacte.",
                CultivationSubPath.UnitedProfundity => $"{m.FullName} fond son mandat de vie dans le domaine du clan : un Serviteur Divin, qui ne le quittera plus.",
                _ => $"{m.FullName} entreprend de posséder une Profondeur : un Serviteur Divin, en route vers la divinité."
            });
            bus.OnDivineCore += m => Add($"{m.FullName} condense un Noyau Divin.");
            bus.OnBorrowedMightLost += (m, patron) => Add($"le pacte a pris fin : la puissance que {m.FullName} empruntait s'effondre.");
            bus.OnDiverseMansionFormed += m => Add($"{m.FullName} forme un Manoir Divers avec du sang : la voie du Démon Embryon Céleste.");
            bus.OnDevilFoundOut += m => Add($"les puissances orthodoxes apprennent que {m.FullName} suit le Dao du Diable : elles se méfient du clan.");
            bus.OnSoulKept += (m, temple) => Add($"le {temple} garde dans sa Terre Pure l'âme de {m.FullName} : le clan ne la reverra pas.");
            bus.OnMonkReborn += (child, monk) => Add($"l'âme de {monk} renaît en {child.FullName}.");
            bus.OnMonkRegained += (child, monk) => Add($"{child.FullName} se souvient de {monk} et retrouve son rang : {RankCatalog.DisplayName(child)}.");
            bus.OnLentPowerLost += (m, temple) => Add($"le {temple} n'est plus : la puissance que {m.FullName} lui empruntait s'éteint.");
            bus.OnVowTaken += (m, vow) => Add($"{m.FullName} fait {DiplomacyView.VowLabel(vow)}.");
            bus.OnVowBroken += (m, vow) => Add($"{m.FullName} rompt {DiplomacyView.VowLabel(vow)} : son mandat de vie en est blessé.");
            bus.OnDebateChallenge += temple => Add($"le {temple} défie le clan en débat du Dao (écran Diplomatie).");
            bus.OnDebateSettled += (m, temple, fate) => Add(fate switch
            {
                DebateFate.Won => $"{m.FullName} l'emporte dans le débat du Dao contre le {temple} : le clan y gagne la face.",
                DebateFate.Converted => $"{m.FullName} perd le débat du Dao contre le {temple}, qui impose sa conversion.",
                DebateFate.Wounded => $"{m.FullName} perd le débat du Dao contre le {temple} : son mandat de vie en est blessé.",
                DebateFate.Fell => $"{m.FullName} perd le débat du Dao contre le {temple} : sa voie du Dharma se brise, un royaume plus bas.",
                DebateFate.Inducted => $"{m.FullName} perd le débat du Dao contre le {temple}, qui le fait entrer dans sa doctrine : {RankCatalog.DisplayName(m)}.",
                _ => $"le clan refuse le débat du {temple} et perd la face."
            });
            bus.OnBuddhistConverted += (m, temple) => Add($"{m.FullName} se convertit au {temple}, renonçant à sa voie : {RankCatalog.DisplayName(m)}.");
            bus.OnSoulRecalled += (m, temple) => Add($"le {temple} rappelle l'âme de {m.FullName}, qu'il gardait : le moine quitte le clan.");
            bus.OnMonkRose += m => Add($"{m.FullName} s'élève : {RankCatalog.DisplayName(m)}.");
            bus.OnBeastBound += b => Add($"{b.FullName} est lié(e) au clan : une bête du Dao Démoniaque, qui garde et chasse pour lui.");
            bus.OnLineageAwakened += b => Add($"la lignée de {b.FullName} s'éveille : {RankCatalog.DisplayName(b)}.");
            bus.OnRenegadeTakenBack += (m, temple) => Add($"{m.FullName}, renégat devenu moine du {temple}, est battu à la tête de son armée : le clan le reprend, et le ramène à lui.");
            bus.OnHauntingBegan += h => Add($"{h.Name} s'est imprimé si profondément dans le monde que sa mémoire hante le clan : l'honorer ou l'exorciser.");
            bus.OnHauntingMadness += (h, m) => Add($"la hantise de {h.Name} égare l'esprit de {m.FullName} : son Dao est blessé.");
            bus.OnHauntingCalamity += (h, stones) => Add($"la hantise de {h.Name} attire une calamité sur le domaine : {stones} pierres perdues.");
            bus.OnHauntingHonoured += h => Add($"le clan honore la mémoire de {h.Name} : son culte lui vaut du prestige, et des dangers.");
            bus.OnHauntingExorcised += h => Add($"la hantise de {h.Name} est exorcisée.");
            bus.OnHauntingFaded += h => Add($"la hantise de {h.Name} s'efface enfin.");
            bus.OnDemonicCore += b => Add($"la lignée de {b.FullName} s'éveille jusqu'au bout : un Noyau d'Or démoniaque.");
            bus.OnDragonBloodRevealed += b => Add($"le sang du Vrai Serpent-Dragon céleste s'éveille en {b.FullName} : il suit désormais le Dao du Serpent-Dragon.");
            bus.OnDragonReclaimed += d => Add($"les dragons de la Mer Orientale viennent reprendre {d.FullName}, de leur sang : il quitte le clan.");
            bus.OnDragonsRepelled += d => Add($"les dragons de la Mer Orientale réclament {d.FullName} ; devant un Vrai Monarque du clan, ils repartent.");
            bus.OnSpiritualNatureElected += (member, fruitionId) =>
            {
                string name = session.Context.Content.Fruitions.FirstOrDefault(f => f.Id == fruitionId)?.Name ?? fruitionId;
                Add($"{member.FullName} porte le destin de la Fruition {name} : un élu de la Nature Spirituelle, que sa vertu oblige.");
            };
            bus.OnPowerMarriage += (a, b) =>
            {
                string home = session.Context.Content.Clan.HomeRegion;
                if (session.Factions.GetFactionByName(a)?.RegionId == home || session.Factions.GetFactionByName(b)?.RegionId == home)
                    Add($"{a} et {b} unissent deux de leurs aînés par le mariage.");
            };
            bus.OnPowerScheme += (what, schemer, victim) =>
            {
                string home = session.Context.Content.Clan.HomeRegion;
                if (session.Factions.GetFactionByName(schemer)?.RegionId == home || session.Factions.GetFactionByName(victim)?.RegionId == home)
                    Add($"{schemer} ourdit contre {victim} : {what}.");
            };
            bus.OnWorldDemon += d => Add($"{d.Name} devient un Démon d'Essence Métallique, qui ravage {RegionName(session, d.RegionId)}.");
            bus.OnDemonSubdued += d => Add($"le démon né de {d.Name} est soumis.");
            bus.OnUnderworldProvoked += () => Add("le clan garde une essence au mépris de la coutume : le Monde Souterrain s'en souviendra.");
            bus.OnPhenomenon += p => { if (p.RegionId == session.Context.Content.Clan.HomeRegion) Add(p.Kind == PhenomenonKind.Failure
                ? $"un phénomène céleste durable couvre {RegionName(session, p.RegionId)} après l'échec de {p.Source} : la cultivation y ralentit."
                : $"un phénomène trouble le temps sur {RegionName(session, p.RegionId)} à la mort de {p.Source} ; sa lignée y cultive plus vite."); };
            bus.OnClanKingdomFounded += () => Add("le clan fonde son royaume : il règne, et son souverain cultive en gouvernant.");
            bus.OnSectFounded += () => Add("le clan fonde sa secte : les pics en haut, la ville en bas.");
            bus.OnRuinsRevealed += shard => Add($"des ruines anciennes sont découvertes ; le miroir y sent {shard.Name}.");
            bus.OnFruitionFreed += (id, holder, reborn) => Add($"{holder} ne tient plus la lignée {FruitionName(session, id)}"
                + (reborn ? " ; réincarné, il pourrait revenir" : "") + " : la course est ouverte.");
            bus.OnAncestorReborn += (child, ancestor) => Add($"{ancestor} renaît en {child.FullName} : l'essence du Vrai Monarque est intacte.");
            bus.OnChosenHarvested += chosen => Add($"une puissance moissonne {chosen.FullName}, l'ancêtre renaissant : l'Élu du Destin n'est plus.");
            bus.OnFruitionRevealed += id => Add($"le miroir lève le voile sur la lignée {FruitionName(session, id)}.");
            bus.OnFruitionTaken += (id, holder) => Add($"{holder} prend la Réalisation de la lignée {FruitionName(session, id)}.");
            bus.OnPowerFell += (fallen, heir) => { fell.Add(fallen); Add($"{fallen} se disperse ; {Who(heir)} recueille ses restes."); };
            bus.OnPowerRose += (power, parent) => Add(parent == null ? $"la {power.Name} s'élève parmi les puissances."
                : $"une branche quitte {parent} et fonde {power.Name}.");
            bus.OnKingdomFounded += power => Add($"{power.Name} fonde un royaume.");
            bus.OnElderDied += (power, elder, demon) =>
            {
                if (elder.RenegadeId != null)
                {
                    Add($"{elder.Name}, renégat du clan devenu moine du {power.Name}, meurt.");
                    return;
                }
                if (elder.Realm >= CultivationRealm.PurpleMansion)
                    Add(demon ? $"{elder.Name}, de {power.Name}, échoue au Noyau d'Or : un démon d'essence métallique naît."
                              : $"{elder.Name}, de {power.Name}, meurt au terme de sa vie.");
            };
            bus.OnElderConverted += (power, temple, elder) =>
            {
                if (elder.Realm >= CultivationRealm.Foundation)
                    Add($"{elder.Name}, de {power.Name}, aux portes de la mort, se convertit au {temple.Name}.");
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
                if (!session.Clan.LivingMembers.Any(m => m.Species == Species.Human)) Add("la lignée s'éteint."); // bound beasts do not carry it (R16) // a seized mirror and an absorbed clan are told above
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

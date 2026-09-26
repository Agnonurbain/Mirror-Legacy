using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>The top of the clan domain screen.</summary>
    public sealed record DomainHeader(int Year, string Phase, int SpiritStones, int MirrorPower, int Generation);

    /// <summary>A cultivation method a member may take up.</summary>
    public sealed record MethodChoice(string Id, string Label);

    /// <summary>
    /// One member of the roster, with the tasks they may take this year, the method they practise and the
    /// methods they may take up, their temper, their foundation (null before the Foundation), the retreat
    /// under way (null outside one) and their divine abilities (null before the Purple Mansion).
    /// </summary>
    public sealed record MemberRow(string Id, string Name, int Age, string Rank, int Stability,
        TaskType Task, IReadOnlyList<TaskType> AllowedTasks, bool IsPatriarch,
        string MethodId, string Method, IReadOnlyList<MethodChoice> Methods,
        string Temperament, string Foundation, string Retreat, string Abilities, string HeartDemon = null, string Position = null);

    /// <summary>Portions of one spiritual Qi in the clan's store.</summary>
    public sealed record QiLine(string Name, int Portions);

    /// <summary>
    /// What the clan domain screen shows, computed from a session without any engine (the Godot scene
    /// only binds it). Player-facing labels are French, like the rank names.
    /// </summary>
    public static class ClanDomainView
    {
        public static DomainHeader Header(GameSession session) => new DomainHeader(
            session.Clock.Year,
            PhaseLabel(session.Clock.Phase),
            session.Resources.SpiritStones,
            session.Mirror.MirrorPower,
            session.Karma.GenerationCount);

        /// <summary>The living, patriarch first, then by realm, stage and age.</summary>
        public static IReadOnlyList<MemberRow> Roster(GameSession session)
        {
            string patriarchId = session.Clan.PatriarchID;
            return session.Clan.LivingMembers
                .OrderByDescending(m => m.ID == patriarchId)
                .ThenByDescending(m => (int)m.Realm)
                .ThenByDescending(m => m.RealmStage)
                .ThenByDescending(m => m.Age)
                .Select(m => new MemberRow(m.ID, m.FullName, m.Age, RankCatalog.DisplayName(m), m.MentalStability,
                    m.CurrentTask, TaskRules.AllowedTasks(m), m.ID == patriarchId,
                    m.CultivationMethodId, PractisedMethod(session, m),
                    session.Techniques.MethodsFor(m).Select(t => new MethodChoice(t.ID, MethodLabel(t))).ToList(),
                    TemperamentLabel(m.Temperament), FoundationLabel(session, m.FoundationId), RetreatLabel(m), AbilitiesLabel(m),
                    m.HeartDemonYearsLeft > 0 ? $"Démon du Cœur ({m.HeartDemonYearsLeft} an{(m.HeartDemonYearsLeft > 1 ? "s" : "")})" : null,
                    PositionLabel(session, m)))
                .ToList();
        }

        /// <summary>« Mer sans Rivage (Eau Orthodoxe) », or null without a foundation.</summary>
        public static string FoundationLabel(GameSession session, string foundationId)
        {
            var lineage = FoundationRules.FruitionOf(foundationId, session.Context.Content.Fruitions);
            if (lineage == null) return null;
            var (_, abilityId) = FoundationRef.Parse(foundationId);
            string name = lineage.Abilities.FirstOrDefault(a => a.Id == abilityId)?.Name ?? "Fondation non révélée";
            return $"{name} ({lineage.Name})";
        }

        /// <summary>A Golden Core's standing (LORE.md §5.5.1, §6.9): its position, or its Left Hand path; null before.</summary>
        public static string PositionLabel(GameSession session, CharacterData member)
        {
            var lineage = session.Context.Content.Fruitions.FirstOrDefault(f => f.Id == member.FruitionId);
            if (lineage == null) return null;
            return member.GoldenCore switch
            {
                GoldenCoreState.MetallicEssenceOnly => $"Essence métallique sans position (vise {lineage.Name})",
                GoldenCoreState.Realization => $"Réalisation — {lineage.Name}",
                GoldenCoreState.Surplus => $"Surplus — {lineage.Name}",
                GoldenCoreState.Intercalary => $"Intercalaire — {lineage.Name}",
                GoldenCoreState.TrueLeftHand => $"Main Gauche vraie — {lineage.LeftHand} ({lineage.Name})",
                GoldenCoreState.FalseLeftHand => $"Main Gauche fausse — au service de {member.PatronId} ({lineage.Name})",
                _ => null
            };
        }

        /// <summary>The retreat of the Purple Mansion's breakthrough under way, or null.</summary>
        public static string RetreatLabel(CharacterData member)
        {
            if (member.Retreat == Retreat.None) return null;
            if (member.ImprisonedInVoid) return "prisonnier du Grand Vide";
            string stage = member.Retreat == Retreat.Manifestation ? "Manifestation" : "Grand Vide";
            return $"en retraite : {stage} ({member.RetreatYearsLeft} an{(member.RetreatYearsLeft > 1 ? "s" : "")})";
        }

        /// <summary>« 2/5 capacités divines » at the Purple Mansion and beyond, otherwise null.</summary>
        public static string AbilitiesLabel(CharacterData member) =>
            member.Realm < CultivationRealm.PurpleMansion ? null
                : $"{member.DivineAbilities?.Count ?? 0}/{DivineAbilitySystem.MaxAbilities} capacités divines";

        public static string TemperamentLabel(Temperament temperament) => temperament switch
        {
            Temperament.Dominant => "Dominateur",
            Temperament.Solitary => "Solitaire",
            Temperament.Patient => "Patient",
            Temperament.Fiery => "Fougueux",
            Temperament.Cunning => "Rusé",
            Temperament.Serene => "Serein",
            _ => "Tempérament inconnu"
        };

        /// <summary>The Qi in store, by name.</summary>
        public static IReadOnlyList<QiLine> QiStock(GameSession session) =>
            session.Resources.SpiritualQi
                .Where(kv => kv.Value > 0)
                .Select(kv => new QiLine(session.Techniques.FindQi(kv.Key)?.Name ?? kv.Key, kv.Value))
                .OrderBy(line => line.Name, StringComparer.Ordinal)
                .ToList();

        /// <summary>« Sutra de la Source Claire (grade 3) »; the highest grade reads « 7+ » (LORE.md §2.2).</summary>
        public static string MethodLabel(TechniqueData technique) =>
            $"{technique.Name} (grade {(technique.Grade >= TechniqueRules.MaxGrade ? "7+" : technique.Grade.ToString())})";

        /// <summary>The member's method, the common breathing of a breathing member without one, none, or « — » for a mortal.</summary>
        private static string PractisedMethod(GameSession session, CharacterData member)
        {
            if (!SpiritualOrificeRules.CanCultivate(member)) return "—";
            var method = session.Techniques.MethodOf(member);
            if (method != null) return MethodLabel(method);
            return member.Realm == CultivationRealm.Embryonic ? "Respiration commune" : "Aucune méthode";
        }

        public static string PhaseLabel(GamePhase phase) => phase switch
        {
            GamePhase.Management => "Gestion",
            GamePhase.Events => "Événements",
            GamePhase.Breakthrough => "Percées",
            GamePhase.Inheritance => "Héritage",
            _ => phase.ToString()
        };

        public static string TaskLabel(TaskType task) => task switch
        {
            TaskType.None => "Aucune tâche",
            TaskType.Cultivation => "Cultivation",
            TaskType.Mine => "Mine",
            TaskType.Patrol => "Patrouille",
            TaskType.Study => "Étude",
            TaskType.Teaching => "Enseignement",
            TaskType.Diplomacy => "Diplomatie",
            TaskType.Espionage => "Espionnage",
            TaskType.Rest => "Repos",
            TaskType.GatherQi => "Récolte de Qi",
            _ => task.ToString()
        };

        /// <summary>Completes "X meurt …".</summary>
        public static string DeathLabel(DeathCause cause) => cause switch
        {
            DeathCause.OldAge => "de vieillesse",
            DeathCause.Combat => "au combat",
            DeathCause.QiDeviation => "d'une déviation du Qi",
            DeathCause.Assassination => "assassiné(e)",
            DeathCause.Illness => "de maladie",
            DeathCause.SpiritualDissolution => "d'une dissolution spirituelle",
            DeathCause.AscentCollapse => "épuisé(e) avant le Manoir Shenyang",
            DeathCause.FoundationDevoured => "sa fondation dévorée par un Partenaire Dao",
            DeathCause.ManifestationCollapse => "en échouant à manifester son pouvoir divin",
            DeathCause.MetalEssenceDemon => "en échouant au Noyau d'Or : un Démon d'Essence Métallique est né",
            _ => ""
        };
    }
}

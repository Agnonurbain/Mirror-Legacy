using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Characters;
using MirrorChronicles.Data;
using MirrorChronicles.Session;

namespace MirrorChronicles.Presentation
{
    /// <summary>The top of the clan domain screen.</summary>
    public sealed record DomainHeader(int Year, string Phase, int SpiritStones, int MirrorPower, int Generation);

    /// <summary>One member of the roster, with the tasks they may take this year.</summary>
    public sealed record MemberRow(string Id, string Name, int Age, string Rank, int Stability,
        TaskType Task, IReadOnlyList<TaskType> AllowedTasks, bool IsPatriarch);

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
                    m.CurrentTask, TaskRules.AllowedTasks(m), m.ID == patriarchId))
                .ToList();
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
            _ => ""
        };
    }
}

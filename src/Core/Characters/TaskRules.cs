using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Which tasks a member may take (LORE.md §4). Children under six have none; mortals cannot
    /// cultivate and only rest until sixteen, then work the clan's land; Embryonic Breathing
    /// cultivators are too fragile for anything but cultivation and rest; from Qi Refinement on,
    /// every task is open. Pure logic shared by the task system and the UI.
    /// </summary>
    public static class TaskRules
    {
        /// <summary>Before this age a child neither works nor cultivates.</summary>
        public const int CultivationAge = 6;

        /// <summary>A mortal child only rests until this age.</summary>
        public const int WorkingAge = 16;

        private static readonly TaskType[] InfantTasks = { TaskType.None };

        private static readonly TaskType[] ChildTasks = { TaskType.None, TaskType.Rest };

        private static readonly TaskType[] MortalTasks =
            { TaskType.None, TaskType.Mine, TaskType.Patrol, TaskType.Diplomacy, TaskType.Rest };

        private static readonly TaskType[] EmbryonicChildTasks =
            { TaskType.None, TaskType.Cultivation, TaskType.Rest };

        /// <summary>A breathing adult does at least what a mortal does, and cultivates (AUDIT_LORE.md §7.4).</summary>
        private static readonly TaskType[] EmbryonicTasks =
            { TaskType.None, TaskType.Cultivation, TaskType.Mine, TaskType.Patrol, TaskType.Diplomacy, TaskType.Rest };

        /// <summary>The chakra that perceives spiritual Qi (LORE.md §5.1).</summary>
        public const int QiPerceptionChakra = 5;

        /// <summary>From this chakra a breathing adult is sharp enough to spy (AUDIT_LORE.md §7.4).</summary>
        public const int EspionageChakra = 3;

        /// <summary>A task far from the domain (diplomacy, espionage, the hunt).</summary>
        public static bool IsAway(TaskType task) => AwayTasks.Contains(task);

        private static readonly TaskType[] AllTasks =
            Enum.GetValues(typeof(TaskType)).Cast<TaskType>().ToArray();

        private static bool IsHuntTask(TaskType t) => t == TaskType.HuntBeast || t == TaskType.ScoutBeasts || t == TaskType.Diversion;

        /// <param name="huntOpen">The hunt is open only in the window before the mirror's ritual (user decision, 2026-09-26).</param>
        /// <param name="lakeOpen">The lake is searched only until its shard is found (B3c).</param>
        public static IReadOnlyList<TaskType> AllowedTasks(CharacterData character, bool huntOpen = false, bool lakeOpen = false)
        {
            var allowed = AllowedByAge(character, huntOpen, lakeOpen);
            if (DivineDaoSystem.IsUnited(character)) allowed = allowed.Where(t => !AwayTasks.Contains(t)).ToArray(); // bound to the domain (R15)
            return character.ForbiddenTasks == null || character.ForbiddenTasks.Count == 0 ? allowed
                : allowed.Where(t => !character.ForbiddenTasks.Contains(t)).ToArray(); // an elect's Fruition dictates its conduct (R10)
        }

        /// <summary>The tasks that take a member away from the domain.</summary>
        private static readonly TaskType[] AwayTasks =
            { TaskType.Diplomacy, TaskType.Espionage, TaskType.HuntBeast, TaskType.ScoutBeasts, TaskType.Diversion };

        /// <summary>A bound beast of the Demonic Dao guards and hunts (R16, the user's choice of 2026-10-12).</summary>
        private static readonly TaskType[] BeastTasks =
            { TaskType.None, TaskType.Patrol, TaskType.Rest, TaskType.HuntBeast, TaskType.ScoutBeasts, TaskType.Diversion };

        private static IReadOnlyList<TaskType> AllowedByAge(CharacterData character, bool huntOpen, bool lakeOpen)
        {
            if (character.Age < CultivationAge || character.Retreat != Retreat.None || character.CaptorFaction != null || character.DiscipleOf != null)
                return InfantTasks; // a retreat, or captivity, leaves no task
            if (character.Species != Species.Human)
                return BeastTasks.Where(t => huntOpen || !IsHuntTask(t)).ToArray();
            if (!SpiritualOrificeRules.Cultivates(character)) return character.Age < WorkingAge ? ChildTasks : MortalTasks;
            if (character.Realm == CultivationRealm.Embryonic) return BreathingTasks(character);
            return AllTasks.Where(t => (huntOpen || !IsHuntTask(t)) && (lakeOpen || t != TaskType.SearchLake)).ToArray();
        }

        /// <summary>
        /// A breathing cultivator: a child cultivates and rests; an adult does what a mortal does besides, spies from the third
        /// chakra; from the Summit Eye (fifth chakra) either perceives spiritual Qi and can gather it.
        /// </summary>
        private static TaskType[] BreathingTasks(CharacterData character)
        {
            bool adult = character.Age >= WorkingAge;
            var tasks = new List<TaskType>(adult ? EmbryonicTasks : EmbryonicChildTasks);
            tasks.Remove(TaskType.Rest); // rest stays last
            if (adult && character.RealmStage >= EspionageChakra) tasks.Add(TaskType.Espionage);
            if (character.RealmStage >= QiPerceptionChakra) tasks.Add(TaskType.GatherQi);
            tasks.Add(TaskType.Rest);
            return tasks.ToArray();
        }

        public static bool IsAllowed(CharacterData character, TaskType task, bool huntOpen = false, bool lakeOpen = false)
        {
            return AllowedTasks(character, huntOpen, lakeOpen).Contains(task);
        }
    }
}

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

        private static readonly TaskType[] EmbryonicTasks =
            { TaskType.None, TaskType.Cultivation, TaskType.Rest };

        /// <summary>From the Summit Eye (fifth chakra) a breathing cultivator perceives spiritual Qi and can gather it.</summary>
        private static readonly TaskType[] SummitEyeTasks =
            { TaskType.None, TaskType.Cultivation, TaskType.GatherQi, TaskType.Rest };

        /// <summary>The chakra that perceives spiritual Qi (LORE.md §5.1).</summary>
        public const int QiPerceptionChakra = 5;

        private static readonly TaskType[] AllTasks =
            Enum.GetValues(typeof(TaskType)).Cast<TaskType>().ToArray();

        public static IReadOnlyList<TaskType> AllowedTasks(CharacterData character)
        {
            if (character.Age < CultivationAge || character.Retreat != Retreat.None) return InfantTasks; // a retreat leaves no task
            if (!SpiritualOrificeRules.CanCultivate(character)) return character.Age < WorkingAge ? ChildTasks : MortalTasks;
            if (character.Realm == CultivationRealm.Embryonic)
                return character.RealmStage >= QiPerceptionChakra ? SummitEyeTasks : EmbryonicTasks;
            return AllTasks;
        }

        public static bool IsAllowed(CharacterData character, TaskType task)
        {
            return AllowedTasks(character).Contains(task);
        }
    }
}

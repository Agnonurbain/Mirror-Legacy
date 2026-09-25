using System;
using System.Collections.Generic;
using System.Linq;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>
    /// Which tasks a member may take (LORE.md §4). Mortals cannot cultivate but work the clan's
    /// land; Embryonic Breathing cultivators are too fragile for anything but cultivation and rest;
    /// from Qi Refinement on, every task is open. Pure logic shared by the task system and the UI.
    /// </summary>
    public static class TaskRules
    {
        private static readonly TaskType[] MortalTasks =
            { TaskType.None, TaskType.Mine, TaskType.Patrol, TaskType.Diplomacy, TaskType.Rest };

        private static readonly TaskType[] EmbryonicTasks =
            { TaskType.None, TaskType.Cultivation, TaskType.Rest };

        private static readonly TaskType[] AllTasks =
            Enum.GetValues(typeof(TaskType)).Cast<TaskType>().ToArray();

        public static IReadOnlyList<TaskType> AllowedTasks(CharacterData character)
        {
            if (!SpiritualOrificeRules.CanCultivate(character)) return MortalTasks;
            if (character.Realm == CultivationRealm.Embryonic) return EmbryonicTasks;
            return AllTasks;
        }

        public static bool IsAllowed(CharacterData character, TaskType task)
        {
            return AllowedTasks(character).Contains(task);
        }
    }
}

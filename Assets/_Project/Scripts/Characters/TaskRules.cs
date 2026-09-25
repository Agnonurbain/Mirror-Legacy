using System.Collections.Generic;
using MirrorChronicles.Data;

namespace MirrorChronicles.Characters
{
    /// <summary>Tasks a member may take. TDD stub.</summary>
    public static class TaskRules
    {
        public static IReadOnlyList<TaskType> AllowedTasks(CharacterData character) => new List<TaskType>();
        public static bool IsAllowed(CharacterData character, TaskType task) => true;
    }
}

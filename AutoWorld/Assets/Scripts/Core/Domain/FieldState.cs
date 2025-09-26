using System.Collections.Generic;
using AutoWorld.Core;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 필드 단위 상태를 보관한다.
    /// </summary>
    public sealed class FieldState
    {
        public FieldState(FieldDefinition definition)
        {
            Definition = definition;
            ActiveTasks = new List<TaskDefinition>(definition.Tasks);
        }

        public FieldDefinition Definition { get; }

        public List<TaskDefinition> ActiveTasks { get; }

        public bool IsEmpty => Definition.IsEmpty;
    }
}

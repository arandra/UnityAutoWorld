using System.Collections.Generic;

namespace AutoWorld.Core
{
    /// <summary>
    /// 필드의 기본 구성을 정의한다.
    /// </summary>
    public sealed class FieldDefinition
    {
        public FieldDefinition(FieldType type, int territoryCost, IReadOnlyList<TaskDefinition> tasks)
        {
            Type = type;
            TerritoryCost = territoryCost;
            Tasks = tasks;
        }

        public FieldType Type { get; }

        public int TerritoryCost { get; }

        public IReadOnlyList<TaskDefinition> Tasks { get; }
    }
}

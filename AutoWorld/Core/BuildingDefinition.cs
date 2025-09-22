using System.Collections.Generic;

namespace AutoWorld.Core
{
    /// <summary>
    /// 건물의 기본 구성을 정의한다.
    /// </summary>
    public sealed class BuildingDefinition
    {
        public BuildingDefinition(BuildingType type, int territoryCost, IReadOnlyList<TaskDefinition> tasks)
        {
            Type = type;
            TerritoryCost = territoryCost;
            Tasks = tasks;
        }

        public BuildingType Type { get; }

        public int TerritoryCost { get; }

        public IReadOnlyList<TaskDefinition> Tasks { get; }
    }
}

using System.Collections.Generic;

namespace AutoWorld.Core
{
    /// <summary>
    /// 필드의 기본 구성을 정의한다.
    /// </summary>
    public sealed class FieldDefinition
    {
        public FieldDefinition(
            FieldType type,
            bool isEmpty,
            int size,
            int slot,
            int constructionTicks,
            IReadOnlyList<ResourceAmount> constructionCosts,
            IReadOnlyList<FieldType> requirements,
            IReadOnlyList<TaskDefinition> tasks)
        {
            Type = type;
            IsEmpty = isEmpty;
            Size = size;
            Slot = slot;
            ConstructionTicks = constructionTicks;
            ConstructionCosts = constructionCosts;
            Requirements = requirements;
            Tasks = tasks;
        }

        public FieldType Type { get; }

        public bool IsEmpty { get; }

        public int Size { get; }

        public int Slot { get; }

        public int ConstructionTicks { get; }

        public IReadOnlyList<ResourceAmount> ConstructionCosts { get; }

        public IReadOnlyList<FieldType> Requirements { get; }

        public IReadOnlyList<TaskDefinition> Tasks { get; }
    }
}

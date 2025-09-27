using System;
using System.Collections.Generic;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 필드 변환 진행 상태를 추적한다.
    /// </summary>
    public sealed class FieldTransformation
    {
        public FieldTransformation(FieldState sourceField, FieldDefinition targetDefinition, IReadOnlyList<FieldCoordinate> coordinates)
        {
            SourceField = sourceField ?? throw new ArgumentNullException(nameof(sourceField));
            TargetDefinition = targetDefinition ?? throw new ArgumentNullException(nameof(targetDefinition));
            Coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));
            RequiredTicks = Math.Max(1, TargetDefinition.ConstructionTicks);
        }

        public FieldState SourceField { get; }

        public FieldDefinition TargetDefinition { get; }

        public IReadOnlyList<FieldCoordinate> Coordinates { get; }

        public int RequiredTicks { get; }

        public int ProgressTicks { get; private set; }

        public int? AssignedCitizenId { get; private set; }

        public bool IsCompleted => ProgressTicks >= RequiredTicks;

        public bool HasAssignedWorker => AssignedCitizenId.HasValue;

        public void AssignWorker(int citizenId)
        {
            AssignedCitizenId = citizenId;
        }

        public void ReleaseWorker()
        {
            AssignedCitizenId = null;
        }

        public void Advance(int ticks)
        {
            if (ticks <= 0)
            {
                return;
            }

            ProgressTicks += ticks;
        }

        public void ResetProgress()
        {
            ProgressTicks = 0;
        }
    }
}

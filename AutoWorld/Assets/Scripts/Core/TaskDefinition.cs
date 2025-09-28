using System.Collections.Generic;

namespace AutoWorld.Core
{
    /// <summary>
    /// 단일 작업 슬롯에서 수행할 작업 정의다.
    /// </summary>
    public sealed class TaskDefinition
    {
        public TaskDefinition(
            string name,
            JobType? job,
            bool allowsAnyJob,
            int durationTicks,
            string riseEvent)
        {
            Name = name ?? string.Empty;
            Job = job;
            AllowsAnyJob = allowsAnyJob;
            DurationTicks = durationTicks;
            RiseEvent = riseEvent ?? string.Empty;
        }

        public string Name { get; }

        public JobType? Job { get; }

        public bool AllowsAnyJob { get; }

        public int DurationTicks { get; }

        public string RiseEvent { get; }
    }
}

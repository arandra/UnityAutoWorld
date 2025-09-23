using System;
using System.Collections.Generic;

namespace AutoWorld.Core
{
    /// <summary>
    /// 단일 작업 슬롯에서 수행할 작업 정의다.
    /// </summary>
    public sealed class TaskDefinition
    {
        public TaskDefinition(JobType job, IReadOnlyList<ResourceAmount> requirements, IReadOnlyList<ResourceAmount> results, TimeSpan duration)
        {
            Job = job;
            Requirements = requirements;
            Results = results;
            Duration = duration;
        }

        public JobType Job { get; }

        public IReadOnlyList<ResourceAmount> Requirements { get; }

        public IReadOnlyList<ResourceAmount> Results { get; }

        public TimeSpan Duration { get; }
    }
}

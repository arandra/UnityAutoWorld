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
            IReadOnlyList<ResourceAmount> requirements,
            IReadOnlyList<ResourceAmount> results,
            TaskOutcome outcome)
        {
            Name = name;
            Job = job;
            AllowsAnyJob = allowsAnyJob;
            DurationTicks = durationTicks;
            Requirements = requirements;
            Results = results;
            Outcome = outcome;
        }

        public string Name { get; }

        public JobType? Job { get; }

        public bool AllowsAnyJob { get; }

        public int DurationTicks { get; }

        public IReadOnlyList<ResourceAmount> Requirements { get; }

        public IReadOnlyList<ResourceAmount> Results { get; }

        public TaskOutcome Outcome { get; }
    }
}

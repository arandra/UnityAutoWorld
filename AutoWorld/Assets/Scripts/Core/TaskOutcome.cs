using System;

namespace AutoWorld.Core
{
    public enum TaskOutcomeKind
    {
        None = 0,
        Resource,
        Field
    }

    public readonly struct TaskOutcome
    {
        private readonly ResourceAmount resource;
        private readonly FieldType field;

        private TaskOutcome(TaskOutcomeKind kind, ResourceAmount resource, FieldType field)
        {
            Kind = kind;
            this.resource = resource;
            this.field = field;
        }

        public TaskOutcomeKind Kind { get; }

        public ResourceAmount Resource
        {
            get
            {
                if (Kind != TaskOutcomeKind.Resource)
                {
                    throw new InvalidOperationException("Outcome이 Resource가 아닙니다.");
                }

                return resource;
            }
        }

        public FieldType Field
        {
            get
            {
                if (Kind != TaskOutcomeKind.Field)
                {
                    throw new InvalidOperationException("Outcome이 Field가 아닙니다.");
                }

                return field;
            }
        }

        public static TaskOutcome None() => new TaskOutcome(TaskOutcomeKind.None, default, default);

        public static TaskOutcome ForResource(ResourceAmount resource) => new TaskOutcome(TaskOutcomeKind.Resource, resource, default);

        public static TaskOutcome ForField(FieldType field) => new TaskOutcome(TaskOutcomeKind.Field, default, field);
    }
}

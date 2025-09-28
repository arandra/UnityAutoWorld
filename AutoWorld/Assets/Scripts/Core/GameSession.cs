using System;
using AutoWorld.Core.Domain;
using AutoWorld.Core.Services;

namespace AutoWorld.Core
{
    public sealed class GameSession
    {
        public GameSession(
            ManualTickScheduler scheduler,
            CitizenManager citizenManager,
            FieldManager fieldManager,
            ResourceManager resourceManager,
            EventRegistryService registryService)
        {
            Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            Citizens = citizenManager ?? throw new ArgumentNullException(nameof(citizenManager));
            Fields = fieldManager ?? throw new ArgumentNullException(nameof(fieldManager));
            Resources = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            Registry = registryService ?? throw new ArgumentNullException(nameof(registryService));

            Scheduler.Register(Citizens);
            Scheduler.Register(Fields);
            Scheduler.Register(Resources);
        }

        public ManualTickScheduler Scheduler { get; }

        public CitizenManager Citizens { get; }

        public FieldManager Fields { get; }

        public ResourceManager Resources { get; }

        public EventRegistryService Registry { get; }

        public TickContext AdvanceTick()
        {
            return Scheduler.AdvanceTick();
        }

        public bool RequestFieldTransformation(FieldType targetType)
        {
            return Fields.RequestFieldTransformation(targetType, Resources);
        }

        public bool IncreaseJob(JobType job)
        {
            return Citizens.TryIncreaseJob(job);
        }

        public bool DecreaseJob(JobType job)
        {
            return Citizens.TryDecreaseJob(job);
        }
    }
}

using System;
using AutoWorld.Core.Domain;
using AutoWorld.Core.Services;

namespace AutoWorld.Core
{
    public sealed class GameSession
    {
        public GameSession(ManualTickScheduler scheduler, PopulationManager populationManager, FieldManager fieldManager, ResourceStore resourceStore, EventRegistryService registryService)
        {
            Scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            Population = populationManager ?? throw new ArgumentNullException(nameof(populationManager));
            Fields = fieldManager ?? throw new ArgumentNullException(nameof(fieldManager));
            Resources = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
            Registry = registryService ?? throw new ArgumentNullException(nameof(registryService));

            Scheduler.Register(Population);
        }

        public ManualTickScheduler Scheduler { get; }

        public PopulationManager Population { get; }

        public FieldManager Fields { get; }

        public ResourceStore Resources { get; }

        public EventRegistryService Registry { get; }

        public TickContext AdvanceTick()
        {
            return Scheduler.AdvanceTick();
        }
    }
}

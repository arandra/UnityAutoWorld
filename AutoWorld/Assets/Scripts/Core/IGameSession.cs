using AutoWorld.Core.Domain;
using AutoWorld.Core.Services;

namespace AutoWorld.Core
{
    public interface IGameSession
    {
        ManualTickScheduler Scheduler { get; }

        CitizenManager Citizens { get; }

        FieldManager Fields { get; }

        ResourceManager Resources { get; }

        EventRegistryService Registry { get; }

        IDebugLog DebugLog { get; }

        TickContext AdvanceTick();

        bool RequestFieldTransformation(FieldType targetType);

        bool IncreaseJob(JobType job);

        bool DecreaseJob(JobType job);
    }
}

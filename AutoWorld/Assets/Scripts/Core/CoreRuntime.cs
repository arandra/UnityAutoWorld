using System;
using System.Collections.Generic;
using AutoWorld.Core.Data;
using AutoWorld.Core.Domain;
using AutoWorld.Core.Services;

namespace AutoWorld.Core
{
    public static class CoreRuntime
    {
        private static IGameSession session;

        public static IGameSession Session
        {
            get
            {
                if (session == null)
                {
                    throw new InvalidOperationException("GameSession이 초기화되지 않았습니다.");
                }

                return session;
            }
        }

        public static IGameSession InitializeSession(
            InitConst initConst,
            IReadOnlyDictionary<FieldType, FieldDefinition> definitions,
            IReadOnlyDictionary<int, GridMap> gridMaps,
            IReadOnlyDictionary<JobType, IReadOnlyList<ResourceAmount>> jobCosts,
            IReadOnlyList<EventAction> eventActions,
            IDebugLog debugLog)
        {
            if (HasSession)
            {
                throw new InvalidOperationException("GameSession이 이미 초기화되었습니다.");
            }

            if (initConst == null)
            {
                throw new ArgumentNullException(nameof(initConst));
            }

            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (gridMaps == null)
            {
                throw new ArgumentNullException(nameof(gridMaps));
            }

            var scheduler = new ManualTickScheduler(initConst.MillisecondPerTick);

            var resourceStore = new ResourceStore();
            resourceStore.Add(ResourceType.Food, initConst.InitFood);

            var registryService = new EventRegistryService();

            var resourceManager = new ResourceManager(resourceStore, registryService);

            var fieldManager = new FieldManager(definitions, gridMaps, registryService, debugLog);
            fieldManager.InitializeTerritory(initConst.InitBadLandSize);

            var citizenManager = new CitizenManager(
                resourceManager,
                fieldManager,
                registryService,
                initConst.FoodConsumeTicks,
                initConst.SoldierUpgradeTicks,
                initConst.MaxSoldierLevel,
                initConst.WorkerTicks,
                initConst.TicksForRest,
                jobCosts ?? new Dictionary<JobType, IReadOnlyList<ResourceAmount>>());

            InitializeFields(fieldManager, initConst.InitFields);
            InitializePopulation(citizenManager, initConst.InitJobs);

            var eventActionList = eventActions ?? Array.Empty<EventAction>();
            citizenManager.ConfigureEventActions(eventActionList);
            resourceManager.ConfigureEventActions(eventActionList);
            fieldManager.ConfigureEventActions(eventActionList);

            session = new GameSession(
                scheduler,
                citizenManager,
                fieldManager,
                resourceManager,
                registryService,
                debugLog);
            return session;
        }

        public static bool HasSession => session != null;

        private static void InitializePopulation(CitizenManager citizens, IList<string> jobNames)
        {
            if (citizens == null)
            {
                throw new ArgumentNullException(nameof(citizens));
            }

            if (jobNames == null)
            {
                return;
            }

            foreach (var jobName in jobNames)
            {
                if (!Enum.TryParse(jobName, false, out JobType jobType))
                {
                    continue;
                }

                citizens.AddCitizen(jobType);
            }
        }

        private static void InitializeFields(FieldManager fieldManager, IList<string> fieldNames)
        {
            if (fieldManager == null)
            {
                throw new ArgumentNullException(nameof(fieldManager));
            }

            if (fieldNames == null)
            {
                return;
            }

            foreach (var fieldName in fieldNames)
            {
                if (!Enum.TryParse(fieldName, false, out FieldType fieldType))
                {
                    continue;
                }

                if (!fieldManager.TryPlaceInitialField(fieldType))
                {
                    throw new InvalidOperationException($"초기 필드를 배치할 수 없습니다: {fieldType}");
                }
            }
        }
    }
}

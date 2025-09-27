using System;
using System.Collections.Generic;
using AutoWorld.Core.Domain;

namespace AutoWorld.Core.Services
{
    public static class CoreBootstrapper
    {
        public static GameSession CreateGameSession(
            AutoWorld.Core.Data.InitConst initConst,
            ManualTickScheduler scheduler,
            IReadOnlyDictionary<FieldType, FieldDefinition> definitions,
            IReadOnlyDictionary<int, AutoWorld.Core.Data.GridMap> gridMaps,
            IReadOnlyDictionary<JobType, IReadOnlyList<ResourceAmount>> jobCosts)
        {
            if (initConst == null)
            {
                throw new ArgumentNullException(nameof(initConst));
            }

            if (scheduler == null)
            {
                throw new ArgumentNullException(nameof(scheduler));
            }

            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (gridMaps == null)
            {
                throw new ArgumentNullException(nameof(gridMaps));
            }

            var resourceStore = new ResourceStore();
            resourceStore.Add(ResourceType.Food, initConst.InitFood);

            var registryService = new EventRegistryService();

            var fieldManager = new FieldManager(definitions, gridMaps);
            fieldManager.InitializeTerritory(initConst.InitBadLandSize);

            var population = new PopulationManager(
                resourceStore,
                fieldManager,
                registryService,
                initConst.FoodConsumeTicks,
                initConst.SoldierUpgradeTicks,
                initConst.MaxSoldierLevel,
                initConst.WorkerTicks,
                initConst.TicksForRest,
                jobCosts ?? new Dictionary<JobType, IReadOnlyList<ResourceAmount>>());

            InitializeFields(fieldManager, initConst.InitFields);

            InitializePopulation(population, initConst.InitJobs);

            return new GameSession(scheduler, population, fieldManager, resourceStore, registryService);
        }

        private static void InitializePopulation(PopulationManager population, IList<string> jobNames)
        {
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

                population.AddCitizen(jobType);
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

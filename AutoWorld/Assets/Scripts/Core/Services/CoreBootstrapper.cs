using System;
using System.Collections.Generic;
using AutoWorld.Core.Domain;

namespace AutoWorld.Core.Services
{
    public static class CoreBootstrapper
    {
        public static GameSession CreateGameSession(Datas.Const.InitConst initConst, ManualTickScheduler scheduler, IReadOnlyDictionary<FieldType, FieldDefinition> definitions, ICoreEvents coreEvents)
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

            var resourceStore = new ResourceStore();
            resourceStore.Add(ResourceType.Food, initConst.InitFood);

            var population = new PopulationManager(resourceStore, coreEvents, initConst.FoodConsumeTicks, initConst.SoldierUpgradeTicks, initConst.MaxSoldierLevel);

            var registryService = new EventRegistryService();

            var fieldManager = new FieldManager(definitions);
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

            var nextId = 1;
            foreach (var jobName in jobNames)
            {
                if (!Enum.TryParse(jobName, false, out JobType jobType))
                {
                    continue;
                }

                population.AddCitizen(nextId++, jobType);
            }
        }

        private static void InitializeFields(FieldManager fieldManager, IList<string> fieldNames)
        {
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

                fieldManager.CreateField(fieldType);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using AutoWorld.Core;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 주민 리스트를 관리하며 먹이 소비와 직업별 로직을 처리한다.
    /// </summary>
    public sealed class PopulationManager : ITickListener
    {
        private readonly List<Citizen> citizens = new List<Citizen>();
        private readonly ResourceStore resourceStore;
        private readonly ICoreEvents coreEvents;
        private readonly int foodConsumeTicks;
        private readonly int soldierUpgradeTicks;
        private readonly int maxSoldierLevel;

        public PopulationManager(ResourceStore resourceStore, ICoreEvents coreEvents, int foodConsumeTicks, int soldierUpgradeTicks, int maxSoldierLevel)
        {
            this.resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
            this.coreEvents = coreEvents;
            this.foodConsumeTicks = Math.Max(1, foodConsumeTicks);
            this.soldierUpgradeTicks = Math.Max(1, soldierUpgradeTicks);
            this.maxSoldierLevel = Math.Max(1, maxSoldierLevel);
        }

        public IReadOnlyList<Citizen> Citizens => citizens;

        public Citizen AddCitizen(int identifier, JobType job)
        {
            var citizen = new Citizen(identifier, job)
            {
                TicksUntilFoodConsume = foodConsumeTicks,
                TicksUntilSoldierUpgrade = soldierUpgradeTicks
            };

            citizens.Add(citizen);
            return citizen;
        }

        public void OnTick(TickContext context)
        {
            foreach (var citizen in citizens)
            {
                citizen.TicksUntilFoodConsume -= 1;
                if (citizen.TicksUntilFoodConsume <= 0)
                {
                    if (resourceStore.TryConsume(ResourceType.Food, 1))
                    {
                        citizen.TicksUntilFoodConsume = foodConsumeTicks;
                        coreEvents?.OnFoodConsumed(citizen.Identifier);
                    }
                    else
                    {
                        citizen.TicksUntilFoodConsume = foodConsumeTicks;
                        coreEvents?.OnFoodShortage(citizen.Identifier);
                    }
                }

                if (citizen.Job == JobType.Soldier)
                {
                    citizen.TicksUntilSoldierUpgrade -= 1;
                    if (citizen.TicksUntilSoldierUpgrade <= 0)
                    {
                        var previousLevel = citizen.Level;
                        citizen.IncreaseSoldierLevel(maxSoldierLevel);
                        citizen.TicksUntilSoldierUpgrade = soldierUpgradeTicks;
                        if (citizen.Level != previousLevel)
                        {
                            coreEvents?.OnSoldierLevelUp(citizen.Identifier, citizen.Level);
                        }
                    }
                }
            }
        }
    }
}

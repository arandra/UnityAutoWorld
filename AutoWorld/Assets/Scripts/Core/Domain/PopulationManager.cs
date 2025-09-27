using System;
using System.Collections.Generic;
using AutoWorld.Core;
using AutoWorld.Core.Services;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 주민 리스트를 관리하며 먹이 소비와 직업별 로직을 처리한다.
    /// </summary>
    public sealed class PopulationManager : ITickListener
    {
        private readonly List<Citizen> citizens = new List<Citizen>();
        private readonly Dictionary<int, Citizen> citizenLookup = new Dictionary<int, Citizen>();
        private readonly Dictionary<JobType, HashSet<Citizen>> citizensByJob = new Dictionary<JobType, HashSet<Citizen>>();
        private readonly Dictionary<JobType, int> jobFieldCursor = new Dictionary<JobType, int>();
        private readonly ResourceStore resourceStore;
        private readonly FieldManager fieldManager;
        private readonly EventRegistryService eventRegistry;
        private readonly IReadOnlyDictionary<JobType, IReadOnlyList<ResourceAmount>> jobCosts;
        private readonly ICoreEvents coreEvents;
        private readonly int foodConsumeTicks;
        private readonly int soldierUpgradeTicks;
        private readonly int maxSoldierLevel;
        private readonly int workerTicksInterval;
        private readonly int ticksForRest;
        private int workerTickCountdown;
        private bool populationGrowthPaused;
        private int nextCitizenIdentifier = 1;
        private readonly Dictionary<int, EventObject> citizenEventObjects = new Dictionary<int, EventObject>();
        private readonly EventObject managerEventObject;
        private readonly Dictionary<int, FieldTransformation> transformationByCitizen = new Dictionary<int, FieldTransformation>();

        public PopulationManager(
            ResourceStore resourceStore,
            FieldManager fieldManager,
            EventRegistryService eventRegistry,
            ICoreEvents coreEvents,
            int foodConsumeTicks,
            int soldierUpgradeTicks,
            int maxSoldierLevel,
            int workerTicksInterval,
            int ticksForRest,
            IReadOnlyDictionary<JobType, IReadOnlyList<ResourceAmount>> jobCosts)
        {
            this.resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
            this.fieldManager = fieldManager ?? throw new ArgumentNullException(nameof(fieldManager));
            this.eventRegistry = eventRegistry ?? throw new ArgumentNullException(nameof(eventRegistry));
            this.jobCosts = jobCosts ?? new Dictionary<JobType, IReadOnlyList<ResourceAmount>>();
            this.coreEvents = coreEvents;
            this.foodConsumeTicks = Math.Max(1, foodConsumeTicks);
            this.soldierUpgradeTicks = Math.Max(1, soldierUpgradeTicks);
            this.maxSoldierLevel = Math.Max(1, maxSoldierLevel);
            this.workerTicksInterval = Math.Max(1, workerTicksInterval);
            this.ticksForRest = Math.Max(1, ticksForRest);
            workerTickCountdown = this.workerTicksInterval;
            managerEventObject = this.eventRegistry.CreateIdentifier(EventObjectType.Manager);

            foreach (JobType job in Enum.GetValues(typeof(JobType)))
            {
                if (!citizensByJob.ContainsKey(job))
                {
                    citizensByJob[job] = new HashSet<Citizen>();
                }

                if (!jobFieldCursor.ContainsKey(job))
                {
                    jobFieldCursor[job] = 0;
                }
            }
        }

        public IReadOnlyList<Citizen> Citizens => citizens;

        private EventObject GetCitizenEventObject(int citizenId)
        {
            return citizenEventObjects.TryGetValue(citizenId, out var identifier) ? identifier : default;
        }

        public Citizen AddCitizen(JobType job)
        {
            var citizen = new Citizen(nextCitizenIdentifier++, job)
            {
                TicksUntilFoodConsume = foodConsumeTicks,
                TicksUntilSoldierUpgrade = soldierUpgradeTicks,
                AwakenTicks = 0
            };

            citizens.Add(citizen);
            citizensByJob[citizen.Job].Add(citizen);
            citizenLookup[citizen.Identifier] = citizen;
            citizenEventObjects[citizen.Identifier] = eventRegistry.CreateIdentifier(EventObjectType.Worker);
            return citizen;
        }

        public void OnTick(TickContext context)
        {
            AdvancePopulationGrowth();
            ProcessCitizensLifecycle();
            UpdateAssignments();
            AssignIdleCitizens();
        }

        private void AdvancePopulationGrowth()
        {
            var capacity = fieldManager.GetTotalHousingCapacity();
            var population = citizens.Count;

            if (capacity <= population)
            {
                if (!populationGrowthPaused)
                {
                    populationGrowthPaused = true;
                    RaiseManagerEvent(EventType.PopulationGrowthPaused, $"주거 공간 부족: {population}/{capacity}");
                }

                return;
            }

            if (populationGrowthPaused)
            {
                populationGrowthPaused = false;
                RaiseManagerEvent(EventType.PopulationGrowthResumed, $"인구 성장이 재개되었습니다: {population}/{capacity}");
            }

            if (--workerTickCountdown > 0)
            {
                return;
            }

            workerTickCountdown = workerTicksInterval;
            var citizen = AddCitizen(JobType.Worker);
            RaiseCitizenEvent(EventType.PopulationGrowth, citizen, "새 일꾼이 합류했습니다.");
        }

        private void ProcessCitizensLifecycle()
        {
            for (var i = citizens.Count - 1; i >= 0; i--)
            {
                var citizen = citizens[i];

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
                        HandleCitizenDeath(i, citizen, "FoodShortage");
                        continue;
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

                if (citizen.Activity != CitizenActivity.Resting)
                {
                    citizen.AwakenTicks += 1;
                }

                if (citizen.AwakenTicks >= ticksForRest)
                {
                    citizen.NeedsRest = true;
                }
            }
        }

        private void HandleCitizenDeath(int index, Citizen citizen, string reason)
        {
            var target = GetCitizenEventObject(citizen.Identifier);
            ReleaseAssignment(citizen);
            citizen.Kill();

            RaiseCitizenEvent(EventType.CitizenDied, citizen, reason, citizen.Identifier, target);

            citizensByJob[citizen.Job].Remove(citizen);
            citizenLookup.Remove(citizen.Identifier);
            citizenEventObjects.Remove(citizen.Identifier);
            citizens.RemoveAt(index);
        }

        private void ReleaseAssignment(Citizen citizen)
        {
            var field = citizen.AssignedField;
            var task = citizen.AssignedTask;

            if (field != null)
            {
                if (citizen.Activity == CitizenActivity.Working || citizen.Activity == CitizenActivity.Resting)
                {
                    field.ReleaseSlot(task);
                }
                else if (citizen.Activity == CitizenActivity.Transforming)
                {
                    if (transformationByCitizen.TryGetValue(citizen.Identifier, out var transformation))
                    {
                        transformation.ReleaseWorker();
                        transformationByCitizen.Remove(citizen.Identifier);
                    }
                }
            }

            citizen.ClearAssignment();
        }

        private void UpdateAssignments()
        {
            foreach (var citizen in citizens)
            {
                switch (citizen.Activity)
                {
                    case CitizenActivity.Working:
                        ProcessWorkTick(citizen);
                        break;
                    case CitizenActivity.Resting:
                        ProcessRestTick(citizen);
                        break;
                    case CitizenActivity.Transforming:
                        ProcessTransformationTick(citizen);
                        break;
                }
            }
        }

        private void AssignIdleCitizens()
        {
            foreach (var citizen in citizens)
            {
                if (citizen.Activity != CitizenActivity.Idle)
                {
                    continue;
                }

                if (citizen.NeedsRest)
                {
                    if (TryAssignRest(citizen))
                    {
                        continue;
                    }

                    continue;
                }

                if (TryAssignTransformation(citizen))
                {
                    continue;
                }

                TryAssignWork(citizen);
            }
        }

        private bool TryAssignRest(Citizen citizen)
        {
            foreach (var field in fieldManager.Fields)
            {
                if (!IsRestField(field))
                {
                    continue;
                }

                if (!field.HasAvailableSlot)
                {
                    continue;
                }

                if (!field.TryReserveSlot(citizen.Job, out var task))
                {
                    continue;
                }

                citizen.AssignRest(field, task);
                RaiseCitizenEvent(EventType.RestStarted, citizen, field.Definition.Type.ToString());
                return true;
            }

            return false;
        }

        private bool TryAssignTransformation(Citizen citizen)
        {
            if (citizen.Job != JobType.Worker)
            {
                return false;
            }

            foreach (var transformation in fieldManager.ActiveTransformations)
            {
                if (transformation.HasAssignedWorker)
                {
                    continue;
                }

                transformation.AssignWorker(citizen.Identifier);
                transformationByCitizen[citizen.Identifier] = transformation;
                citizen.AssignTransformation(transformation.SourceField);
                RaiseCitizenEvent(EventType.FieldTransformationStarted, citizen, transformation.TargetDefinition.Type.ToString());
                return true;
            }

            return false;
        }

        private bool TryAssignWork(Citizen citizen)
        {
            if (!citizensByJob.TryGetValue(citizen.Job, out _))
            {
                return false;
            }

            var fields = fieldManager.Fields;
            if (fields.Count == 0)
            {
                return false;
            }

            var cursor = jobFieldCursor[citizen.Job] % Math.Max(1, fields.Count);

            for (var i = 0; i < fields.Count; i++)
            {
                var index = (cursor + i) % fields.Count;
                var field = fields[index];

                if (!IsWorkField(field))
                {
                    continue;
                }

                if (!field.HasAvailableSlot)
                {
                    continue;
                }

                if (!field.TryReserveSlot(citizen.Job, out var task))
                {
                    continue;
                }

                citizen.AssignWork(field, task);
                jobFieldCursor[citizen.Job] = (index + 1) % fields.Count;
                RaiseCitizenEvent(EventType.TaskStarted, citizen, task.Name);
                return true;
            }

            return false;
        }

        private static bool IsRestField(FieldState field)
        {
            var type = field.Definition.Type;
            return type == FieldType.Residence || type == FieldType.Residence2;
        }

        private static bool IsWorkField(FieldState field)
        {
            var type = field.Definition.Type;
            if (type == FieldType.Transforming)
            {
                return false;
            }

            if (type == FieldType.Residence || type == FieldType.Residence2)
            {
                return false;
            }

            return field.Tasks.Count > 0;
        }

        private void ProcessWorkTick(Citizen citizen)
        {
            var field = citizen.AssignedField;
            var task = citizen.AssignedTask;

            if (field == null || task == null)
            {
                citizen.ClearAssignment();
                return;
            }

            field.AddProgress(task, 1);
            var progress = field.GetProgress(task);
            if (progress < task.DurationTicks)
            {
                return;
            }

            HandleTaskCompletion(citizen, field, task);
        }

        private void ProcessRestTick(Citizen citizen)
        {
            var field = citizen.AssignedField;
            var task = citizen.AssignedTask;

            if (field == null || task == null)
            {
                citizen.ClearAssignment();
                return;
            }

            field.AddProgress(task, 1);
            var progress = field.GetProgress(task);
            if (progress < task.DurationTicks)
            {
                return;
            }

            field.ResetProgress(task);
            field.ReleaseSlot(task);

            citizen.AwakenTicks = 0;
            citizen.NeedsRest = false;
            citizen.ClearAssignment();

            RaiseCitizenEvent(EventType.RestCompleted, citizen, field.Definition.Type.ToString());
        }

        private void ProcessTransformationTick(Citizen citizen)
        {
            if (!transformationByCitizen.TryGetValue(citizen.Identifier, out var transformation))
            {
                citizen.ClearAssignment();
                return;
            }

            transformation.Advance(1);
            if (!transformation.IsCompleted)
            {
                return;
            }

            transformation.ReleaseWorker();
            transformationByCitizen.Remove(citizen.Identifier);
            fieldManager.CompleteTransformation(transformation);
            citizen.ClearAssignment();

            RaiseCitizenEvent(EventType.FieldTransformationCompleted, citizen, transformation.TargetDefinition.Type.ToString());
            RaiseManagerEvent(EventType.BuildingCompleted, transformation.TargetDefinition.Type.ToString());
        }

        private void HandleTaskCompletion(Citizen citizen, FieldState field, TaskDefinition task)
        {
            foreach (var result in task.Results)
            {
                resourceStore.Add(result.Type, result.Amount);
            }

            if (task.Outcome.Kind == TaskOutcomeKind.Field)
            {
                HandleFieldOutcome(field, task.Outcome.Field);
            }
            else if (task.Outcome.Kind == TaskOutcomeKind.Resource && task.Results.Count == 0)
            {
                var resource = task.Outcome.Resource;
                resourceStore.Add(resource.Type, resource.Amount);
            }

            field.ResetProgress(task);
            field.ReleaseSlot(task);
            citizen.ClearAssignment();

            RaiseCitizenEvent(EventType.TaskCompleted, citizen, task.Name);
        }

        private void HandleFieldOutcome(FieldState field, FieldType targetField)
        {
            if (targetField == FieldType.BadLand)
            {
                HandleTerritoryExpansion(field);
            }
            else
            {
                // TODO: 추가 필드 결과 처리
            }
        }

        private void HandleTerritoryExpansion(FieldState originField)
        {
            if (fieldManager.TryExpandTerritory())
            {
                RaiseManagerEvent(EventType.TerritoryExpansion, "영토가 확장되었습니다.");
            }
            else
            {
                RaiseManagerEvent(EventType.TerritoryExpansionFailed, "확장할 수 있는 영역이 없습니다.");
            }
        }

        private void RaiseCitizenEvent(EventType eventType, Citizen citizen, string message = null, int intValue = 0, EventObject? explicitTarget = null)
        {
            var parameter = new EventParameter
            {
                IntValue = intValue != 0 ? intValue : citizen?.Identifier ?? 0,
                StringValue = message ?? string.Empty,
                TargetTypes = citizen != null ? EventObjectType.Worker : EventObjectType.None
            };

            if (citizen != null)
            {
                var target = explicitTarget ?? GetCitizenEventObject(citizen.Identifier);
                if (target.Type != EventObjectType.None)
                {
                    parameter.Target = target;
                }
            }

            EventManager.Instance.Invoke(eventType, managerEventObject, parameter);
        }

        public bool RequestFieldTransformation(FieldType targetType)
        {
            var definition = fieldManager.GetDefinition(targetType);

            if (!HasSufficientResources(definition.ConstructionCosts))
            {
                RaiseManagerEvent(EventType.FieldTransformationFailed, $"자원 부족: {targetType}");
                return false;
            }

            var transformation = fieldManager.BeginTransformation(targetType);
            if (transformation == null)
            {
                RaiseManagerEvent(EventType.FieldTransformationFailed, $"배치 실패: {targetType}");
                return false;
            }

            ConsumeResources(definition.ConstructionCosts);
            RaiseManagerEvent(EventType.FieldTransformationStarted, targetType.ToString());
            return true;
        }

        private bool HasSufficientResources(IReadOnlyCollection<ResourceAmount> costs)
        {
            if (costs == null)
            {
                return true;
            }

            foreach (var cost in costs)
            {
                if (resourceStore.GetAmount(cost.Type) < cost.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        private void ConsumeResources(IReadOnlyCollection<ResourceAmount> costs)
        {
            if (costs == null)
            {
                return;
            }

            foreach (var cost in costs)
            {
                if (!resourceStore.TryConsume(cost.Type, cost.Amount))
                {
                    throw new InvalidOperationException($"자원 차감 실패: {cost.Type} {cost.Amount}");
                }
            }
        }

        private void RaiseManagerEvent(EventType eventType, string message = null, int intValue = 0)
        {
            var parameter = new EventParameter
            {
                IntValue = intValue,
                StringValue = message ?? string.Empty
            };

            EventManager.Instance.Invoke(eventType, managerEventObject, parameter);
        }

        public bool TryIncreaseJob(JobType job)
        {
            if (job == JobType.Worker)
            {
                return false;
            }

            if (!citizensByJob.TryGetValue(JobType.Worker, out var workerSet) || workerSet.Count == 0)
            {
                RaiseManagerEvent(EventType.JobChangeFailed, $"Worker 부족: {job}");
                return false;
            }

            var worker = GetFirstCitizen(workerSet);
            if (worker == null)
            {
                return false;
            }

            var costs = GetJobCost(job);
            if (!HasSufficientResources(costs))
            {
                RaiseManagerEvent(EventType.JobChangeFailed, $"자원 부족: {job}");
                return false;
            }

            ConsumeResources(costs);
            ChangeCitizenJob(worker, job);
            RaiseCitizenEvent(EventType.JobAssignmentChanged, worker, job.ToString());
            return true;
        }

        public bool TryDecreaseJob(JobType job)
        {
            if (job == JobType.Worker)
            {
                return false;
            }

            if (!citizensByJob.TryGetValue(job, out var jobSet) || jobSet.Count == 0)
            {
                RaiseManagerEvent(EventType.JobChangeFailed, $"해당 직업 인원 없음: {job}");
                return false;
            }

            var citizen = GetFirstCitizen(jobSet);
            if (citizen == null)
            {
                return false;
            }

            ChangeCitizenJob(citizen, JobType.Worker);
            RaiseCitizenEvent(EventType.JobAssignmentChanged, citizen, JobType.Worker.ToString());
            return true;
        }

        private Citizen GetFirstCitizen(HashSet<Citizen> set)
        {
            foreach (var citizen in set)
            {
                return citizen;
            }

            return null;
        }

        private IReadOnlyList<ResourceAmount> GetJobCost(JobType job)
        {
            return jobCosts != null && jobCosts.TryGetValue(job, out var costs)
                ? costs
                : Array.Empty<ResourceAmount>();
        }

        private void ChangeCitizenJob(Citizen citizen, JobType newJob)
        {
            if (citizen.Job == newJob)
            {
                return;
            }

            citizensByJob[citizen.Job].Remove(citizen);
            if (citizen.Activity != CitizenActivity.Idle)
            {
                ReleaseAssignment(citizen);
            }

            citizen.TryPromoteTo(newJob);
            citizensByJob[citizen.Job].Add(citizen);
        }
    }
}

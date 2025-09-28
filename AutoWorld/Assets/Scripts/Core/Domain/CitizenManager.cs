using System;
using System.Collections.Generic;
using AutoWorld.Core;
using AutoWorld.Core.Data;
using AutoWorld.Core.Services;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 주민 리스트를 관리하며 먹이 소비와 직업별 로직을 처리한다.
    /// </summary>
    public sealed class CitizenManager : ITickListener, IEventListener
    {
        private readonly List<Citizen> citizens = new List<Citizen>();
        private readonly Dictionary<int, Citizen> citizenLookup = new Dictionary<int, Citizen>();
        private readonly Dictionary<JobType, HashSet<Citizen>> citizensByJob = new Dictionary<JobType, HashSet<Citizen>>();
        private readonly Dictionary<JobType, int> jobFieldCursor = new Dictionary<JobType, int>();
        private readonly ResourceManager resourceManager;
        private readonly FieldManager fieldManager;
        private readonly EventRegistryService eventRegistry;
        private readonly IReadOnlyDictionary<JobType, IReadOnlyList<ResourceAmount>> jobCosts;
        private readonly Dictionary<string, List<EventAction>> actionsByEvent = new Dictionary<string, List<EventAction>>(StringComparer.Ordinal);
        private readonly HashSet<string> registeredEvents = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<PendingAction> deferredActions = new List<PendingAction>();
        private readonly int foodConsumeTicks;
        private readonly int soldierUpgradeTicks;
        private readonly int maxSoldierLevel;
        private readonly int workerTicksInterval;
        private readonly int ticksForRest;
        private int workerTickCountdown;
        private bool populationGrowthPaused;
        private int nextCitizenIdentifier = 1;
        private readonly Dictionary<int, EventObject> citizenEventObjects = new Dictionary<int, EventObject>();
        private readonly Dictionary<int, int> citizenIdByEventObjectId = new Dictionary<int, int>();
        private readonly EventObject managerEventObject;
        private readonly Dictionary<int, FieldTransformation> transformationByCitizen = new Dictionary<int, FieldTransformation>();
        private const int JobChangeFailureMissingWorker = 1;
        private const int JobChangeFailureInsufficientResource = 2;
        private const int JobChangeFailureNoCitizen = 3;
        private const string TiredState = "Tired";

        private readonly struct PendingAction
        {
            public PendingAction(EventAction action, EventObject source, EventParameter parameter)
            {
                Action = action;
                Source = source;
                Parameter = parameter;
            }

            public EventAction Action { get; }

            public EventObject Source { get; }

            public EventParameter Parameter { get; }
        }

        public CitizenManager(
            ResourceManager resourceManager,
            FieldManager fieldManager,
            EventRegistryService eventRegistry,
            int foodConsumeTicks,
            int soldierUpgradeTicks,
            int maxSoldierLevel,
            int workerTicksInterval,
            int ticksForRest,
            IReadOnlyDictionary<JobType, IReadOnlyList<ResourceAmount>> jobCosts)
        {
            this.resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            this.fieldManager = fieldManager ?? throw new ArgumentNullException(nameof(fieldManager));
            this.eventRegistry = eventRegistry ?? throw new ArgumentNullException(nameof(eventRegistry));
            this.jobCosts = jobCosts ?? new Dictionary<JobType, IReadOnlyList<ResourceAmount>>();
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

        public void ConfigureEventActions(IEnumerable<EventAction> eventActions)
        {
            if (eventActions == null)
            {
                return;
            }

            foreach (var action in eventActions)
            {
                if (action == null)
                {
                    continue;
                }

                if (!string.Equals(action.EventListener, nameof(CitizenManager), StringComparison.Ordinal))
                {
                    continue;
                }

                if (!actionsByEvent.TryGetValue(action.EventName, out var list))
                {
                    list = new List<EventAction>();
                    actionsByEvent[action.EventName] = list;
                }

                list.Add(action);

                if (registeredEvents.Add(action.EventName))
                {
                    EventManager.Instance.Register(action.EventName, this);
                }
            }
        }

        public IReadOnlyList<Citizen> Citizens => citizens;

        private EventObject GetCitizenEventObject(int citizenId)
        {
            return citizenEventObjects.TryGetValue(citizenId, out var identifier) ? identifier : default;
        }

        private bool TryGetCitizen(EventObject eventObject, out Citizen citizen)
        {
            citizen = null;
            if (eventObject.Type != EventObjectType.Citizen)
            {
                return false;
            }

            if (!citizenIdByEventObjectId.TryGetValue(eventObject.Id, out var citizenId))
            {
                return false;
            }

            return citizenLookup.TryGetValue(citizenId, out citizen);
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

            var identifier = eventRegistry.CreateIdentifier(EventObjectType.Citizen);
            citizenEventObjects[citizen.Identifier] = identifier;
            citizenIdByEventObjectId[identifier.Id] = citizen.Identifier;
            return citizen;
        }

        public int GetJobCount(JobType job)
        {
            return citizensByJob.TryGetValue(job, out var collection) ? collection.Count : 0;
        }

        public void OnTick(TickContext context)
        {
            AdvancePopulationGrowth();
            ProcessCitizensLifecycle();
            UpdateAssignments();
            AssignIdleCitizens();
            ProcessDeferredActions();
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
                    RaiseManagerEvent(GameEvents.PopulationGrowthPaused, capacity.ToString(), population);
                }

                return;
            }

            if (populationGrowthPaused)
            {
                populationGrowthPaused = false;
                RaiseManagerEvent(GameEvents.PopulationGrowthResumed, capacity.ToString(), population);
            }

            if (--workerTickCountdown > 0)
            {
                return;
            }

            workerTickCountdown = workerTicksInterval;
            var citizen = AddCitizen(JobType.Worker);
            RaiseCitizenEvent(GameEvents.PopulationGrowth, citizen);
        }

        private void ProcessCitizensLifecycle()
        {
            for (var i = citizens.Count - 1; i >= 0; i--)
            {
                var citizen = citizens[i];

                citizen.TicksUntilFoodConsume -= 1;
                if (citizen.TicksUntilFoodConsume <= 0)
                {
                    if (resourceManager.TryConsume(ResourceType.Food, 1))
                    {
                        citizen.TicksUntilFoodConsume = foodConsumeTicks;
                        RaiseCitizenEvent(GameEvents.CitizenFoodConsumed, citizen);
                    }
                    else
                    {
                        citizen.TicksUntilFoodConsume = foodConsumeTicks;
                        RaiseCitizenEvent(GameEvents.CitizenFoodShortage, citizen);
                        RaiseCitizenEvent(GameEvents.MissingMeal, citizen, "FoodShortage");
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
                            RaiseCitizenEvent(GameEvents.SoldierLevelUpgraded, citizen, null, citizen.Level);
                        }
                    }
                }

                if (citizen.Activity != CitizenActivity.Resting)
                {
                    citizen.AwakenTicks += 1;
                }

                if (citizen.AwakenTicks >= ticksForRest)
                {
                    if (!citizen.HasState(TiredState))
                    {
                        RaiseCitizenEvent(GameEvents.OverflowingTicksForRest, citizen, TiredState);
                    }

                    citizen.NeedsRest = true;
                }
            }
        }

        private void HandleCitizenDeath(int index, Citizen citizen, string reason)
        {
            var target = GetCitizenEventObject(citizen.Identifier);
            ReleaseAssignment(citizen);
            citizen.Kill();

            RaiseCitizenEvent(GameEvents.CitizenDied, citizen, reason, citizen.Identifier, target);

            citizensByJob[citizen.Job].Remove(citizen);
            citizenLookup.Remove(citizen.Identifier);
            if (citizenEventObjects.TryGetValue(citizen.Identifier, out var identifier))
            {
                citizenIdByEventObjectId.Remove(identifier.Id);
                citizenEventObjects.Remove(citizen.Identifier);
            }
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

        public void OnEvent(string eventName, EventObject source, EventParameter parameter)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            if (!actionsByEvent.TryGetValue(eventName, out var actions))
            {
                return;
            }

            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action.ActionImmediately)
                {
                    ExecuteAction(action, source, parameter);
                }
                else
                {
                    deferredActions.Add(new PendingAction(action, source, parameter));
                }
            }
        }

        private void ProcessDeferredActions()
        {
            if (deferredActions.Count == 0)
            {
                return;
            }

            for (var i = 0; i < deferredActions.Count; i++)
            {
                var pending = deferredActions[i];
                ExecuteAction(pending.Action, pending.Source, pending.Parameter);
            }

            deferredActions.Clear();
        }

        private void ExecuteAction(EventAction action, EventObject source, EventParameter parameter)
        {
            switch (action.ActionName)
            {
                case "AddCitizenState":
                    if (TryResolveCitizen(source, parameter, out var citizenToAdd))
                    {
                        ApplyCitizenState(citizenToAdd, action.StringParameter, add: true);
                    }

                    break;
                case "RemoveCitizenState":
                    if (TryResolveCitizen(source, parameter, out var citizenToRemove))
                    {
                        ApplyCitizenState(citizenToRemove, action.StringParameter, add: false);
                    }

                    break;
                case "DestroyCitizen":
                    if (TryResolveCitizen(source, parameter, out var citizenToDestroy))
                    {
                        var reason = !string.IsNullOrWhiteSpace(action.StringParameter)
                            ? action.StringParameter
                            : (!string.IsNullOrWhiteSpace(parameter.StringValue) ? parameter.StringValue : action.EventName);
                        DestroyCitizen(citizenToDestroy, reason);
                    }

                    break;
            }
        }

        private void ApplyCitizenState(Citizen citizen, string stateName, bool add)
        {
            if (citizen == null)
            {
                return;
            }

            var state = stateName?.Trim();
            if (string.IsNullOrEmpty(state))
            {
                return;
            }

            if (add)
            {
                if (citizen.AddState(state) && string.Equals(state, TiredState, StringComparison.OrdinalIgnoreCase))
                {
                    citizen.NeedsRest = true;
                }
            }
            else
            {
                if (citizen.RemoveState(state) && string.Equals(state, TiredState, StringComparison.OrdinalIgnoreCase))
                {
                    citizen.NeedsRest = false;
                }
            }
        }

        private bool TryResolveCitizen(EventObject source, EventParameter parameter, out Citizen citizen)
        {
            if (TryGetCitizen(source, out citizen))
            {
                return true;
            }

            if (parameter.Target.HasValue && TryGetCitizen(parameter.Target.Value, out citizen))
            {
                return true;
            }

            if (parameter.CustomObject is int citizenId && citizenId > 0 && citizenLookup.TryGetValue(citizenId, out citizen))
            {
                return true;
            }

            citizen = null;
            return false;
        }

        private void DestroyCitizen(Citizen citizen, string reason)
        {
            if (citizen == null)
            {
                return;
            }

            var index = citizens.IndexOf(citizen);
            if (index < 0)
            {
                return;
            }

            HandleCitizenDeath(index, citizen, reason ?? string.Empty);
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
                RaiseCitizenEvent(GameEvents.RestStarted, citizen, field.Definition.Type.ToString());
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
                RaiseCitizenEvent(GameEvents.FieldTransformationStarted, citizen, transformation.TargetDefinition.Type.ToString());
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
                RaiseCitizenEvent(GameEvents.TaskStarted, citizen, task.Name);
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

            RaiseCitizenEvent(GameEvents.RestCompleted, citizen, field.Definition.Type.ToString());
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

            RaiseCitizenEvent(GameEvents.FieldTransformationCompleted, citizen, transformation.TargetDefinition.Type.ToString());
            RaiseManagerEvent(GameEvents.BuildingCompleted, transformation.TargetDefinition.Type.ToString());
        }

        private void HandleTaskCompletion(Citizen citizen, FieldState field, TaskDefinition task)
        {
            field.ResetProgress(task);
            field.ReleaseSlot(task);
            citizen.ClearAssignment();

            RaiseCitizenEvent(GameEvents.TaskCompleted, citizen, task.Name);

            if (!string.IsNullOrWhiteSpace(task.RiseEvent))
            {
                var parameter = new EventParameter
                {
                    CustomObject = citizen.Identifier
                };

                var fieldIdentifier = fieldManager.GetEventObject(field);
                if (fieldIdentifier.Type != EventObjectType.None)
                {
                    parameter.Target = fieldIdentifier;
                    parameter.TargetTypes = EventObjectType.Field;
                }

                fieldManager.RaiseFieldEvent(field, task.RiseEvent, parameter);
            }
        }

        private void RaiseCitizenEvent(string eventName, Citizen citizen, string data = null, int intValue = 0, EventObject? explicitTarget = null)
        {
            var parameter = new EventParameter
            {
                IntValue = intValue,
                StringValue = data ?? string.Empty,
                TargetTypes = citizen != null ? EventObjectType.Citizen : EventObjectType.None,
                CustomObject = citizen?.Identifier ?? 0
            };

            if (citizen != null)
            {
                var target = explicitTarget ?? GetCitizenEventObject(citizen.Identifier);
                if (target.Type != EventObjectType.None)
                {
                    parameter.Target = target;
                }
            }

            var source = citizen != null ? GetCitizenEventObject(citizen.Identifier) : managerEventObject;
            if (source.Type == EventObjectType.None)
            {
                source = managerEventObject;
            }

            source.RaiseEvent(eventName, parameter);
        }

        private bool HasSufficientResources(IReadOnlyCollection<ResourceAmount> costs)
        {
            return resourceManager.HasSufficientResources(costs);
        }

        private void ConsumeResources(IReadOnlyCollection<ResourceAmount> costs)
        {
            resourceManager.ConsumeResources(costs);
        }

        private void RaiseManagerEvent(string eventName, string data = null, int intValue = 0)
        {
            var parameter = new EventParameter
            {
                IntValue = intValue,
                StringValue = data ?? string.Empty
            };

            managerEventObject.RaiseEvent(eventName, parameter);
        }

        public bool TryIncreaseJob(JobType job)
        {
            if (job == JobType.Worker)
            {
                return false;
            }

            if (!citizensByJob.TryGetValue(JobType.Worker, out var workerSet) || workerSet.Count == 0)
            {
                RaiseManagerEvent(GameEvents.JobChangeFailed, job.ToString(), JobChangeFailureMissingWorker);
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
                RaiseManagerEvent(GameEvents.JobChangeFailed, job.ToString(), JobChangeFailureInsufficientResource);
                return false;
            }

            ConsumeResources(costs);
            ChangeCitizenJob(worker, job);
            RaiseCitizenEvent(GameEvents.JobAssignmentChanged, worker, job.ToString());
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
                RaiseManagerEvent(GameEvents.JobChangeFailed, job.ToString(), JobChangeFailureNoCitizen);
                return false;
            }

            var citizen = GetFirstCitizen(jobSet);
            if (citizen == null)
            {
                return false;
            }

            ChangeCitizenJob(citizen, JobType.Worker);
            RaiseCitizenEvent(GameEvents.JobAssignmentChanged, citizen, JobType.Worker.ToString());
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

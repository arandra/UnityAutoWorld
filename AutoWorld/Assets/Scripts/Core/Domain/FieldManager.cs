using System;
using System.Collections.Generic;
using AutoWorld.Core.Data;
using AutoWorld.Core.Services;
using AutoWorld.Core;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 필드 상태를 생성하고 조회하는 매니저다.
    /// </summary>
    public sealed class FieldManager : IEventListener, ITickListener
    {
        private readonly Dictionary<FieldType, FieldDefinition> definitions;
        private readonly Dictionary<int, GridMap> gridMaps;
        private readonly Dictionary<FieldCoordinate, FieldState> coordinateMap = new Dictionary<FieldCoordinate, FieldState>();
        private readonly List<FieldState> fields = new List<FieldState>();
        private readonly List<FieldTransformation> transformations = new List<FieldTransformation>();
        private readonly EventRegistryService eventRegistry;
        private readonly Dictionary<FieldState, EventObject> fieldEventObjects = new Dictionary<FieldState, EventObject>();
        private readonly Dictionary<int, FieldState> fieldByEventId = new Dictionary<int, FieldState>();
        private readonly Dictionary<string, List<EventAction>> actionsByEvent = new Dictionary<string, List<EventAction>>(StringComparer.Ordinal);
        private readonly HashSet<string> registeredEvents = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<PendingAction> deferredActions = new List<PendingAction>();
        private readonly EventObject managerEventObject;
        private const int FieldTransformationFailureInsufficientResources = 1;
        private const int FieldTransformationFailurePlacement = 2;
        private int minX;
        private int maxX;
        private int minY;
        private int maxY;
        private IDebugLog DebugLog { get; }

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

        public FieldManager(
            IReadOnlyDictionary<FieldType, FieldDefinition> definitions,
            IReadOnlyDictionary<int, GridMap> gridMaps,
            EventRegistryService eventRegistry,
            IDebugLog debugLog)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (gridMaps == null)
            {
                throw new ArgumentNullException(nameof(gridMaps));
            }

            if (eventRegistry == null)
            {
                throw new ArgumentNullException(nameof(eventRegistry));
            }

            this.definitions = new Dictionary<FieldType, FieldDefinition>(definitions);
            this.gridMaps = new Dictionary<int, GridMap>(gridMaps);
            this.eventRegistry = eventRegistry;
            this.DebugLog = debugLog;
            managerEventObject = this.eventRegistry.CreateIdentifier(EventObjectType.Manager);
        }

        public IReadOnlyList<FieldState> Fields => fields;

        public IReadOnlyDictionary<FieldCoordinate, FieldState> Coordinates => coordinateMap;

        public FieldState TownHall { get; private set; }

        public IReadOnlyList<FieldTransformation> ActiveTransformations => transformations;

        public EventObject GetEventObject(FieldState field)
        {
            return field != null && fieldEventObjects.TryGetValue(field, out var identifier) ? identifier : default;
        }

        public void RaiseFieldEvent(FieldState field, string eventName, EventParameter parameter)
        {
            if (field == null || string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            if (!fieldEventObjects.TryGetValue(field, out var identifier))
            {
                return;
            }

            identifier.RaiseEvent(eventName, parameter);
        }

        public bool RequestFieldTransformation(FieldType targetType, ResourceManager resourceManager)
        {
            if (resourceManager == null)
            {
                throw new ArgumentNullException(nameof(resourceManager));
            }

            var definition = GetDefinition(targetType);

            if (!resourceManager.HasSufficientResources(definition.ConstructionCosts))
            {
                RaiseManagerEvent(GameEvents.FieldTransformationFailed, targetType.ToString(), FieldTransformationFailureInsufficientResources);
                return false;
            }

            var transformation = BeginTransformation(targetType);
            if (transformation == null)
            {
                RaiseManagerEvent(GameEvents.FieldTransformationFailed, targetType.ToString(), FieldTransformationFailurePlacement);
                return false;
            }

            resourceManager.ConsumeResources(definition.ConstructionCosts);
            RaiseManagerEvent(GameEvents.FieldTransformationStarted, targetType.ToString());
            return true;
        }

        private void RaiseManagerEvent(string eventName, string data = null, int intValue = 0)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            var parameter = new EventParameter
            {
                StringValue = data ?? string.Empty,
                IntValue = intValue
            };

            managerEventObject.RaiseEvent(eventName, parameter);
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

                if (!string.Equals(action.EventListener, nameof(FieldManager), StringComparison.Ordinal))
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

        public void OnEvent(string eventName, EventObject source, EventParameter parameter)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }
            
            DebugLog?.Log($"[FieldManager.OnEvent:{eventName}] source:({source.ToString()}),  parameter:({parameter.ToString()})");

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

        public void OnTick(TickContext context)
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

        public FieldDefinition GetDefinition(FieldType type)
        {
            if (!definitions.TryGetValue(type, out var definition))
            {
                throw new InvalidOperationException($"정의되지 않은 필드 타입입니다: {type}");
            }

            return definition;
        }

        public int GetFieldCount(FieldType type)
        {
            var count = 0;
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field?.Definition?.Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        public int GetTotalHousingCapacity()
        {
            var capacity = 0;
            foreach (var field in fields)
            {
                if (IsHousingField(field.Definition.Type))
                {
                    capacity += Math.Max(0, field.Definition.Slot);
                }
            }

            return capacity;
        }

        public void InitializeTerritory(int initialBadLandSize)
        {
            if (initialBadLandSize <= 0)
            {
                return;
            }

            if (!gridMaps.TryGetValue(initialBadLandSize, out var grid))
            {
                throw new InvalidOperationException($"GridMap에 {initialBadLandSize} 크기 정보가 없습니다.");
            }

            var width = Math.Max(1, grid.X);
            var height = Math.Max(1, grid.Y);

            var badLandDefinition = GetDefinition(FieldType.BadLand);

            coordinateMap.Clear();
            fields.Clear();
            fieldEventObjects.Clear();
            fieldByEventId.Clear();
            TownHall = null;

            minX = 0;
            minY = 0;
            maxX = width - 1;
            maxY = height - 1;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var coordinate = new FieldCoordinate(x, y);
                    var field = new FieldState(badLandDefinition, new[] { coordinate });
                    coordinateMap[coordinate] = field;
                    fields.Add(field);
                    RegisterField(field);
                }
            }
        }

        public bool TryPlaceInitialField(FieldType type)
        {
            var definition = GetDefinition(type);
            var area = FindPlacementArea(definition.Size > 0 ? definition.Size : 1);
            if (area.Count == 0)
            {
                return false;
            }

            ReplaceAreaWithField(type, area);
            return true;
        }

        private List<FieldCoordinate> FindPlacementArea(int requiredSize)
        {
            var area = new List<FieldCoordinate>();

            if (requiredSize <= 1)
            {
                foreach (var pair in coordinateMap)
                {
                    if (pair.Value.IsEmpty)
                    {
                        area.Add(pair.Key);
                        if (IsSurroundingAreaEmpty(area))
                        {
                            return new List<FieldCoordinate>(area);
                        }

                        area.Clear();
                    }
                }

                return area;
            }

            if (!gridMaps.TryGetValue(requiredSize, out var grid))
            {
                return area;
            }

            var width = Math.Max(1, grid.X);
            var height = Math.Max(1, grid.Y);

            var maxStartX = maxX - width + 1;
            var maxStartY = maxY - height + 1;

            for (var y = minY; y <= maxStartY; y++)
            {
                for (var x = minX; x <= maxStartX; x++)
                {
                    area.Clear();
                    if (!TryCollectArea(x, y, width, height, area))
                    {
                        continue;
                    }

                    if (IsSurroundingAreaEmpty(area))
                    {
                        return new List<FieldCoordinate>(area);
                    }
                }
            }

            area.Clear();
            return area;
        }

        private bool TryCollectArea(int startX, int startY, int width, int height, List<FieldCoordinate> buffer)
        {
            buffer.Clear();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var coordinate = new FieldCoordinate(startX + x, startY + y);
                    if (!coordinateMap.TryGetValue(coordinate, out var field))
                    {
                        return false;
                    }

                    if (!field.IsEmpty)
                    {
                        return false;
                    }

                    buffer.Add(coordinate);
                }
            }

            return buffer.Count > 0;
        }

        private bool IsSurroundingAreaEmpty(List<FieldCoordinate> area)
        {
            var offsets = new[] { -1, 0, 1 };
            var areaSet = new HashSet<FieldCoordinate>(area);

            foreach (var coordinate in area)
            {
                foreach (var dx in offsets)
                {
                    foreach (var dy in offsets)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        var neighbor = new FieldCoordinate(coordinate.X + dx, coordinate.Y + dy);
                        if (!coordinateMap.TryGetValue(neighbor, out var field))
                        {
                            continue;
                        }

                        if (areaSet.Contains(neighbor))
                        {
                            continue;
                        }

                        if (!field.IsEmpty)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private FieldState ReplaceAreaWithField(FieldType type, IReadOnlyList<FieldCoordinate> area)
        {
            var definition = GetDefinition(type);
            var newField = new FieldState(definition, area);
            var removed = new HashSet<FieldState>();

            foreach (var coordinate in area)
            {
                if (coordinateMap.TryGetValue(coordinate, out var existingField))
                {
                    if (removed.Add(existingField))
                    {
                        RemoveField(existingField);
                    }
                }

                coordinateMap[coordinate] = newField;
                UpdateBounds(coordinate);
            }

            fields.Add(newField);
            RegisterField(newField);

            if (type == FieldType.TownHall)
            {
                TownHall = newField;
            }

            return newField;
        }

        public FieldTransformation BeginTransformation(FieldType targetType)
        {
            var targetDefinition = GetDefinition(targetType);
            var requiredSize = targetDefinition.Size > 0 ? targetDefinition.Size : 1;
            var area = FindPlacementArea(requiredSize);
            if (area.Count == 0)
            {
                return null;
            }

            var transformField = ReplaceAreaWithField(FieldType.Transforming, area);
            var transformation = new FieldTransformation(transformField, targetDefinition, area);
            transformations.Add(transformation);
            return transformation;
        }

        public void CompleteTransformation(FieldTransformation transformation)
        {
            if (transformation == null)
            {
                return;
            }

            ReplaceAreaWithField(transformation.TargetDefinition.Type, transformation.Coordinates);
            transformations.Remove(transformation);
        }

        public void CancelTransformation(FieldTransformation transformation)
        {
            if (transformation == null)
            {
                return;
            }

            ReplaceAreaWithField(FieldType.BadLand, transformation.Coordinates);
            transformations.Remove(transformation);
        }

        public FieldTransformation FindTransformation(FieldState field)
        {
            foreach (var transformation in transformations)
            {
                if (transformation.SourceField == field)
                {
                    return transformation;
                }
            }

            return null;
        }

        private void RegisterField(FieldState field)
        {
            if (field == null)
            {
                return;
            }

            if (fieldEventObjects.ContainsKey(field))
            {
                return;
            }

            var identifier = eventRegistry.CreateIdentifier(EventObjectType.Field);
            fieldEventObjects[field] = identifier;
            fieldByEventId[identifier.Id] = field;
        }

        private void RemoveField(FieldState field)
        {
            if (field == null)
            {
                return;
            }

            fields.Remove(field);

            if (fieldEventObjects.TryGetValue(field, out var identifier))
            {
                fieldEventObjects.Remove(field);
                fieldByEventId.Remove(identifier.Id);
            }
        }

        private bool TryGetField(EventObject identifier, out FieldState field)
        {
            if (identifier.Type == EventObjectType.Field && fieldByEventId.TryGetValue(identifier.Id, out field))
            {
                return true;
            }

            field = null;
            return false;
        }

        private void ExecuteAction(EventAction action, EventObject source, EventParameter parameter)
        {
            DebugLog?.Log($"[FieldManager.ExecuteAction:{action?.EventName}] source:({source.ToString()}),  parameter:({parameter.ToString()})");

            if (action == null)
            {
                return;
            }

            switch (action.ActionName)
            {
                case "TransformField":
                    HandleTransformField(action, source, parameter);
                    break;
            }
        }

        private void HandleTransformField(EventAction action, EventObject source, EventParameter parameter)
        {
            if (string.IsNullOrWhiteSpace(action.StringParameter))
            {
                TransformFieldToBadLand(source);
                return;
            }

            if (!Enum.TryParse(action.StringParameter, false, out FieldType targetType))
            {
                return;
            }

            var targetIdentifier = parameter.Target ?? source;
            if (!TryGetField(targetIdentifier, out var field))
            {
                return;
            }

            var area = new List<FieldCoordinate>(field.Coordinates);
            ReplaceAreaWithField(targetType, area);
        }
        
        private void TransformFieldToBadLand(EventObject target)
        {
            if (!TryGetField(target, out var field))
            {
                return;
            }
            DebugLog?.Log($"[FieldManager.TransformFieldToBadLand] target:({target.ToString()})");
            var area = new List<FieldCoordinate>(field.Coordinates);
            ReplaceAreaWithField(FieldType.BadLand, area);
        }

        private void UpdateBounds(FieldCoordinate coordinate)
        {
            if (coordinateMap.Count == 0 && fields.Count == 0)
            {
                minX = maxX = coordinate.X;
                minY = maxY = coordinate.Y;
                return;
            }

            if (coordinate.X < minX)
            {
                minX = coordinate.X;
            }

            if (coordinate.X > maxX)
            {
                maxX = coordinate.X;
            }

            if (coordinate.Y < minY)
            {
                minY = coordinate.Y;
            }

            if (coordinate.Y > maxY)
            {
                maxY = coordinate.Y;
            }
        }

        public bool TryExpandTerritory()
        {
            if (TownHall == null)
            {
                return false;
            }

            var candidates = new HashSet<FieldCoordinate>();

            foreach (var field in fields)
            {
                foreach (var coordinate in field.Coordinates)
                {
                    foreach (var neighbor in EnumerateNeighbors(coordinate))
                    {
                        if (coordinateMap.ContainsKey(neighbor))
                        {
                            continue;
                        }

                        candidates.Add(neighbor);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var townHallCoordinate = TownHall.Root;
            FieldCoordinate best = default;
            var bestDistance = double.MaxValue;

            foreach (var candidate in candidates)
            {
                var dx = candidate.X - townHallCoordinate.X;
                var dy = candidate.Y - townHallCoordinate.Y;
                var distance = (dx * dx) + (dy * dy);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            ReplaceAreaWithField(FieldType.BadLand, new[] { best });
            return true;
        }

        private static IEnumerable<FieldCoordinate> EnumerateNeighbors(FieldCoordinate coordinate)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    yield return new FieldCoordinate(coordinate.X + dx, coordinate.Y + dy);
                }
            }
        }

        private static bool IsHousingField(FieldType type)
        {
            return type == FieldType.Residence || type == FieldType.Residence2;
        }
    }
}

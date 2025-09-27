using System;
using System.Collections.Generic;
using AutoWorld.Core.Data;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 필드 상태를 생성하고 조회하는 매니저다.
    /// </summary>
    public sealed class FieldManager
    {
        private readonly Dictionary<FieldType, FieldDefinition> definitions;
        private readonly Dictionary<int, GridMap> gridMaps;
        private readonly Dictionary<FieldCoordinate, FieldState> coordinateMap = new Dictionary<FieldCoordinate, FieldState>();
        private readonly List<FieldState> fields = new List<FieldState>();
        private readonly List<FieldTransformation> transformations = new List<FieldTransformation>();
        private int minX;
        private int maxX;
        private int minY;
        private int maxY;

        public FieldManager(IReadOnlyDictionary<FieldType, FieldDefinition> definitions, IReadOnlyDictionary<int, GridMap> gridMaps)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (gridMaps == null)
            {
                throw new ArgumentNullException(nameof(gridMaps));
            }

            this.definitions = new Dictionary<FieldType, FieldDefinition>(definitions);
            this.gridMaps = new Dictionary<int, GridMap>(gridMaps);
        }

        public IReadOnlyList<FieldState> Fields => fields;

        public IReadOnlyDictionary<FieldCoordinate, FieldState> Coordinates => coordinateMap;

        public FieldState TownHall { get; private set; }

        public IReadOnlyList<FieldTransformation> ActiveTransformations => transformations;

        public FieldDefinition GetDefinition(FieldType type)
        {
            if (!definitions.TryGetValue(type, out var definition))
            {
                throw new InvalidOperationException($"정의되지 않은 필드 타입입니다: {type}");
            }

            return definition;
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
                        fields.Remove(existingField);
                    }
                }

                coordinateMap[coordinate] = newField;
                UpdateBounds(coordinate);
            }

            fields.Add(newField);

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

using System;
using System.Collections.Generic;
using AutoWorld.Core;
using Datas;
using AutoWorld.Core.Data;

namespace AutoWorld.Loading.Steps
{
    public sealed class FieldDefinitionBuildStep : ILoadStep
    {
        public string Description => "필드 정의 생성";

        public void Run(LoadingContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.FieldsAsset?.Values == null || context.FieldTransformsAsset?.Values == null || context.GridMapsAsset?.Values == null || context.JobsAsset?.Values == null)
            {
                throw new InvalidOperationException("필드 관련 데이터가 로드되지 않았습니다.");
            }

            var tasksByField = BuildAllTasks(context.TasksAsset);

            var definitions = new Dictionary<FieldType, FieldDefinition>();

            foreach (var field in context.FieldsAsset.Values)
            {
                if (!TryParseFieldType(field.Name, out var fieldType))
                {
                    throw new InvalidOperationException($"FieldType 변환 실패: {field.Name}");
                }

                var tasks = tasksByField.TryGetValue(field.Name, out var taskList)
                    ? taskList
                    : Array.Empty<TaskDefinition>();

                var definition = new FieldDefinition(
                    fieldType,
                    field.Empty,
                    size: 1,
                    slot: 0,
                    constructionTicks: 0,
                    constructionCosts: Array.Empty<ResourceAmount>(),
                    requirements: Array.Empty<FieldType>(),
                    tasks);

                definitions[fieldType] = definition;
            }

            foreach (var transform in context.FieldTransformsAsset.Values)
            {
                if (!TryParseFieldType(transform.Name, out var fieldType))
                {
                    throw new InvalidOperationException($"FieldType 변환 실패: {transform.Name}");
                }

                if (!definitions.TryGetValue(fieldType, out var baseDefinition))
                {
                    throw new InvalidOperationException($"Fields 데이터에 {transform.Name} 엔트리가 없습니다.");
                }

                var costResources = BuildResourceAmounts(transform.CostResources);
                var requirements = BuildFieldRequirements(transform.Requires);
                var tasks = baseDefinition.Tasks;

                var definition = new FieldDefinition(
                    fieldType,
                    baseDefinition.IsEmpty,
                    transform.Size,
                    transform.Slot,
                    transform.CostTicks,
                    costResources,
                    requirements,
                    tasks);

                definitions[fieldType] = definition;
            }

            context.FieldDefinitions = definitions;
            context.GridMapLookup = BuildGridMapLookup(context.GridMapsAsset);
            context.JobCosts = BuildJobCosts(context.JobsAsset);
            context.EventActions = context.EventActionsAsset?.Values;
        }

        private static Dictionary<string, IReadOnlyList<TaskDefinition>> BuildAllTasks(Tasks tasksAsset)
        {
            var intermediate = new Dictionary<string, List<TaskDefinition>>(StringComparer.Ordinal);

            if (tasksAsset?.Values == null)
            {
                return new Dictionary<string, IReadOnlyList<TaskDefinition>>(StringComparer.Ordinal);
            }

            foreach (var task in tasksAsset.Values)
            {
                if (!intermediate.TryGetValue(task.Field, out var list))
                {
                    list = new List<TaskDefinition>();
                    intermediate[task.Field] = list;
                }

                list.Add(BuildTask(task));
            }

            var result = new Dictionary<string, IReadOnlyList<TaskDefinition>>(StringComparer.Ordinal);
            foreach (var pair in intermediate)
            {
                result[pair.Key] = pair.Value;
            }

            return result;
        }

        private static IReadOnlyDictionary<int, GridMap> BuildGridMapLookup(GridMaps gridMaps)
        {
            var result = new Dictionary<int, GridMap>();

            if (gridMaps?.Values == null)
            {
                return result;
            }

            foreach (var entry in gridMaps.Values)
            {
                if (!result.ContainsKey(entry.Size))
                {
                    result[entry.Size] = entry;
                }
            }

            return result;
        }

        private static IReadOnlyDictionary<JobType, IReadOnlyList<ResourceAmount>> BuildJobCosts(Jobs jobsAsset)
        {
            var result = new Dictionary<JobType, IReadOnlyList<ResourceAmount>>();

            if (jobsAsset?.Values == null)
            {
                return result;
            }

            foreach (var job in jobsAsset.Values)
            {
                if (!Enum.TryParse(job.Name, false, out JobType jobType))
                {
                    continue;
                }

                var costs = BuildResourceAmounts(job.CostResources);
                result[jobType] = costs;
            }

            return result;
        }

        private static TaskDefinition BuildTask(Core.Data.Task task)
        {
            var jobParse = ParseJob(task.JobName);
            return new TaskDefinition(
                task.Name,
                jobParse.Job,
                jobParse.AllowsAny,
                task.Tick,
                task.RiseEvent);
        }

        private static IReadOnlyList<ResourceAmount> BuildResourceAmounts(IEnumerable<Pair<string, int>> pairs)
        {
            if (pairs == null)
            {
                return Array.Empty<ResourceAmount>();
            }

            var list = new List<ResourceAmount>();
            foreach (var (key, value) in pairs)
            {
                if (!TryParseResourceType(key, out var resourceType))
                {
                    throw new InvalidOperationException($"ResourceType 변환 실패: {key}");
                }

                list.Add(new ResourceAmount(resourceType, value));
            }

            return list;
        }

        private static IReadOnlyList<FieldType> BuildFieldRequirements(IEnumerable<string> names)
        {
            if (names == null)
            {
                return Array.Empty<FieldType>();
            }

            var list = new List<FieldType>();
            foreach (var name in names)
            {
                if (!TryParseFieldType(name, out var fieldType))
                {
                    throw new InvalidOperationException($"필드 요구사항 변환 실패: {name}");
                }

                list.Add(fieldType);
            }

            return list;
        }

        private static (JobType? Job, bool AllowsAny) ParseJob(string jobName)
        {
            if (string.IsNullOrWhiteSpace(jobName))
            {
                return (null, false);
            }

            if (string.Equals(jobName, "Any", StringComparison.OrdinalIgnoreCase))
            {
                return (null, true);
            }

            if (!TryParseJobType(jobName, out var jobType))
            {
                throw new InvalidOperationException($"JobType 변환 실패: {jobName}");
            }

            return (jobType, false);
        }

        private static bool TryParseFieldType(string name, out FieldType type)
        {
            return Enum.TryParse(name, false, out type);
        }

        private static bool TryParseResourceType(string name, out ResourceType type)
        {
            return Enum.TryParse(name, false, out type);
        }

        private static bool TryParseJobType(string name, out JobType type)
        {
            return Enum.TryParse(name, false, out type);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using AutoWorld.Core;
using Datas;
using Datas.Tables;

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

            var fieldLookup = context.FieldsAsset.Values.ToDictionary(f => f.Name, StringComparer.Ordinal);

            var definitions = new Dictionary<FieldType, FieldDefinition>();

            foreach (var transform in context.FieldTransformsAsset.Values)
            {
                if (!TryParseFieldType(transform.Name, out var fieldType))
                {
                    throw new InvalidOperationException($"FieldType 변환 실패: {transform.Name}");
                }

                if (!fieldLookup.TryGetValue(transform.Name, out var fieldData))
                {
                    throw new InvalidOperationException($"Fields 데이터에 {transform.Name} 엔트리가 없습니다.");
                }

                var costResources = BuildResourceAmounts(transform.CostResources);
                var requirements = BuildFieldRequirements(transform.Requires);
                var tasks = BuildTasks(context.TasksAsset, transform.Name);

                var definition = new FieldDefinition(
                    fieldType,
                    fieldData.Empty,
                    transform.Size,
                    transform.Slot,
                    transform.CostTicks,
                    costResources,
                    requirements,
                    tasks);

                definitions[fieldType] = definition;
            }

            context.FieldDefinitions = definitions;
            // TODO: gridMaps와 jobs 데이터를 활용한 추가 초기화를 연결할 수 있습니다.
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

        private static IReadOnlyList<TaskDefinition> BuildTasks(Tasks tasksAsset, string fieldName)
        {
            if (tasksAsset?.Values == null)
            {
                return Array.Empty<TaskDefinition>();
            }

            var list = new List<TaskDefinition>();
            foreach (var task in tasksAsset.Values.Where(t => string.Equals(t.Field, fieldName, StringComparison.Ordinal)))
            {
                var jobParse = ParseJob(task.JobName);
                var results = BuildResultAmounts(task.Result);
                var outcome = BuildOutcome(task.Result);

                list.Add(new TaskDefinition(
                    task.Name,
                    jobParse.Job,
                    jobParse.AllowsAny,
                    task.Tick,
                    Array.Empty<ResourceAmount>(),
                    results,
                    outcome));
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

        private static IReadOnlyList<ResourceAmount> BuildResultAmounts(string result)
        {
            if (string.IsNullOrWhiteSpace(result))
            {
                return Array.Empty<ResourceAmount>();
            }

            if (!TryParseResourceType(result, out var resourceType))
            {
                return Array.Empty<ResourceAmount>();
            }

            return new[] { new ResourceAmount(resourceType, 1) };
        }

        private static TaskOutcome BuildOutcome(string result)
        {
            if (string.IsNullOrWhiteSpace(result))
            {
                return TaskOutcome.None();
            }

            if (TryParseResourceType(result, out var resourceType))
            {
                return TaskOutcome.ForResource(new ResourceAmount(resourceType, 1));
            }

            if (TryParseFieldType(result, out var fieldType))
            {
                return TaskOutcome.ForField(fieldType);
            }

            throw new InvalidOperationException($"Task 결과 변환 실패: {result}");
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

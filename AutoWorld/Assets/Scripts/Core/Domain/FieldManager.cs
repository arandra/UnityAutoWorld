using System;
using System.Collections.Generic;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 필드 상태를 생성하고 조회하는 매니저다.
    /// </summary>
    public sealed class FieldManager
    {
        private readonly Dictionary<FieldType, FieldDefinition> definitions;
        private readonly List<FieldState> fields = new List<FieldState>();

        public FieldManager(IReadOnlyDictionary<FieldType, FieldDefinition> definitions)
        {
            this.definitions = new Dictionary<FieldType, FieldDefinition>(definitions ?? throw new ArgumentNullException(nameof(definitions)));
        }

        public IReadOnlyList<FieldState> Fields => fields;

        public FieldState CreateField(FieldType type)
        {
            if (!definitions.TryGetValue(type, out var definition))
            {
                throw new InvalidOperationException($"정의되지 않은 필드 타입입니다: {type}");
            }

            var field = new FieldState(definition);
            fields.Add(field);
            return field;
        }
    }
}

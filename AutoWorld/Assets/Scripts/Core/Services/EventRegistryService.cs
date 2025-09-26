using System;
using System.Collections.Generic;

namespace AutoWorld.Core.Services
{
    /// <summary>
    /// EventObjectRegistry에 대한 안전한 등록과 ID 발급을 제공한다.
    /// </summary>
    public sealed class EventRegistryService
    {
        private readonly EventObjectRegistry registry = new EventObjectRegistry();
        private readonly Dictionary<EventObjectType, int> counters = new Dictionary<EventObjectType, int>();

        public EventObject CreateIdentifier(EventObjectType type)
        {
            if (type == EventObjectType.None)
            {
                throw new ArgumentException("유효한 이벤트 타입이 필요합니다.", nameof(type));
            }

            var next = counters.TryGetValue(type, out var value) ? value + 1 : 1;
            counters[type] = next;
            return new EventObject(type, next);
        }

        public void Register(IEventParticipant participant)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            registry.Register(participant);
        }

        public bool Unregister(EventObject identifier)
        {
            return registry.Unregister(identifier);
        }

        public bool TryGet(EventObject identifier, out IEventParticipant participant)
        {
            return registry.TryGet(identifier, out participant);
        }

        public IReadOnlyCollection<IEventParticipant> GetParticipants(EventObjectType type)
        {
            return registry.GetParticipants(type);
        }
    }
}

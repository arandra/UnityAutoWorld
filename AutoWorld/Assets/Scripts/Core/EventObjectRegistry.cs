using System;
using System.Collections.Generic;

namespace AutoWorld.Core
{
    public sealed class EventObjectRegistry
    {
        private readonly Dictionary<EventObjectType, Dictionary<int, IEventParticipant>> participants = new();

        public void Register(IEventParticipant participant)
        {
            if (participant == null)
            {
                throw new ArgumentNullException(nameof(participant));
            }

            var identifier = participant.EventObject;
            if (identifier.Type == EventObjectType.None)
            {
                throw new ArgumentException("유효한 이벤트 객체 타입이 필요합니다.", nameof(participant));
            }

            if (!participants.TryGetValue(identifier.Type, out var collection))
            {
                collection = new Dictionary<int, IEventParticipant>();
                participants[identifier.Type] = collection;
            }

            collection[identifier.Id] = participant;
        }

        public bool Unregister(EventObject identifier)
        {
            if (!participants.TryGetValue(identifier.Type, out var collection))
            {
                return false;
            }

            if (!collection.Remove(identifier.Id))
            {
                return false;
            }

            if (collection.Count == 0)
            {
                participants.Remove(identifier.Type);
            }

            return true;
        }

        public bool TryGet(EventObject identifier, out IEventParticipant participant)
        {
            if (participants.TryGetValue(identifier.Type, out var collection) && collection.TryGetValue(identifier.Id, out participant))
            {
                return true;
            }

            participant = default!;
            return false;
        }

        public bool Contains(EventObject identifier)
        {
            return participants.TryGetValue(identifier.Type, out var collection) && collection.ContainsKey(identifier.Id);
        }

        public IReadOnlyCollection<IEventParticipant> GetParticipants(EventObjectType type)
        {
            if (participants.TryGetValue(type, out var collection))
            {
                return collection.Values;
            }

            return Array.Empty<IEventParticipant>();
        }
    }
}

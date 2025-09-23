using System;
using System.Collections.Generic;

namespace AutoWorld.Core
{
    public sealed class EventManager
    {
        private static readonly Lazy<EventManager> sharedInstance = new(() => new EventManager());

        private readonly Dictionary<EventType, HashSet<EventObject>> registrations = new();

        private EventManager()
        {
            Registry = new EventObjectRegistry();
        }

        public static EventManager Instance => sharedInstance.Value;

        public EventObjectRegistry Registry { get; }

        public void RegisterParticipant(IEventParticipant participant)
        {
            Registry.Register(participant);
        }

        public bool UnregisterParticipant(EventObject identifier)
        {
            RemoveFromRegistrations(identifier);
            return Registry.Unregister(identifier);
        }

        public void Register(EventType eventType, EventObject registeredObject)
        {
            if (eventType == EventType.None)
            {
                throw new ArgumentException("유효한 이벤트 타입이 필요합니다.", nameof(eventType));
            }

            if (!Registry.Contains(registeredObject))
            {
                throw new InvalidOperationException($"이벤트 객체가 레지스트리에 존재하지 않습니다: {registeredObject}");
            }

            if (!registrations.TryGetValue(eventType, out var collection))
            {
                collection = new HashSet<EventObject>();
                registrations[eventType] = collection;
            }

            collection.Add(registeredObject);
        }

        public bool Unregister(EventType eventType, EventObject registeredObject)
        {
            if (!registrations.TryGetValue(eventType, out var collection))
            {
                return false;
            }

            var removed = collection.Remove(registeredObject);
            if (collection.Count == 0)
            {
                registrations.Remove(eventType);
            }

            return removed;
        }

        public void Invoke(EventType eventType, EventObject source, EventParameter parameter)
        {
            if (!registrations.TryGetValue(eventType, out var collection))
            {
                return;
            }

            var targets = new List<EventObject>(collection);
            foreach (var target in targets)
            {
                if (parameter.Target.HasValue && target != parameter.Target.Value)
                {
                    continue;
                }

                if (parameter.TargetTypes != EventObjectType.None && (parameter.TargetTypes & target.Type) == 0)
                {
                    continue;
                }

                if (!Registry.TryGet(target, out var participant))
                {
                    continue;
                }

                if (participant is IEventListener listener)
                {
                    listener.OnEvent(eventType, source, parameter);
                }
            }
        }

        private void RemoveFromRegistrations(EventObject identifier)
        {
            var emptyEventTypes = new List<EventType>();
            foreach (var pair in registrations)
            {
                if (pair.Value.Remove(identifier) && pair.Value.Count == 0)
                {
                    emptyEventTypes.Add(pair.Key);
                }
            }

            foreach (var eventType in emptyEventTypes)
            {
                registrations.Remove(eventType);
            }
        }
    }
}

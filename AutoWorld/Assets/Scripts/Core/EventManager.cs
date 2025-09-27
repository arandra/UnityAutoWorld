using System;
using System.Collections.Generic;

namespace AutoWorld.Core
{
    public sealed class EventManager
    {
        private static readonly Lazy<EventManager> sharedInstance = new Lazy<EventManager>(() => new EventManager());

        private readonly Dictionary<EventType, HashSet<IEventListener>> listenersByEvent = new Dictionary<EventType, HashSet<IEventListener>>();
        private readonly HashSet<IEventListener> globalListeners = new HashSet<IEventListener>();

        public static EventManager Instance => sharedInstance.Value;

        public void Register(EventType eventType, IEventListener listener)
        {
            if (eventType == EventType.None)
            {
                throw new ArgumentException("유효한 이벤트 타입이 필요합니다.", nameof(eventType));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (!listenersByEvent.TryGetValue(eventType, out var collection))
            {
                collection = new HashSet<IEventListener>();
                listenersByEvent[eventType] = collection;
            }

            collection.Add(listener);
        }

        public void RegisterAll(IEventListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            globalListeners.Add(listener);
        }

        public bool Unregister(EventType eventType, IEventListener listener)
        {
            if (listener == null)
            {
                return false;
            }

            if (!listenersByEvent.TryGetValue(eventType, out var collection))
            {
                return false;
            }

            var removed = collection.Remove(listener);
            if (removed && collection.Count == 0)
            {
                listenersByEvent.Remove(eventType);
            }

            return removed;
        }

        public bool UnregisterAll(IEventListener listener)
        {
            return globalListeners.Remove(listener);
        }

        public void Unregister(IEventListener listener)
        {
            if (listener == null)
            {
                return;
            }

            globalListeners.Remove(listener);

            var emptyKeys = new List<EventType>();
            foreach (var pair in listenersByEvent)
            {
                pair.Value.Remove(listener);
                if (pair.Value.Count == 0)
                {
                    emptyKeys.Add(pair.Key);
                }
            }

            for (var i = 0; i < emptyKeys.Count; i++)
            {
                listenersByEvent.Remove(emptyKeys[i]);
            }
        }

        public void Invoke(EventType eventType, EventObject source, EventParameter parameter)
        {
            var dispatchSet = new HashSet<IEventListener>();

            if (listenersByEvent.TryGetValue(eventType, out var specificListeners))
            {
                foreach (var listener in specificListeners)
                {
                    dispatchSet.Add(listener);
                }
            }

            foreach (var listener in globalListeners)
            {
                dispatchSet.Add(listener);
            }

            foreach (var listener in dispatchSet)
            {
                listener?.OnEvent(eventType, source, parameter);
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace AutoWorld.Core
{
    public sealed class EventManager : IEventManager
    {
        private static readonly Lazy<EventManager> sharedInstance = new Lazy<EventManager>(() => new EventManager());

        private readonly Dictionary<string, HashSet<IEventListener>> listenersByEvent = new Dictionary<string, HashSet<IEventListener>>(StringComparer.Ordinal);
        private readonly HashSet<IEventListener> globalListeners = new HashSet<IEventListener>();

        public static IEventManager Instance => sharedInstance.Value;
        
        private IDebugLog DebugLog { get; set; } 
        public void SetDebugLog(IDebugLog debugLog) => DebugLog = debugLog;

        public void Register(string eventName, IEventListener listener)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("이벤트 이름이 필요합니다.", nameof(eventName));
            }

            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            var key = eventName.Trim();
            if (!listenersByEvent.TryGetValue(key, out var collection))
            {
                collection = new HashSet<IEventListener>();
                listenersByEvent[key] = collection;
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

        public bool Unregister(string eventName, IEventListener listener)
        {
            if (string.IsNullOrWhiteSpace(eventName) || listener == null)
            {
                return false;
            }

            var key = eventName.Trim();
            if (!listenersByEvent.TryGetValue(key, out var collection))
            {
                return false;
            }

            var removed = collection.Remove(listener);
            if (removed && collection.Count == 0)
            {
                listenersByEvent.Remove(key);
            }

            return removed;
        }

        public bool UnregisterAll(IEventListener listener)
        {
            return listener != null && globalListeners.Remove(listener);
        }

        public void Unregister(IEventListener listener)
        {
            if (listener == null)
            {
                return;
            }

            globalListeners.Remove(listener);

            var emptyKeys = new List<string>();
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

        public void Invoke(string eventName, EventObject source, EventParameter parameter)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("이벤트 이름이 필요합니다.", nameof(eventName));
            }
            else
            {
                DebugLog?.Log($"[EventManager.invoke:{eventName}] source:({source.ToString()}),  parameter:({parameter.ToString()})");
            }

            var key = eventName.Trim();
            var dispatchSet = new HashSet<IEventListener>();

            if (listenersByEvent.TryGetValue(key, out var specificListeners))
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
                listener?.OnEvent(key, source, parameter);
            }
        }
    }
}

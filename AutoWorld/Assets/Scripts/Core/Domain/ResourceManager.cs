using System;
using System.Collections.Generic;
using AutoWorld.Core.Data;
using AutoWorld.Core.Services;

namespace AutoWorld.Core.Domain
{
    public sealed class ResourceManager : ITickListener, IEventListener
    {
        private readonly ResourceStore store;
        private readonly EventRegistryService registry;
        private readonly Dictionary<string, List<EventAction>> actionsByEvent = new Dictionary<string, List<EventAction>>(StringComparer.Ordinal);
        private readonly Dictionary<ResourceType, int> deferredAdditions = new Dictionary<ResourceType, int>();
        private readonly HashSet<string> registeredEvents = new HashSet<string>(StringComparer.Ordinal);

        public ResourceManager(ResourceStore store, EventRegistryService registry)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public void ConfigureEventActions(IEnumerable<EventAction> actions)
        {
            if (actions == null)
            {
                return;
            }

            foreach (var action in actions)
            {
                if (action == null)
                {
                    continue;
                }

                if (!string.Equals(action.EventListener, nameof(ResourceManager), StringComparison.Ordinal))
                {
                    continue;
                }

                RegisterAction(action);
            }
        }

        public int GetAmount(ResourceType type)
        {
            return store.GetAmount(type);
        }

        public bool TryConsume(ResourceType type, int amount)
        {
            return store.TryConsume(type, amount);
        }

        public bool HasSufficientResources(IReadOnlyCollection<ResourceAmount> costs)
        {
            if (costs == null)
            {
                return true;
            }

            foreach (var cost in costs)
            {
                if (GetAmount(cost.Type) < cost.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        public void ConsumeResources(IReadOnlyCollection<ResourceAmount> costs)
        {
            if (costs == null)
            {
                return;
            }

            foreach (var cost in costs)
            {
                if (!TryConsume(cost.Type, cost.Amount))
                {
                    throw new InvalidOperationException($"자원 차감 실패: {cost.Type} {cost.Amount}");
                }
            }
        }

        public void AddResource(ResourceType type, int amount)
        {
            store.Add(type, amount);
        }

        public void OnTick(TickContext context)
        {
            FlushDeferred();
        }

        public void OnEvent(string eventName, EventObject source, EventParameter parameter)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            if (!actionsByEvent.TryGetValue(eventName, out var actions))
            {
                return;
            }

            for (var i = 0; i < actions.Count; i++)
            {
                ExecuteAction(actions[i], parameter);
            }
        }

        private void RegisterAction(EventAction action)
        {
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

        private void ExecuteAction(EventAction action, EventParameter parameter)
        {
            if (!string.Equals(action.ActionName, "AddResource", StringComparison.Ordinal))
            {
                return;
            }

            if (action.PairParameters == null)
            {
                return;
            }

            foreach (var pair in action.PairParameters)
            {
                if (!TryParseResourceType(pair.Key, out var resourceType))
                {
                    continue;
                }

                ApplyResourceDelta(resourceType, pair.Value, action.ActionImmediately);
            }
        }

        private void ApplyResourceDelta(ResourceType type, int amount, bool immediate)
        {
            if (immediate)
            {
                store.Add(type, amount);
                return;
            }

            if (deferredAdditions.TryGetValue(type, out var current))
            {
                deferredAdditions[type] = current + amount;
            }
            else
            {
                deferredAdditions[type] = amount;
            }
        }

        private void FlushDeferred()
        {
            if (deferredAdditions.Count == 0)
            {
                return;
            }

            foreach (var pair in deferredAdditions)
            {
                if (pair.Value != 0)
                {
                    store.Add(pair.Key, pair.Value);
                }
            }

            deferredAdditions.Clear();
        }

        private static bool TryParseResourceType(string name, out ResourceType type)
        {
            return Enum.TryParse(name, false, out type);
        }
    }
}

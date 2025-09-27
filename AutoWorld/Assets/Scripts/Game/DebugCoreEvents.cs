using AutoWorld.Core;
using UnityEngine;
using EventType = AutoWorld.Core.EventType;

namespace AutoWorld.Game
{
    public sealed class DebugCoreEvents : MonoBehaviour, IEventListener
    {
        private static readonly EventType[] ObservedEvents =
        {
            EventType.CitizenFoodConsumed,
            EventType.CitizenFoodShortage,
            EventType.SoldierLevelUpgraded
        };

        private void OnEnable()
        {
            foreach (var eventType in ObservedEvents)
            {
                EventManager.Instance.Register(eventType, this);
            }
        }

        private void OnDisable()
        {
            foreach (var eventType in ObservedEvents)
            {
                EventManager.Instance.Unregister(eventType, this);
            }
        }

        public void OnEvent(EventType eventType, EventObject source, EventParameter parameter)
        {
            switch (eventType)
            {
                case EventType.CitizenFoodConsumed:
                    Debug.Log($"Food consumed by citizen {parameter.IntValue}");
                    break;
                case EventType.CitizenFoodShortage:
                    Debug.LogWarning($"Food shortage for citizen {parameter.IntValue}");
                    break;
                case EventType.SoldierLevelUpgraded:
                    Debug.Log($"Soldier {parameter.Target?.Id ?? parameter.IntValue} level up to {parameter.IntValue}");
                    break;
            }
        }
    }
}

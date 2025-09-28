using AutoWorld.Core;
using UnityEngine;

namespace AutoWorld.Game
{
    public sealed class DebugCoreEvents : MonoBehaviour, IEventListener
    {
        private static readonly string[] ObservedEvents =
        {
            GameEvents.CitizenFoodConsumed,
            GameEvents.CitizenFoodShortage,
            GameEvents.SoldierLevelUpgraded
        };

        private void OnEnable()
        {
            foreach (var eventName in ObservedEvents)
            {
                EventManager.Instance.Register(eventName, this);
            }
        }

        private void OnDisable()
        {
            foreach (var eventName in ObservedEvents)
            {
                EventManager.Instance.Unregister(eventName, this);
            }
        }

        public void OnEvent(string eventName, EventObject source, EventParameter parameter)
        {
            switch (eventName)
            {
                case GameEvents.CitizenFoodConsumed:
                    Debug.Log($"Food consumed by citizen {GetCitizenId(parameter)}");
                    break;
                case GameEvents.CitizenFoodShortage:
                    Debug.LogWarning($"Food shortage for citizen {GetCitizenId(parameter)}");
                    break;
                case GameEvents.SoldierLevelUpgraded:
                    Debug.Log($"Soldier {GetCitizenId(parameter)} level up to {parameter.IntValue}");
                    break;
            }
        }

        private static int GetCitizenId(EventParameter parameter)
        {
            return parameter.CustomObject is int citizenId && citizenId > 0 ? citizenId : 0;
        }
    }
}

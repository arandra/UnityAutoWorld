using AutoWorld.Core;
using UnityEngine;

namespace AutoWorld.Game
{
    public sealed class DebugCoreEvents : MonoBehaviour, ICoreEvents
    {
        public void OnFoodConsumed(int citizenId)
        {
            Debug.Log($"Food consumed by citizen {citizenId}");
        }

        public void OnFoodShortage(int citizenId)
        {
            Debug.LogWarning($"Food shortage for citizen {citizenId}");
        }

        public void OnSoldierLevelUp(int citizenId, int level)
        {
            Debug.Log($"Soldier {citizenId} level up to {level}");
        }
    }
}

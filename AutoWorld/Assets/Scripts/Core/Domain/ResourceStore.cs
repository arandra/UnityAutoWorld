using System;
using System.Collections.Generic;
using AutoWorld.Core;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 전역 자원 수량을 관리한다.
    /// </summary>
    public sealed class ResourceStore
    {
        private readonly Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();

        public int GetAmount(ResourceType type)
        {
            return resources.TryGetValue(type, out var amount) ? amount : 0;
        }

        public void Add(ResourceType type, int amount)
        {
            if (amount == 0)
            {
                return;
            }

            if (!resources.TryGetValue(type, out var current))
            {
                resources[type] = amount;
            }
            else
            {
                resources[type] = current + amount;
            }
        }

        public bool TryConsume(ResourceType type, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "소비량은 양수여야 합니다.");
            }

            var current = GetAmount(type);
            if (current < amount)
            {
                return false;
            }

            resources[type] = current - amount;
            return true;
        }
    }
}

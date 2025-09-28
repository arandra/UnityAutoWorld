using System;

namespace AutoWorld.Core
{
    public static class EventObjectExtensions
    {
        public static void RaiseEvent(this EventObject source, string eventName, EventParameter parameter = default)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("이벤트 이름이 필요합니다.", nameof(eventName));
            }

            EventManager.Instance.Invoke(eventName.Trim(), source, parameter);
        }
    }
}

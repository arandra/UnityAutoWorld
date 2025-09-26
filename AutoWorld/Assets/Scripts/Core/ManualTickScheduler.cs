using System;
using System.Collections.Generic;

namespace AutoWorld.Core
{
    /// <summary>
    /// 수동으로 틱을 진행하는 스케줄러 구현이다.
    /// </summary>
    public sealed class ManualTickScheduler : ITickScheduler
    {
        private readonly List<ITickListener> listeners = new List<ITickListener>();
        private long tickIndex;
        private double tickDurationMillis;

        public ManualTickScheduler(double initialTickDurationMillis)
        {
            ApplyTickDuration(initialTickDurationMillis);
        }

        public double TickDurationMillis => tickDurationMillis;

        public void SetTickDuration(double durationMillis)
        {
            ApplyTickDuration(durationMillis);
        }

        public void Register(ITickListener listener)
        {
            if (listener == null)
            {
                throw new ArgumentNullException(nameof(listener));
            }

            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }

        public void Unregister(ITickListener listener)
        {
            if (listener == null)
            {
                return;
            }

            listeners.Remove(listener);
        }

        public TickContext AdvanceTick(double? overrideDurationMillis = null)
        {
            var durationMillis = overrideDurationMillis ?? tickDurationMillis;
            if (durationMillis < 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(overrideDurationMillis), "틱 지속시간은 1ms 이상이어야 합니다.");
            }

            var deltaSeconds = durationMillis / 1000d;
            var context = new TickContext(++tickIndex, deltaSeconds);

            for (var i = 0; i < listeners.Count; i++)
            {
                listeners[i].OnTick(context);
            }

            return context;
        }

        private void ApplyTickDuration(double durationMillis)
        {
            if (durationMillis < 1d)
            {
                throw new ArgumentOutOfRangeException(nameof(durationMillis), "틱 지속시간은 1ms 이상이어야 합니다.");
            }

            tickDurationMillis = durationMillis;
            TickConfig.TickDurationMillis = durationMillis;
        }
    }
}

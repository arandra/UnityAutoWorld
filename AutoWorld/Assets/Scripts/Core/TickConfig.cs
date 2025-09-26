using System;

namespace AutoWorld.Core
{
    /// <summary>
    /// 글로벌 tick 운영 설정을 보관한다.
    /// </summary>
    public static class TickConfig
    {
        private const double MinTickDurationMillis = 1d;
        private static double tickDurationMillis = 100d;

        /// <summary>
        /// 현재 tick 한 주기의 길이를 밀리초 단위로 제공한다.
        /// </summary>
        public static double TickDurationMillis
        {
            get => tickDurationMillis;
            set
            {
                if (value < MinTickDurationMillis)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "틱 지속시간은 1ms 이상이어야 합니다.");
                }

                tickDurationMillis = value;
            }
        }
    }
}

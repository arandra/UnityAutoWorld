namespace AutoWorld.Core
{
    /// <summary>
    /// tick 루프에 참여하는 요소를 관리하는 매니저 계약이다.
    /// </summary>
    public interface ITickScheduler
    {
        double TickDurationMillis { get; }

        /// <summary>
        /// 런타임 동안 tick 간격을 변경한다.
        /// </summary>
        /// <param name="durationMillis">밀리초 단위 간격</param>
        void SetTickDuration(double durationMillis);

        void Register(ITickListener listener);

        void Unregister(ITickListener listener);
    }
}

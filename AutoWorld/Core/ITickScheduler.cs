namespace AutoWorld.Core
{
    /// <summary>
    /// tick 루프에 참여하는 요소를 관리하는 매니저 계약이다.
    /// </summary>
    public interface ITickScheduler
    {
        double TickDurationMillis { get; }

        void Register(ITickListener listener);

        void Unregister(ITickListener listener);
    }
}

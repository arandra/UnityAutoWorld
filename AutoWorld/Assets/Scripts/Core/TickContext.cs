namespace AutoWorld.Core
{
    /// <summary>
    /// 게임 내부 tick 업데이트 정보를 제공한다.
    /// </summary>
    public struct TickContext
    {
        public TickContext(long tickIndex, double deltaSeconds)
        {
            TickIndex = tickIndex;
            DeltaSeconds = deltaSeconds;
        }

        public long TickIndex { get; }

        public double DeltaSeconds { get; }
    }
}

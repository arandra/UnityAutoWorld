namespace AutoWorld.Core
{
    /// <summary>
    /// 게임 내부 tick 업데이트 정보를 제공한다.
    /// </summary>
    public readonly record struct TickContext(long TickIndex, double DeltaSeconds);
}

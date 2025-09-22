namespace AutoWorld.Core
{
    /// <summary>
    /// tick 업데이트를 전달받는 구성 요소를 위한 계약이다.
    /// </summary>
    public interface ITickListener
    {
        void OnTick(TickContext context);
    }
}

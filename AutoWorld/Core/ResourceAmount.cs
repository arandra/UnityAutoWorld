namespace AutoWorld.Core
{
    /// <summary>
    /// 자원 변화량을 표현한다.
    /// </summary>
    public readonly record struct ResourceAmount(ResourceType Type, int Amount)
    {
        public bool IsConsumable => Amount < 0;

        public bool IsProduction => Amount > 0;
    }
}

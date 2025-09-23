namespace AutoWorld.Core
{
    /// <summary>
    /// 자원 변화량을 표현한다.
    /// </summary>
    public struct ResourceAmount
    {
        public ResourceAmount(ResourceType type, int amount)
        {
            Type = type;
            Amount = amount;
        }

        public ResourceType Type { get; }

        public int Amount { get; }

        public bool IsConsumable
        {
            get { return Amount < 0; }
        }

        public bool IsProduction
        {
            get { return Amount > 0; }
        }
    }
}

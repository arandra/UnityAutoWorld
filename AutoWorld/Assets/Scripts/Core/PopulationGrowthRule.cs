namespace AutoWorld.Core
{
    /// <summary>
    /// 인구 성장 규칙을 정의한다.
    /// </summary>
    public struct PopulationGrowthRule
    {
        public PopulationGrowthRule(int requiredPoints, int growthPerTick)
        {
            RequiredPoints = requiredPoints;
            GrowthPerTick = growthPerTick;
        }

        public int RequiredPoints { get; }

        public int GrowthPerTick { get; }
    }
}

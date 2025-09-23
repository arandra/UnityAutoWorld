namespace AutoWorld.Core
{
    /// <summary>
    /// 인구 성장 규칙을 정의한다.
    /// </summary>
    public readonly record struct PopulationGrowthRule(int RequiredPoints, int GrowthPerTick);
}

namespace AutoWorld.Core
{
    public enum EventType
    {
        None = 0,
        TerritoryExpansion,
        PopulationGrowth,
        JobAssignmentChanged,
        BuildingCompleted,
        PopulationGrowthPaused,
        PopulationGrowthResumed,
        JobChangeFailed,
        FieldTransformationStarted,
        FieldTransformationCompleted,
        FieldTransformationFailed,
        TaskStarted,
        TaskCompleted,
        TaskInterrupted,
        RestStarted,
        RestCompleted,
        CitizenDied,
        TerritoryExpansionFailed
    }
}

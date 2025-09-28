namespace AutoWorld.Core
{
    public static class GameEvents
    {
        public const string TerritoryExpansion = nameof(TerritoryExpansion);
        public const string PopulationGrowth = nameof(PopulationGrowth);
        public const string JobAssignmentChanged = nameof(JobAssignmentChanged);
        public const string BuildingCompleted = nameof(BuildingCompleted);
        public const string PopulationGrowthPaused = nameof(PopulationGrowthPaused);
        public const string PopulationGrowthResumed = nameof(PopulationGrowthResumed);
        public const string CitizenFoodConsumed = nameof(CitizenFoodConsumed);
        public const string CitizenFoodShortage = nameof(CitizenFoodShortage);
        public const string JobChangeFailed = nameof(JobChangeFailed);
        public const string FieldTransformationStarted = nameof(FieldTransformationStarted);
        public const string FieldTransformationCompleted = nameof(FieldTransformationCompleted);
        public const string FieldTransformationFailed = nameof(FieldTransformationFailed);
        public const string TaskStarted = nameof(TaskStarted);
        public const string TaskCompleted = nameof(TaskCompleted);
        public const string TaskInterrupted = nameof(TaskInterrupted);
        public const string RestStarted = nameof(RestStarted);
        public const string RestCompleted = nameof(RestCompleted);
        public const string CitizenDied = nameof(CitizenDied);
        public const string SoldierLevelUpgraded = nameof(SoldierLevelUpgraded);
        public const string TerritoryExpansionFailed = nameof(TerritoryExpansionFailed);
        public const string MissingMeal = nameof(MissingMeal);
        public const string OverflowingTicksForRest = nameof(OverflowingTicksForRest);
        public const string RestingTaskComplete = nameof(RestingTaskComplete);
        public const string OccupyingTaskComplete = nameof(OccupyingTaskComplete);
        public const string HarvestingTaskComplete = nameof(HarvestingTaskComplete);
        public const string WoodCuttingTaskComplete = nameof(WoodCuttingTaskComplete);
        public const string QuarryingTaskComplete = nameof(QuarryingTaskComplete);
        public const string MakingWeaponTaskComplete = nameof(MakingWeaponTaskComplete);
        public const string MakingArmorTaskComplete = nameof(MakingArmorTaskComplete);
    }
}

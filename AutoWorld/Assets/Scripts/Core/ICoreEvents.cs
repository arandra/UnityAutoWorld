namespace AutoWorld.Core
{
    public interface ICoreEvents
    {
        void OnFoodConsumed(int citizenId);

        void OnFoodShortage(int citizenId);

        void OnSoldierLevelUp(int citizenId, int level);
    }
}

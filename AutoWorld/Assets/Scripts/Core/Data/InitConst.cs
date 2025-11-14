namespace AutoWorld.Core.Data
{
    using System;
    using System.Collections.Generic;
    using SerializableTypes;

    [Serializable]
    public class InitConst
    {
        public int WorkerTicks = default;
        public int DestroyTicks = default;
        public int FoodConsumeTicks = default;
        public int SoldierUpgradeTicks = default;
        public List<string> InitJobs = new List<string>();
        public int InitBadLandSize = default;
        public List<string> InitFields = new List<string>();
        public int InitFood = default;
        public int MillisecondPerTick = default;
        public int MaxSoldierLevel = default;
        public int TicksForRest = default;
    }

}

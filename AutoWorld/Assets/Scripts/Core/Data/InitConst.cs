using System;
using System.Collections.Generic;

namespace AutoWorld.Core.Data
{
    [Serializable]
    public class InitConst
    {
        public int WorkerTicks;
        public int DestroyTicks;
        public int FoodConsumeTicks;
        public int SoldierUpgradeTicks;
        public List<string> InitJobs = new();
        public int InitBadLandSize;
        public List<string> InitFields = new();
        public int InitFood;
        public int MillisecondPerTick;
        public int MaxSoldierLevel;
        public int TicksForRest;
    }
}


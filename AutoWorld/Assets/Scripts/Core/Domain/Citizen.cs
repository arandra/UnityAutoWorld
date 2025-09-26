using AutoWorld.Core;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 주민 하나의 상태를 표현한다.
    /// </summary>
    public sealed class Citizen
    {
        public Citizen(int identifier, JobType job)
        {
            Identifier = identifier;
            Job = job;
            Level = job == JobType.Soldier ? 1 : 0;
        }

        public int Identifier { get; }

        public JobType Job { get; private set; }

        public int Level { get; private set; }

        public int TicksUntilFoodConsume { get; set; }

        public int TicksUntilSoldierUpgrade { get; set; }

        public bool TryPromoteTo(JobType job)
        {
            if (Job == job)
            {
                return false;
            }

            Job = job;
            if (job == JobType.Soldier)
            {
                Level = 1;
                TicksUntilSoldierUpgrade = 0;
            }
            else
            {
                Level = 0;
            }

            return true;
        }

        public void IncreaseSoldierLevel(int maxLevel)
        {
            if (Job != JobType.Soldier)
            {
                return;
            }

            if (Level < maxLevel)
            {
                Level += 1;
            }
        }
    }
}

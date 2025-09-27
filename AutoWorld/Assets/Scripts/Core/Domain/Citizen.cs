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
            IsAlive = true;
            Activity = CitizenActivity.Idle;
        }

        public int Identifier { get; }

        public JobType Job { get; private set; }

        public int Level { get; private set; }

        public int TicksUntilFoodConsume { get; set; }

        public int TicksUntilSoldierUpgrade { get; set; }

        public int AwakenTicks { get; set; }

        public bool NeedsRest { get; set; }

        public bool IsAlive { get; private set; }

        public CitizenActivity Activity { get; private set; }

        public FieldState AssignedField { get; private set; }

        public TaskDefinition AssignedTask { get; private set; }

        public bool IsIdle => Activity == CitizenActivity.Idle;

        public bool IsWorking => Activity == CitizenActivity.Working || Activity == CitizenActivity.Transforming;

        public void AssignWork(FieldState field, TaskDefinition task)
        {
            AssignedField = field;
            AssignedTask = task;
            Activity = CitizenActivity.Working;
        }

        public void AssignRest(FieldState field, TaskDefinition task)
        {
            AssignedField = field;
            AssignedTask = task;
            Activity = CitizenActivity.Resting;
        }

        public void AssignTransformation(FieldState field)
        {
            AssignedField = field;
            AssignedTask = null;
            Activity = CitizenActivity.Transforming;
        }

        public void ClearAssignment()
        {
            AssignedField = null;
            AssignedTask = null;
            Activity = CitizenActivity.Idle;
        }

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

        public void Kill()
        {
            IsAlive = false;
            ClearAssignment();
        }
    }
}

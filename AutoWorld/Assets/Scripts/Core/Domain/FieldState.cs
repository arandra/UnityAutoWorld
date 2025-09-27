using System;
using System.Collections.Generic;
using AutoWorld.Core;

namespace AutoWorld.Core.Domain
{
    /// <summary>
    /// 필드 단위 상태와 작업 진행도를 보관한다.
    /// </summary>
    public sealed class FieldState
    {
        public FieldState(FieldDefinition definition, IEnumerable<FieldCoordinate> coordinates)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            taskRotation = new Queue<TaskDefinition>(definition.Tasks);
            taskProgress = new Dictionary<TaskDefinition, int>();
            Coordinates = new List<FieldCoordinate>(coordinates ?? Array.Empty<FieldCoordinate>());
            if (Coordinates.Count > 0)
            {
                Root = Coordinates[0];
            }

            foreach (var task in definition.Tasks)
            {
                taskProgress[task] = 0;
            }
        }

        private readonly Queue<TaskDefinition> taskRotation;
        private readonly Dictionary<TaskDefinition, int> taskProgress;
        private readonly Dictionary<TaskDefinition, int> activeAssignments = new Dictionary<TaskDefinition, int>();
        private int occupiedSlots;

        public FieldDefinition Definition { get; private set; }

        public FieldCoordinate Root { get; private set; }

        public List<FieldCoordinate> Coordinates { get; }

        public bool IsEmpty => Definition.IsEmpty;

        public int OccupiedSlots => occupiedSlots;

        public bool HasAvailableSlot => occupiedSlots < Definition.Slot;

        public IReadOnlyCollection<TaskDefinition> Tasks => Definition.Tasks;

        public void SetDefinition(FieldDefinition definition, IEnumerable<FieldCoordinate> coordinates)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));

            taskRotation.Clear();
            taskProgress.Clear();
            activeAssignments.Clear();

            foreach (var task in definition.Tasks)
            {
                taskRotation.Enqueue(task);
                taskProgress[task] = 0;
            }

            occupiedSlots = 0;
            Coordinates.Clear();
            if (coordinates != null)
            {
                Coordinates.AddRange(coordinates);
            }

            Root = Coordinates.Count > 0 ? Coordinates[0] : default;
        }

        public bool TryReserveSlot(JobType job, out TaskDefinition task)
        {
            if (!HasAvailableSlot || taskRotation.Count == 0)
            {
                task = null;
                return false;
            }

            var attempts = taskRotation.Count;
            for (var i = 0; i < attempts; i++)
            {
                var candidate = DequeueNextTask();
                if (!IsTaskAssignable(candidate, job))
                {
                    continue;
                }

                occupiedSlots++;
                IncrementAssignment(candidate);
                task = candidate;
                return true;
            }

            task = null;
            return false;
        }

        public void ReleaseSlot(TaskDefinition task)
        {
            if (occupiedSlots > 0)
            {
                occupiedSlots--;
            }

            if (task != null && activeAssignments.TryGetValue(task, out var count))
            {
                count -= 1;
                if (count <= 0)
                {
                    activeAssignments.Remove(task);
                }
                else
                {
                    activeAssignments[task] = count;
                }
            }
        }

        public int GetProgress(TaskDefinition task)
        {
            return taskProgress.TryGetValue(task, out var value) ? value : 0;
        }

        public void AddProgress(TaskDefinition task, int delta)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (!taskProgress.ContainsKey(task))
            {
                taskProgress[task] = 0;
            }

            taskProgress[task] += delta;
        }

        public void ResetProgress(TaskDefinition task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (taskProgress.ContainsKey(task))
            {
                taskProgress[task] = 0;
            }
        }

        private TaskDefinition DequeueNextTask()
        {
            var task = taskRotation.Dequeue();
            taskRotation.Enqueue(task);
            return task;
        }

        private static bool IsTaskAssignable(TaskDefinition task, JobType job)
        {
            if (task == null)
            {
                return false;
            }

            if (task.AllowsAnyJob)
            {
                return true;
            }

            if (!task.Job.HasValue)
            {
                return false;
            }

            return task.Job.Value == job;
        }

        private void IncrementAssignment(TaskDefinition task)
        {
            if (!activeAssignments.TryGetValue(task, out var count))
            {
                activeAssignments[task] = 1;
            }
            else
            {
                activeAssignments[task] = count + 1;
            }
        }
    }
}

using System;

namespace AutoWorld.Core.Data
{
    [Serializable]
    public class Task
    {
        public string Field = string.Empty;
        public string Name = string.Empty;
        public string JobName = string.Empty;
        public int Tick;
        public string RiseEvent = string.Empty;
    }
}

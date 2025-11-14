namespace AutoWorld.Core.Data
{
    using System;
    using System.Collections.Generic;
    using SerializableTypes;

    [Serializable]
    public class Task
    {
        public string Field = string.Empty;
        public string Name = string.Empty;
        public string JobName = string.Empty;
        public int Tick = default;
        public string RiseEvent = string.Empty;
    }

}

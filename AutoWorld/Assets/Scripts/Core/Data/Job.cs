namespace AutoWorld.Core.Data
{
    using System;
    using System.Collections.Generic;
    using SerializableTypes;

    [Serializable]
    public class Job
    {
        public string Name = string.Empty;
        public List<Pair<string, int>> CostResources = new List<Pair<string, int>>();
    }

}

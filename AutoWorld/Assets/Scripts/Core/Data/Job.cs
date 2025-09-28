using System;
using System.Collections.Generic;

namespace AutoWorld.Core.Data
{
    [Serializable]
    public class Job
    {
        public string Name = string.Empty;
        public List<Pair<string, int>> CostResources = new();
    }
}


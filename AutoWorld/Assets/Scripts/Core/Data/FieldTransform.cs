using System;
using System.Collections.Generic;

namespace AutoWorld.Core.Data
{
    [Serializable]
    public class FieldTransform
    {
        public string Name = string.Empty;
        public int Size;
        public int Slot;
        public int CostTicks;
        public List<Pair<string, int>> CostResources = new();
        public List<string> Requires = new();
    }
}


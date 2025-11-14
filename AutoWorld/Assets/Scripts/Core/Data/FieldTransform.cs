namespace AutoWorld.Core.Data
{
    using System;
    using System.Collections.Generic;
    using SerializableTypes;

    [Serializable]
    public class FieldTransform
    {
        public string Name = string.Empty;
        public int Size = default;
        public int Slot = default;
        public int CostTicks = default;
        public List<Pair<string, int>> CostResources = new List<Pair<string, int>>();
        public List<string> Requires = new List<string>();
    }

}

namespace AutoWorld.Core.Data
{
    using System;
    using System.Collections.Generic;
    using SerializableTypes;

    [Serializable]
    public class EventAction
    {
        public string EventName = string.Empty;
        public string EventListener = string.Empty;
        public string ActionName = string.Empty;
        public bool ActionImmediately = default;
        public List<Pair<string, int>> PairParameters = new List<Pair<string, int>>();
        public string StringParameter = string.Empty;
        public int IntParameter = default;
    }

}

using System;
using System.Collections.Generic;

namespace AutoWorld.Core.Data
{
    [Serializable]
    public class EventAction
    {
        public string EventName = string.Empty;
        public string EventListener = string.Empty;
        public string ActionName = string.Empty;
        public bool ActionImmediately;
        public List<Pair<string, int>> PairParameters = new();
        public string StringParameter = string.Empty;
        public int IntParameter = 0;
    }
}
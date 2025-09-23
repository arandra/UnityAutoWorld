using System;

namespace AutoWorld.Core
{
    [Flags]
    public enum EventObjectType
    {
        None = 0,
        Manager = 1,
        Worker = 2,
        Building = 4,
        Field = 8
    }
}

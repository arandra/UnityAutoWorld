using System;

namespace AutoWorld.Core
{
    [Flags]
    public enum EventObjectType
    {
        None = 0,
        Manager = 1,
        Citizen = 2,
        Field = 4
    }
}

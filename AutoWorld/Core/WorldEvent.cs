using System.Collections.Generic;

namespace AutoWorld.Core
{
    /// <summary>
    /// 월드 이벤트 데이터를 캡슐화한다.
    /// </summary>
    public readonly record struct WorldEvent(WorldEventType Type, string Message, IReadOnlyList<ResourceAmount> Effects);
}

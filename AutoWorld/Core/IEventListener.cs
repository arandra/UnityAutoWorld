namespace AutoWorld.Core
{
    public interface IEventListener : IEventParticipant
    {
        void OnEvent(EventType eventType, EventObject source, EventParameter parameter);
    }
}

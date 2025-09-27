namespace AutoWorld.Core
{
    public interface IEventListener
    {
        void OnEvent(EventType eventType, EventObject source, EventParameter parameter);
    }
}

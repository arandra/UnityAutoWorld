namespace AutoWorld.Core
{
    public interface IEventListener
    {
        void OnEvent(string eventName, EventObject source, EventParameter parameter);
    }
}

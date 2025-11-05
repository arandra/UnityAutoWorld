namespace AutoWorld.Core
{
    public interface IEventManager
    {
        void SetDebugLog(IDebugLog debugLog);

        void Register(string eventName, IEventListener listener);

        void RegisterAll(IEventListener listener);

        bool Unregister(string eventName, IEventListener listener);

        bool UnregisterAll(IEventListener listener);

        void Unregister(IEventListener listener);

        void Invoke(string eventName, EventObject source, EventParameter parameter);
    }
}

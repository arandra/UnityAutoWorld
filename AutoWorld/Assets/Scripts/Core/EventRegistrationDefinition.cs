namespace AutoWorld.Core
{
    public struct EventRegistrationDefinition
    {
        public EventRegistrationDefinition(EventType eventType, EventObject registeredObject)
        {
            EventType = eventType;
            RegisteredObject = registeredObject;
        }

        public EventType EventType { get; }

        public EventObject RegisteredObject { get; }
    }
}

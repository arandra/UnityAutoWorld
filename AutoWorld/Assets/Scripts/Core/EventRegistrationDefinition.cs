namespace AutoWorld.Core
{
    public readonly record struct EventRegistrationDefinition(EventType EventType, EventObject RegisteredObject);
}

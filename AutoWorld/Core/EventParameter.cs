namespace AutoWorld.Core
{
    public readonly record struct EventParameter
    {
        public EventObject? Target { get; init; }

        public EventObjectType TargetTypes { get; init; }

        public int IntValue { get; init; }

        public string StringValue { get; init; } = string.Empty;

        public object? CustomObject { get; init; }
    }
}

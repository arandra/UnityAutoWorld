namespace AutoWorld.Core
{
    public struct EventParameter
    {
        private string stringValue;

        public EventObject? Target { get; set; }

        public EventObjectType TargetTypes { get; set; }

        public int IntValue { get; set; }

        public string StringValue
        {
            get => stringValue ?? string.Empty;
            set => stringValue = value ?? string.Empty;
        }

        public object CustomObject { get; set; }
    }
}

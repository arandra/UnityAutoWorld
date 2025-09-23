using System;
using System.Collections.Generic;

namespace AutoWorld.Core
{
    public static class EventRegistrationApplier
    {
        public static void Apply(EventManager manager, IEnumerable<EventRegistrationDefinition> definitions)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            foreach (var definition in definitions)
            {
                manager.Register(definition.EventType, definition.RegisteredObject);
            }
        }
    }
}

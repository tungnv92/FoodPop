using System;
using System.Collections.Generic;

namespace SharedModules.ED
{
    public class EventDispatcher
    {
        static Dictionary<EventId, Action<object>> eventListeners = new();

        public static void RegisterListener(EventId eventId, Action<object> callback)
        {
            if (!eventListeners.ContainsKey(eventId))
            {
                eventListeners.Add(eventId, null);
            }

            eventListeners[eventId] += callback;
        }

        public static void UnregisterListener(EventId eventId, Action<object> callback)
        {
            if (eventListeners.ContainsKey(eventId))
            {
                eventListeners[eventId] -= callback;
            }
        }

        public static void DispatchEvent(EventId eventId, object parameter = null)
        {
            if (!eventListeners.ContainsKey(eventId))
            {
                return;
            }

            if (eventListeners[eventId] == null)
            {
                eventListeners.Remove(eventId);
                return;
            }

            eventListeners[eventId].Invoke(parameter);
        }

        public static void ClearAllListeners()
        {
            //eventListeners?.Clear();
        }
    } 
}

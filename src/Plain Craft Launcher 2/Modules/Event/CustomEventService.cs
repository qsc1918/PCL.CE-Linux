using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace PCL
{
    public static class CustomEventService
    {
        public static readonly AvaloniaProperty EventsProperty =
            AvaloniaProperty.RegisterAttached<AvaloniaObject, CustomEventCollection>("Events", typeof(CustomEventService));

        public static void SetEvents(AvaloniaObject d, CustomEventCollection value) =>
            d.SetValue(EventsProperty, value);

        public static CustomEventCollection GetEvents(AvaloniaObject d)
        {
            if (d.GetValue(EventsProperty) is null)
                d.SetValue(EventsProperty, new CustomEventCollection());
            return (CustomEventCollection)d.GetValue(EventsProperty);
        }

        public static readonly AvaloniaProperty EventTypeProperty =
            AvaloniaProperty.RegisterAttached<AvaloniaObject, EventType>("EventType", typeof(CustomEventService));

        public static void SetEventType(AvaloniaObject d, EventType value) =>
            d.SetValue(EventTypeProperty, value);

        public static EventType GetEventType(AvaloniaObject d) =>
            (EventType)d.GetValue(EventTypeProperty);

        public static readonly AvaloniaProperty EventDataProperty =
            AvaloniaProperty.RegisterAttached<AvaloniaObject, string>("EventData", typeof(CustomEventService));

        public static void SetEventData(AvaloniaObject d, string value) =>
            d.SetValue(EventDataProperty, value);

        public static string GetEventData(AvaloniaObject d) =>
            (string)d.GetValue(EventDataProperty);
    }
}

using System.Collections;
using Avalonia.Markup;
using Avalonia.Metadata;

namespace PCL
{
    public class CustomEventCollection : IEnumerable<CustomEvent>
    {
        private readonly List<CustomEvent> _events = new();
    [Content] // [port] WPF 绫荤骇 [ContentProperty("Events")] 鈫?Avalonia 12 灞炴€х骇 [Content]

        public List<CustomEvent> Events => _events;

        public IEnumerator<CustomEvent> GetEnumerator() => Events.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

var b = new Border();
b.PropertyChanged += (s, e) => { if (e.Property == InputElement.IsEnabledProperty) Console.WriteLine("enabled=" + e.NewValue); };
b.IsEnabled = false;

// AddClassHandler in static context
InputElement.IsEnabledProperty.Changed.AddClassHandler<Border>((o, e) => Console.WriteLine("classhandler " + e.NewValue));
b.IsEnabled = true;

Console.WriteLine("PropertyChanged args type: " + typeof(Avalonia.AvaloniaPropertyChangedEventArgs).FullName);
Console.WriteLine("OK");

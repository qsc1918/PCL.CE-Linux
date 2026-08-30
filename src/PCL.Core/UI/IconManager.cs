using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace PCL.Core.UI;

// [port] WPF XamlReader.Parse（运行时字符串 XAML）→ Avalonia 12 无公开运行时 XAML 解析 API。
// 本实现针对图标 XAML 的固定子集（Viewbox/Canvas/Rectangle/Path）做等价解析，
// 输出为对应的 Avalonia 控件树，视觉与上游一致；超出子集的节点会被跳过。

// 图标管理器（处理集合和选择逻辑）
public class IconManager : INotifyPropertyChanged {
    private readonly Dictionary<string, IconModel> _iconIndex = new();

    public IconModel? SelectedIcon
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedIcon));
        }
    }

    public bool SetSelectedIconByName(string name) {
        if (_iconIndex.TryGetValue(name, out var icon)) {
            SelectedIcon = icon;
            return true;
        }
        return false;
    }

    public bool AddIconFromXaml(string name, string xamlString) {
        if (string.IsNullOrWhiteSpace(name) || _iconIndex.ContainsKey(name)) return false; // 避免重复

        if (TryLoadIconFromXaml(xamlString, out var content)) {
            var model = new IconModel(name, content);
            _iconIndex[name] = model;
            return true;
        }
        return false;
    }

    // 可选：添加移除方法
    public void RemoveIconByName(string name) {
        _iconIndex.Remove(name);
    }

    // 从 XAML 字符串加载图标
    public static bool TryLoadIconFromXaml(string xamlString, out Control? icon) {
        icon = null;
        if (string.IsNullOrWhiteSpace(xamlString)) return false;

        // 确保在UI线程执行
        if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess()) {
            return false;
        }

        try {
            icon = _ParseIconDocument(xamlString);
            return icon is not null;
        }
        catch (Exception) {
            return false;
        }
    }

    // 从 XAML 字符串加载图标
    public static bool LoadIconFromXaml(string xamlString, out Control? icon) {
        icon = null;
        if (string.IsNullOrWhiteSpace(xamlString)) {
            throw new ArgumentNullException(nameof(xamlString), "XAML 字符串不能为空或空白。");
        }

        // 确保在UI线程执行
        if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess()) {
            throw new InvalidOperationException("XAML 解析需要在 UI 线程执行。");
        }

        icon = _ParseIconDocument(xamlString);
        return icon is not null;
    }

    #region 图标 XAML 子集解析

    private static Control? _ParseIconDocument(string xaml) {
        using var stringReader = new StringReader(xaml);
        using var xmlReader = XmlReader.Create(stringReader, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        });
        var document = XDocument.Load(xmlReader);
        var root = document.Root;
        if (root is null || root.Name.LocalName != "Viewbox") return null;

        var viewbox = new Viewbox();
        if (double.TryParse(_Attr(root, "Width"), NumberStyles.Float, CultureInfo.InvariantCulture, out var width))
            viewbox.Width = width;
        if (double.TryParse(_Attr(root, "Height"), NumberStyles.Float, CultureInfo.InvariantCulture, out var height))
            viewbox.Height = height;
        if (root.Attribute("Margin") is { } marginAttr) {
            if (_TryParseThickness(marginAttr.Value, out var margin)) viewbox.Margin = margin;
        }

        var content = root.Elements().FirstOrDefault(e => e.Name.LocalName == "Canvas");
        if (content is not null)
            viewbox.Child = _ParseCanvas(content);

        return viewbox;
    }

    private static Canvas _ParseCanvas(XElement element) {
        var canvas = new Canvas();
        _ApplyCanvasSize(element, canvas);
        foreach (var child in element.Elements()) {
            Control? control = child.Name.LocalName switch
            {
                "Canvas" => _ParseCanvas(child),
                "Rectangle" => _ParseRectangle(child),
                "Path" => _ParsePath(child),
                _ => null
            };
            if (control is null) continue;

            if (double.TryParse(_Attr(child, "Left"), NumberStyles.Float, CultureInfo.InvariantCulture, out var left))
                Canvas.SetLeft(control, left);
            if (double.TryParse(_Attr(child, "Top"), NumberStyles.Float, CultureInfo.InvariantCulture, out var top))
                Canvas.SetTop(control, top);

            canvas.Children.Add(control);
        }
        return canvas;
    }

    private static void _ApplyCanvasSize(XElement element, Canvas canvas) {
        if (double.TryParse(_Attr(element, "Width"), NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
            canvas.Width = w;
        if (double.TryParse(_Attr(element, "Height"), NumberStyles.Float, CultureInfo.InvariantCulture, out var h))
            canvas.Height = h;
    }

    private static Rectangle _ParseRectangle(XElement element) {
        var rect = new Rectangle();
        if (double.TryParse(_Attr(element, "Width"), NumberStyles.Float, CultureInfo.InvariantCulture, out var w))
            rect.Width = w;
        if (double.TryParse(_Attr(element, "Height"), NumberStyles.Float, CultureInfo.InvariantCulture, out var h))
            rect.Height = h;
        if (double.TryParse(_Attr(element, "RadiusX"), NumberStyles.Float, CultureInfo.InvariantCulture, out var rx))
            rect.RadiusX = rx;
        if (double.TryParse(_Attr(element, "RadiusY"), NumberStyles.Float, CultureInfo.InvariantCulture, out var ry))
            rect.RadiusY = ry;
        var fill = _Attr(element, "Fill");
        if (!string.IsNullOrEmpty(fill) && Color.TryParse(fill, out var color))
            rect.Fill = new SolidColorBrush(color);
        return rect;
    }

    private static Avalonia.Controls.Shapes.Path _ParsePath(XElement element) {
        var path = new Avalonia.Controls.Shapes.Path();
        var fill = _Attr(element, "Fill");
        if (!string.IsNullOrEmpty(fill) && Color.TryParse(fill, out var color))
            path.Fill = new SolidColorBrush(color);

        // WPF <Path.Data><PathGeometry Figures="..." FillRule="..."/></Path.Data> 子结构
        var dataElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "Path.Data")
            ?? element.Elements().FirstOrDefault(e => e.Name.LocalName == "Data");
        var geometryElement = dataElement?.Elements().FirstOrDefault(e => e.Name.LocalName.EndsWith("Geometry"));
        var figures = geometryElement is not null
            ? _Attr(geometryElement, "Figures")
            : _Attr(element, "Data");
        if (!string.IsNullOrWhiteSpace(figures)) {
            var geometry = Geometry.Parse(figures);
            if (geometry is PathGeometry pathGeometry
                && string.Equals(_Attr(geometryElement, "FillRule"), "Nonzero", StringComparison.OrdinalIgnoreCase)) {
                pathGeometry.FillRule = FillRule.NonZero;
            }
            path.Data = geometry;
        }
        return path;
    }

    private static string? _Attr(XElement element, string name) {
        return element.Attribute(name)?.Value;
    }

    // Avalonia 12 无 Thickness.TryParse，手动解析 "left,top,right,bottom" 或统一值
    private static bool _TryParseThickness(string value, out Thickness thickness) {
        thickness = default;
        try {
            var parts = value.Split(',');
            var values = parts.Select(p => double.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();
            thickness = values.Length switch
            {
                1 => new Thickness(values[0]),
                2 => new Thickness(values[0], values[1], values[0], values[1]),
                4 => new Thickness(values[0], values[1], values[2], values[3]),
                _ => throw new FormatException()
            };
            return true;
        } catch {
            return false;
        }
    }

    #endregion

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

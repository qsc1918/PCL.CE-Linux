using System.Collections.Concurrent;
using System.IO;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using SkiaSharp;

// 一个万能的自动图片类型转换工具类
// [port] System.Drawing(GDI+) + WPF BitmapSource/WIC → SkiaSharp + Avalonia Bitmap。
// 对外 API 形状与上游一致；WebP 由 Skia 原生解码，删除 WIC 特殊分支；
// 图片源字符串 pack://application:,,,/images/ 前缀仍映射到本地 Images 目录。

namespace PCL;

// [port] System.Drawing.RotateFlipType 随 GDI+ 一起不可用，定义等价枚举（数值与 GDI+ 对齐）
public enum RotateFlipType
{
    RotateNoneFlipNone = 0,
    Rotate90FlipNone = 4,
    Rotate180FlipNone = 8,
    Rotate270FlipNone = 12,
    RotateNoneFlipX = 1,
    Rotate90FlipX = 5,
    Rotate180FlipX = 9,
    Rotate270FlipX = 13,
}

public class MyBitmap
{
    // 使用缓存
    private readonly ConcurrentDictionary<string, MyBitmap> _Cache = new();

    /// <summary>
    ///     存储的图片
    /// </summary>
    public SKBitmap pic;

    /// <summary>[port] 缓存的 Avalonia 位图（隐式转 IImage/ImageBrush 时生成一次）</summary>
    private Bitmap _AvaloniaCache;

    // 构造函数
    public MyBitmap()
    {
    }

    public MyBitmap(string filePathOrResourceName)
    {
        do
        {
            try
            {
                filePathOrResourceName =
                    filePathOrResourceName.Replace("pack://application:,,,/images/", ModBase.pathImage);
                if (filePathOrResourceName.StartsWithF(ModBase.pathImage))
                {
                    if (_Cache.ContainsKey(filePathOrResourceName))
                    {
                        pic = _Cache[filePathOrResourceName].pic;
                        _AvaloniaCache = _Cache[filePathOrResourceName]._AvaloniaCache;
                    }
                    else
                    {
                        using (var picStream = new FileStream(filePathOrResourceName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            pic = SKBitmap.Decode(picStream);
                        }
                        _Cache.TryAdd(filePathOrResourceName, this);
                    }
                }
                else
                {
                    // [port] 使用这种自己接管 FileStream 的方法加载才能解除文件占用；Skia 原生支持 WebP
                    using (var picStream = new FileStream(filePathOrResourceName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        pic = SKBitmap.Decode(picStream);
                    }
                }
            }
            catch (Exception ex)
            {
                var resource = Avalonia.Application.Current?.TryFindResource(filePathOrResourceName) as Bitmap;
                if (resource is null)
                {
                    pic = new SKBitmap(1, 1);
                    if (ex is ArgumentException) throw new Exception($"图片格式不支持，或图片文件损坏（{filePathOrResourceName}）", ex);

                    throw new Exception($"加载 MyBitmap 意外失败（{filePathOrResourceName}）", ex);
                }

                pic = _FromAvalonia(resource);
                _AvaloniaCache = resource;

                ModBase.Log(ex, $"指定类型有误的 MyBitmap 加载（{filePathOrResourceName}）", ModBase.LogLevel.Developer);
                break;
            }
        } while (false);
    }

    public MyBitmap(IImage image)
    {
        // [port] WPF 把任意 ImageSource 编码为 PNG 再解码；Avalonia 用 RenderTargetBitmap 光栅化
        if (image is Bitmap bitmap)
        {
            _AvaloniaCache = bitmap;
            pic = _FromAvalonia(bitmap);
        }
        else
        {
            var width = Math.Max(1, (int)Math.Ceiling(image.Size.Width));
            var height = Math.Max(1, (int)Math.Ceiling(image.Size.Height));
            using var rtb = new RenderTargetBitmap(new PixelSize(width, height));
            using (var ctx = rtb.CreateDrawingContext())
            {
                image.Draw(ctx, new Rect(0, 0, width, height));
            }
            using var ms = new MemoryStream();
            rtb.Save(ms);
            pic = SKBitmap.Decode(ms);
        }
    }

    public MyBitmap(ImageBrush image) : this(image.Source) { }

    // 自动类型转换
    // 支持的类：IImage，Bitmap，ImageBrush
    public static implicit operator MyBitmap(IImage image)
    {
        if (image is null)
            return null;
        return new MyBitmap(image);
    }

    public static implicit operator Bitmap(MyBitmap image)
    {
        if (image is null)
            return null;
        return image._ToAvalonia();
    }

    public static implicit operator MyBitmap(Bitmap image)
    {
        if (image is null)
            return null;
        return new MyBitmap(image);
    }

    public static implicit operator ImageBrush(MyBitmap image)
    {
        if (image is null)
            return null;
        return new ImageBrush(image._ToAvalonia());
    }

    /// <summary>
    ///     获取裁切的图片，这个方法不会导致原对象改变且会返回一个新的对象。
    /// </summary>
    public MyBitmap Clip(int x, int y, int width, int height)
    {
        // [port] GDI+ TranslateTransform + DrawImage 等价于裁出 (x,y) 起 width×height 的区域
        var subset = new SKBitmap();
        pic.ExtractSubset(subset, new SKRectI(x, y, x + width, y + height));
        return new MyBitmap { pic = subset };
    }

    /// <summary>
    ///     获取旋转或翻转后的图片，这个方法不会导致原对象改变且会返回一个新的对象。
    /// </summary>
    public MyBitmap RotateFlip(RotateFlipType type)
    {
        // [port] GDI+ RotateFlip → Skia 矩阵绘制（上游枚举保留以维持调用点不变）
        var rotate = ((int)type / 4) switch
        {
            1 => 90f,
            2 => 180f,
            3 => 270f,
            _ => 0f
        };
        var flipX = ((int)type % 2) == 1; // Rotate/FlipX 组合约定与 GDI+ 一致
        var w = rotate is 90f or 270f ? pic.Height : pic.Width;
        var h = rotate is 90f or 270f ? pic.Width : pic.Height;
        var bmp = new SKBitmap(w, h);
        using (var surface = new SKCanvas(bmp))
        {
            surface.Translate(w / 2f, h / 2f);
            surface.RotateDegrees(rotate);
            if (flipX) surface.Scale(-1, 1);
            surface.DrawBitmap(pic, new SKPoint(-pic.Width / 2f, -pic.Height / 2f));
            surface.Restore();
        }
        return new MyBitmap { pic = bmp };
    }

    /// <summary>
    ///     将图像保存到文件。
    /// </summary>
    public void Save(string filePath)
    {
        using var image = SKImage.FromBitmap(pic);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var fileStream = new FileStream(filePath, FileMode.Create);
        data.SaveTo(fileStream);
    }

    private Bitmap _ToAvalonia()
    {
        if (_AvaloniaCache is not null) return _AvaloniaCache;
        using var image = SKImage.FromBitmap(pic);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream();
        data.SaveTo(ms);
        ms.Position = 0;
        _AvaloniaCache = new Bitmap(ms);
        return _AvaloniaCache;
    }

    private static SKBitmap _FromAvalonia(Bitmap bitmap)
    {
        using var ms = new MemoryStream();
        bitmap.Save(ms);
        ms.Position = 0;
        return SKBitmap.Decode(ms);
    }
}

using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using PCL.Core.UI.Animation.Core;

namespace PCL.Core.UI;

public struct NRotateTransform : 
    IEquatable<NRotateTransform>,
    IAdditionOperators<NRotateTransform, NRotateTransform, NRotateTransform>,
    ISubtractionOperators<NRotateTransform, NRotateTransform, NRotateTransform>,
    IMultiplyOperators<NRotateTransform, float, NRotateTransform>,
    IDivisionOperators<NRotateTransform, float, NRotateTransform>
{
    private Vector3 _rotate;
    
    public float Angle
    {
        get => _rotate.X;
        set => _rotate.X = value;
    }
    
    public float CenterX
    {
        get => _rotate.Y;
        set => _rotate.Y = value;
    }
    
    public float CenterY
    {
        get => _rotate.Z;
        set => _rotate.Z = value;
    }

    #region 构造函数

    public NRotateTransform()
    {
        _rotate = new Vector3(0, 0, 0);
    }
    
    public NRotateTransform(float angle, float centerX = 0f, float centerY = 0f)
    {
        _rotate = new Vector3(angle, centerX, centerY);
    }

    public NRotateTransform(RotateTransform rotateTransform)
    {
        var uiAccessProvider = AnimationService.UIAccessProvider;
        if (uiAccessProvider.CheckAccess())
        {
            _rotate = GetVector(rotateTransform);
        }
        else
        {
            Vector3 localScale = default;
            uiAccessProvider.Invoke(() => localScale = GetVector(rotateTransform));
            _rotate = localScale;
        }

        return;

        // [port] Avalonia RotateTransform 无 CenterX/CenterY，中心点记为 (0,0)
        Vector3 GetVector(RotateTransform rt)
        {
            return new Vector3((float)rt.Angle, 0f, 0f);
        }
    }
    
    #endregion
    
    #region 运算符重载

    public static NRotateTransform operator +(NRotateTransform a, NRotateTransform b) => new(a.Angle + b.Angle, a.CenterX + b.CenterX, a.CenterY + b.CenterY);
    public static NRotateTransform operator -(NRotateTransform a, NRotateTransform b) => new(a.Angle - b.Angle, a.CenterX - b.CenterX, a.CenterY - b.CenterY);
    public static NRotateTransform operator *(NRotateTransform a, float b) => new(a.Angle * b, a.CenterX * b, a.CenterY * b);

    public static NRotateTransform operator /(NRotateTransform a, float b) =>
        b == 0 ? throw new DivideByZeroException("除数不能为零。") : new NRotateTransform(a.Angle / b, a.CenterX / b, a.CenterY / b);

    public static bool operator ==(NRotateTransform a, NRotateTransform b) => a._rotate == b._rotate;
    public static bool operator !=(NRotateTransform a, NRotateTransform b) => a._rotate != b._rotate;

    #endregion
    
    #region IEquatable

    public bool Equals(NRotateTransform other)
    {
        return _rotate.Equals(other._rotate);
    }

    public override bool Equals(object? obj)
    {
        if (obj is NRotateTransform color)
            return Equals(color);
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Angle, CenterX, CenterY);
    }

    #endregion

    #region 隐式转换

    // [port] Avalonia RotateTransform 无中心点概念 → 以 MatrixTransform 显式构造“绕中心旋转”矩阵：
    // p' = (p − c)·R + c，R = [cos sin; −sin cos]（行向量约定），平移项 tx = cx − cx·cos + cy·sin，ty = cy − cx·sin − cy·cos
    public static implicit operator MatrixTransform(NRotateTransform rt)
    {
        var angle = rt.Angle * Math.PI / 180f;
        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);
        var cx = rt.CenterX;
        var cy = rt.CenterY;
        var tx = cx - cx * cos + cy * sin;
        var ty = cy - cx * sin - cy * cos;
        var m = new Matrix(cos, sin, -sin, cos, tx, ty);
        return new MatrixTransform { Matrix = m };
    }

    public static implicit operator NRotateTransform(RotateTransform rt) => new(rt);

    #endregion
}
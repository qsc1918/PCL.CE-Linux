using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using PCL.Core.UI.Animation.Core;

namespace PCL.Core.UI;

public struct NScaleTransform : 
    IEquatable<NScaleTransform>,
    IAdditionOperators<NScaleTransform, NScaleTransform, NScaleTransform>,
    ISubtractionOperators<NScaleTransform, NScaleTransform, NScaleTransform>,
    IMultiplyOperators<NScaleTransform, float, NScaleTransform>,
    IDivisionOperators<NScaleTransform, float, NScaleTransform>
{
    private Vector4 _scale;

    public float ScaleX
    {
        get => _scale.X;
        set => _scale.X = value;
    }
    
    public float ScaleY
    {
        get => _scale.Y;
        set => _scale.Y = value;
    }
    
    public float CenterX
    {
        get => _scale.Z;
        set => _scale.Z = value;
    }
    
    public float CenterY
    {
        get => _scale.W;
        set => _scale.W = value;
    }

    #region 构造函数

    public NScaleTransform()
    {
        _scale = new Vector4(1, 1, 0, 0);
    }
    
    public NScaleTransform(float scaleX, float scaleY, float centerX = 0f, float centerY = 0f)
    {
        _scale = new Vector4(scaleX, scaleY, centerX, centerY);
    }

    public NScaleTransform(ScaleTransform scaleTransform)
    {
        var uiAccessProvider = AnimationService.UIAccessProvider;
        if (uiAccessProvider.CheckAccess())
        {
            _scale = GetVector(scaleTransform);
        }
        else
        {
            Vector4 localScale = default;
            uiAccessProvider.Invoke(() => localScale = GetVector(scaleTransform));
            _scale = localScale;
        }

        return;

        // [port] Avalonia ScaleTransform 无 CenterX/CenterY，中心点记为 (0,0)
        Vector4 GetVector(ScaleTransform st)
        {
            return new Vector4((float)st.ScaleX, (float)st.ScaleY, 0f, 0f);
        }
    }
    
    #endregion
    
    #region 运算符重载

    public static NScaleTransform operator +(NScaleTransform a, NScaleTransform b) => new(a.ScaleX + b.ScaleX, a.ScaleY + b.ScaleY, a.CenterX + b.CenterX, a.CenterY + b.CenterY);
    public static NScaleTransform operator -(NScaleTransform a, NScaleTransform b) => new(a.ScaleX - b.ScaleX, a.ScaleY - b.ScaleY, a.CenterX - b.CenterX, a.CenterY - b.CenterY);
    public static NScaleTransform operator *(NScaleTransform a, float b) => new(a.ScaleX * b, a.ScaleY * b, a.CenterX * b, a.CenterY * b);

    public static NScaleTransform operator /(NScaleTransform a, float b) =>
        b == 0 ? throw new DivideByZeroException("除数不能为零。") : new NScaleTransform(a.ScaleX / b, a.ScaleY / b, a.CenterX / b, a.CenterY / b);

    public static bool operator ==(NScaleTransform a, NScaleTransform b) => a._scale == b._scale;
    public static bool operator !=(NScaleTransform a, NScaleTransform b) => a._scale != b._scale;

    #endregion
    
    #region IEquatable

    public bool Equals(NScaleTransform other)
    {
        return _scale.Equals(other._scale);
    }

    public override bool Equals(object? obj)
    {
        if (obj is NScaleTransform color)
            return Equals(color);
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ScaleX, ScaleY, CenterX, CenterY);
    }

    #endregion

    #region 隐式转换

    // [port] Avalonia ScaleTransform 无中心点概念 → 以 MatrixTransform 显式构造“绕中心缩放”矩阵：
    // p' = (p − c)·S + c ⇔ M = [sx 0; 0 sy; cx−sx·cx, cy−sy·cy]（行向量约定，平移在 M31/M32）
    public static implicit operator MatrixTransform(NScaleTransform st)
    {
        var sx = st.ScaleX;
        var sy = st.ScaleY;
        var cx = st.CenterX;
        var cy = st.CenterY;
        var m = new Matrix(sx, 0, 0, sy, cx - sx * cx, cy - sy * cy);
        return new MatrixTransform { Matrix = m };
    }

    public static implicit operator NScaleTransform(ScaleTransform st) => new(st);

    #endregion
}
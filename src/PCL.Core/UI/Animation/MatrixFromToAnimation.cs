using Avalonia;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.UI.Animation.Core;
using PCL.Core.UI.Animation.ValueProcessor;

namespace PCL.Core.UI.Animation;

// [port] WPF Matrix.OffsetX/OffsetY → Avalonia Matrix.M31/M32（平移分量命名差异）。

public class MatrixFromToAnimation : FromToAnimationBase<Matrix>
{
    public override IAnimationFrame? ComputeNextFrame(IAnimatable target)
    {
        // 应用缓动函数
        var easedProgress = Easing.Ease(CurrentFrame, TotalFrames);

        // 计算当前值
        CurrentValue = ValueType == AnimationValueType.Relative
            ? ValueProcessorManager.Add(From, ValueProcessorManager.Scale(To, easedProgress))
            : ValueProcessorManager.Add(From,
                ValueProcessorManager.Scale(ValueProcessorManager.Subtract(To, From), easedProgress));

        return base.ComputeNextFrame(target);
    }
}

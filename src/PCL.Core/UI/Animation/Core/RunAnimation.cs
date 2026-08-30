using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup;
using Avalonia.Xaml.Interactivity;
using PCL.Core.UI.Animation.Animatable;
using PCL.Core.Utils;

namespace PCL.Core.UI.Animation.Core;

[ContentProperty(nameof(Animation))]
public class RunAnimationAction : TriggerAction<AvaloniaObject>
{
    public static readonly AvaloniaProperty AnimationProperty = AvaloniaProperty.Register(
        nameof(Animation),
        typeof(IAnimation),
        typeof(RunAnimationAction),
        new PropertyMetadata(default(IAnimation)));

    public IAnimation Animation
    {
        get => (IAnimation)GetValue(AnimationProperty);
        set => SetValue(AnimationProperty, value);
    }

    public static readonly AvaloniaProperty TargetPropertyProperty = AvaloniaProperty.Register(
        nameof(TargetProperty),
        typeof(AvaloniaProperty),
        typeof(RunAnimationAction),
        new PropertyMetadata(default(AvaloniaProperty)));

    public AvaloniaProperty TargetProperty
    {
        get => (AvaloniaProperty)GetValue(TargetPropertyProperty);
        set => SetValue(TargetPropertyProperty, value);
    }

    protected override void Invoke(object parameter)
    {
        AvaloniaObject? targetObject;
        AvaloniaProperty? targetProperty;

        var aniDependencyObject = (AvaloniaObject)Animation;

        // 判断对象
        if (WpfUtils.IsDependencyPropertySet(aniDependencyObject, AnimationExtensions.TargetProperty))
        {
            targetObject = (AvaloniaObject)aniDependencyObject.GetValue(AnimationExtensions.TargetProperty);
        }
        else
        {
            if (AssociatedObject is not null)
            {
                targetObject = AssociatedObject;
            }
            else
            {
                // 按理来说不可能出现这种情况，但是还是抛个异常吧
                throw new InvalidOperationException("未指定动画的目标对象。");
            }
        }

        // 判断属性
        if (WpfUtils.IsDependencyPropertySet(aniDependencyObject, AnimationExtensions.TargetPropertyProperty))
        {
            targetProperty =
                (AvaloniaProperty)aniDependencyObject.GetValue(AnimationExtensions.TargetPropertyProperty);
        }
        else
        {
            if (WpfUtils.IsDependencyPropertySet(this, TargetPropertyProperty))
            {
                targetProperty = TargetProperty;
            }
            else
            {
                if (Animation is not AnimationGroup)
                {
                    // 这里就有可能出现这种情况
                    throw new InvalidOperationException("未指定动画的目标属性。");
                }

                // AnimationGroup 可以没有目标属性
                targetProperty = null;
            }
        }
        
        Animation.RunFireAndForget(new AvaloniaAnimatable(targetObject, targetProperty));
    }
}
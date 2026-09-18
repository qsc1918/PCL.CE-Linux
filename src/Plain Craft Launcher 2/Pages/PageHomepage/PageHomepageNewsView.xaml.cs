using System.Windows.Input;
using PCL.Core.ViewModel.Homepage;
using PCL.Core.Model.Tools.News;
using Avalonia.Controls;

namespace PCL;

public partial class PageHomepageNewsView : MyPageRight
{
    public PageHomepageNewsView()
    {
        InitializeComponent();
        DataContext = new NewsViewModel();
    }

    // [port] WPF Behaviors（i:Interaction.Triggers + EventTrigger + InvokeCommandAction，Avalonia 12 无 Avalonia.Xaml.Behaviors）
    // → 改为 Click 事件处理器：经 DataContext（MyPageRight 的 NewsViewModel）上的 OpenReadCommand 打开新闻链接。
    // 生成命令属性名以反射获取，避免对 CommunityToolkit 生成器输出名的强耦合。
    private void ReadMore_Click(object? sender, EventArgs e)
    {
        if (sender is Control control && control.DataContext is NewsItem item && DataContext is NewsViewModel vm &&
            vm.GetType().GetProperty("OpenReadCommand")?.GetValue(vm) is ICommand cmd && cmd.CanExecute(item.Url))
        {
            cmd.Execute(item.Url);
        }
    }
}

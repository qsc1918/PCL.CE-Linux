using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Controls.Shapes;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Input;

namespace PCL;

public partial class MinecraftServerQuery : Grid
{
    public MinecraftServerQuery()
    {
        InitializeComponent();
        BtnServerQuery.Click += BtnServerQuery_Click;
    }
    private void BtnServerQuery_Click(object sender, PointerPressedEventArgs e)
    {
        Dispatcher.InvokeAsync(new Func<Task>(() => ServerQueryAsync()));
    }

    private async Task ServerQueryAsync()
    {
        await PanMcServer.UpdateServerInfoAsync(LabServerIp.Text);
        ServerInfo.IsVisible = true;
    }
}

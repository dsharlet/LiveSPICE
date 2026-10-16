using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace LiveSPICE.Avalonia;

public sealed class ConfirmWindow : Window
{
    private bool result;

    private ConfirmWindow(string title, string message)
    {
        Title = title;
        Width = 460;
        Height = 180;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        TextBlock text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Margin = new global::Avalonia.Thickness(16)
        };

        StackPanel buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new global::Avalonia.Thickness(16)
        };
        buttons.Children.Add(Button("Yes", true));
        buttons.Children.Add(Button("No", false));

        DockPanel dock = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        dock.Children.Add(buttons);
        dock.Children.Add(text);
        Content = dock;
    }

    public static async Task<bool> ShowAsync(Window owner, string title, string message)
    {
        ConfirmWindow window = new ConfirmWindow(title, message);
        await window.ShowDialog(owner);
        return window.result;
    }

    private Button Button(string text, bool value)
    {
        Button button = new Button { Content = text, MinWidth = 80 };
        button.Click += (_, _) =>
        {
            result = value;
            Close();
        };
        return button;
    }
}
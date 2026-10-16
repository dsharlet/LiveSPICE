using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace LiveSPICE.Avalonia;

public enum SaveChoice
{
    Save,
    Discard,
    Cancel
}

public sealed class SavePromptWindow : Window
{
    private SaveChoice choice = SaveChoice.Cancel;

    private SavePromptWindow(string documentTitle)
    {
        Title = "Unsaved Changes";
        Width = 420;
        Height = 170;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        TextBlock message = new TextBlock
        {
            Text = $"Save changes to {documentTitle}?",
            TextWrapping = TextWrapping.Wrap,
            Margin = new global::Avalonia.Thickness(16, 16, 16, 10)
        };

        StackPanel buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new global::Avalonia.Thickness(16)
        };
        buttons.Children.Add(Button("Save", SaveChoice.Save));
        buttons.Children.Add(Button("Discard", SaveChoice.Discard));
        buttons.Children.Add(Button("Cancel", SaveChoice.Cancel));

        DockPanel dock = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Bottom);
        dock.Children.Add(buttons);
        dock.Children.Add(message);
        Content = dock;
    }

    public static async Task<SaveChoice> ShowAsync(Window owner, string documentTitle)
    {
        SavePromptWindow window = new SavePromptWindow(documentTitle);
        await window.ShowDialog(owner);
        return window.choice;
    }

    private Button Button(string text, SaveChoice result)
    {
        Button button = new Button { Content = text, MinWidth = 86 };
        button.Click += (_, _) =>
        {
            choice = result;
            Close();
        };
        return button;
    }
}
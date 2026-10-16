using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Data;

namespace LiveSPICE.Avalonia;

public sealed class AudioConfigWindow : Window
{
    private readonly AppSettings settings;
    private readonly ComboBox drivers = new ComboBox();
    private readonly ComboBox devices = new ComboBox();
    private readonly ListBox inputs = new ListBox { SelectionMode = SelectionMode.Multiple };
    private readonly ListBox outputs = new ListBox { SelectionMode = SelectionMode.Multiple };
    private readonly TextBlock status = new TextBlock();
    private Audio.Stream? testStream;

    public AudioConfigWindow(AppSettings settings)
    {
        this.settings = settings;
        Title = "Audio Configuration";
        Width = 640;
        Height = 500;
        MinWidth = 520;
        MinHeight = 360;
        drivers.SelectionChanged += (_, _) => PopulateDevices();
        devices.SelectionChanged += (_, _) => PopulateChannels();
        drivers.DisplayMemberBinding = new Binding(nameof(Audio.Driver.Name));
        devices.DisplayMemberBinding = new Binding(nameof(Audio.Device.Name));
        Content = BuildContent();
        PopulateDrivers();
        Closed += (_, _) => StopTest();
    }

    private Control BuildContent()
    {
        DockPanel root = new DockPanel();

        StackPanel buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new global::Avalonia.Thickness(10)
        };
        buttons.Children.Add(Button("Refresh", (_, _) => PopulateDrivers()));
        buttons.Children.Add(Button("Test", (_, _) => ToggleTest()));
        buttons.Children.Add(Button("Save", (_, _) => SaveAndClose()));
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        StackPanel panel = new StackPanel { Spacing = 8, Margin = new global::Avalonia.Thickness(12) };
        panel.Children.Add(Label("Driver"));
        panel.Children.Add(drivers);
        panel.Children.Add(Label("Device / port graph"));
        panel.Children.Add(devices);

        Grid channels = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 10 };
        channels.Children.Add(ChannelPanel("Inputs", inputs));
        Border outputPanel = ChannelPanel("Outputs", outputs);
        Grid.SetColumn(outputPanel, 1);
        channels.Children.Add(outputPanel);
        panel.Children.Add(channels);
        panel.Children.Add(status);

        root.Children.Add(panel);
        return root;
    }

    private void PopulateDrivers()
    {
        List<Audio.Driver> availableDrivers = AvaloniaAudioDrivers.Available().ToList();
        drivers.ItemsSource = availableDrivers;
        drivers.SelectedItem = availableDrivers.FirstOrDefault(i => i.Name == settings.AudioDriver) ?? availableDrivers.FirstOrDefault();
        PopulateDevices();
        status.Text = availableDrivers.Count == 0 ? "No audio drivers are available in this build." : DriverHint(drivers.SelectedItem as Audio.Driver);
    }

    private void PopulateDevices()
    {
        Audio.Driver? driver = drivers.SelectedItem as Audio.Driver;
        List<Audio.Device> availableDevices = driver?.Devices.ToList() ?? new List<Audio.Device>();
        devices.ItemsSource = availableDevices;
        devices.SelectedItem = availableDevices.FirstOrDefault(i => i.Name == settings.AudioDevice) ?? availableDevices.FirstOrDefault();
        PopulateChannels();
        status.Text = driver == null ? "No audio driver selected." : DriverHint(driver);
    }

    private void PopulateChannels()
    {
        Audio.Device? device = devices.SelectedItem as Audio.Device;
        inputs.ItemsSource = device?.InputChannels ?? Array.Empty<Audio.Channel>();
        outputs.ItemsSource = device?.OutputChannels ?? Array.Empty<Audio.Channel>();
        SelectSaved(inputs, settings.AudioInputs);
        SelectSaved(outputs, settings.AudioOutputs);
        if (device != null)
            status.Text = DeviceHint(device);
    }

    private void SaveAndClose()
    {
        StopTest();
        settings.AudioDriver = (drivers.SelectedItem as Audio.Driver)?.Name ?? string.Empty;
        settings.AudioDevice = (devices.SelectedItem as Audio.Device)?.Name ?? string.Empty;
        settings.AudioInputs = inputs.SelectedItems?.OfType<Audio.Channel>().Select(i => i.Name).ToList() ?? new List<string>();
        settings.AudioOutputs = outputs.SelectedItems?.OfType<Audio.Channel>().Select(i => i.Name).ToList() ?? new List<string>();
        settings.Save();
        Close();
    }

    private void ToggleTest()
    {
        if (testStream != null)
        {
            StopTest();
            return;
        }

        Audio.Device? device = devices.SelectedItem as Audio.Device;
        if (device == null)
        {
            status.Text = "No audio device selected.";
            return;
        }

        try
        {
            testStream = device.Open(TestCallback, SelectedChannels(inputs), SelectedChannels(outputs));
            drivers.IsEnabled = false;
            devices.IsEnabled = false;
            inputs.IsEnabled = false;
            outputs.IsEnabled = false;
            status.Text = $"Testing at {testStream.SampleRate:0} Hz";
        }
        catch (Exception ex)
        {
            status.Text = ex.Message;
        }
    }

    private void StopTest()
    {
        if (testStream == null)
            return;

        try
        {
            testStream.Stop();
        }
        finally
        {
            testStream = null;
            drivers.IsEnabled = true;
            devices.IsEnabled = true;
            inputs.IsEnabled = true;
            outputs.IsEnabled = true;
            status.Text = "Ready";
        }
    }

    private static void TestCallback(int count, Audio.SampleBuffer[] input, Audio.SampleBuffer[] output, double rate)
    {
        for (int sample = 0; sample < count; sample++)
        {
            double value = 0;
            foreach (Audio.SampleBuffer buffer in input)
                value += buffer[sample];
            if (input.Length == 0)
                value = 0.15 * Math.Sin(2 * Math.PI * 440 * sample / rate);

            foreach (Audio.SampleBuffer buffer in output)
                buffer[sample] = value;
        }
    }

    private static Audio.Channel[] SelectedChannels(ListBox list)
    {
        return list.SelectedItems?.OfType<Audio.Channel>().ToArray() ?? Array.Empty<Audio.Channel>();
    }

    private static void SelectSaved(ListBox list, IEnumerable<string> names)
    {
        if (list.SelectedItems == null)
            return;

        list.SelectedItems.Clear();
        HashSet<string> selected = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (object? item in list.Items)
            if (item is Audio.Channel channel && selected.Contains(channel.Name))
                list.SelectedItems.Add(item);
    }

    private static string DriverHint(Audio.Driver? driver)
    {
        if (driver == null)
            return "No audio driver selected.";
        return driver.Name == "PipeWire/JACK"
            ? "PipeWire/JACK exposes individual ports below; choose capture and playback ports there."
            : "Ready";
    }

    private static string DeviceHint(Audio.Device device)
    {
        return device.Name == LinuxAudioDevice.DeviceName
            ? "Select actual PipeWire/JACK input and output ports below."
            : "Ready";
    }

    private static Border ChannelPanel(string title, ListBox list)
    {
        DockPanel dock = new DockPanel();
        TextBlock header = new TextBlock { Text = title, FontWeight = FontWeight.Bold, Margin = new global::Avalonia.Thickness(4) };
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        dock.Children.Add(list);
        return new Border { BorderBrush = Brushes.LightGray, BorderThickness = new global::Avalonia.Thickness(1), MinHeight = 220, Child = dock };
    }

    private static TextBlock Label(string text)
    {
        return new TextBlock { Text = text, FontWeight = FontWeight.Bold };
    }

    private static Button Button(string text, EventHandler<global::Avalonia.Interactivity.RoutedEventArgs> click)
    {
        Button button = new Button { Content = text, MinWidth = 82 };
        button.Click += click;
        return button;
    }
}
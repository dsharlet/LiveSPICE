using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Circuit;
using ComputerAlgebra;
using ComputerAlgebra.LinqCompiler;
using APoint = Avalonia.Point;

namespace LiveSPICE.Avalonia;

public sealed class WaveformWindow : Window
{
    private readonly Schematic schematic;
    private readonly AppSettings settings;
    private readonly WaveformView waveform = new WaveformView();
    private readonly TextBox oversample = new TextBox { Text = "8", Width = 52 };
    private readonly TextBox iterations = new TextBox { Text = "8", Width = 52 };
    private readonly TextBox samples = new TextBox { Text = "4096", Width = 72 };
    private readonly TextBox frequency = new TextBox { Text = "440", Width = 72 };
    private readonly TextBox inputExpression = new TextBox { Watermark = "Optional expression in t", MinWidth = 160 };
    private readonly Slider inputGain = new Slider { Minimum = -40, Maximum = 40, Value = 0, Width = 160 };
    private readonly Slider outputGain = new Slider { Minimum = -40, Maximum = 40, Value = 0, Width = 160 };
    private readonly ListBox probeList = new ListBox { SelectionMode = SelectionMode.Multiple, MinHeight = 96, MaxHeight = 180 };
    private readonly TextBlock audioConfig = new TextBlock { TextWrapping = TextWrapping.Wrap };
    private readonly TextBox log = new TextBox { IsReadOnly = true, AcceptsReturn = true, MinHeight = 100, TextWrapping = TextWrapping.Wrap };
    private readonly LiveAudioProcessor liveProcessor;
    private double liveInputScale = 1;
    private double liveOutputScale = 1;
    private Audio.Stream? liveStream;

    public WaveformWindow(Schematic schematic, AppSettings settings)
    {
        this.schematic = schematic;
        this.settings = settings;
        liveProcessor = new LiveAudioProcessor(schematic);
        Title = "Simulation Scope";
        Width = 1000;
        Height = 700;
        MinWidth = 520;
        MinHeight = 420;
        Content = BuildContent();
        UpdateAudioSummary();
        RunSimulation();
        Closed += (_, _) => StopLive();
    }

    private Control BuildContent()
    {
        DockPanel root = new DockPanel();

        StackPanel toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new global::Avalonia.Thickness(8),
            VerticalAlignment = VerticalAlignment.Center
        };
        toolbar.Children.Add(Label("Oversample"));
        toolbar.Children.Add(oversample);
        toolbar.Children.Add(Label("Iterations"));
        toolbar.Children.Add(iterations);
        toolbar.Children.Add(Label("Samples"));
        toolbar.Children.Add(samples);
        toolbar.Children.Add(Label("Hz"));
        toolbar.Children.Add(frequency);
        toolbar.Children.Add(Label("Input"));
        toolbar.Children.Add(inputExpression);
        toolbar.Children.Add(Button("Run", (_, _) => RunSimulation()));
        toolbar.Children.Add(Button("Live", (_, _) => ToggleLive()));
        DockPanel.SetDock(toolbar, Dock.Top);
        root.Children.Add(toolbar);

        Grid main = new Grid { ColumnDefinitions = new ColumnDefinitions("220,*"), RowDefinitions = new RowDefinitions("*,150") };

        StackPanel audio = new StackPanel { Spacing = 10, Margin = new global::Avalonia.Thickness(10) };
        inputGain.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) liveInputScale = DbToLinear(inputGain.Value); };
        outputGain.PropertyChanged += (_, e) => { if (e.Property == Slider.ValueProperty) liveOutputScale = DbToLinear(outputGain.Value); };
        audio.Children.Add(Header("Audio"));
        audio.Children.Add(audioConfig);
        audio.Children.Add(Button("Configure", (_, _) =>
        {
            AudioConfigWindow window = new AudioConfigWindow(settings);
            window.Closed += (_, _) => UpdateAudioSummary();
            window.Show();
        }));
        audio.Children.Add(Label("Input gain (dB)"));
        audio.Children.Add(inputGain);
        audio.Children.Add(Label("Output gain (dB)"));
        audio.Children.Add(outputGain);
        audio.Children.Add(Header("Input"));
        audio.Children.Add(new TextBlock { Text = "Generated sine" });
        audio.Children.Add(Header("Output"));
        audio.Children.Add(new TextBlock { Text = "First output channel" });
        audio.Children.Add(Header("Probes"));
        audio.Children.Add(probeList);
        Grid.SetColumn(audio, 0);
        Grid.SetRowSpan(audio, 2);
        main.Children.Add(audio);

        Border plotBorder = new Border { BorderBrush = Brushes.LightGray, BorderThickness = new global::Avalonia.Thickness(1), Child = waveform };
        Grid.SetColumn(plotBorder, 1);
        Grid.SetRow(plotBorder, 0);
        main.Children.Add(plotBorder);

        log.Margin = new global::Avalonia.Thickness(8);
        Grid.SetColumn(log, 1);
        Grid.SetRow(log, 1);
        main.Children.Add(log);

        root.Children.Add(main);
        return root;
    }

    private void RunSimulation()
    {
        try
        {
            int oversampleValue = ParseInt(oversample.Text, 8, 1, 64);
            int iterationValue = ParseInt(iterations.Text, 8, 1, 64);
            int sampleCount = ParseInt(samples.Text, 4096, 64, 262144);
            double frequencyValue = ParseDouble(frequency.Text, 440, 0.1, 96000);
            int sampleRate = 48000;

            Circuit.Circuit circuit = schematic.Build();
            RefreshProbeCandidates(circuit);
            Expression inputExpression = circuit.Components.OfType<Input>().Select(i => i.In).SingleOrDefault()
                ?? throw new NotSupportedException("Circuit has no inputs.");
            Expression speakerOutput = SpeakerOutputExpression(circuit);
            ProbeCandidate[] selectedProbes = SelectedProbeCandidates().ToArray();
            Expression[] outputExpressions = new[] { speakerOutput }.Concat(selectedProbes.Select(i => i.Expression)).ToArray();
            Simulation simulation = new Simulation(AudioSimulationFactory.Solve(circuit, sampleRate, oversampleValue))
            {
                Input = new[] { inputExpression },
                Output = outputExpressions,
                Oversample = oversampleValue,
                Iterations = iterationValue,
            };

            double inGain = DbToLinear(inputGain.Value);
            double outGain = DbToLinear(outputGain.Value);
            double[] input = new double[sampleCount];
            string[] outputNames = new[] { settings.AudioOutputs.Count == 0 ? "Speaker output" : string.Join(" + ", settings.AudioOutputs) }
                .Concat(selectedProbes.Select(i => i.Name))
                .ToArray();
            double[][] outputs = outputNames.Select(_ => new double[sampleCount]).ToArray();
            FillInputBuffer(input, sampleRate, frequencyValue, inGain);

            simulation.Run(input.Length, new[] { input }, outputs);
            for (int channel = 0; channel < outputs.Length; channel++)
            {
                for (int i = 0; i < outputs[channel].Length; i++)
                    outputs[channel][i] *= outGain;
            }

            waveform.SetTraces(outputNames.Zip(outputs, (name, data) => new WaveformTrace(name, data)), sampleRate);
            log.Text = $"Build succeeded\nAudio driver: {AudioName(settings.AudioDriver)}\nDevice: {AudioName(settings.AudioDevice)}\nInputs: {ChannelNames(settings.AudioInputs)}\nOutputs: {ChannelNames(settings.AudioOutputs)}\nProbes: {ChannelNames(selectedProbes.Select(i => i.Name).ToArray())}\nInput signal: {InputDescription(frequencyValue)}\nSample rate: {sampleRate}\nOversample: {oversampleValue}\nIterations: {iterationValue}\nSamples: {sampleCount}\nPeak: {outputs.SelectMany(i => i).Select(Math.Abs).DefaultIfEmpty().Max():R}";
        }
        catch (Exception ex)
        {
            log.Text = ex.ToString();
        }
    }

    private void ToggleLive()
    {
        if (liveStream != null)
        {
            StopLive();
            return;
        }

        try
        {
            Audio.Device device = SelectedDevice();
            Audio.Channel[] input = SelectedChannels(device.InputChannels, settings.AudioInputs).ToArray();
            Audio.Channel[] output = SelectedChannels(device.OutputChannels, settings.AudioOutputs).ToArray();
            if (input.Length == 0 && device.InputChannels.Length > 0)
                input = new[] { device.InputChannels[0] };
            if (output.Length == 0 && device.OutputChannels.Length > 0)
                output = new[] { device.OutputChannels[0] };

            int oversampleValue = ParseInt(oversample.Text, 8, 1, 64);
            int iterationValue = ParseInt(iterations.Text, 8, 1, 64);
            liveInputScale = DbToLinear(inputGain.Value);
            liveOutputScale = DbToLinear(outputGain.Value);
            StartLiveSimulation(48000, oversampleValue, iterationValue);

            liveStream = device.Open(ProcessLiveSamples, input, output);
            StartLiveSimulation(liveStream.SampleRate, oversampleValue, iterationValue);
            log.Text = $"Live stream started\nAudio driver: {AudioName(settings.AudioDriver)}\nDevice: {device.Name}\nSample rate: {liveStream.SampleRate:0}";
        }
        catch (Exception ex)
        {
            StopLive();
            log.Text = ex.ToString();
        }
    }

    private void StopLive()
    {
        Audio.Stream? stream = liveStream;
        liveStream = null;
        if (stream != null)
            stream.Stop();
        liveProcessor.Stop();
    }

    private void ProcessLiveSamples(int count, Audio.SampleBuffer[] input, Audio.SampleBuffer[] output, double rate)
    {
        try
        {
            liveProcessor.InputScale = liveInputScale;
            liveProcessor.OutputScale = liveOutputScale;
            double[] outputSamples = liveProcessor.Process(count, input, output, rate);

            if (outputSamples.Length > 0)
                Dispatcher.UIThread.Post(() => waveform.SetSamples(outputSamples, (int)rate));
        }
        catch (Exception ex)
        {
            foreach (Audio.SampleBuffer buffer in output)
                buffer.Clear();
            Dispatcher.UIThread.Post(() => log.Text = ex.ToString());
        }
    }

    internal void StartLiveSimulation(double sampleRate, int oversampleValue = 8, int iterationValue = 8)
    {
        liveInputScale = DbToLinear(inputGain.Value);
        liveOutputScale = DbToLinear(outputGain.Value);
        liveProcessor.InputScale = liveInputScale;
        liveProcessor.OutputScale = liveOutputScale;
        liveProcessor.Start(sampleRate, oversampleValue, iterationValue);
    }

    internal void TestProcessLiveSamples(int count, Audio.SampleBuffer[] input, Audio.SampleBuffer[] output, double rate)
    {
        ProcessLiveSamples(count, input, output, rate);
    }

    private Audio.Device SelectedDevice()
    {
        IReadOnlyList<Audio.Driver> drivers = AvaloniaAudioDrivers.Available();
        Audio.Driver driver = drivers.FirstOrDefault(i => i.Name == settings.AudioDriver && i.Devices.Any()) ?? drivers.First(i => i.Devices.Any());
        return driver.Devices.FirstOrDefault(i => i.Name == settings.AudioDevice) ?? driver.Devices.First();
    }

    private static System.Collections.Generic.IEnumerable<Audio.Channel> SelectedChannels(Audio.Channel[] channels, System.Collections.Generic.IEnumerable<string> names)
    {
        System.Collections.Generic.HashSet<string> selected = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return channels.Where(i => selected.Contains(i.Name));
    }

    private static TextBlock Header(string text)
    {
        return new TextBlock { Text = text, FontWeight = FontWeight.Bold, Margin = new global::Avalonia.Thickness(0, 8, 0, 0) };
    }

    private void UpdateAudioSummary()
    {
        audioConfig.Text = $"{AudioName(settings.AudioDriver)} / {AudioName(settings.AudioDevice)}\nIn: {ChannelNames(settings.AudioInputs)}\nOut: {ChannelNames(settings.AudioOutputs)}";
    }

    private static string AudioName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "None" : value;
    }

    private static string ChannelNames(System.Collections.Generic.IReadOnlyCollection<string> channels)
    {
        return channels.Count == 0 ? "None" : string.Join(", ", channels);
    }

    private static TextBlock Label(string text)
    {
        return new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
    }

    private static Button Button(string text, EventHandler<global::Avalonia.Interactivity.RoutedEventArgs> click)
    {
        Button button = new Button { Content = text, MinWidth = 70 };
        button.Click += click;
        return button;
    }

    private static int ParseInt(string? text, int fallback, int min, int max)
    {
        return int.TryParse(text, out int value) ? Math.Clamp(value, min, max) : fallback;
    }

    private static double ParseDouble(string? text, double fallback, double min, double max)
    {
        return double.TryParse(text, out double value) ? Math.Clamp(value, min, max) : fallback;
    }

    private static double DbToLinear(double db)
    {
        return Math.Pow(10, db / 20);
    }

    private void FillInputBuffer(double[] input, int sampleRate, double frequencyValue, double gain)
    {
        FillInputBuffer(input, sampleRate, frequencyValue, gain, inputExpression.Text);
    }

    internal static void FillInputBuffer(double[] input, int sampleRate, double frequencyValue, double gain, string? expressionText)
    {
        Func<double, double>? signal = CompileInputExpression(expressionText);
        for (int i = 0; i < input.Length; i++)
        {
            double time = i / (double)sampleRate;
            double value = signal == null ? 0.25 * Math.Sin(2 * Math.PI * frequencyValue * time) : signal(time);
            input[i] = gain * value;
        }
    }

    private static Func<double, double>? CompileInputExpression(string? expressionText)
    {
        if (string.IsNullOrWhiteSpace(expressionText))
            return null;

        Expression expression = Expression.Parse(expressionText);
        return expression.Compile<Func<double, double>>(Circuit.Component.t);
    }

    private string InputDescription(double frequencyValue)
    {
        return string.IsNullOrWhiteSpace(inputExpression.Text) ? $"Generated sine {frequencyValue} Hz" : inputExpression.Text;
    }

    private void RefreshProbeCandidates(Circuit.Circuit circuit)
    {
        string[] selected = SelectedProbeCandidates().Select(i => i.Name).ToArray();
        ProbeCandidate[] candidates = ProbeCandidates(circuit).ToArray();
        probeList.ItemsSource = candidates;
        if (probeList.SelectedItems == null)
            return;

        probeList.SelectedItems.Clear();
        foreach (ProbeCandidate candidate in candidates.Where(i => selected.Contains(i.Name, StringComparer.OrdinalIgnoreCase)))
            probeList.SelectedItems.Add(candidate);
    }

    private System.Collections.Generic.IEnumerable<ProbeCandidate> SelectedProbeCandidates()
    {
        return probeList.SelectedItems?.OfType<ProbeCandidate>() ?? Enumerable.Empty<ProbeCandidate>();
    }

    internal static System.Collections.Generic.IEnumerable<ProbeCandidate> ProbeCandidates(Circuit.Circuit circuit)
    {
        return circuit.Nodes
            .Where(i => !string.IsNullOrWhiteSpace(i.Name) && i.Name != "0")
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .Select(i => new ProbeCandidate(i.Name, i.V));
    }

    private static Expression SpeakerOutputExpression(Circuit.Circuit circuit)
    {
        Expression expression = 0;
        foreach (Speaker speaker in circuit.Components.OfType<Speaker>())
            expression += speaker.Out;
        if (expression.EqualsZero())
            throw new NotSupportedException("Circuit has no speaker outputs.");
        return expression;
    }
}

public sealed class WaveformView : Control
{
    private static readonly Typeface TextTypeface = new Typeface("Inter");
    private static readonly IBrush[] TraceBrushes = { Brushes.DodgerBlue, Brushes.OrangeRed, Brushes.SeaGreen, Brushes.MediumPurple };
    private WaveformTrace[] traces = Array.Empty<WaveformTrace>();
    private int sampleRate = 48000;

    public void SetSamples(double[] value, int rate)
    {
        SetTraces(new[] { new WaveformTrace("Output", value) }, rate);
    }

    public void SetTraces(System.Collections.Generic.IEnumerable<WaveformTrace> value, int rate)
    {
        traces = value.ToArray();
        sampleRate = rate;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(Brushes.White, Bounds);
        Rect plot = new Rect(48, 28, Math.Max(1, Bounds.Width - 72), Math.Max(1, Bounds.Height - 72));
        context.DrawRectangle(null, new Pen(Brushes.LightGray, 1), plot);
        context.DrawLine(new Pen(Brushes.Gray, 1), new APoint(plot.Left, plot.Center.Y), new APoint(plot.Right, plot.Center.Y));

        if (traces.Length == 0 || traces.All(i => i.Samples.Length == 0))
            return;

        double peak = Math.Max(traces.SelectMany(i => i.Samples).Select(Math.Abs).DefaultIfEmpty().Max(), 1e-9);
        int maxSamples = traces.Max(i => i.Samples.Length);
        for (int traceIndex = 0; traceIndex < traces.Length; traceIndex++)
        {
            double[] samples = traces[traceIndex].Samples;
            if (samples.Length == 0)
                continue;

            Pen waveform = new Pen(TraceBrushes[traceIndex % TraceBrushes.Length], 1.4);
            APoint previous = SamplePoint(samples, 0, peak, plot);
            for (int i = 1; i < samples.Length; i++)
            {
                APoint next = SamplePoint(samples, i, peak, plot);
                context.DrawLine(waveform, previous, next);
                previous = next;
            }
            DrawText(context, traces[traceIndex].Name, new APoint(plot.Right - 130, plot.Top + traceIndex * 16), TraceBrushes[traceIndex % TraceBrushes.Length]);
        }

        DrawText(context, $"{maxSamples / (double)sampleRate:0.000}s  peak {peak:0.000000}", new APoint(48, 6), Brushes.DimGray);
        DrawText(context, "+peak", new APoint(6, plot.Top), Brushes.DimGray);
        DrawText(context, "0", new APoint(24, plot.Center.Y - 8), Brushes.DimGray);
        DrawText(context, "-peak", new APoint(6, plot.Bottom - 16), Brushes.DimGray);
    }

    private static APoint SamplePoint(double[] samples, int index, double peak, Rect plot)
    {
        double x = plot.Left + (samples.Length == 1 ? 0 : index * plot.Width / (samples.Length - 1));
        double y = plot.Center.Y - samples[index] / peak * plot.Height / 2;
        return new APoint(x, y);
    }

    private static void DrawText(DrawingContext context, string text, APoint point, IBrush brush)
    {
        FormattedText formatted = new FormattedText(text, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, TextTypeface, 12, brush);
        context.DrawText(formatted, point);
    }
}

public sealed record WaveformTrace(string Name, double[] Samples);

public sealed record ProbeCandidate(string Name, Expression Expression)
{
    public override string ToString()
    {
        return Name;
    }
}
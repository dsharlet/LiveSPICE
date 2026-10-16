using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace LiveSPICE.Avalonia;

public sealed class LinuxAudioDriver : Audio.Driver
{
    public LinuxAudioDriver()
    {
        LinuxAudioChannel[] inputs = LinuxAudioDiscovery.InputPorts().Select(i => new LinuxAudioChannel(i)).ToArray();
        LinuxAudioChannel[] outputs = LinuxAudioDiscovery.OutputPorts().Select(i => new LinuxAudioChannel(i)).ToArray();
        if (inputs.Length > 0 || outputs.Length > 0)
            devices.Add(new LinuxAudioDevice(inputs, outputs));
    }

    public override string Name => "PipeWire/JACK";

    public override string ToString()
    {
        return Name;
    }
}

internal sealed class LinuxAudioChannel : Audio.Channel
{
    public LinuxAudioChannel(string name)
    {
        Name = name;
    }

    public override string Name { get; }

    public override string ToString()
    {
        return Name;
    }
}

internal sealed class LinuxAudioDevice : Audio.Device
{
    public const string DeviceName = "PipeWire/JACK port graph";

    public LinuxAudioDevice(Audio.Channel[] input, Audio.Channel[] output) : base(DeviceName)
    {
        inputs = input;
        outputs = output;
    }

    public override string ToString()
    {
        return Name;
    }

    public override Audio.Stream Open(Audio.Stream.SampleHandler callback, Audio.Channel[] input, Audio.Channel[] output)
    {
        return new JackAudioStream(callback, input.Cast<LinuxAudioChannel>().ToArray(), output.Cast<LinuxAudioChannel>().ToArray());
    }
}

internal sealed unsafe class JackAudioStream : Audio.Stream
{
    private const uint JackNullOption = 0;
    private const ulong JackPortIsInput = 1;
    private const ulong JackPortIsOutput = 2;
    private const string DefaultAudioType = "32 bit float mono audio";

    private readonly SampleHandler callback;
    private readonly IntPtr client;
    private readonly IntPtr[] inputPorts;
    private readonly IntPtr[] outputPorts;
    private readonly Audio.SampleBuffer[] inputBuffers;
    private readonly Audio.SampleBuffer[] outputBuffers;
    private readonly JackProcessCallback processCallback;
    private readonly double sampleRate;
    private bool stopped;

    public JackAudioStream(SampleHandler callback, LinuxAudioChannel[] input, LinuxAudioChannel[] output) : base(input, output)
    {
        this.callback = callback;
        int status;
        client = JackNative.jack_client_open("LiveSPICE", JackNullOption, out status);
        if (client == IntPtr.Zero)
            throw new InvalidOperationException("Could not open JACK client. Is JACK or PipeWire JACK running?");

        sampleRate = JackNative.jack_get_sample_rate(client);
        inputPorts = new IntPtr[input.Length];
        outputPorts = new IntPtr[output.Length];
        inputBuffers = input.Select(_ => new Audio.SampleBuffer(1)).ToArray();
        outputBuffers = output.Select(_ => new Audio.SampleBuffer(1)).ToArray();

        for (int i = 0; i < inputPorts.Length; i++)
            inputPorts[i] = RegisterPort($"input_{i + 1}", JackPortIsInput);
        for (int i = 0; i < outputPorts.Length; i++)
            outputPorts[i] = RegisterPort($"output_{i + 1}", JackPortIsOutput);

        processCallback = Process;
        JackNative.Check(JackNative.jack_set_process_callback(client, processCallback, IntPtr.Zero), "set JACK process callback");
        JackNative.Check(JackNative.jack_activate(client), "activate JACK client");

        for (int i = 0; i < input.Length; i++)
        {
            string sourcePort = input[i].Name;
            string destinationPort = JackNative.PortName(inputPorts[i]);
            JackNative.Check(JackNative.jack_connect(client, sourcePort, destinationPort), $"connect JACK port {sourcePort} to {destinationPort}");
        }
        for (int i = 0; i < output.Length; i++)
        {
            string sourcePort = JackNative.PortName(outputPorts[i]);
            string destinationPort = output[i].Name;
            JackNative.Check(JackNative.jack_connect(client, sourcePort, destinationPort), $"connect JACK port {sourcePort} to {destinationPort}");
        }
    }

    public override double SampleRate => sampleRate;

    public override void Stop()
    {
        if (stopped)
            return;

        stopped = true;
        JackNative.jack_deactivate(client);
        JackNative.jack_client_close(client);
        foreach (Audio.SampleBuffer buffer in inputBuffers.Concat(outputBuffers))
            buffer.Dispose();
    }

    private IntPtr RegisterPort(string name, ulong flags)
    {
        IntPtr port = JackNative.jack_port_register(client, name, DefaultAudioType, flags, 0);
        if (port == IntPtr.Zero)
            throw new InvalidOperationException($"Could not register JACK port '{name}'.");
        return port;
    }

    private int Process(uint frameCount, IntPtr arg)
    {
        if (stopped)
            return 0;

        try
        {
            EnsureBuffers((int)frameCount);
            for (int channel = 0; channel < inputPorts.Length; channel++)
            {
                float* source = (float*)JackNative.jack_port_get_buffer(inputPorts[channel], frameCount);
                for (int sample = 0; sample < frameCount; sample++)
                    inputBuffers[channel][sample] = source[sample];
            }

            callback((int)frameCount, inputBuffers, outputBuffers, sampleRate);

            for (int channel = 0; channel < outputPorts.Length; channel++)
            {
                float* destination = (float*)JackNative.jack_port_get_buffer(outputPorts[channel], frameCount);
                for (int sample = 0; sample < frameCount; sample++)
                    destination[sample] = (float)Math.Clamp(outputBuffers[channel][sample], -1, 1);
            }
        }
        catch
        {
            for (int channel = 0; channel < outputPorts.Length; channel++)
            {
                float* destination = (float*)JackNative.jack_port_get_buffer(outputPorts[channel], frameCount);
                for (int sample = 0; sample < frameCount; sample++)
                    destination[sample] = 0;
            }
        }
        return 0;
    }

    private void EnsureBuffers(int frameCount)
    {
        for (int i = 0; i < inputBuffers.Length; i++)
            if (inputBuffers[i].Samples.Length != frameCount)
            {
                inputBuffers[i].Dispose();
                inputBuffers[i] = new Audio.SampleBuffer(frameCount);
            }
        for (int i = 0; i < outputBuffers.Length; i++)
            if (outputBuffers[i].Samples.Length != frameCount)
            {
                outputBuffers[i].Dispose();
                outputBuffers[i] = new Audio.SampleBuffer(frameCount);
            }
    }
}

internal delegate int JackProcessCallback(uint frames, IntPtr arg);

internal static class JackNative
{
    [DllImport("jack", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jack_client_open(string clientName, uint options, out int status);

    [DllImport("jack", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jack_client_close(IntPtr client);

    [DllImport("jack", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jack_port_register(IntPtr client, string portName, string portType, ulong flags, ulong bufferSize);

    [DllImport("jack", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jack_set_process_callback(IntPtr client, JackProcessCallback callback, IntPtr arg);

    [DllImport("jack", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jack_activate(IntPtr client);

    [DllImport("jack", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jack_deactivate(IntPtr client);

    [DllImport("jack", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint jack_get_sample_rate(IntPtr client);

    [DllImport("jack", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr jack_port_get_buffer(IntPtr port, uint frames);

    [DllImport("jack", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr jack_port_name(IntPtr port);

    [DllImport("jack", CallingConvention = CallingConvention.Cdecl)]
    public static extern int jack_connect(IntPtr client, string sourcePort, string destinationPort);

    public static string PortName(IntPtr port)
    {
        return Marshal.PtrToStringAnsi(jack_port_name(port)) ?? string.Empty;
    }

    public static void Check(int result, string operation)
    {
        if (result != 0)
            throw new InvalidOperationException($"Could not {operation}. JACK error code {result}.");
    }
}

internal static class LinuxAudioDiscovery
{
    public static IEnumerable<string> InputPorts()
    {
        return PreferredPorts(JackPorts(":capture_", ":monitor_"), AudioPorts("pw-link", "-o"));
    }

    public static IEnumerable<string> OutputPorts()
    {
        return PreferredPorts(JackPorts(":playback_"), AudioPorts("pw-link", "-i"));
    }

    internal static IEnumerable<string> PreferredPorts(IEnumerable<string> jackPorts, IEnumerable<string> pipeWirePorts)
    {
        string[] preferred = jackPorts.Where(IsAudioPort).ToArray();
        return (preferred.Length > 0 ? preferred : pipeWirePorts)
            .Where(IsAudioPort)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(i => i, StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> JackPorts(params string[] portKinds)
    {
        return AudioPorts("jack_lsp", null)
            .Where(port => portKinds.Any(kind => port.Contains(kind, StringComparison.OrdinalIgnoreCase)));
    }

    private static IEnumerable<string> AudioPorts(string command, string? arguments)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(command, arguments ?? string.Empty)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process process = Process.Start(startInfo)!;
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(1000))
            {
                process.Kill(true);
                process.WaitForExit();
                return Array.Empty<string>();
            }
            _ = errorTask.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
                return Array.Empty<string>();

            string output = outputTask.GetAwaiter().GetResult();

            return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Trim())
                .Where(i => i.Length > 0)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static bool IsAudioPort(string port)
    {
        return !port.StartsWith("Midi-Bridge:", StringComparison.OrdinalIgnoreCase)
            && !port.StartsWith("v4l2_input.", StringComparison.OrdinalIgnoreCase)
            && !port.StartsWith("libcamera_input.", StringComparison.OrdinalIgnoreCase);
    }
}
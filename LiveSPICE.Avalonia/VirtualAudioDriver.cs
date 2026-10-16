using System;
using System.Threading;

namespace LiveSPICE.Avalonia;

public sealed class VirtualAudioDriver : Audio.Driver
{
    public VirtualAudioDriver()
    {
        devices.Add(new VirtualAudioDevice());
    }

    public override string Name => "LiveSPICE Virtual Audio";

    public override string ToString()
    {
        return Name;
    }
}

internal sealed class VirtualAudioChannel : Audio.Channel
{
    private readonly string name;

    public VirtualAudioChannel(string name)
    {
        this.name = name;
    }

    public override string Name => name;

    public override string ToString()
    {
        return name;
    }
}

internal sealed class VirtualAudioDevice : Audio.Device
{
    public VirtualAudioDevice() : base("Managed loopback")
    {
        inputs = new Audio.Channel[] { new VirtualAudioChannel("Input 1") };
        outputs = new Audio.Channel[] { new VirtualAudioChannel("Output 1") };
    }

    public override string ToString()
    {
        return Name;
    }

    public override Audio.Stream Open(Audio.Stream.SampleHandler callback, Audio.Channel[] input, Audio.Channel[] output)
    {
        return new VirtualAudioStream(callback, input, output);
    }
}

internal sealed class VirtualAudioStream : Audio.Stream
{
    private const int Rate = 48000;
    private const int BufferSize = 256;
    private readonly SampleHandler callback;
    private readonly Thread thread;
    private volatile bool stopped;

    public VirtualAudioStream(SampleHandler callback, Audio.Channel[] input, Audio.Channel[] output) : base(input, output)
    {
        this.callback = callback;
        thread = new Thread(Run) { Name = "Virtual Audio Stream", IsBackground = true };
        thread.Start();
    }

    public override double SampleRate => Rate;

    public override void Stop()
    {
        stopped = true;
        thread.Join();
    }

    private void Run()
    {
        using Audio.SampleBuffer input = new Audio.SampleBuffer(BufferSize);
        using Audio.SampleBuffer output = new Audio.SampleBuffer(BufferSize);
        Audio.SampleBuffer[] inputBuffers = InputChannels.Length == 0 ? Array.Empty<Audio.SampleBuffer>() : new[] { input };
        Audio.SampleBuffer[] outputBuffers = OutputChannels.Length == 0 ? Array.Empty<Audio.SampleBuffer>() : new[] { output };
        TimeSpan interval = TimeSpan.FromSeconds(BufferSize / (double)Rate);

        while (!stopped)
        {
            input.Clear();
            output.Clear();
            callback(BufferSize, inputBuffers, outputBuffers, Rate);
            Thread.Sleep(interval);
        }
    }
}
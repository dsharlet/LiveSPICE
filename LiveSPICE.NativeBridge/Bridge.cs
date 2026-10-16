using System.Runtime;
using System.Runtime.InteropServices;
using LiveSPICE.PluginCore;

namespace LiveSPICE.NativeBridge;

public static unsafe class Bridge
{
    private sealed class Instance(int maxBlock)
    {
        public readonly SimulationProcessor Processor = new();
        public readonly int MaxBlock = maxBlock;
        public readonly double[][] Input = { new double[maxBlock] };
        public readonly double[][] Output = { new double[maxBlock] };
    }

    private static Instance Get(nint handle) => (Instance)GCHandle.FromIntPtr(handle).Target!;

    [UnmanagedCallersOnly]
    public static int Create(byte* path, int sampleRate, int maxBlock, nint* handle)
    {
        try
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
            Instance instance = new(maxBlock);
            instance.Processor.SampleRate = sampleRate;
            instance.Processor.LoadSchematic(Marshal.PtrToStringUTF8((nint)path)!);
            // The first block only starts the background build; keep feeding silence until it lands so the
            // Expression compile and JIT happen here instead of on the audio thread.
            for (long start = Environment.TickCount64; !instance.Processor.IsReady;)
            {
                if (Environment.TickCount64 - start > 30000)
                    throw new TimeoutException("Building the simulation took longer than 30 s.");
                instance.Processor.RunSimulation(instance.Input, instance.Output, maxBlock);
                Thread.Sleep(10);
            }
            instance.Processor.RunSimulation(instance.Input, instance.Output, maxBlock);
            *handle = GCHandle.ToIntPtr(GCHandle.Alloc(instance));
            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return 1;
        }
    }

    [UnmanagedCallersOnly]
    public static int SetControl(nint handle, byte* name, double value)
    {
        try
        {
            string controlName = Marshal.PtrToStringUTF8((nint)name)!;
            switch (Get(handle).Processor.InteractiveComponents.FirstOrDefault(i => i.Name == controlName))
            {
                case PotWrapper pot:
                    pot.PotValue = value;
                    return 0;
                case DoubleThrowWrapper toggle:
                    if (toggle.Position != (int)value)
                        toggle.Engaged = !toggle.Engaged;
                    return 0;
                case MultiThrowWrapper selector:
                    selector.Position = (int)value;
                    return 0;
                default:
                    return 1;
            }
        }
        catch
        {
            return 1;
        }
    }

    [UnmanagedCallersOnly]
    public static int Process(nint handle, float* input, float* output, int n)
    {
        try
        {
            Instance instance = Get(handle);
            for (int offset = 0; offset < n; offset += instance.MaxBlock)
            {
                int count = Math.Min(instance.MaxBlock, n - offset);
                for (int i = 0; i < count; i++)
                    instance.Input[0][i] = input[offset + i];
                instance.Processor.RunSimulation(instance.Input, instance.Output, count);
                for (int i = 0; i < count; i++)
                    output[offset + i] = (float)instance.Output[0][i];
            }
            return 0;
        }
        catch
        {
            return 1;
        }
    }

    [UnmanagedCallersOnly]
    public static int Destroy(nint handle)
    {
        try
        {
            GCHandle.FromIntPtr(handle).Free();
            return 0;
        }
        catch
        {
            return 1;
        }
    }
}

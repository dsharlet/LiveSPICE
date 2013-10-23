using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Asio
{
    public class Stream : Audio.Stream
    {
        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        private class Buffer
        {
            private AsioWrapper.Buffers buffers;
            private SampleType type;
            private double[] samples;

            public AsioWrapper.Buffers Buffers { get { return buffers; } }
            public SampleType Type { get { return type; } }
            public double[] Samples { get { return samples; } }

            public Buffer(Channel C, int Count)
            {
                buffers = new AsioWrapper.Buffers(C.Index);
                type = (SampleType)C.Type;
                samples = new double[Count];
            }
        }

        private AsioWrapper.Asio instance;
        private Audio.Stream.SampleHandler callback;
        private Buffer input, output;

        private void OnBufferSwitch(int Index, bool Direct)
        {
            CopyMemory(output.Buffers.get_Buffer(Index), input.Buffers.get_Buffer(Index), (uint)output.Samples.Length);
            //SamplesToDouble(input.Type, input.Buffers.get_Buffer(Index), input.Samples);
            //callback(input.Samples, output.Samples);
            //DoubleToSamples(output.Type, output.Samples, output.Buffers.get_Buffer(Index));
        }

        private void OnSampleRateChange(double SampleRate)
        {

        }

        public Stream(AsioWrapper.Asio Instance, Audio.Stream.SampleHandler Callback, Channel Input, Channel Output, double Latency)
        {
            instance = Instance;
            callback = Callback;

            int size = instance.BufferSize.Preferred;
            input = new Buffer(Input, size);
            output = new Buffer(Output, size);

            instance.CreateBuffers(
                new AsioWrapper.Buffers[] { input.Buffers },
                new AsioWrapper.Buffers[] { output.Buffers }, 
                size, 
                OnBufferSwitch, 
                OnSampleRateChange);
            instance.Start();
        }

        public override void Stop()
        {
            instance.Stop();
            instance.DisposeBuffers();
        }

        private static void SamplesToDouble(SampleType Type, IntPtr In, double[] Out)
        {
            switch (Type)
            {
                //case SampleType.Int16MSB:
                //case SampleType.Int24MSB:
                //case SampleType.Int32MSB:
                //case SampleType.Float32MSB:
                //case SampleType.Float64MSB:
                //case SampleType.Int32MSB16:
                //case SampleType.Int32MSB18:
                //case SampleType.Int32MSB20:
                //case SampleType.Int32MSB24:
                case SampleType.Int16LSB: Audio.Util.LEi16ToLEf64(In, Out, Out.Length); return;
                //case SampleType.Int24LSB:
                case SampleType.Int32LSB: Audio.Util.LEi32ToLEf64(In, Out, Out.Length); return;
                //case SampleType.Float32LSB: 
                case SampleType.Float64LSB: Marshal.Copy(In, Out, 0, Out.Length); return;
                //case SampleType.Int32LSB16:
                //case SampleType.Int32LSB18:
                //case SampleType.Int32LSB20:
                //case SampleType.Int32LSB24:
                default: throw new NotImplementedException("Unsupported sample type");
            }
        }

        private static void DoubleToSamples(SampleType Type, double[] In, IntPtr Out)
        {
            switch (Type)
            {
                //case SampleType.Int16MSB:
                //case SampleType.Int24MSB:
                //case SampleType.Int32MSB:
                //case SampleType.Float32MSB:
                //case SampleType.Float64MSB:
                //case SampleType.Int32MSB16:
                //case SampleType.Int32MSB18:
                //case SampleType.Int32MSB20:
                //case SampleType.Int32MSB24:
                case SampleType.Int16LSB: Audio.Util.LEf64ToLEi16(In, Out, In.Length); return;
                //case SampleType.Int24LSB:
                case SampleType.Int32LSB: Audio.Util.LEf64ToLEi32(In, Out, In.Length); return;
                //case SampleType.Float32LSB:
                case SampleType.Float64LSB: Marshal.Copy(In, 0, Out, In.Length); return;
                //case SampleType.Int32LSB16:
                //case SampleType.Int32LSB18:
                //case SampleType.Int32LSB20:
                //case SampleType.Int32LSB24:
                default: throw new NotImplementedException("Unsupported sample type");
            }
        }

    }
}

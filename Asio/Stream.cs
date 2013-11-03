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
        private class Buffer
        {
            private AsioWrapper.Buffers buffers;
            private SampleType type;
            private Audio.SampleBuffer samples;

            public AsioWrapper.Buffers Buffers { get { return buffers; } }
            public SampleType Type { get { return type; } }
            public Audio.SampleBuffer Samples { get { return samples; } }

            public Buffer(Channel C, int Count)
            {
                buffers = new AsioWrapper.Buffers(C.Index);
                type = (SampleType)C.Type;
                samples = new Audio.SampleBuffer(Count);
            }
        }

        private double sampleRate;
        public override double SampleRate { get { return sampleRate; } }

        private AsioWrapper.Asio instance;
        private Audio.Stream.SampleHandler callback;
        private Buffer[] input;
        private Buffer[] output;

        private int buffer;

        private void OnBufferSwitch(int Index, bool Direct)
        {
            Audio.SampleBuffer[] a = new Audio.SampleBuffer[input.Length];
            for (int i = 0; i < input.Length; ++i)
            {
                a[i] = input[i].Samples;

                using(Audio.RawLock l = new Audio.RawLock(a[i], false, true))
                    ConvertSamples(
                        input[i].Buffers.get_Buffer(Index),
                        input[i].Type, 
                        l,
                        l.Count);
            }
            
            Audio.SampleBuffer[] b = new Audio.SampleBuffer[output.Length];
            for (int i = 0; i < output.Length; ++i)
                b[i] = output[i].Samples;

            callback(buffer, a, b, sampleRate);

            for (int i = 0; i < output.Length; ++i)
            {
                using (Audio.RawLock l = new Audio.RawLock(b[i], true, false))
                    ConvertSamples(
                        l,
                        output[i].Buffers.get_Buffer(Index),
                        output[i].Type,
                        l.Count);
            }
        }

        private void OnSampleRateChange(double SampleRate)
        {
            sampleRate = SampleRate;
        }

        public Stream(AsioWrapper.Asio Instance, Audio.Stream.SampleHandler Callback, Channel[] Input, Channel[] Output) : base(Input, Output)
        {
            instance = Instance;
            callback = Callback;

            buffer = instance.BufferSize.Preferred;
            input = Input.Select(i => new Buffer(i, buffer)).ToArray();
            output = Output.Select(i => new Buffer(i, buffer)).ToArray();

            instance.CreateBuffers(
                input.Select(i => i.Buffers).ToArray(),
                output.Select(i => i.Buffers).ToArray(),
                buffer, 
                OnBufferSwitch, 
                OnSampleRateChange);
            sampleRate = instance.SampleRate;
            instance.Start();
        }

        public override void Stop()
        {
            instance.Stop();
            instance.DisposeBuffers();
        }

        private static void ConvertSamples(IntPtr In, IntPtr Out, SampleType Type, int Count)
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
                case SampleType.Int16LSB: Audio.Util.LEf64ToLEi16(In, Out, Count); break;
                //case SampleType.Int24LSB:
                case SampleType.Int32LSB: Audio.Util.LEf64ToLEi32(In, Out, Count); break;
                case SampleType.Float32LSB: Audio.Util.LEf64ToLEf32(In, Out, Count); break;
                case SampleType.Float64LSB: Audio.Util.CopyMemory(Out, In, Count * sizeof(double)); break;
                //case SampleType.Int32LSB16:
                //case SampleType.Int32LSB18:
                //case SampleType.Int32LSB20:
                //case SampleType.Int32LSB24:
                default: throw new NotImplementedException("Unsupported sample type");
            }
        }

        private static void ConvertSamples(IntPtr In, SampleType Type, IntPtr Out, int Count)
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
                case SampleType.Int16LSB: Audio.Util.LEi16ToLEf64(In, Out, Count); break;
                //case SampleType.Int24LSB:
                case SampleType.Int32LSB: Audio.Util.LEi32ToLEf64(In, Out, Count); break;
                case SampleType.Float32LSB: Audio.Util.LEf32ToLEf64(In, Out, Count); break;
                case SampleType.Float64LSB: Audio.Util.CopyMemory(Out, In, Count * sizeof(double)); break;
                //case SampleType.Int32LSB16:
                //case SampleType.Int32LSB18:
                //case SampleType.Int32LSB20:
                //case SampleType.Int32LSB24:
                default: throw new NotImplementedException("Unsupported sample type");
            }
        }
    }
}

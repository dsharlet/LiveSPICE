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

        private double sampleRate;
        public override double SampleRate { get { return sampleRate; } }

        private AsioWrapper.Asio instance;
        private Audio.Stream.SampleHandler callback;
        private Buffer[] input;
        private Buffer[] output;

        private void OnBufferSwitch(int Index, bool Direct)
        {
            Audio.SampleBuffer[] a = new Audio.SampleBuffer[input.Length];
            for (int i = 0; i < input.Length; ++i)
                a[i] = Audio.SampleBuffer.NewInputBuffer(input[i].Buffers.get_Buffer(Index), AudioSampleType(input[i].Type), input[i].Samples);
            
            Audio.SampleBuffer[] b = new Audio.SampleBuffer[output.Length];
            for (int i = 0; i < output.Length; ++i)
                b[i] = Audio.SampleBuffer.NewOutputBuffer(output[i].Buffers.get_Buffer(Index), AudioSampleType(output[i].Type), output[i].Samples);

            callback(a, b, sampleRate);

            for (int i = 0; i < output.Length; ++i)
                b[i].SyncRaw();
        }

        private void OnSampleRateChange(double SampleRate)
        {
            sampleRate = SampleRate;
        }

        public Stream(AsioWrapper.Asio Instance, Audio.Stream.SampleHandler Callback, Channel[] Input, Channel[] Output)
        {
            instance = Instance;
            callback = Callback;

            int count = instance.BufferSize.Preferred;
            input = Input.Select(i => new Buffer(i, count)).ToArray();
            output = Output.Select(i => new Buffer(i, count)).ToArray();

            instance.CreateBuffers(
                input.Select(i => i.Buffers).ToArray(),
                output.Select(i => i.Buffers).ToArray(), 
                count, 
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

        private static Audio.SampleType AudioSampleType(SampleType Type)
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
                case SampleType.Int16LSB: return Audio.SampleType.i16;
                //case SampleType.Int24LSB:
                case SampleType.Int32LSB: return Audio.SampleType.i32;
                case SampleType.Float32LSB: return Audio.SampleType.f32;
                case SampleType.Float64LSB: return Audio.SampleType.f64;
                //case SampleType.Int32LSB16:
                //case SampleType.Int32LSB18:
                //case SampleType.Int32LSB20:
                //case SampleType.Int32LSB24:
                default: throw new NotImplementedException("Unsupported sample type");
            }
        }

        private static int SampleSize(SampleType Type)
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
                case SampleType.Int16LSB: return 2;
                //case SampleType.Int24LSB:
                case SampleType.Int32LSB: return 4;
                case SampleType.Float32LSB: return 4;
                case SampleType.Float64LSB: return 8;
                //case SampleType.Int32LSB16:
                //case SampleType.Int32LSB18:
                //case SampleType.Int32LSB20:
                //case SampleType.Int32LSB24:
                default: throw new NotImplementedException("Unsupported sample type");
            }
        }
    }
}

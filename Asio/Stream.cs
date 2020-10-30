using System;
using Util;

namespace Asio
{
    class Stream : Audio.Stream
    {
        private struct BufferInfo
        {
            private ASIOBufferInfo info;
            private ASIOSampleType type;

            public ASIOBufferInfo Info { get { return info; } }
            public ASIOSampleType Type { get { return type; } }

            public BufferInfo(ASIOBufferInfo Info, ASIOSampleType Type)
            {
                info = Info;
                type = Type;
            }
        }

        private double sampleRate;
        public override double SampleRate { get { return sampleRate; } }

        private AsioObject asio;
        private Audio.Stream.SampleHandler callback;
        private BufferInfo[] input;
        private BufferInfo[] output;
        private Audio.SampleBuffer[] inputBuffers;
        private Audio.SampleBuffer[] outputBuffers;

        private int bufferSize;

        private void OnBufferSwitch(int Index, ASIOBool Direct)
        {
            for (int i = 0; i < input.Length; ++i)
                ConvertSamples(input[i].Info.buffers[Index], input[i].Type, inputBuffers[i]);

            callback(bufferSize, inputBuffers, outputBuffers, sampleRate);

            for (int i = 0; i < output.Length; ++i)
                ConvertSamples(outputBuffers[i], output[i].Info.buffers[Index], output[i].Type);
        }

        private void OnSampleRateChange(double SampleRate)
        {
            sampleRate = SampleRate;
        }

        private int OnAsioMessage(ASIOMessageSelector selector, int value, IntPtr msg, IntPtr opt)
        {
            switch (selector)
            {
                case ASIOMessageSelector.SelectorSupported:
                    switch ((ASIOMessageSelector)Enum.ToObject(typeof(ASIOMessageSelector), value))
                    {
                        case ASIOMessageSelector.EngineVersion:
                            return 1;
                        default:
                            return 0;
                    }
                case ASIOMessageSelector.EngineVersion:
                    return 2;
                case ASIOMessageSelector.ResetRequest:
                    return 1;
                default:
                    return 0;
            }
        }

        private IntPtr OnBufferSwitchTimeInfo(IntPtr _params, int doubleBufferIndex, ASIOBool directProcess) { return _params; }

        public Stream(Guid DeviceId, Audio.Stream.SampleHandler Callback, Channel[] Input, Channel[] Output)
            : base(Input, Output)
        {
            Log.Global.WriteLine(MessageType.Info, "Instantiating ASIO stream with {0} input channels and {1} output channels.", Input.Length, Output.Length);
            asio = new AsioObject(DeviceId);
            asio.Init(IntPtr.Zero);
            callback = Callback;

            // Just use the driver's preferred buffer size.
            bufferSize = asio.BufferSize.Preferred;

            ASIOBufferInfo[] infos = new ASIOBufferInfo[Input.Length + Output.Length];
            for (int i = 0; i < Input.Length; ++i)
            {
                infos[i].isInput = ASIOBool.True;
                infos[i].channelNum = Input[i].Index;
            }
            for (int i = 0; i < Output.Length; ++i)
            {
                infos[Input.Length + i].isInput = ASIOBool.False;
                infos[Input.Length + i].channelNum = Output[i].Index;
            }

            ASIOCallbacks callbacks = new ASIOCallbacks()
            {
                bufferSwitch = OnBufferSwitch,
                sampleRateDidChange = OnSampleRateChange,
                asioMessage = OnAsioMessage,
                bufferSwitchTimeInfo = OnBufferSwitchTimeInfo
            };
            asio.CreateBuffers(infos, bufferSize, callbacks);

            // Create input buffers.
            input = new BufferInfo[Input.Length];
            inputBuffers = new Audio.SampleBuffer[Input.Length];
            for (int i = 0; i < Input.Length; ++i)
            {
                input[i] = new BufferInfo(infos[i], Input[i].Type);
                inputBuffers[i] = new Audio.SampleBuffer(bufferSize);
            }

            // Create output buffers.
            output = new BufferInfo[Output.Length];
            outputBuffers = new Audio.SampleBuffer[Output.Length];
            for (int i = 0; i < Output.Length; ++i)
            {
                output[i] = new BufferInfo(infos[Input.Length + i], Output[i].Type);
                outputBuffers[i] = new Audio.SampleBuffer(bufferSize);
            }

            sampleRate = asio.SampleRate;

            asio.Start();
        }

        public override void Stop()
        {
            asio.Stop();
            asio.DisposeBuffers();
            asio.Dispose();
            asio = null;
        }

        private static void ConvertSamples(Audio.SampleBuffer In, IntPtr Out, ASIOSampleType OutType)
        {
            switch (OutType)
            {
                //case ASIOSampleType.Int16MSB:
                //case ASIOSampleType.Int24MSB:
                //case ASIOSampleType.Int32MSB:
                //case ASIOSampleType.Float32MSB:
                //case ASIOSampleType.Float64MSB:
                //case ASIOSampleType.Int32MSB16:
                //case ASIOSampleType.Int32MSB18:
                //case ASIOSampleType.Int32MSB20:
                //case ASIOSampleType.Int32MSB24:
                case ASIOSampleType.Int16LSB: Audio.Util.LEf64ToLEi16(In.Raw, Out, In.Count); break;
                //case ASIOSampleType.Int24LSB:
                case ASIOSampleType.Int32LSB: Audio.Util.LEf64ToLEi32(In.Raw, Out, In.Count); break;
                case ASIOSampleType.Float32LSB: Audio.Util.LEf64ToLEf32(In.Raw, Out, In.Count); break;
                case ASIOSampleType.Float64LSB: Audio.Util.CopyMemory(Out, In.Raw, In.Count * sizeof(double)); break;
                //case ASIOSampleType.Int32LSB16:
                //case ASIOSampleType.Int32LSB18:
                //case ASIOSampleType.Int32LSB20:
                //case ASIOSampleType.Int32LSB24:
                default: throw new NotImplementedException("Unsupported sample type");
            }
        }

        private static void ConvertSamples(IntPtr In, ASIOSampleType InType, Audio.SampleBuffer Out)
        {
            switch (InType)
            {
                //case ASIOSampleType.Int16MSB:
                //case ASIOSampleType.Int24MSB:
                //case ASIOSampleType.Int32MSB:
                //case ASIOSampleType.Float32MSB:
                //case ASIOSampleType.Float64MSB:
                //case ASIOSampleType.Int32MSB16:
                //case ASIOSampleType.Int32MSB18:
                //case ASIOSampleType.Int32MSB20:
                //case ASIOSampleType.Int32MSB24:
                case ASIOSampleType.Int16LSB: Audio.Util.LEi16ToLEf64(In, Out.Raw, Out.Count); break;
                //case ASIOSampleType.Int24LSB:
                case ASIOSampleType.Int32LSB: Audio.Util.LEi32ToLEf64(In, Out.Raw, Out.Count); break;
                case ASIOSampleType.Float32LSB: Audio.Util.LEf32ToLEf64(In, Out.Raw, Out.Count); break;
                case ASIOSampleType.Float64LSB: Audio.Util.CopyMemory(Out.Raw, In, Out.Count * sizeof(double)); break;
                //case ASIOSampleType.Int32LSB16:
                //case ASIOSampleType.Int32LSB18:
                //case ASIOSampleType.Int32LSB20:
                //case ASIOSampleType.Int32LSB24:
                default: throw new NotImplementedException("Unsupported sample type");
            }
        }
    }
}

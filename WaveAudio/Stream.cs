using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Util;

namespace WaveAudio
{
    class Stream : Audio.Stream
    {
        private SampleHandler callback;
        private WAVEFORMATEX format;
        private WaveIn[] waveIn;
        private WaveOut[] waveOut;

        private Thread proc;

        public override double SampleRate { get { return format.nSamplesPerSec; } }

        private int buffer;

        public Stream(SampleHandler SampleCallback, Channel[] Input, Channel[] Output, double Latency) : base(Input, Output)
        {
            callback = SampleCallback;

            int Rate = 48000;
            int Bits = 16;
            int Channels = 1;
            format = new WAVEFORMATEX(Rate, Bits, Channels);

            buffer = (int)Math.Ceiling(Latency / 2 * Rate * Channels);
            waveIn = Input.Select(i => new WaveIn(i.Device, format, buffer)).ToArray();
            waveOut = Output.Select(i => new WaveOut(i.Device, format, buffer)).ToArray();

            proc = new Thread(new ThreadStart(Proc));
            proc.Start();
        }
        private volatile bool stop = false;

        public override void Stop()
        {
            stop = true;
            proc.Join();

            foreach (WaveIn i in waveIn)
                i.Stop();
            foreach (WaveOut i in waveOut)
                i.Stop();
        }

        private void Proc()
        {
            Thread.CurrentThread.Name = "WaveAudio Stream";

            try
            {
                Log.Global.WriteLine(MessageType.Info, "Entering streaming thread");

                Audio.SampleBuffer[] input = new Audio.SampleBuffer[waveIn.Length];
                Audio.SampleBuffer[] output = new Audio.SampleBuffer[waveOut.Length];
                while (!stop)
                {
                    // Read from the inputs.
                    for (int i = 0; i < waveIn.Length; ++i)
                    {
                        InBuffer b = null;
                        do
                        {
                            b = waveIn[i].GetBuffer();
                        } while (b == null && !stop);
                        if (b != null)
                        {
                            ConvertSamples(b.Data, format, b.Samples.Raw, b.Samples.Count);
                            b.Record();
                            input[i] = b.Samples;
                        }
                    }

                    // Get an available buffer from the outputs.
                    for (int i = 0; i < waveOut.Length; ++i)
                    {
                        OutBuffer b = null;
                        do
                        {
                            b = waveOut[i].GetBuffer();
                        } while (b == null && !stop);
                        if (b != null)
                            output[i] = b.Samples;
                    }

                    if (!stop)
                    {
                        Debug.Assert(input.All(i => i != null));
                        Debug.Assert(output.All(i => i != null));

                        // Call the callback.
                        callback(buffer, input, output, format.nSamplesPerSec);

                        // Play the results.
                        for (int i = 0; i < output.Length; ++i)
                        {
                            OutBuffer b = (OutBuffer)output[i].Tag;
                            ConvertSamples(b.Samples.Raw, b.Data, format, b.Samples.Count);
                            b.Play();
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                Log.Global.WriteLine(MessageType.Error, "Unhandled exception on streaming thread '{0}': {1}", Ex.GetType().FullName, Ex.ToString());
            }
            Log.Global.WriteLine(MessageType.Info, "Exiting streaming thread");
        }

        private static void ConvertSamples(IntPtr In, WAVEFORMATEX InFormat, IntPtr Out, uint Count)
        {
            switch (InFormat.wBitsPerSample)
            {
                case 16: Audio.Util.LEi16ToLEf64(In, Out, Count); break;
                case 32: Audio.Util.LEi32ToLEf64(In, Out, Count); break;
                default: throw new NotImplementedException("Unsupported sample type.");
            }
        }

        private static void ConvertSamples(IntPtr In, IntPtr Out, WAVEFORMATEX OutFormat, uint Count)
        {
            switch (OutFormat.wBitsPerSample)
            {
                case 16: Audio.Util.LEf64ToLEi16(In, Out, Count); break;
                case 32: Audio.Util.LEf64ToLEi32(In, Out, Count); break;
                default: throw new NotImplementedException("Unsupported sample type.");
            }
        }
    }
}

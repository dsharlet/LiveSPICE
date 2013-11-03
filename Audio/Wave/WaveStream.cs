using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace Audio
{
    class WaveStream : Stream
    {                     
        private SampleHandler callback;
        private WAVEFORMATEX format;
        private WaveIn[] waveIn;
        private WaveOut[] waveOut;
        
        private Thread proc;

        public override double SampleRate { get { return format.nSamplesPerSec; } }

        private int buffer;

        public WaveStream(SampleHandler SampleCallback, WaveChannel[] Input, WaveChannel[] Output, double Latency) : base(Input, Output)
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
            foreach (WaveIn i in waveIn)
                i.Stop();
            foreach (WaveOut i in waveOut)
                i.Stop();

            proc.Join();
        }

        private void Proc()
        {
            WaitHandle[] events = waveIn.Select(i => i.Callback).Concat(waveOut.Select(i => i.Callback)).ToArray();

            while (!stop)
            {
                //if (!WaitHandle.WaitAll(events, 100))
                //    continue;

                // Read from the inputs.
                SampleBuffer[] input = new SampleBuffer[waveIn.Length];
                for (int i = 0; i < waveIn.Length; ++i)
                {
                    WaveInBuffer b = waveIn[i].GetBuffer();
                    if (b == null)
                        return;
                    input[i] = b.NewSampleBuffer();
                }

                // Get an available buffer from the outputs.
                SampleBuffer[] output = new SampleBuffer[waveOut.Length];
                for (int i = 0; i < waveOut.Length; ++i)
                {
                    WaveOutBuffer b = waveOut[i].GetBuffer();
                    if (b == null)
                        return;
                    output[i] = b.NewSampleBuffer();
                }

                // Call the callback.
                callback(buffer, input, output, format.nSamplesPerSec);

                // Play the results.
                for (int i = 0; i < output.Length; ++i)
                {
                    output[i].SyncRaw();
                    ((WaveOutBuffer)output[i].Tag).Play();
                }
            }
        }
    }
}

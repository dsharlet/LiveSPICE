using System;
using System.Threading;

namespace LiveSPICE
{
    /// <summary>
    /// This is an audio stream class that just sends empty signals at the specified sample rate.
    /// </summary>
    public class NullStream : Audio.Stream
    {
        public override double SampleRate { get { return 48000; } }

        private readonly SampleHandler callback;

        private bool run = true;
        private Thread thread;

        public NullStream(SampleHandler Callback) : base(new Audio.Channel[] { }, new Audio.Channel[] { })
        {
            callback = Callback;
            thread = new Thread(Proc);
            thread.Start();
        }

        private void Proc()
        {
            Audio.SampleBuffer[] input = new Audio.SampleBuffer[] { };
            Audio.SampleBuffer[] output = new Audio.SampleBuffer[] { };

            long samples = 0;
            DateTime start = DateTime.Now;
            while (run)
            {
                // Run at ~50 callbacks/second. This doesn't need to be super precise. In
                // practice, Thread.Sleep is going to be +/- 10s of ms, but we'll still deliver
                // the right number of samples on average.
                Thread.Sleep(20);
                double elapsed = (DateTime.Now - start).TotalSeconds;
                int needed_samples = (int)(Math.Round(elapsed * SampleRate) - samples);
                callback(needed_samples, input, output, SampleRate);
                samples += needed_samples;
            }
        }

        public override void Stop()
        {
            run = false;
            thread.Join();
            thread = null;
        }
    }
}

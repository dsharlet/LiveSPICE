using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiveSPICE
{
    /// <summary>
    /// This is an audio stream class that just sends empty signals at the specified sample rate.
    /// </summary>
    public class NullStream : Audio.Stream
    {
        public override double SampleRate { get { return 48000; } }

        private SampleHandler callback;

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
            // Send 60 chunks/second. This code won't be perfectly accurate if 60 doesn't divide SampleRate.
            int count = (int)(SampleRate / 60);
            Audio.SampleBuffer[] input = new Audio.SampleBuffer[] { };
            Audio.SampleBuffer[] output = new Audio.SampleBuffer[] { };

            long t0 = Circuit.Timer.Counter;
            while (run)
            {
                long t1 = Circuit.Timer.Counter;
                if ((t1 - t0) / Circuit.Timer.Frequency > count / SampleRate)
                {
                    callback(count, input, output, SampleRate);
                    t0 = t1;
                }
                else
                {
                    Thread.Sleep(0);
                }
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

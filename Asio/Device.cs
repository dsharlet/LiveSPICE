using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asio
{
    public class Device : Audio.Device
    {
        private AsioWrapper instance;

        public override Audio.Channel[] InputChannels
        {
            get 
            { 
                int inputCount, outputCount;
                instance.getChannels(out inputCount, out outputCount);
                return new Audio.Channel[inputCount];
            }
        }

        public override Audio.Channel[] OutputChannels
        {
            get { throw new NotImplementedException(); }
        }

        public Device(string Name, AsioWrapper Instance) : base(Name) { instance = Instance; }

        public override Audio.Stream Open(Audio.Stream.SampleHandler Callback, Audio.Channel InputChannel, Audio.Channel OutputChannel, double SampleRate, int BitsPerSample, double Latency)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asio
{
    public class Channel : Audio.Channel
    {
        private int index;
        private string name;
        private AsioWrapper.SampleType type;
        public int Index { get { return index; } }
        public override string Name { get { return name; } }
        public AsioWrapper.SampleType Type { get { return type; } }

        public Channel(AsioWrapper.Channel Info)
        {
            index = Info.Index;
            name = Info.Name;
            type = Info.Type;
        }

        public override string ToString()
        {
            return name + " " + Enum.GetName(typeof(AsioWrapper.SampleType), type);
        }
    }

    public class Device : Audio.Device
    {
        private AsioWrapper.Asio instance;

        public override Audio.Channel[] InputChannels { get { return instance.InputChannels.Select(i => new Asio.Channel(i)).ToArray(); } }
        public override Audio.Channel[] OutputChannels { get { return instance.OutputChannels.Select(i => new Asio.Channel(i)).ToArray(); } }

        public Device(AsioWrapper.Asio Instance) : base(Instance.DriverName) { instance = Instance; }

        public override Audio.Stream Open(Audio.Stream.SampleHandler Callback, Audio.Channel Input, Audio.Channel Output, double Latency)
        {
            return new Stream(
                instance,
                Callback,
                (Channel)Input,
                (Channel)Output,
                Latency);
        }

        public override void ShowControlPanel() { instance.ShowControlPanel(); }
    }
}

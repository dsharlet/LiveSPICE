using System;
using System.Linq;

namespace Asio
{
    class Channel : Audio.Channel
    {
        private int index;
        private string name;
        private ASIOSampleType type;
        public int Index { get { return index; } }
        public override string Name { get { return name; } }
        public ASIOSampleType Type { get { return type; } }

        public Channel(ASIOChannelInfo Info)
        {
            index = Info.channel;
            name = Info.name;
            type = Info.type;
        }

        public override string ToString()
        {
            return name + " " + Enum.GetName(typeof(ASIOSampleType), type);
        }
    }

    class Device : Audio.Device
    {
        private Guid classid;

        public Device(Guid ClassId)
        {
            using (AsioObject obj = new AsioObject(ClassId))
            {
                obj.Init(IntPtr.Zero);
                name = obj.DriverName;
                inputs = obj.InputChannels.Select(i => new Asio.Channel(i)).ToArray();
                outputs = obj.OutputChannels.Select(i => new Asio.Channel(i)).ToArray();
            }
            classid = ClassId;
        }

        public override Audio.Stream Open(Audio.Stream.SampleHandler Callback, Audio.Channel[] Input, Audio.Channel[] Output)
        {
            return new Stream(
                classid,
                Callback,
                Input.Cast<Channel>().ToArray(),
                Output.Cast<Channel>().ToArray());
        }
    }
}

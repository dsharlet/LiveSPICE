namespace Audio
{
    public abstract class Channel
    {
        public abstract string Name { get; }
    };

    /// <summary>
    /// Devices describe the supported audio stream properties.
    /// </summary>
    public abstract class Device
    {
        protected string name;
        public string Name { get { return name; } }

        protected Channel[] inputs;
        public Channel[] InputChannels { get { return inputs; } }
        protected Channel[] outputs;
        public Channel[] OutputChannels { get { return outputs; } }

        protected Device() { }
        protected Device(string Name) { name = Name; }

        public abstract Stream Open(Stream.SampleHandler Callback, Channel[] Input, Channel[] Output);
    }
}

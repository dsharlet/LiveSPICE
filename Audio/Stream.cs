namespace Audio
{
    /// <summary>
    /// Base class for a running audio stream.
    /// </summary>
    public abstract class Stream
    {
        private Channel[] inputs, outputs;

        protected Stream(Channel[] Inputs, Channel[] Outputs) { inputs = Inputs; outputs = Outputs; }

        /// <summary>
        /// Handler for accepting new samples in and writing output samples out.
        /// </summary>
        /// <param name="Samples"></param>
        public delegate void SampleHandler(int Count, SampleBuffer[] In, SampleBuffer[] Out, double Rate);

        public Channel[] InputChannels { get { return inputs; } }
        public Channel[] OutputChannels { get { return outputs; } }

        public abstract double SampleRate { get; }

        public abstract void Stop();
    }
}

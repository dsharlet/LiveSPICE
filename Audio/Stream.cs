using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audio
{
    /// <summary>
    /// Base class for a running audio stream.
    /// </summary>
    public abstract class Stream
    {
        /// <summary>
        /// Handler for accepting new samples in and writing output samples out.
        /// </summary>
        /// <param name="Samples"></param>
        public delegate void SampleHandler(double[] Samples);

        public abstract void Stop();
    }
}

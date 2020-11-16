using System;
using System.Runtime.InteropServices;

namespace Audio
{
    /// <summary>
    /// This object defines a pinned sample array, suitable for sharing
    /// with native code.
    /// </summary>
    public class SampleBuffer : IDisposable
    {
        /// <summary>
        /// Number of samples contained in this buffer.
        /// </summary>
        public uint Count => (uint)Samples.Length;

        /// <summary>
        /// Samples in this buffer.
        /// </summary>
        public double[] Samples { get; private set; }
        private GCHandle pin;

        /// <summary>
        /// Access samples of this buffer.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public double this[int i] { get { return Samples[i]; } set { Samples[i] = value; } }

        /// <summary>
        /// Pointer to raw samples in this buffer.
        /// </summary>
        public IntPtr Raw { get { return pin.AddrOfPinnedObject(); } }

        private object tag = null;
        /// <summary>
        /// User defined tag object.
        /// </summary>
        public object Tag { get { return tag; } set { tag = value; } }

        public SampleBuffer(int Count)
        {
            Samples = new double[Count];
            pin = GCHandle.Alloc(Samples, GCHandleType.Pinned);
        }

        ~SampleBuffer() { Dispose(false); }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        private void Dispose(bool Disposing)
        {
            if (pin.IsAllocated)
                pin.Free();

            Samples = null;
        }

        /// <summary>
        /// Set this buffer to have the zero signal.
        /// </summary>
        public void Clear()
        {
            Util.ZeroMemory(Raw, Count * sizeof(double));
        }

        /// <summary>
        /// Amplify the samples in this buffer.
        /// </summary>
        /// <param name="Gain"></param>
        public double Amplify(double Gain)
        {
            return Util.Amplify(Raw, Count, Gain);
        }
    }
}

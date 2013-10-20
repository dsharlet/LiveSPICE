using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Audio
{
    /// <summary>
    /// Base class for a driver that can enumerate devices.
    /// </summary>
    public abstract class Driver
    {
        public static IEnumerable<Driver> Drivers
        {
            get { return new Driver[] { new WaveDriver() }; }
        }

        public abstract string Name { get; }
        public abstract IEnumerable<Device> Devices { get; }
    }
}

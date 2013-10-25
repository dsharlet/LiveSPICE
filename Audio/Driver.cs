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
        protected static List<Driver> drivers = new List<Driver>() 
        { 
            new WaveDriver(),
        };

        public static IEnumerable<Driver> Drivers { get { return drivers; } }

        public abstract string Name { get; }
        public abstract IEnumerable<Device> Devices { get; }
    }
}

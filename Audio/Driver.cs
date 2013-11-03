using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Audio
{
    /// <summary>
    /// Base class for a driver that can enumerate devices.
    /// </summary>
    public abstract class Driver
    {
        private static List<Driver> drivers = new List<Driver>();
        public static IEnumerable<Driver> Drivers 
        { 
            get 
            {
                foreach (Assembly i in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type j in i.DefinedTypes.Where(x => !drivers.Any(j => j.GetType() == x) && typeof(Driver).IsAssignableFrom(x)))
                    {
                        try
                        {
                            drivers.Add((Driver)Activator.CreateInstance(j));
                        }
                        catch (Exception) { }
                    }
                }
                return drivers;
            } 
        }
        
        public abstract string Name { get; }

        protected List<Device> devices = new List<Device>();
        public IEnumerable<Device> Devices { get { return devices; } }
    }
}

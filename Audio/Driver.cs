using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Util;

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
                foreach (Assembly i in AppDomain.CurrentDomain.GetAssemblies().Where(i => !i.IsDynamic))
                {
                    try
                    {
                        foreach (Type j in i.GetExportedTypes().Where(x => !x.IsAbstract && !drivers.Any(j => j.GetType() == x) && typeof(Driver).IsAssignableFrom(x)))
                        {
                            try
                            {
                                drivers.Add((Driver)Activator.CreateInstance(j));
                                Log.Global.WriteLine(MessageType.Info, "Loaded Audio implementation class '{0}'.", j.FullName);
                            }
                            catch (Exception Ex)
                            {
                                Log.Global.WriteLine(MessageType.Error, "Error instantiating Audio implementation class '{0}': {1}", j.FullName, Ex.Message);
                            }
                        }
                    } 
                    catch (Exception Ex)
                    {
                        Log.Global.WriteLine(MessageType.Error, "Error enumerating types in '{0}': {1}", i.FullName, Ex.Message);
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

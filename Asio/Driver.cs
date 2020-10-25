using Microsoft.Win32;
using System;
using Util;

namespace Asio
{
    public class Driver : Audio.Driver
    {
        public override string Name
        {
            get { return "ASIO"; }
        }

        public Driver()
        {
            // our settings are in the local machine
            using (RegistryKey lm = Registry.LocalMachine)
            // in the software/asio folder
            using (RegistryKey asio = lm.OpenSubKey("SOFTWARE\\ASIO"))
            {
                if (asio != null)
                {
                    string[] names = asio.GetSubKeyNames();

                    Log.Global.WriteLine(MessageType.Info, "Found {0} ASIO drivers.", names.Length);

                    foreach (string i in names)
                    {
                        Device d = null;
                        try
                        {
                            using (RegistryKey driver = asio.OpenSubKey(i))
                            {
                                d = new Device(new Guid((string)driver.GetValue("CLSID")));
                                Log.Global.WriteLine(MessageType.Info, "Loaded ASIO driver '{0}'.", i);
                            }
                        }
                        catch (Exception Ex)
                        {
                            Log.Global.WriteLine(MessageType.Warning, "Error instantiating ASIO driver '{0}': {1}", i, Ex.Message);
                        }
                        if (d != null)
                            devices.Add(d);
                    }
                }
                else
                {
                    Log.Global.WriteLine(MessageType.Info, "Found 0 ASIO drivers.");
                }
            }
        }
    }
}

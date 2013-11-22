using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

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
                string[] names = asio.GetSubKeyNames();

                foreach (string i in names)
                {
                    Device d = null;
                    try
                    {
                        using (RegistryKey driver = asio.OpenSubKey(i))
                        {
                            d = new Device(new Guid((string)driver.GetValue("CLSID")));
                        }
                    }
                    catch (System.Exception) { }
                    if (d != null)
                        devices.Add(d);
                }
            }
        }
    }
}

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

        public override IEnumerable<Audio.Device> Devices
        {
            get 
            {
                // our settings are in the local machine
                RegistryKey lm = Registry.LocalMachine;

                // in the software/asio folder
                RegistryKey asio = lm.OpenSubKey("SOFTWARE\\ASIO");

                string[] names = asio.GetSubKeyNames();

                foreach (string i in names)
                {
                    RegistryKey driver = asio.OpenSubKey(i);

                    string name = null;
                    object instance = null;
                    try
                    {
                        name = (string)driver.GetValue("Description");
                        if (name == null)
                            name = i;
                        Type T = Type.GetTypeFromCLSID(new Guid((string)driver.GetValue("CLSID")));
                        instance = Activator.CreateInstance(T);
                    }
                    catch (System.Exception) { }

                    driver.Close();

                    if (instance != null)
                        yield return new Device(name, new AsioWrapper(instance));
                }
                asio.Close();
                lm.Close();
            }
        }
    }
}

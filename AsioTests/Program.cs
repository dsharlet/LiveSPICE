using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsioTests
{
    class Program
    {
        private static void WriteChannel(Audio.Channel C)
        {
            System.Console.WriteLine(C.ToString());
        }

        [STAThread]
        static void Main(string[] args)
        {
            Asio.Driver driver = new Asio.Driver();
            
            foreach (Audio.Device i in driver.Devices)
            {
                try
                {
                    System.Console.WriteLine("----");
                    System.Console.WriteLine(i.Name);

                    System.Console.WriteLine("{0} input channels:", i.InputChannels.Count());
                    foreach (Audio.Channel j in i.InputChannels)
                        WriteChannel(j);

                    System.Console.WriteLine();
                    System.Console.WriteLine("{0} output channels:", i.OutputChannels.Count());
                    foreach (Audio.Channel j in i.OutputChannels)
                        WriteChannel(j);
                }
                catch (System.Exception ex)
                {
                    System.Console.WriteLine(ex.Message);
                }
            }
        }
    }
}

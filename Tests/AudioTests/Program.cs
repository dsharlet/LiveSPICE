using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace AsioTests
{
    class Program
    {
        private static void WriteChannel(Audio.Channel C)
        {
            System.Console.WriteLine(C.ToString());
        }

        private static void Callback(Audio.SampleBuffer In, Audio.SampleBuffer Out, double SampleRate)
        {
            for (int i = 0; i < In.Count; ++i)
                Out.Samples[i] = In.Samples[i];
        }

        [STAThread]
        static void Main(string[] args)
        {
            Audio.Driver[] drivers = Audio.Driver.Drivers.Concat(new Audio.Driver[] { new Asio.Driver() }).ToArray();
            System.Console.WriteLine("{0} driver", drivers.Length);
            for (int i = 0; i < drivers.Length; ++i)
                System.Console.WriteLine("{0}. {1}", i, drivers[i].Name);
            System.Console.Write("Select a driver: ");
            Audio.Driver driver = drivers[int.Parse(System.Console.ReadLine())];
            
            Audio.Device[] devices = driver.Devices.ToArray();
            System.Console.WriteLine("{0} devices", devices.Length);
            for (int i = 0; i < devices.Length; ++i)
                System.Console.WriteLine("{0}. {1}", i, devices[i].Name);
            System.Console.Write("Select a device: ");
            string line = System.Console.ReadLine();
            Audio.Device device = devices[int.Parse(line.Substring(0, 1))];
            if (line.EndsWith("!"))
            {
                device.ShowControlPanel();
                System.Console.ReadKey();
            }
            
            Audio.Channel[] inputs = device.InputChannels.ToArray();
            System.Console.WriteLine("{0} inputs", inputs.Length);
            for (int i = 0; i < inputs.Length; ++i)
                System.Console.WriteLine("{0}. {1}", i, inputs[i].Name);
            System.Console.Write("Select input: ");
            Audio.Channel input = inputs[int.Parse(System.Console.ReadLine())];

            Audio.Channel[] outputs = device.OutputChannels.ToArray();
            System.Console.WriteLine("{0} outputs", outputs.Length);
            for (int i = 0; i < outputs.Length; ++i)
                System.Console.WriteLine("{0}. {1}", i, outputs[i].Name);
            System.Console.Write("Select output: ");
            Audio.Channel output = outputs[int.Parse(System.Console.ReadLine())];

            System.Console.Write("Latency (ms): ");
            double latency = double.Parse(System.Console.ReadLine()) * 1e-3;

            System.Console.WriteLine("Running, press any key to exit...");

            Audio.Stream s = device.Open(Callback, input, output, latency);
            System.Console.ReadKey();
            s.Stop();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Circuit;
using SyMath;

namespace MetricTests
{
    class Program
    {
        static void Main(string[] args)
        {
            Tuple<double, string>[] tests = 
            {
                new Tuple<double, string>(1e-12, "+1 pV"),
                new Tuple<double, string>(1e-11, "+10 pV"),
                new Tuple<double, string>(1e-10, "+100 pV"),
                new Tuple<double, string>(1e-09, "+1 nV"),
                new Tuple<double, string>(1e-08, "+10 nV"),
                new Tuple<double, string>(1e-07, "+100 nV"),
                new Tuple<double, string>(1e-06, "+1 \u03BCV"),
                new Tuple<double, string>(1e-05, "+10 \u03BCV"),
                new Tuple<double, string>(1e-04, "+100 \u03BCV"),
                new Tuple<double, string>(1e-03, "+1 mV"),
                new Tuple<double, string>(1e-02, "+10 mV"),
                new Tuple<double, string>(1e-01, "+100 mV"),
                new Tuple<double, string>(1e+00, "+1 V"),
                new Tuple<double, string>(1e+01, "+10 V"),
                new Tuple<double, string>(1e+02, "+100 V"),
                new Tuple<double, string>(1e+03, "+1 kV"),
                new Tuple<double, string>(1e+04, "+10 kV"),
                new Tuple<double, string>(1e+05, "+100 kV"),
                new Tuple<double, string>(1e+06, "+1 MV"),
                new Tuple<double, string>(1e+07, "+10 MV"),
                new Tuple<double, string>(1e+08, "+100 MV"),
                new Tuple<double, string>(1e+09, "+1 GV"),
                new Tuple<double, string>(1e+10, "+10 GV"),
                new Tuple<double, string>(1e+11, "+100 GV"),

                new Tuple<double, string>(-1e-12, "-1 pV"),
                new Tuple<double, string>(-1e-11, "-10 pV"),
                new Tuple<double, string>(-1e-10, "-100 pV"),
                new Tuple<double, string>(-1e-09, "-1 nV"),
                new Tuple<double, string>(-1e-08, "-10 nV"),
                new Tuple<double, string>(-1e-07, "-100 nV"),
                new Tuple<double, string>(-1e-06, "-1 \u03BCV"),
                new Tuple<double, string>(-1e-05, "-10 \u03BCV"),
                new Tuple<double, string>(-1e-04, "-100 \u03BCV"),
                new Tuple<double, string>(-1e-03, "-1 mV"),
                new Tuple<double, string>(-1e-02, "-10 mV"),
                new Tuple<double, string>(-1e-01, "-100 mV"),
                new Tuple<double, string>(-1e+00, "-1 V"),
                new Tuple<double, string>(-1e+01, "-10 V"),
                new Tuple<double, string>(-1e+02, "-100 V"),
                new Tuple<double, string>(-1e+03, "-1 kV"),
                new Tuple<double, string>(-1e+04, "-10 kV"),
                new Tuple<double, string>(-1e+05, "-100 kV"),
                new Tuple<double, string>(-1e+06, "-1 MV"),
                new Tuple<double, string>(-1e+07, "-10 MV"),
                new Tuple<double, string>(-1e+08, "-100 MV"),
                new Tuple<double, string>(-1e+09, "-1 GV"),
                new Tuple<double, string>(-1e+10, "-10 GV"),
                new Tuple<double, string>(-1e+11, "-100 GV"),
            };

            foreach (Tuple<double, string> i in tests)
            {
                string s = Quantity.ToString(i.Item1, Units.V);
                if (s != i.Item2)
                    System.Console.WriteLine("{0} != {1}", s, i.Item2);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using SyMath;
using Circuit;
using LinqExpressions = System.Linq.Expressions;
using LinqExpression = System.Linq.Expressions.Expression;

// Filter design tool: http://sim.okawa-denshi.jp/en/CRtool.php

namespace CircuitTests
{
    class Program
    {        
        public static Expression VSt = "VS[t]";

        static void Main(string[] args)
        {
            Func<double, double> VS = ExprFunction.New("VS", "Sin[t*100*2*3.1415]", Component.t).Compile<Func<double, double>>();

            //Run(SeriesDiodeClipper(), VS);
            //Run(CommonCathodeTriodeAmp(), VS);
            Run(ToneStack(), VS);
            Run(MinimalRepro(), VS);
            Run(PassiveHighPassRC(), VS);
            Run(PassiveLowPassRLC(), VS);
            Run(PassiveBandPassRLC(), VS);
            Run(VoltageDivider(), VS);
            //Run(Potentiometer(), VS);
            Run(PassiveLowPassRL(), VS);
            Run(DiodeHalfClipper(), VS);
            Run(ParallelDiodeClipper(), VS);
            Run(Supernode(), VS);
            Run(MinimalMixedSystem(), VS);
            Run(PassiveLowPassRC(), VS);
            Run(NonInvertingAmplifier(), VS);
            //Run(InvertingAmplifier(), VS);
            Run(ActiveLowPassRC(), VS);
            Run(PassiveSecondOrderLowpassRC(), VS);
        }

        public static void Run(Circuit.Circuit Circuit, Func<double, double> VS)
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            System.Console.WriteLine(Circuit.Name);
            timer.Start();
            Simulation S = new Simulation(Circuit, new Quantity(48000, Units.Hz), 8, 8);
            System.Console.WriteLine("Build: {0} ms", timer.ElapsedMilliseconds);

            RunTest(S, VS, 48000, Circuit.Name);
        }

        public static void RunTest(Simulation S, Func<double, double> VS, int N, string Name)
        {            
            double t0 = (double)S.Time;
            
            Dictionary<Expression, double[]> input = new Dictionary<Expression, double[]>();
            double[] vs = new double[N];
            for (int n = 0; n < vs.Length; ++n)
                vs[n] = VS(n * S.TimeStep);
            input.Add(VSt, vs);

            Dictionary<Expression, double[]> output = S.Nodes.ToDictionary(i => i, i => new double[vs.Length]);
            
            // Ensure that the simulation is compiled before benchmarking.
            S.Process(1, input, output);
            S.Reset();

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            S.Process(vs.Length, input, output);
            timer.Stop();

            int t1 = 5000;

            Dictionary<Expression, List<Arrow>> plots = new Dictionary<Expression, List<Arrow>>();
            foreach (KeyValuePair<Expression, double[]> i in input.Concat(output))
                plots.Add(i.Key, i.Value.Take(t1).Select((j, n) => Arrow.New(n * S.TimeStep, j)).ToList());

            System.Console.WriteLine("Run: {0}x", (N * S.TimeStep) / ((double)timer.ElapsedMilliseconds / 1000.0));
            
            IEnumerable<double[]> series = input.Concat(output).Select(i => i.Value);
            Plot p = new Plot(
                Name, 
                800, 400, 
                t0, series.Min(i => i.Min()) * 1.25, 
                S.TimeStep * t1, series.Max(i => i.Max()) * 1.25, 
                plots.ToDictionary(i => i.Key.ToString(), i => (Plot.Series)new Plot.Scatter(i.Value)));
        }

        private static Circuit.Circuit CreateVoltageDivider(Expression V, TwoTerminal R1, TwoTerminal R2, string Name)
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = Name };

            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);
            
            Node Vin = new Node("Vin");
            Node Vo = new Node("Vo");
            Node Vg = new Node("Vg");
            C.Nodes = new NodeCollection() { Vin, Vo, Vg };

            C.Components.Add(R1);
            C.Components.Add(R2);

            VS.ConnectTo(Vin, Vg);
            R1.ConnectTo(Vin, Vo);
            R2.ConnectTo(Vg, Vo);
            G.ConnectTo(Vg);

            return C;
        }

        private static Circuit.Circuit VoltageDivider()
        {
            Resistor R1 = new Resistor() { Resistance = 10 };
            Resistor R2 = new Resistor() { Resistance = 10 };
            return CreateVoltageDivider(VSt, R1, R2, "Voltage divider");
        }

        private static Circuit.Circuit Potentiometer()
        {
            Potentiometer R1 = new Potentiometer() { Resistance = 20 };

            Circuit.Circuit C = new Circuit.Circuit() { Name = "Potentiometer" };

            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);
            
            Node Vin = new Node("Vin");
            Node Vo = new Node("Vo");
            Node Vg = new Node("Vg");
            C.Nodes = new NodeCollection() { Vin, Vo, Vg };

            C.Components.Add(R1);

            VS.ConnectTo(Vin, Vg);
            G.ConnectTo(Vg);

            R1.ConnectTo(Vin, Vg, Vo);

            return C;
        }

        /// <summary>
        /// http://en.wikipedia.org/wiki/Low-pass_filter#Passive_electronic_realization
        /// </summary>
        /// <returns></returns>
        private static Circuit.Circuit PassiveLowPassRC()
        {
            Resistor R1 = new Resistor() { Resistance = 10e3m };
            Capacitor C1 = new Capacitor() { Capacitance = 10e-7m };
            return CreateVoltageDivider(VSt, R1, C1, "Passive low-pass RC");
        }

        private static Circuit.Circuit PassiveHighPassRC()
        {
            Resistor R1 = new Resistor() { Resistance = 10e3m };
            Capacitor C1 = new Capacitor() { Capacitance = 10e-9m };
            return CreateVoltageDivider(VSt, C1, R1, "Passive high-pass RC");
        }

        private static Circuit.Circuit PassiveLowPassRL()
        {
            Resistor R1 = new Resistor() { Resistance = 10e3m };
            Inductor L1 = new Inductor() { Inductance = 10 };
            return CreateVoltageDivider(VSt, L1, R1, "Passive low-pass RL");
        }

        /// <summary>
        /// http://en.wikipedia.org/wiki/Operational_amplifier#Non-inverting_amplifier
        /// </summary>
        /// <returns></returns>
        public static Circuit.Circuit NonInvertingAmplifier()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Non-inverting amplifier" };

            Resistor R1 = new Resistor() { Resistance = 10 };
            Resistor R2 = new Resistor() { Resistance = 20 };
            
            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            IdealOpAmp A = new IdealOpAmp();

            Node Vp = new Node("Vp");
            Node Vn = new Node("Vn");
            Node Vo = new Node("Vo");
            Node Vg = new Node("Vg");
            C.Nodes = new NodeCollection() { Vp, Vn, Vo, Vg };

            C.Components.Add(R1);
            C.Components.Add(R2);
            C.Components.Add(A);

            VS.ConnectTo(Vp, Vg);
            G.ConnectTo(Vg);

            R1.ConnectTo(Vn, Vg);
            R2.ConnectTo(Vn, Vo);

            A.Positive.ConnectTo(Vp);
            A.Negative.ConnectTo(Vn);
            A.Out.ConnectTo(Vo);
                        
            return C;
        }

        /// <summary>
        /// http://en.wikipedia.org/wiki/Operational_amplifier#Inverting_amplifier
        /// </summary>
        /// <returns></returns>
        public static Circuit.Circuit InvertingAmplifier()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Inverting amplifier" };

            Resistor Rin = new Resistor() { Resistance = 10 };
            Resistor Rf = new Resistor() { Resistance = 20 };

            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            IdealOpAmp A = new IdealOpAmp();

            Node Vin = new Node("Vin");
            Node Vn = new Node("Vn");
            Node Vp = new Node("Vp");
            Node Vo = new Node("Vo");
            C.Nodes = new NodeCollection() { Vin, Vn, Vp, Vo };

            C.Components.Add(Rin);
            C.Components.Add(Rf);
            C.Components.Add(A);

            VS.ConnectTo(Vin, Vp);
            G.ConnectTo(Vp);

            Rin.ConnectTo(Vin, Vn);
            Rf.ConnectTo(Vn, Vo);

            A.Negative.ConnectTo(Vn);
            A.Positive.ConnectTo(Vp);
            A.Out.ConnectTo(Vo);
            
            return C;
        }

        /// <summary>
        /// http://en.wikipedia.org/wiki/Low-pass_filter#Active_electronic_realization
        /// </summary>
        /// <returns></returns>
        public static Circuit.Circuit ActiveLowPassRC()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Active low-pass" };

            Resistor R1 = new Resistor() { Resistance = 10e3m };
            Resistor R2 = new Resistor() { Resistance = 10e3m };
            Capacitor C1 = new Capacitor() { Capacitance = 10e-7m };

            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            IdealOpAmp A = new IdealOpAmp();

            Node Vin = new Node("Vin");
            Node Vn = new Node("Vn");
            Node Vp = new Node("Vp");
            Node Vo = new Node("Vo");
            C.Nodes = new NodeCollection() { Vin, Vn, Vp, Vo };

            C.Components.Add(R1);
            C.Components.Add(R2);
            C.Components.Add(C1);
            C.Components.Add(A);

            VS.ConnectTo(Vin, Vp);
            G.ConnectTo(Vp);

            R1.ConnectTo(Vin, Vn);
            R2.ConnectTo(Vn, Vo);
            C1.ConnectTo(Vn, Vo);

            A.Negative.ConnectTo(Vn);
            A.Positive.ConnectTo(Vp);
            A.Out.ConnectTo(Vo);

            return C;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static Circuit.Circuit PassiveSecondOrderLowpassRC()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Passive second-order low-pass (RC)" };

            Resistor R1 = new Resistor() { Resistance = 10e3m };
            Resistor R2 = new Resistor() { Resistance = 10e3m };
            Capacitor C1 = new Capacitor() { Capacitance = 1e-6m };
            Capacitor C2 = new Capacitor() { Capacitance = 1e-6m };

            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            Node Vin = new Node("Vin");
            Node Va = new Node("Va");
            Node Vo = new Node("Vo");
            Node Vg = new Node("Vg");
            C.Nodes = new NodeCollection() { Vin, Va, Vo, Vg };

            C.Components.Add(R1);
            C.Components.Add(R2);
            C.Components.Add(C1);
            C.Components.Add(C2);

            VS.ConnectTo(Vin, Vg);
            G.ConnectTo(Vg);

            R1.ConnectTo(Vin, Va);
            C1.ConnectTo(Va, Vg);
            R2.ConnectTo(Va, Vo);
            C2.ConnectTo(Vo, Vg);

            return C;
        }

        /// <summary>
        /// http://ecee.colorado.edu/~mathys/ecen2260/pdf/filters02.pdf
        /// </summary>
        /// <returns></returns>
        public static Circuit.Circuit PassiveLowPassRLC()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Passive lowpass (RLC)" };

            Resistor R1 = new Resistor() { Resistance = 100 };
            Inductor L1 = new Inductor() { Inductance = 100m };
            Capacitor C1 = new Capacitor() { Capacitance = 200e-9m };
            
            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            Node Vin = new Node("Vin");
            Node Va = new Node("Va");
            Node Vo = new Node("Vo");
            Node Vg = new Node("Vg");

            C.Nodes = new NodeCollection() { Vin, Va, Vo, Vg };

            C.Components.Add(R1);
            C.Components.Add(L1);
            C.Components.Add(C1);

            VS.ConnectTo(Vin, Vg);
            G.ConnectTo(Vg);

            R1.ConnectTo(Vin, Va);
            L1.ConnectTo(Va, Vo);
            C1.ConnectTo(Vo, Vg);

            return C;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static Circuit.Circuit PassiveBandPassRLC()
        {
            Resistor R1 = new Resistor() { Resistance = 100 };
            Inductor L1 = new Inductor() { Inductance = 50e-3m };
            Capacitor C1 = new Capacitor() { Capacitance = 200e-9m };

            Circuit.Circuit C = new Circuit.Circuit() { Name = "Passive bandpass (RLC)" };

            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            Node Vin = new Node("Vin");
            Node Vo = new Node("Vo");
            Node Vg = new Node("Vg");

            C.Nodes = new NodeCollection() { Vin, Vo, Vg };

            C.Components.Add(R1);
            C.Components.Add(L1);
            C.Components.Add(C1);

            VS.ConnectTo(Vin, Vg);
            G.ConnectTo(Vg);

            R1.ConnectTo(Vin, Vo);
            C1.ConnectTo(Vo, Vg);
            L1.ConnectTo(Vo, Vg);

            return C;
        }

        public static Circuit.Circuit MinimalRepro()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Minimal repro" };

            TwoTerminal R1 = new Resistor() { Resistance = 100 };
            TwoTerminal R2 = new Resistor() { Resistance = 100 };
            TwoTerminal C1 = new Capacitor() { Capacitance = 1e-5m };
            
            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            Node Va = new Node("Va");
            Node Vb = new Node("Vb");
            Node Vc = new Node("Vc");
            Node Vg = new Node("Vg");
            C.Nodes = new NodeCollection() { Va, Vb, Vc, Vg };

            C.Components.Add(R1);
            C.Components.Add(R2);
            C.Components.Add(C1);

            VS.ConnectTo(Va, Vg);
            G.ConnectTo(Vg);

            R1.ConnectTo(Va, Vb);
            C1.ConnectTo(Vb, Vc);
            R2.ConnectTo(Vc, Vg);

            return C;
        }

        public static Circuit.Circuit ToneStack()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Tone stack" };
            
            Potentiometer R1 = new Potentiometer() { Resistance = 250e3m };
            TwoTerminal R2 = new VariableResistor() { Resistance = 1e6m };
            Potentiometer R3 = new Potentiometer() { Resistance = 25e3m };
            Resistor R4 = new Resistor() { Resistance = 56e3m };
            TwoTerminal C1 = new Capacitor() { Capacitance = 0.25e-9m };
            TwoTerminal C2 = new Capacitor() { Capacitance = 20e-9m };
            TwoTerminal C3 = new Capacitor() { Capacitance = 20e-9m };
            
            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            Node Vin = new Node("Vin");
            Node Vo = new Node("Vo");
            Node Va = new Node("Va");
            Node Vb = new Node("Vb");
            Node Vc = new Node("Vc");
            Node Vd = new Node("Vd");
            Node Ve = new Node("Ve");
            Node Vg = new Node("Vg");

            C.Nodes = new NodeCollection() { Vin, Vo, Va, Vb, Vc, Vd, Ve, Vg };

            C.Components.Add(R1);
            C.Components.Add(R2);
            C.Components.Add(R3);
            C.Components.Add(R4);
            C.Components.Add(C1);
            C.Components.Add(C2);
            C.Components.Add(C3);

            VS.ConnectTo(Vin, Vg);
            G.ConnectTo(Vg);

            C1.ConnectTo(Vin, Va);
            R4.ConnectTo(Vin, Vb);
            R1.ConnectTo(Va, Vc, Vo);
            R2.ConnectTo(Vc, Vd);
            R3.ConnectTo(Vd, Vg, Ve);
            C2.ConnectTo(Vb, Vc);
            C3.ConnectTo(Vb, Ve);

            return C;
        }

        public static Circuit.Circuit CommonCathodeTriodeAmp()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Common cathode triode amp" };

            Resistor Ri = new Resistor() { Resistance = 1e6m };
            Resistor Rg = new Resistor() { Resistance = 70e3m };
            Resistor Rp = new Resistor() { Resistance = 100e3m };
            Resistor Rk = new Resistor() { Resistance = 1500 };
            Capacitor Ci = new Capacitor() { Capacitance = 0.047e-6m };
            Capacitor Cf = new Capacitor() { Capacitance = 2.5e-12m };
            Capacitor Ck = new Capacitor() { Capacitance = 25e-6m };
            VoltageSource VPP = new VoltageSource() { Voltage = 325 };
            Triode T = new Triode();

            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            Node Vin = new Node("Vin");
            Node Vp = new Node("Vp");

            Node Vg = new Node("Vg");
            Node Vgr = new Node("Vgr");
            Node Vk = new Node("Vk");
            Node Vx = new Node("Vx");
            Node Vpp = new Node("Vpp");

            C.Nodes = new NodeCollection() { Vin, Vp, Vg, Vk, Vx, Vpp, Vgr };

            C.Components.Add(Ci);
            C.Components.Add(Ri);
            C.Components.Add(Rg);
            C.Components.Add(Cf);
            C.Components.Add(Rp);
            C.Components.Add(Rk);
            C.Components.Add(Ck);
            C.Components.Add(VPP);
            C.Components.Add(T);

            VS.ConnectTo(Vin, Vgr);
            G.ConnectTo(Vgr);

            Ci.ConnectTo(Vin, Vx);
            Ri.ConnectTo(Vx, Vgr);
            Rg.ConnectTo(Vx, Vg);
            Cf.ConnectTo(Vp, Vg);
            Rp.ConnectTo(Vpp, Vp);
            VPP.ConnectTo(Vpp, Vgr);
            Rk.ConnectTo(Vk, Vgr);
            Ck.ConnectTo(Vk, Vgr);
            T.ConnectTo(Vp, Vg, Vk);

            return C;
        }


        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static Circuit.Circuit DiodeHalfClipper()
        {
            Diode D1 = new Diode();
            Resistor R1 = new Resistor() { Resistance = 100m };

            return CreateVoltageDivider(VSt, R1, D1, "Diode half wave clipper");
        }

        public static Circuit.Circuit ParallelDiodeClipper()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Parallel diode clipper" };

            Resistor R1 = new Resistor() { Resistance = 100 };
            Diode D1 = new Diode();
            Diode D2 = new Diode();
            
            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            Node Vin = new Node("Vin");
            Node Vo = new Node("Vo");
            Node Vg = new Node("Vg");

            C.Nodes = new NodeCollection() { Vin, Vo, Vg };

            C.Components.Add(R1);
            C.Components.Add(D1);
            C.Components.Add(D2);

            VS.ConnectTo(Vin, Vg);
            G.ConnectTo(Vg);

            R1.ConnectTo(Vin, Vo);
            D1.ConnectTo(Vo, Vg);
            D2.ConnectTo(Vg, Vo);

            return C;
        }

        public static Circuit.Circuit SeriesDiodeClipper()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Series diode clipper" };

            Resistor R1 = new Resistor() { Resistance = 100 };
            Diode D1 = new Diode();
            Diode D2 = new Diode();

            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            Node Vin = new Node("Vin");
            Node Vo = new Node("Vo");
            Node Va = new Node("Va");
            Node Vg = new Node("Vg");

            C.Nodes = new NodeCollection() { Vin, Vo, Va, Vg };

            C.Components.Add(R1);
            C.Components.Add(D1);
            C.Components.Add(D2);

            VS.ConnectTo(Vin, Vg);
            G.ConnectTo(Vg);

            R1.ConnectTo(Vin, Vo);
            D1.ConnectTo(Vo, Va);
            D2.ConnectTo(Va, Vg);

            return C;
        }

        /// <summary>
        /// Circuit with a minimal system of equations containing an algebraic node and differential node.
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static Circuit.Circuit MinimalMixedSystem()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Minimal Mixed System" };

            Resistor R1 = new Resistor() { Resistance = 1 };
            Resistor R2 = new Resistor() { Resistance = 1 };

            Capacitor C1 = new Capacitor() { Capacitance = 1 };
            
            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(G);

            Node Vin = new Node("Vin");
            Node Vo = new Node("Vo");
            Node Vg = new Node("Vg");
            Node Vx = new Node("Vx");

            C.Nodes = new NodeCollection() { Vin, Vo, Vg, Vx };

            C.Components.Add(R1);
            C.Components.Add(R2);
            C.Components.Add(C1);

            VS.ConnectTo(Vin, Vg);
            G.ConnectTo(Vg);

            R1.ConnectTo(Vin, Vo);
            R2.ConnectTo(Vo, Vx);
            C1.ConnectTo(Vx, Vg);

            return C;
        }

        /// <summary>
        /// Circuit with the voltage source not attached to ground.
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static Circuit.Circuit Supernode()
        {
            Circuit.Circuit C = new Circuit.Circuit() { Name = "Supernode" };

            Resistor R1 = new Resistor() { Resistance = 1 };
            Resistor R2 = new Resistor() { Resistance = 1 };
            Resistor R3 = new Resistor() { Resistance = 1 };
            Resistor R4 = new Resistor() { Resistance = 1 };
            
            VoltageSource VS = new VoltageSource() { Voltage = VSt };
            VoltageSource VSc = new VoltageSource() { Voltage = 2 };
            Ground G = new Ground();

            C.Components.Add(VS);
            C.Components.Add(VSc);
            C.Components.Add(G);

            Node Va = new Node("Va");
            Node Vb = new Node("Vb");
            Node Vc = new Node("Vc");
            Node Vo = new Node("Vo");
            Node Vg = new Node("Vg");

            C.Nodes = new NodeCollection() { Va, Vb, Vc, Vo, Vg };

            C.Components.Add(R1);
            C.Components.Add(R2);
            C.Components.Add(R3);
            C.Components.Add(R4);

            VS.ConnectTo(Va, Vb);
            VSc.ConnectTo(Vc, Vg);
            G.ConnectTo(Vg);

            R1.ConnectTo(Va, Vo);
            R2.ConnectTo(Vo, Vc);
            R3.ConnectTo(Vb, Vc);
            R4.ConnectTo(Vb, Vg);

            return C;
        }
    }
}

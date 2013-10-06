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
    class Test
    {
        public static Expression VSt = "VS[t]";

        protected string name;
        protected Circuit.Circuit circuit;

        public Test(string Name, Circuit.Circuit Circuit)
        {
            name = Name;
            circuit = Circuit;
        }

        public static void RunTest(Simulation S, Function VS, int N, string Name)
        {
            // Compile the VS expression for speed.
            Func<double, double> VS_ = VS.Compile<Func<double, double>>();
            
            double t0 = (double)S.Time;
            
            Dictionary<Expression, double[]> input = new Dictionary<Expression, double[]>();
            double[] vs = new double[N];
            for (int n = 0; n < vs.Length; ++n)
                vs[n] = VS_(n * S.TimeStep);
            input.Add("VS[t]", vs);

            Dictionary<Expression, double[]> output = new Dictionary<Expression, double[]>();
            double[] vout = new double[vs.Length];
            output.Add("Vo[t]", vout);
            
            // Ensure that the simulation is compiled before benchmarking.
            S.Process(1, input, output);
            S.Reset();

            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            S.Process(vs.Length, input, output);
            timer.Stop();

            Dictionary<Expression, List<Arrow>> plots = new Dictionary<Expression, List<Arrow>>();
            foreach (KeyValuePair<Expression, double[]> i in input.Concat(output))
                plots.Add(i.Key, i.Value.Take(5000).Select((j, n) => Arrow.New(n * S.TimeStep, j)).ToList());

            System.Console.WriteLine("Run: {0}x", (N * S.TimeStep) / ((double)timer.ElapsedMilliseconds / 1000.0));

            Plot p = new Plot(Name, 400, 400, t0, -2.0, S.TimeStep * 5000, 2.0, plots.ToDictionary(i => i.Key.ToString(), i => (Plot.Series)new Plot.Scatter(i.Value)));
        }

        public void Run()
        {
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            System.Console.WriteLine(name);
            timer.Start();
            Simulation S = new Simulation(circuit, new Quantity(48000, Units.Hz), 8, 8);
            System.Console.WriteLine("Build: {0} ms", timer.ElapsedMilliseconds);

            RunTest(S, ExprFunction.New("VS", "Sin[t*100*2*3.1415]", Component.t), 48000 * 10, name);
            //RunTest(S, ExprFunction.New("VS", "1", Component.t), 48000 * 10, name);
        }
    }

    class Program
    {        
        static void Main(string[] args)
        {
            //SeriesDiodeClipper(Test.VSt).Run();
            PassiveLowPassRLC(Test.VSt).Run();
            PassiveBandPassRLC(Test.VSt).Run();
            //ToneStack(Test.VSt).Run();
            //Triode(Test.VSt).Run();
            VoltageDivider(Test.VSt).Run();
            Potentiometer(Test.VSt).Run();
            PassiveLowPassRL(Test.VSt).Run();
            DiodeHalfClipper(Test.VSt).Run();
            ParallelDiodeClipper(Test.VSt).Run();
            Supernode(Test.VSt).Run();
            MinimalMixedSystem(Test.VSt).Run();
            PassiveLowPassRC(Test.VSt).Run();
            NonInvertingAmplifier(Test.VSt).Run();
            InvertingAmplifier(Test.VSt).Run();
            ActiveLowPassRC(Test.VSt).Run();
            PassiveSecondOrderLowpassRC(Test.VSt).Run();
        }

        private static Circuit.Circuit CreateVoltageDivider(Expression V, TwoTerminal R1, TwoTerminal R2)
        {
            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

        private static Test VoltageDivider(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 10 };
            Resistor R2 = new Resistor() { Resistance = 10 };
            return new Test("Voltage divider", CreateVoltageDivider(V, R1, R2));
        }

        private static Test Potentiometer(Expression V)
        {
            Potentiometer R1 = new Potentiometer() { Resistance = 20 };

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Potentiometer", C);
        }

        /// <summary>
        /// http://en.wikipedia.org/wiki/Low-pass_filter#Passive_electronic_realization
        /// </summary>
        /// <returns></returns>
        private static Test PassiveLowPassRC(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 10e3m };
            Capacitor C1 = new Capacitor() { Capacitance = 10e-7m };
            return new Test("Passive low-pass RC", CreateVoltageDivider(V, R1, C1));
        }

        private static Test PassiveLowPassRL(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 10e3m };
            Inductor L1 = new Inductor() { Inductance = 10 };
            return new Test("Passive low-pass RL", CreateVoltageDivider(V, L1, R1));
        }

        /// <summary>
        /// http://en.wikipedia.org/wiki/Operational_amplifier#Non-inverting_amplifier
        /// </summary>
        /// <returns></returns>
        public static Test NonInvertingAmplifier(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 10 };
            Resistor R2 = new Resistor() { Resistance = 20 };

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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
                        
            return new Test("Non-inverting amplifier", C);
        }

        /// <summary>
        /// http://en.wikipedia.org/wiki/Operational_amplifier#Inverting_amplifier
        /// </summary>
        /// <returns></returns>
        public static Test InvertingAmplifier(Expression V)
        {
            Resistor Rin = new Resistor() { Resistance = 10 };
            Resistor Rf = new Resistor() { Resistance = 20 };

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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
            
            return new Test("Inverting amplifier", C);
        }

        /// <summary>
        /// http://en.wikipedia.org/wiki/Low-pass_filter#Active_electronic_realization
        /// </summary>
        /// <returns></returns>
        public static Test ActiveLowPassRC(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 10e3m };
            Resistor R2 = new Resistor() { Resistance = 10e3m };
            Capacitor C1 = new Capacitor() { Capacitance = 10e-7m };

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Active low-pass", C);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static Test PassiveSecondOrderLowpassRC(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 10e3m };
            Resistor R2 = new Resistor() { Resistance = 10e3m };
            Capacitor C1 = new Capacitor() { Capacitance = 1e-6m };
            Capacitor C2 = new Capacitor() { Capacitance = 1e-6m };

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Passive second-order low-pass (RC)", C);
        }

        /// <summary>
        /// http://ecee.colorado.edu/~mathys/ecen2260/pdf/filters02.pdf
        /// </summary>
        /// <returns></returns>
        public static Test PassiveLowPassRLC(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 100 };
            Inductor L1 = new Inductor() { Inductance = 100m };
            Capacitor C1 = new Capacitor() { Capacitance = 200e-9m };

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Passive lowpass (RLC)", C);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static Test PassiveBandPassRLC(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 100 };
            Inductor L1 = new Inductor() { Inductance = 50e-3m };
            Capacitor C1 = new Capacitor() { Capacitance = 200e-9m };

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Passive bandpass (RLC)", C);
        }

        public static Test ToneStack(Expression V)
        {
            //Potentiometer R1 = new Potentiometer(2);
            //VariableResistor R2 = new VariableResistor(2);
            //Potentiometer R3 = new Potentiometer(2);
            //Resistor R4 = new Resistor(1);
            //Capacitor C1 = new Capacitor(1);
            //Capacitor C2 = new Capacitor(1);
            //Capacitor C3 = new Capacitor(1);

            Potentiometer R1 = new Potentiometer() { Resistance = 250e3m };
            VariableResistor R2 = new VariableResistor() { Resistance = 1e6m };
            Potentiometer R3 = new Potentiometer() { Resistance = 25e3m };
            Resistor R4 = new Resistor() { Resistance = 56e3m };
            Capacitor C1 = new Capacitor() { Capacitance = 0.25e-9m };
            Capacitor C2 = new Capacitor() { Capacitance = 20e-9m };
            Capacitor C3 = new Capacitor() { Capacitance = 20e-9m };

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Tone stack", C);
        }

        public static Test Triode(Expression V)
        {
            Resistor Ri = new Resistor() { Resistance = 1e6m };
            Resistor Rg = new Resistor() { Resistance = 70e3m };
            Resistor Rp = new Resistor() { Resistance = 100e3m };
            Resistor Rk = new Resistor() { Resistance = 1500 };
            Capacitor Ci = new Capacitor() { Capacitance = 0.047e-6m };
            Capacitor Cf = new Capacitor() { Capacitance = 2.5e-12m };
            Capacitor Ck = new Capacitor() { Capacitance = 25e-6m };
            VoltageSource VPP = new VoltageSource() { Voltage = 325 };
            Triode T = new Triode();

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Triode", C);
        }


        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static Test DiodeHalfClipper(Expression V)
        {
            Diode D1 = new Diode();
            Resistor R1 = new Resistor() { Resistance = 100m };

            return new Test("Diode half wave clipper", CreateVoltageDivider(V, R1, D1));
        }

        public static Test ParallelDiodeClipper(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 100 };
            Diode D1 = new Diode();
            Diode D2 = new Diode();

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Parallel diode clipper", C);
        }

        public static Test SeriesDiodeClipper(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 100 };
            Diode D1 = new Diode();
            Diode D2 = new Diode();

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Series diode clipper", C);
        }

        /// <summary>
        /// Circuit with a minimal system of equations containing an algebraic node and differential node.
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static Test MinimalMixedSystem(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 1 };
            Resistor R2 = new Resistor() { Resistance = 1 };

            Capacitor C1 = new Capacitor() { Capacitance = 1 };

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Minimal Mixed System", C);
        }

        /// <summary>
        /// Circuit with the voltage source not attached to ground.
        /// </summary>
        /// <param name="V"></param>
        /// <returns></returns>
        public static Test Supernode(Expression V)
        {
            Resistor R1 = new Resistor() { Resistance = 1 };
            Resistor R2 = new Resistor() { Resistance = 1 };
            Resistor R3 = new Resistor() { Resistance = 1 };
            Resistor R4 = new Resistor() { Resistance = 1 };

            Circuit.Circuit C = new Circuit.Circuit();

            VoltageSource VS = new VoltageSource() { Voltage = V };
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

            return new Test("Supernode", C);
        }
    }
}

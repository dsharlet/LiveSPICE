using System;
using System.Collections.Generic;
using System.ComponentModel;
using ComputerAlgebra;

namespace Circuit
{
    public enum TriodeModel
    {
        ChildLangmuir,
        DempwolfZolzer,
        Koren,
    }

    /// <summary>
    /// Base class for a triode.
    /// </summary>
    [Category("Vacuum Tubes")]
    [DisplayName("Triode")]
    public class Triode : Component
    {
        protected TriodeModel model;
        [Serialize, Description("Model implementation to use")]
        public TriodeModel Model { get { return model; } set { model = value; NotifyChanged(nameof(Model)); } }

        private bool simulateCapacitances;

        [Serialize, Category("Dempwolf-Zolzer")]
        public bool SimulateCapacitances { get { return simulateCapacitances; } set { simulateCapacitances = value; NotifyChanged(nameof(SimulateCapacitances)); } }

        private double mu = 100.0;
        [Serialize, Description("Voltage gain.")]
        public double Mu { get { return mu; } set { mu = value; NotifyChanged(nameof(Mu)); } }

        protected double k_ = 1.73e-6;
        [Serialize, Description("Generalized perveance."), Category("Child-Langmuir")]
        public double K { get { return k_; } set { k_ = value; NotifyChanged(nameof(K)); } }

        private double ex = 1.4;
        [Serialize, Category("Koren")]
        public double Ex { get { return ex; } set { ex = value; NotifyChanged(nameof(Ex)); } }

        private double kg = 1060.0;
        [Serialize, Category("Koren")]
        public double Kg { get { return kg; } set { kg = value; NotifyChanged(nameof(Kg)); } }

        private double kp = 600.0;
        [Serialize, Category("Koren")]
        public double Kp { get { return kp; } set { kp = value; NotifyChanged(nameof(Kp)); } }

        private double kvb = 300;
        [Serialize, Category("Koren")]
        public double Kvb { get { return kvb; } set { kvb = value; NotifyChanged(nameof(Kvb)); } }

        private Quantity rgk = new Quantity(1e6, Units.Ohm);
        [Serialize, Category("Koren")]
        public Quantity Rgk { get { return rgk; } set { if (rgk.Set(value)) NotifyChanged(nameof(Rgk)); } }

        private Quantity kn = new Quantity(.5, Units.V);
        [Serialize, Category("Koren"), Description("Knee size")]
        public Quantity Kn { get { return kn; } set { if (kn.Set(value)) NotifyChanged(nameof(Kn)); } }

        private Quantity vg = new Quantity(0.33, Units.V);
        [Serialize, Category("Koren")]
        public Quantity Vg { get { return vg; } set { if (vg.Set(value)) NotifyChanged(nameof(Vg)); } }

        private double gamma = 1.26;
        [Serialize, Category("Dempwolf-Zolzer")]
        public double Gamma { get { return gamma; } set { gamma = value; NotifyChanged(nameof(Gamma)); } }

        private double _g = 2.242E-3;
        [Serialize, Category("Dempwolf-Zolzer")]
        public double G { get { return _g; } set { _g = value; NotifyChanged(nameof(G)); } }

        private double gg = 6.177E-4;
        [Serialize, Category("Dempwolf-Zolzer")]
        public double Gg { get { return gg; } set { gg = value; NotifyChanged(nameof(Gg)); } }

        private double c = 3.4;
        [Serialize, Category("Dempwolf-Zolzer")]
        public double C { get { return c; } set { c = value; NotifyChanged(nameof(C)); } }

        private double cg = 9.901;
        [Serialize, Category("Dempwolf-Zolzer")]
        public double Cg { get { return cg; } set { cg = value; NotifyChanged(nameof(Cg)); } }

        private double xi = 1.314;
        [Serialize, Category("Dempwolf-Zolzer")]
        public double Xi { get { return xi; } set { xi = value; NotifyChanged(nameof(Xi)); } }

        private Quantity ig0 = new Quantity(8.025E-8, Units.A);
        [Serialize, Category("Dempwolf-Zolzer")]
        public Quantity Ig0 { get { return ig0; } set { ig0 = value; NotifyChanged(nameof(Ig0)); } }

        private Quantity _cgp = new Quantity(2.4e-12m, Units.F);
        [Serialize, Description("Grid to plate capacitance.")]
        public Quantity Cgp { get { return _cgp; } set { _cgp = value; NotifyChanged(nameof(Cgp)); } }

        private Quantity _cgk = new Quantity(2.3e-12m, Units.F);
        [Serialize, Description("Grid to cathode capacitance.")]
        public Quantity Cgk { get { return _cgk; } set { _cgk = value; NotifyChanged(nameof(Cgk)); } }

        private Quantity _cpk = new Quantity(9e-13m, Units.F);
        [Serialize, Description("Plate to cathode capacitance.")]
        public Quantity Cpk { get { return _cpk; } set { _cpk = value; NotifyChanged(nameof(Cpk)); } }


        private Terminal p, g, k;
        public override IEnumerable<Terminal> Terminals
        {
            get
            {
                yield return p;
                yield return g;
                yield return k;
            }
        }
        [Browsable(false)]
        public Terminal Plate { get { return p; } }
        [Browsable(false)]
        public Terminal Grid { get { return g; } }
        [Browsable(false)]
        public Terminal Cathode { get { return k; } }

        public Triode()
        {
            p = new Terminal(this, "P");
            g = new Terminal(this, "G");
            k = new Terminal(this, "K");
            Name = "V1";
        }

        public override void Analyze(Analysis Mna)
        {
            Expression Vpk = p.V - k.V;
            Expression Vgk = g.V - k.V;

            Expression ip, ig, ik;
            switch (model)
            {
                case TriodeModel.ChildLangmuir:
                    Expression Ed = Mu * Vgk + Vpk;
                    ip = Call.If(Ed > 0, K * Binary.Power(Ed, 1.5), 0);
                    ig = 0;
                    ik = -(ip + ig);
                    break;
                case TriodeModel.Koren:
                    Expression E1 = Ln1Exp(Kp * (1.0 / Mu + Vgk * Binary.Power(Kvb + Vpk * Vpk, -0.5))) * Vpk / Kp;
                    ip = Mna.AddUnknownEqualTo(Call.If(E1 > 0, 2d * (E1 ^ Ex) / Kg, 0));

                    var vg = (Real)Vg;
                    var knee = (Real)Kn;
                    var rg1 = (Real)Rgk;

                    var a = 1 / (4 * knee * rg1);
                    var b = (knee - vg) / (2 * knee * rg1);
                    var c = (-a * Binary.Power(vg - knee, 2)) - (b * (vg - knee));

                    ig = Mna.AddUnknownEqualTo(Call.If(Vgk < vg - knee, 0, Call.If(Vgk > vg + knee, (Vgk - vg) / rg1, a * Vgk * Vgk + b * Vgk + c)));
                    ik = -(ip + ig);
                    break;
                case TriodeModel.DempwolfZolzer:
                    Expression exg = Cg * Vgk;
                    ig = Call.If(exg > -50, Gg * Binary.Power(Ln1Exp(exg) / Cg, Xi), 0) + Ig0;
                    Expression exk = C * ((Vpk / Mu) + Vgk);
                    ik = Call.If(exk > -50, -G * Binary.Power(Ln1Exp(exk) / C, Gamma), 0);
                    ip = -(ik + ig);
                    break;
                default:
                    throw new NotImplementedException("Triode model " + model.ToString());
            }

            if (SimulateCapacitances)
            {
                Capacitor.Analyze(Mna, Name + "_cgp", p, g, _cgp);
                Capacitor.Analyze(Mna, Name + "_cgk", g, k, _cgk);
                Capacitor.Analyze(Mna, Name + "_cpk", p, k, _cpk);
            }

            Mna.AddTerminal(p, ip);
            Mna.AddTerminal(g, ig);
            Mna.AddTerminal(k, ik);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(p, new Coord(0, 20), new Coord(0, 5));
            Sym.AddWire(new Coord(-10, 5), new Coord(10, 5));

            Sym.AddTerminal(g, new Coord(-20, 0), new Coord(-12, 0));
            for (int i = -10; i < 10; i += 8)
                Sym.AddWire(new Coord(i, 0), new Coord(i + 4, 0));

            Sym.AddTerminal(k, new Coord(-10, -20), new Coord(-10, -7), new Coord(-8, -5), new Coord(8, -5), new Coord(10, -7));

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);

            if (PartNumber != null)
                Sym.DrawText(() => PartNumber, new Coord(-2, 20), Alignment.Far, Alignment.Near);
            Sym.DrawText(() => Name, new Point(-8, -20), Alignment.Near, Alignment.Far);
        }


        // ln(1+e^x) = x for large x, and large x causes numerical issues.
        private static Expression Ln1Exp(Expression x)
        {
            return Call.If(x > 50, x, Call.Ln(1 + Call.Exp(x)));
        }
    }

    // Deprecated triode classes.
    [Obsolete]
    public class KorenTriode : Triode
    {
        public KorenTriode() { model = TriodeModel.Koren; }
    }

    [Obsolete]
    public class ChildLangmuirTriode : Triode
    {
        public ChildLangmuirTriode() { model = TriodeModel.ChildLangmuir; }
    }
}

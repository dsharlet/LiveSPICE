using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    public enum TriodeModel
    {
        ChildLangmuir,
        DempwolfZolzer,

        // TODO: This model doesn't work very well. Not sure if it's a bug or the model is bad.
        [Browsable(false)] Koren,
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
        [Serialize, Category("Koren"), Browsable(false)]
        public double Ex { get { return ex; } set { ex = value; NotifyChanged(nameof(Ex)); } }

        private double kg = 1060.0;
        [Serialize, Category("Koren"), Browsable(false)]
        public double Kg { get { return kg; } set { kg = value; NotifyChanged(nameof(Kg)); } }

        private double kp = 600.0;
        [Serialize, Category("Koren"), Browsable(false)]
        public double Kp { get { return kp; } set { kp = value; NotifyChanged(nameof(Kp)); } }

        private double kvb = 300;
        [Serialize, Category("Koren"), Browsable(false)]
        public double Kvb { get { return kvb; } set { kvb = value; NotifyChanged(nameof(Kvb)); } }

        private Quantity rgk = new Quantity(1e6, Units.Ohm);
        [Serialize, Category("Koren"), Browsable(false)]
        public Quantity Rgk { get { return rgk; } set { if (rgk.Set(value)) NotifyChanged(nameof(Rgk)); } }

        private Quantity vg = new Quantity(0.33, Units.V);
        [Serialize, Category("Koren"), Browsable(false)]
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
            //Vpk = Mna.AddUnknownEqualTo(Name + "pk", Vpk);
            //Vgk = Mna.AddUnknownEqualTo(Name + "gk", Vgk);

            Expression ip, ig, ik;
            switch (model)
            {
                case TriodeModel.ChildLangmuir:
                    Expression Ed = Mu * Vgk + Vpk;
                    ip = Call.If(Ed > 0, K * (Ed ^ 1.5), 0);
                    //ip = Mna.AddUnknownEqualTo("i" + Name + "p", ip);
                    ig = 0;
                    ik = -(ip + ig);
                    break;
                case TriodeModel.Koren:
                    Expression E1 = Ln1Exp(Kp * (1.0 / Mu + Vgk * Binary.Power(Kvb + Vpk * Vpk, -0.5))) * Vpk / Kp;
                    ip = Call.If(E1 > 0, (E1 ^ Ex) / Kg, 0);
                    ig = Call.Max(Vgk - Vg, 0) / Rgk;
                    //ip = Mna.AddUnknownEqualTo("i" + Name + "p", ip);
                    //ig = Mna.AddUnknownEqualTo("i" + Name + "g", ig);
                    ik = -(ip + ig);
                    break;
                case TriodeModel.DempwolfZolzer:
                    Expression exg = Cg * Vgk;
                    ig = Call.If(exg > -50, Gg * Binary.Power(Ln1Exp(exg) / Cg, Xi), 0) + Ig0;
                    Expression exk = C * ((Vpk / Mu) + Vgk);
                    ik = Call.If(exk > -50, -G * Binary.Power(Ln1Exp(exk) / C, Gamma), 0);
                    //ig = Mna.AddUnknownEqualTo("i" + Name + "g", ig);
                    //ik = Mna.AddUnknownEqualTo("i" + Name + "k", ik);
                    if (SimulateCapacitances)
                    {
                        Capacitor.Analyze(Mna, Name + "_cpg", p, g, 2.4e-12);
                        Capacitor.Analyze(Mna, Name + "_cgk", g, k, 2.3e-12);
                        //Capacitor.Analyze(Mna, Name + "_cpk", p, k, .9e-12);
                    }
                    ip = -(ik + ig);
                    break;
                default:
                    throw new NotImplementedException("Triode model " + model.ToString());
            }
            Mna.AddTerminal(p, ip);
            Mna.AddTerminal(g, ig);
            Mna.AddTerminal(k, ik);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(p, new Coord(0, 20), new Coord(0, 4));
            Sym.AddWire(new Coord(-10, 4), new Coord(10, 4));

            Sym.AddTerminal(g, new Coord(-20, 0), new Coord(-12, 0));
            for (int i = -8; i < 16; i += 8)
                Sym.AddWire(new Coord(i, 0), new Coord(i + 4, 0));

            Sym.AddTerminal(k, new Coord(-10, -20), new Coord(-10, -6), new Coord(-8, -4), new Coord(8, -4), new Coord(10, -6));

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);

            if (PartNumber != null)
                Sym.DrawText(() => PartNumber, new Coord(-2, 20), Alignment.Far, Alignment.Near);
            Sym.DrawText(() => Name, new Point(-8, -20), Alignment.Near, Alignment.Far);
        }


        // ln(1+e^x) = x for large x, and large x causes numerical issues.
        private static Expression Ln1Exp(Expression x)
        {
            //return Call.Ln(1 + Call.Exp(x));

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

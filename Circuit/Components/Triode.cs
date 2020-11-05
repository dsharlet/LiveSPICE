using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    public enum TriodeModel
    {
        ChildLangmuir,
        // TODO: This model is broken.
        [Browsable(false)] Koren,
        DempwolfZolzer
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
            Expression Vpk = Mna.AddUnknownEqualTo(Name + "pk", p.V - k.V);
            Expression Vgk = Mna.AddUnknownEqualTo(Name + "gk", g.V - k.V);

            Expression ip, ig;
            switch (model)
            {
                case TriodeModel.ChildLangmuir:
                    Expression Ed = Mu * Vgk + Vpk;
                    ip = Call.If(Ed > 0, K * (Ed ^ 1.5), 0);
                    ig = 0;
                    break;
                case TriodeModel.Koren:
                    Expression E1 = Ln1Exp(Kp * (1.0 / Mu + Vgk * Binary.Power(Kvb + Vpk ^ 2, -0.5))) * Vpk / Kp;
                    ip = (Call.Max(E1, 0) ^ Ex) / Kg;
                    ig = Call.Max(Vgk - Vg, 0) / Rgk;
                    break;
                case TriodeModel.DempwolfZolzer:
                    ig = Call.If(Vgk > -5, Gg * Binary.Power(Ln1Exp(Cg * Vgk) / Cg, Xi), 0) + Ig0;
                    Expression ex = C * ((Vpk / Mu) + Vgk);
                    var ik = Call.If(ex > -5, G * Binary.Power(Ln1Exp(ex) / C, Gamma), 0);
                    ip = ik - ig;
                    break;
                default:
                    throw new NotImplementedException("Triode model " + model.ToString());
            }
            ip = Mna.AddUnknownEqualTo("i" + Name + "p", ip);
            ig = Mna.AddUnknownEqualTo("i" + Name + "g", ig);
            Mna.AddTerminal(p, ip);
            Mna.AddTerminal(g, ig);
            Mna.AddTerminal(k, -(ip + ig));
        }

        public void ConnectTo(Node P, Node G, Node K)
        {
            p.ConnectTo(P);
            g.ConnectTo(G);
            k.ConnectTo(K);
        }

        public static void LayoutSymbol(SymbolLayout Sym, Terminal P, Terminal G, Terminal K, Func<string> Name, Func<string> Part)
        {
            Sym.AddTerminal(P, new Coord(0, 20), new Coord(0, 4));
            Sym.AddWire(new Coord(-10, 4), new Coord(10, 4));

            Sym.AddTerminal(G, new Coord(-20, 0), new Coord(-12, 0));
            for (int i = -8; i < 16; i += 8)
                Sym.AddWire(new Coord(i, 0), new Coord(i + 4, 0));

            Sym.AddTerminal(K, new Coord(-10, -20), new Coord(-10, -6), new Coord(-8, -4), new Coord(8, -4), new Coord(10, -6));

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);

            if (Part != null)
                Sym.DrawText(Part, new Coord(-2, 20), Alignment.Far, Alignment.Near);
            Sym.DrawText(Name, new Point(-8, -20), Alignment.Near, Alignment.Far);
        }

        public override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, p, g, k, () => Name, () => PartNumber); }

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

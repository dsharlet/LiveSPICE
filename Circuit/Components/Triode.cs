using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Circuit
{
    public enum TriodeModel
    {
        ChildLangmuir,
        Koren,
    }

    /// <summary>
    /// Base class for a triode.
    /// </summary>
    [Category("Vacuum Tubes")]
    [DisplayName("Triode")]
    public class Triode : Component
    {
        protected TriodeModel model = TriodeModel.ChildLangmuir;
        [Serialize, Description("Model implementation to use")]
        public TriodeModel Model { get { return model; } set { model = value; NotifyChanged("Model"); } }

        private double mu = 100.0;
        [Serialize, Description("Voltage gain.")]
        public double Mu { get { return mu; } set { mu = value; NotifyChanged("Mu"); } }

        protected double k_ = 1.73e-6;
        [Serialize, Description("Generalized perveance."), CategoryAttribute("Child-Langmuir")]
        public double K { get { return k_; } set { k_ = value; NotifyChanged("K"); } }

        private double ex = 1.4;
        [Serialize, CategoryAttribute("Koren")]
        public double Ex { get { return ex; } set { ex = value; NotifyChanged("Ex"); } }

        private double kg = 1060.0;
        [Serialize, CategoryAttribute("Koren")]
        public double Kg { get { return kg; } set { kg = value; NotifyChanged("Kg"); } }

        private double kp = 600.0;
        [Serialize, CategoryAttribute("Koren")]
        public double Kp { get { return kp; } set { kp = value; NotifyChanged("Kp"); } }

        private double kvb = 300;
        [Serialize, CategoryAttribute("Koren")]
        public double Kvb { get { return kvb; } set { kvb = value; NotifyChanged("Kvb"); } }

        private Quantity rgk = new Quantity(1e6, Units.Ohm);
        [Serialize, CategoryAttribute("Koren")]
        public Quantity Rgk { get { return rgk; } set { if (rgk.Set(value)) NotifyChanged("Rgk"); } }

        private Quantity vg = new Quantity(0.33, Units.V);
        [Serialize, CategoryAttribute("Koren")]
        public Quantity Vg { get { return vg; } set { if (vg.Set(value)) NotifyChanged("Vg"); } }

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
                    Expression E1 = Ln1Exp(Kp * (1.0 / Mu + Vgk * (Kvb + Vpk ^ 2) ^ (-0.5))) * Vpk / Kp;
                    ip = (Call.Max(E1, 0) ^ Ex) / Kg;
                    ig = Call.Max(Vgk - Vg, 0) / Rgk;
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

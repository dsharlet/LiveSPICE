using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    [TypeConverter(typeof(ModelConverter<TriodeModel>))]
    public abstract class TriodeModel : Model
    {
        public TriodeModel(string Name) : base(Name) { }

        public abstract void Evaluate(Expression Vpk, Expression Vgk, out Expression Ip, out Expression Ig, out Expression Ik);

        public static List<TriodeModel> Models { get { return Model.GetModels<TriodeModel>(); } }
        
        static TriodeModel()
        {
            Models.Add(new ChildLangmuirTriode("12AX7", 83.5, 1.73e-6));
            Models.Add(new ChildLangmuirTriode("12AY7", 36.9, 1.73e-6));

            Models.Add(new KorenTriode("12AX7", 100.0,	1.4,	1060.0,	600.0,	300.0,	1e6,	0.33));
	        Models.Add(new KorenTriode("12AZ7", 74.08,	1.371,	382.0,	190.11,	300.0,	1e6,	0.33));
	        Models.Add(new KorenTriode("12AT7", 67.49,	1.234,	419.1,	213.96,	300.0,	1e6,	0.33));
	        Models.Add(new KorenTriode("12AY7", 44.16,	1.113,	1192.4,	409.96,	300.0,	1e6,	0.33));
	        Models.Add(new KorenTriode("12AU7", 21.5, 	1.3,	1180.0,	84.0,	300.0,	1e6,	0.33));
        }
    }

    /// <summary>
    /// Norman Koren's triode model: http://www.normankoren.com/Audio/Tubemodspice_article.html
    /// </summary>
    public class KorenTriode : TriodeModel
    {
        private double mu, ex, kg, kp, kvb, rgk, vg;

        public double Mu { get { return mu; } set { mu = value; } }
        public double Ex { get { return ex; } set { ex = value; } }
        public double Kg { get { return kg; } set { kg = value; } }
        public double Kp { get { return kp; } set { kp = value; } }
        public double Kvb { get { return kvb; } set { kvb = value; } }
        public double Rgk { get { return rgk; } set { rgk = value; } }
        public double Vg { get { return vg; } set { vg = value; } }
        
        public KorenTriode(string Name, double Mu, double Ex, double Kg, double Kp, double Kvb, double Rgk, double Vg) : base(Name)
        {
            mu = Mu;
            ex = Ex;
            kg = Kg;
            kp = Kp;
            kvb = Kvb;
            rgk = Rgk;
            vg = Vg;
        }
        
        public override void Evaluate(Expression Vpk, Expression Vgk, out Expression Ip, out Expression Ig, out Expression Ik)
        {
            Expression ex = Kp * (1.0 / Mu + Vgk * (Kvb + Vpk * Vpk) ^ (-0.5));

            // ln(1+e^x) = x for large x, and large x causes numerical issues.
            Expression E1 = Call.If(ex > 5, ex, Call.Ln(1 + Call.Exp(ex))) * Vpk / Kp;

            Ip = Call.If(E1 > 0, (E1 ^ Ex) / Kg, Constant.Zero);
            Ig = Call.If(Vgk > Vg, (Vgk - Vg) / Rgk, Constant.Zero);
            Ik = -(Ip + Ig);
        }

        public override string ToString() { return base.ToString() + " (Koren)"; }
    };

    /// <summary>
    /// Child-Langmuir triode.
    /// </summary>
    [TypeConverter(typeof(ModelConverter<TriodeModel>))]
    public class ChildLangmuirTriode : TriodeModel
    {
        protected double mu, k;

        public double Mu { get { return mu; } set { mu = value; } }
        public double K { get { return k; } set { k = value; } }

        public ChildLangmuirTriode(string Name, double Mu, double K) : base(Name)
        {
            mu = Mu;
            k = K;
        }

        public override void Evaluate(Expression Vpk, Expression Vgk, out Expression Ip, out Expression Ig, out Expression Ik)
        {
            Expression Ed = Mu * Vgk + Vpk;
            Ip = Call.If(Ed > 0, K * (Ed ^ 1.5), 0);
            Ig = 0;
            Ik = -Ip;
        }

        public override string ToString() { return base.ToString() + " (Child-Langmuir)"; }
    }


    /// <summary>
    /// Base class for a triode.
    /// </summary>
    [CategoryAttribute("Vacuum Tubes")]
    [DisplayName("Triode")]
    public class Triode : Component
    {
        protected Terminal p, g, k;
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
        public Terminal P { get { return p; } }
        [Browsable(false)]
        public Terminal G { get { return g; } }
        [Browsable(false)]
        public Terminal K { get { return k; } }

        protected TriodeModel model = TriodeModel.Models.First();
        [Serialize]
        public TriodeModel Model { get { return model; } set { model = value; NotifyChanged("Model"); } }
        
        public Triode()
        {
            p = new Terminal(this, "P");
            g = new Terminal(this, "G");
            k = new Terminal(this, "K");
            Name = "V1";
        }

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            Expression Vpk = Mna.AddNewUnknownEqualTo(Name + "pk", p.V - k.V);
            Expression Vgk = Mna.AddNewUnknownEqualTo(Name + "gk", g.V - k.V);

            Expression ip, ig, ik;
            model.Evaluate(Vpk, Vgk, out ip, out ig, out ik);
            ip = Mna.AddNewUnknownEqualTo("i" + Name + "p", ip); ;
            ig = Mna.AddNewUnknownEqualTo("i" + Name + "g", ig); ;
            Mna.AddTerminal(p, ip);
            Mna.AddTerminal(g, ig);
            Mna.AddTerminal(k, -(ip + ig));
        }

        public void ConnectTo(Node A, Node G, Node C)
        {
            p.ConnectTo(A);
            g.ConnectTo(G);
            k.ConnectTo(C);
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

        public override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, p, g, k, () => Name, () => Model.Name); }
    }
}

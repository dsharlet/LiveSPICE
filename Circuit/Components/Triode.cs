using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    public abstract class TriodeModel
    {
        public abstract void Evaluate(Expression Vpk, Expression Vgk, out Expression Ip, out Expression Ig, out Expression Ik);
    }

    /// <summary>
    /// Norman Koren's triode model: http://www.normankoren.com/Audio/Tubemodspice_article.html
    /// </summary>
    public class KorenTriode : TriodeModel
    {
        protected decimal Mu, Ex, Kg, Kp, Kvb, Rgk, Vg;
        
        public KorenTriode(decimal Mu, decimal Ex, decimal Kg, decimal Kp, decimal Kvb, decimal Rgk, decimal Vg)
        {
            this.Mu = Mu;
            this.Ex = Ex;
            this.Kg = Kg;
            this.Kp = Kp;
            this.Kvb = Kvb;
            this.Rgk = Rgk;
            this.Vg = Vg;
        }

        // Default is the parameters for 12AX7.
        public KorenTriode() 
            : this(100.0m, 1.4m, 1060.0m, 600.0m, 300.0m, 1.0e6m, 0.33m)
        { }

        public override void Evaluate(Expression Vpk, Expression Vgk, out Expression Ip, out Expression Ig, out Expression Ik)
        {
            Expression ex = Kp * (1 / Mu + Vgk / Call.Sqrt(Kvb + Vpk * Vpk));

            // ln(1+e^x) = x for large x, and large x causes numerical issues.
            Expression E1 = Call.If(ex > 5, ex, Call.Ln(1 + Call.Exp(ex))) * Vpk / Kp;

            Ip = Call.If(E1 > 0, (E1 ^ Ex) / Kg, Constant.Zero);
            // TODO: Use Max instead?
            Ig = Call.If(Vgk > Vg, (Vgk - Vg) / Rgk, Constant.Zero);
            Ik = -(Ip + Ig);
        }
    };

    /// <summary>
    /// Child-Langmuir triode.
    /// </summary>
    public class ChildLangmuirTriode : TriodeModel
    {
        protected decimal Mu, K;
        
        public ChildLangmuirTriode(decimal Mu, decimal K)
        {
            this.Mu = Mu;
            this.K = K;
        }

        // Default is the parameters for 12AX7.
        public ChildLangmuirTriode()
            : this(83.5m, 1.73e-6m)
        { }

        public override void Evaluate(Expression Vpk, Expression Vgk, out Expression Ip, out Expression Ig, out Expression Ik)
        {
            Expression Ed = Mu * Vgk + Vpk;
            Ip = Call.If(Ed > 0, K * (Ed ^ 1.5), 0);
            Ig = 0;
            Ik = -Ip;
        }
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

        protected TriodeModel model = new KorenTriode();
        
        public Triode()
        {
            p = new Terminal(this, "P");
            g = new Terminal(this, "G");
            k = new Terminal(this, "K");
            Name = "V1";
        }


        public override void Analyze(IList<Equal> Mna, IList<Expression> Unknowns)
        {
            Expression Vpk = DependentVariable(Name + "pk", t);
            Expression Vgk = DependentVariable(Name + "gk", t);
            Mna.Add(Equal.New(Vpk, p.V - k.V));
            Mna.Add(Equal.New(Vgk, g.V - k.V));
            Unknowns.Add(Vpk);
            Unknowns.Add(Vgk);

            Expression Ip, Ig, Ik;
            model.Evaluate(Vpk, Vgk, out Ip, out Ig, out Ik);
            p.i = Ip;
            g.i = Ig;
            k.i = Ik;
        }

        public void ConnectTo(Node A, Node G, Node C)
        {
            p.ConnectTo(A);
            g.ConnectTo(G);
            k.ConnectTo(C);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(p, new Coord(0, 30));
            Sym.AddTerminal(g, new Coord(-30, 0));
            Sym.AddTerminal(k, new Coord(-10, -30));

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);

            Sym.AddWire(p, new Coord(0, 4));
            Sym.AddWire(new Coord(-10, 4), new Coord(10, 4));

            Sym.AddWire(g, new Coord(-16, 0));
            for(int i = -12; i < 20; i += 8)
                Sym.AddWire(new Coord(i, 0), new Coord(i + 4, 0));

            Sym.AddWire(k, new Coord(-10, -6), new Coord(-8, -4), new Coord(8, -4), new Coord(10, -6));

            Sym.DrawText(Name, new Coord(0, -20), Alignment.Near, Alignment.Far);
        }
    }
}

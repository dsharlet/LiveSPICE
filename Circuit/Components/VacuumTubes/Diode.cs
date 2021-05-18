using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Circuit.Components
{
    [Category("Vacuum Tubes")]
    [DisplayName("Diode")]
    public class Diode : TwoTerminal
    {
        double _k = 3.26542e-4;
        [Serialize, Description("Generalized perveance.")]
        public double K { get { return _k; } set { _k = value; NotifyChanged(nameof(K)); } }

        double _exp = 1.4606772;
        [Serialize, Description("Exponent.")]
        public double Exp { get { return _exp; } set { _exp = value; NotifyChanged(nameof(Exp)); } }

        double _eps = .2;
        public double EPS { get { return _eps; } set { _eps = value; NotifyChanged(nameof(EPS)); } }

        public override void Analyze(Analysis Mna)
        {
            var i = Call.If(V > 0, K * Binary.Power(V, Exp), 0);
            Mna.AddPassiveComponent(Anode, Cathode, i);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(Anode, new Coord(0, 20), new Coord(0, 4));
            Sym.AddWire(new Coord(-10, 4), new Coord(10, 4));

            Sym.AddTerminal(Cathode, new Coord(-10, -20), new Coord(-10, -6), new Coord(-8, -4), new Coord(8, -4), new Coord(10, -6));

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);

            if (PartNumber != null)
                Sym.DrawText(() => PartNumber, new Coord(-2, 20), Alignment.Far, Alignment.Near);
            Sym.DrawText(() => Name, new Point(-8, -20), Alignment.Near, Alignment.Far);
        }
    }
}

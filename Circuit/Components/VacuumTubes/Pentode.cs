using ComputerAlgebra;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Circuit.Components
{
    [Category("Vacuum Tubes")]
    [DisplayName("Pentode")]
    public class Pentode : Component
    {
        private Terminal _plate, _grid, _grid2, _cathode;

        private double _mu = 10.7;
        [Serialize, Category("Koren"), Description("Voltage gain.")]
        public double Mu { get { return _mu; } set { _mu = value; NotifyChanged(nameof(Mu)); } }

        private double _kg1 = 1672.0;
        [Serialize, Category("Koren"), Browsable(true)]
        public double Kg1 { get { return _kg1; } set { _kg1 = value; NotifyChanged(nameof(Kg1)); } }

        private double _kg2 = 4500;
        [Serialize, Category("Koren"), Browsable(true)]
        public double Kg2 { get { return _kg2; } set { _kg2 = value; NotifyChanged(nameof(Kg2)); } }

        private double _kp = 41.16;
        [Serialize, Category("Koren"), Browsable(true)]
        public double Kp { get { return _kp; } set { _kp = value; NotifyChanged(nameof(Kp)); } }

        private double _kvb = 12.7;
        [Serialize, Category("Koren"), Browsable(true)]
        public double Kvb { get { return _kvb; } set { _kvb = value; NotifyChanged(nameof(Kvb)); } }

        private double _ex = 1.310;
        [Serialize, Category("Koren"), Browsable(true)]
        public double Ex { get { return _ex; } set { _ex = value; NotifyChanged(nameof(Ex)); } }

        public Pentode()
        {
            _plate = new Terminal(this, "P");
            _grid = new Terminal(this, "G");
            _grid2 = new Terminal(this, "G2");
            _cathode = new Terminal(this, "K");
        }

        public override IEnumerable<Terminal> Terminals
        {
            get
            {
                yield return _plate;
                yield return _grid;
                yield return _grid2;
                yield return _cathode;
            }
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(_plate, new Coord(0, 30), new Coord(0, 15));
            Sym.AddWire(new Coord(-10, 15), new Coord(10, 15));

            Sym.AddTerminal(_grid, new Coord(-20, 0), new Coord(-12, 0));
            Sym.AddTerminal(_grid2, new Coord(20, 5), new Coord(12, 5));
            for (int i = -10; i < 10; i += 8)
            {
                Sym.AddWire(new Coord(i, 0), new Coord(i + 4, 0));
                Sym.AddWire(new Coord(i, 5), new Coord(i + 4, 5));
                Sym.AddWire(new Coord(i, 10), new Coord(i + 4, 10));

            }
            Sym.AddTerminal(_cathode, new Coord(-10, -20), new Coord(-10, -7), new Coord(-8, -5), new Coord(8, -5), new Coord(10, -7));

            Sym.DrawArc(EdgeType.Black, new Coord(0, 10), 20d, 0, Math.PI, Direction.Counterclockwise);
            Sym.DrawArc(EdgeType.Black, new Coord(0, 0), 20d, 0, Math.PI);
            Sym.AddLine(EdgeType.Black, new Coord(-20, 0), new Coord(-20, 10));
            Sym.AddLine(EdgeType.Black, new Coord(20, 0), new Coord(20, 10));


            if (PartNumber != null)
                Sym.DrawText(() => PartNumber, new Coord(-2, 30), Alignment.Far, Alignment.Near);
            Sym.DrawText(() => Name, new Point(-8, -20), Alignment.Near, Alignment.Far);

        }

        public override void Analyze(Analysis Mna)
        {
            var vpk = _plate.V - _cathode.V;
            var vgk = _grid.V - _cathode.V;
            var vg2k = _grid2.V - _cathode.V;

            var E1 = vpk / Kp * Ln1Exp(Kp * ((1.0 / Mu) + (vgk * Binary.Power(Kvb + vg2k * vg2k, -.5))));
            var iKoren = Call.If(E1 > 0, Binary.Power(E1, Ex), 0);
            var ip = Call.If(vpk > 0, iKoren / Kg1 * Call.ArcTan(vpk / Kvb), 0);

            var vg = 13d;
            var knee = 3d;
            var rg1 = 2000d;

            var a = 1 / (4 * knee * rg1);
            var b = (knee - vg) / (2 * knee * rg1);
            var c = (-a * Math.Pow(vg - knee, 2)) - (b * (vg - knee));

            var ig = Call.If(vgk < vg - knee, 0, Call.If(vgk > vg + knee, (vgk - vg)/ rg1, a*vgk*vgk + b*vgk + c));
            var ig2 = iKoren / Kg2;
            var ik = -(ip + ig + ig2);

            Mna.AddTerminal(_plate, ip);
            Mna.AddTerminal(_grid, ig);
            Mna.AddTerminal(_grid2, ig2);
            Mna.AddTerminal(_cathode, ik);
        }
        private static Expression Ln1Exp(Expression x)
        {
            return Call.Ln(1 + Call.Exp(x));
            //return Call.If(x > 5, x, Call.Ln(1 + Call.Exp(x)));
        }
    }
}

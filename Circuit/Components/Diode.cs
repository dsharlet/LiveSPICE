using ComputerAlgebra;
using System;
using System.ComponentModel;

namespace Circuit
{
    public enum DiodeType
    {
        Diode,
        LED,
        Zener,
    }

    /// <summary>
    /// Shockley diode model: http://en.wikipedia.org/wiki/Diode_modelling#Shockley_diode_model
    /// </summary>
    [Category("Diodes")]
    [DisplayName("Diode")]
    public class Diode : TwoTerminal
    {
        protected Quantity _is = new Quantity(1e-12m, Units.A);
        [Serialize, Description("Saturation current.")]
        public Quantity IS { get { return _is; } set { if (_is.Set(value)) NotifyChanged(nameof(IS)); } }

        protected Quantity _n = new Quantity(1, Units.None);
        [Serialize, Description("Gate emission coefficient.")]
        public Quantity n { get { return _n; } set { if (_n.Set(value)) NotifyChanged(nameof(n)); } }

        protected DiodeType type = DiodeType.Diode;
        [Serialize, Description("Type of this diode. This property only affects the schematic symbol, it does not affect the simulation.")]
        public DiodeType Type { get { return type; } set { type = value; NotifyChanged(nameof(Type)); } }

        public Diode() { Name = "D1"; }

        public static Expression Analyze(Analysis Mna, string Name, Node Anode, Node Cathode, Expression IS, Expression n)
        {
            // V = Va - Vc
            Expression Vac = Anode.V - Cathode.V;
            Vac = Mna.AddUnknownEqualTo("V" + Name, Vac);

            // Evaluate the model.
            Expression i = IS * LinExpm1(Vac / (n * VT));
            i = Mna.AddUnknownEqualTo("i" + Name, i);

            Mna.AddPassiveComponent(Anode, Cathode, i);

            return i;
        }
        public static Expression Analyze(Analysis Mna, Node Anode, Node Cathode, Expression IS, Expression n) { return Analyze(Mna, Mna.AnonymousName(), Anode, Cathode, IS, n); }

        public override void Analyze(Analysis Mna) { Analyze(Mna, Name, Anode, Cathode, IS, n); }

        public static void LayoutSymbol(SymbolLayout Sym, Terminal A, Terminal C, DiodeType Type, Func<string> Name, Func<string> Part)
        {
            Sym.AddTerminal(A, new Coord(0, 20));
            Sym.AddWire(A, new Coord(0, 10));

            Sym.AddTerminal(C, new Coord(0, -20));
            Sym.AddWire(C, new Coord(0, -10));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(10, 10),
                new Coord(0, -10));
            Sym.AddLine(EdgeType.Black, new Coord(-10, -10), new Coord(10, -10));

            switch (Type)
            {
                case DiodeType.LED:
                    Sym.DrawArrow(EdgeType.Black, new Coord(-12, 5), new Coord(-20, -3), 0.2);
                    Sym.DrawArrow(EdgeType.Black, new Coord(-8, -2), new Coord(-16, -10), 0.2);
                    break;
                case DiodeType.Zener:
                    Sym.AddLine(EdgeType.Black, new Coord(-10, -10), new Coord(-10, -5));
                    Sym.AddLine(EdgeType.Black, new Coord(10, -10), new Coord(10, -15));
                    break;
                default:
                    break;
            }

            if (Part != null)
                Sym.DrawText(Part, new Coord(12, 4), Alignment.Near, Alignment.Near);
            Sym.DrawText(Name, new Coord(12, -4), Alignment.Near, Alignment.Far);
        }

        protected internal override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, Anode, Cathode, Type, () => Name, () => PartNumber); }
    }
}

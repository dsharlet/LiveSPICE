using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    public enum JfetType
    {
        N,
        P,
    };

    /// <summary>
    /// Implementation of the Ebers-Moll transistor model: http://people.seas.harvard.edu/~jones/es154/lectures/lecture_3/bjt_models/ebers_moll/ebers_moll.html
    /// </summary>
    [Category("Transistors")]
    [DisplayName("JFET")]
    [Description("Junction field effect transistor.")]
    public class JunctionFieldEffectTransistor : Component, INotifyPropertyChanged
    {
        private Terminal s, g, d;
        public override IEnumerable<Terminal> Terminals 
        { 
            get 
            {
                yield return s;
                yield return g;
                yield return d;
            } 
        }
        [Browsable(false)]
        public Terminal Source { get { return s; } }
        [Browsable(false)]
        public Terminal Gate { get { return g; } }
        [Browsable(false)]
        public Terminal Drain { get { return d; } }
        
        private JfetType type = JfetType.N;
        [Description("JFET structure.")]
        [Serialize]
        public JfetType Type { get { return type; } set { type = value; NotifyChanged("Type"); } }

        protected Quantity _is = new Quantity(1e-12m, Units.A);
        [Description("Saturation current.")]
        [Serialize]
        public Quantity IS { get { return _is; } set { if (_is.Set(value)) NotifyChanged("IS"); } }

        public JunctionFieldEffectTransistor()
        {
            s = new Terminal(this, "S");
            g = new Terminal(this, "G");
            d = new Terminal(this, "D");
            Name = "J1"; 
        }

        public override void Analyze(Analysis Mna)
        {
            throw new NotImplementedException("JunctionFieldEffectTransistor.Analyze");
        }

        public static void LayoutSymbol(SymbolLayout Sym, JfetType Type, Terminal S, Terminal G, Terminal D, Func<string> Name, Func<string> Part)
        {
            int bx = 0;
            Sym.AddTerminal(S, new Coord(10, 20), new Coord(10, 10), new Coord(0, 10));
            Sym.AddTerminal(G, new Coord(-20, 0), new Coord(-10, 0));
            Sym.AddTerminal(D, new Coord(10, -20), new Coord(10, -10), new Coord(0, -10));

            Sym.DrawLine(EdgeType.Black, new Coord(bx, 12), new Coord(bx, -12));
            switch (Type)
            {
                case JfetType.N: Sym.DrawArrow(EdgeType.Black, new Coord(-10, 0), new Coord(0, 0), 0.2, 0.3); break;
                case JfetType.P: Sym.DrawArrow(EdgeType.Black, new Coord(0, 0), new Coord(-10, 0), 0.2, 0.3); break;
                default: 
                    throw new NotSupportedException("Unknown JFET type.");
            }

            if (Part != null)
                Sym.DrawText(Part, new Coord(8, 20), Alignment.Far, Alignment.Near);
            Sym.DrawText(Name, new Point(8, -20), Alignment.Far, Alignment.Far);

            Sym.AddCircle(EdgeType.Black, new Coord(0, 0), 20);
        }

        public override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, Type, Source, Gate, Drain, () => Name, () => PartNumber); }
    }
}

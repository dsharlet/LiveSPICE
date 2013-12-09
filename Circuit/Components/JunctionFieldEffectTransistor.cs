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
        [Serialize, Description("JFET structure.")]
        public JfetType Type { get { return type; } set { type = value; NotifyChanged("Type"); } }

        protected Quantity _is = new Quantity(1e-14m, Units.A);
        [Serialize, Description("Saturation current.")]
        public Quantity IS { get { return _is; } set { if (_is.Set(value)) NotifyChanged("IS"); } }

        protected Quantity vt0 = new Quantity(-2, Units.V);
        [Spice.ParameterAlias("VTO")]
        [Serialize, Description("Threshold voltage.")]
        public Quantity Vt0 { get { return vt0; } set { if (vt0.Set(value)) NotifyChanged("Vt0"); } }

        private Quantity beta = new Quantity(1e-4m, Units.None);// Units.A / Units.V ^ 2);
        [Serialize, Description("Transconductance.")]
        public Quantity Beta { get { return beta; } set { if (beta.Set(value)) NotifyChanged("Beta"); } }

        private Quantity lambda = new Quantity(0, Units.None);// Units.V ^ -1);
        [Serialize, Description("Channel length modulation.")]
        public Quantity Lambda { get { return lambda; } set { if (lambda.Set(value)) NotifyChanged("Lambda"); } }

        public JunctionFieldEffectTransistor()
        {
            s = new Terminal(this, "S");
            g = new Terminal(this, "G");
            d = new Terminal(this, "D");
            Name = "J1"; 
        }

        public override void Analyze(Analysis Mna)
        {
            Diode.Analyze(Mna, Gate, Source, IS, 1, VT);
            Diode.Analyze(Mna, Gate, Drain, IS, 1, VT);

            // The drain and source terminals are reversible in the JFET model, this 
            // formulation is simpler than explicitly identifying normal/inverted mode.
            Expression Vgs = Gate.V - Call.Min(Source.V, Drain.V);
            Expression Vds = Call.Abs(Drain.V - Source.V);

            //Vgs = Mna.AddNewUnknownEqualTo(Name + "gs", Vgs);

            Expression Vgst0 = Vgs - Vt0;

            Expression id = (Vgs >= Vt0) * Beta * (1 + Lambda * Vds) * 
                Call.If(Vds < Vgst0, 
                    // Linear region.
                    Vds * (2 * Vgst0 - 1), 
                    // Saturation region.
                    Vgst0 ^ 2);
            
            id = Mna.AddUnknownEqualTo("i" + Name + "d", id);
            CurrentSource.Analyze(Mna, Drain, Source, id);
        }

        public static void LayoutSymbol(SymbolLayout Sym, JfetType Type, Terminal S, Terminal G, Terminal D, Func<string> Name, Func<string> Part)
        {
            int bx = 0;
            Sym.AddTerminal(S, new Coord(10, -20), new Coord(10, -10), new Coord(0, -10));
            Sym.AddTerminal(G, new Coord(-20, 0), new Coord(-10, 0));
            Sym.AddTerminal(D, new Coord(10, 20), new Coord(10, 10), new Coord(0, 10));

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

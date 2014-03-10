using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ComputerAlgebra;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// N-pole single throw switch base.
    /// </summary>
    public abstract class SwNPST : Component, IButtonControl
    {
        protected int position = 0;
        [Serialize, Description("Switch position.")]
        public int Position { get { return position; } set { position = value; NotifyChanged("Position"); } }

        public void Click() { Position = (Position + 1) % poles.Length; }

        private Terminal common;
        public Terminal Common { get { return common; } }

        private Terminal[] poles = null;
        public Terminal[] Poles { get { return poles; } }

        public override IEnumerable<Terminal> Terminals { get { return Poles.Append(Common); } }

        public SwNPST(int PoleCount)
        {
            if (PoleCount < 2 || PoleCount > 100)
                throw new ArgumentOutOfRangeException("PoleCount", "PoleCount must be in [2, 100]");

            poles = new Terminal[PoleCount];
            for (int i = 0; i < PoleCount; ++i)
                poles[i] = new Terminal(this, "Pole" + i.ToString());

            common = new Terminal(this, "Common");

            Name = "S1"; 
        }

        public override void Analyze(Analysis Mna)
        {
            if (0 <= position && position < Poles.Length)
                Conductor.Analyze(Mna, Name, Common, Poles[Position]);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            Sym.AddTerminal(common, new Coord(0, -20), new Coord(0, -12));
            Sym.AddCircle(EdgeType.Black, new Coord(0, -12), 2);
                        
            for (int i = 0; i < Poles.Length; ++i)
            {
                int x = (i - Poles.Length / 2) * 20 + (Poles.Length % 2 == 0 ? 10 : 0);
                Sym.AddTerminal(poles[i], new Coord(x, 20), new Coord(x, 12));
                Sym.AddCircle(EdgeType.Black, new Coord(x, 12), 2);

                if (i == Position)
                    Sym.AddWire(new Coord(0, -12), new Coord(x, 12));
            }

            Sym.DrawText(() => Name, new Coord(2, -12), Alignment.Near, Alignment.Far);
        }
    }

    [Category("Generic")]
    [DisplayName("DPST")]
    [DefaultProperty("Position")]
    [Description("2-pole single-throw switch.")]
    public class Sw2PST : SwNPST { public Sw2PST() : base(2) { } }

    [Category("Generic")]
    [DisplayName("3PST")]
    [DefaultProperty("Position")]
    [Description("3-pole single-throw switch.")]
    public class Sw3PST : SwNPST { public Sw3PST() : base(3) { } }

    [Category("Generic")]
    [DisplayName("4PST")]
    [DefaultProperty("Position")]
    [Description("4-pole single-throw switch.")]
    public class Sw4PST : SwNPST { public Sw4PST() : base(4) { } }


    /// <summary>
    /// Switch component that is open or closed. 
    /// Deprecated.
    /// </summary>
    [Category("Generic")]
    [DisplayName("Switch")]
    [DefaultProperty("Closed")]
    [Description("Switch.")]
    [Obsolete]
    public class Switch : TwoTerminal
    {
        protected bool closed = false;
        [Serialize, Description("Switch position.")]
        public bool Closed { get { return closed; } set { closed = value; NotifyChanged("Closed"); } }

        public Switch() { Name = "S1"; }
        public Switch(bool Closed) : this() { closed = Closed; }

        public override void Analyze(Analysis Mna)
        {
            if (closed)
                Conductor.Analyze(Mna, Name, Anode, Cathode);
        }

        public override void LayoutSymbol(SymbolLayout Sym)
        {
            base.LayoutSymbol(Sym);

            Sym.AddWire(Anode, new Coord(0, 12));
            Sym.AddWire(Cathode, new Coord(0, -12));
            Sym.AddCircle(EdgeType.Black, new Coord(0, 12), 2);
            Sym.AddCircle(EdgeType.Black, new Coord(0, -12), 2);
            Sym.InBounds(new Coord(-10, 0), new Coord(10, 0));

            if (closed)
                Sym.AddWire(new Coord(0, -12), new Coord(0, 12));
            else
                Sym.AddWire(new Coord(0, -12), new Coord(-8, 10));

            Sym.DrawText(() => Name, new Coord(2, 0), Alignment.Near, Alignment.Center);
        }
    }
}
